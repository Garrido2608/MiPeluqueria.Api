```markdown
# 05 - REGLAS DE NEGOCIO, ALGORITMOS MATEMÁTICOS Y CASOS BORDE (CAPA 5 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8)[cite: 29]
**Documentos Padres Vinculados:** `00_AUDITORIA_Y_DEUDA_TECNICA.md`, `01_SYSTEM_CONSTRAINTS.md`, `02_ARCHITECTURE.md`, `03_DATA_MODEL.md`, `04_API_CONTRACTS.md`[cite: 29]
**Estado:** INMUTABLE - ESPECIFICACIÓN DE LÓGICA DE DOMINIO Y ALGORITMOS[cite: 29]

---

## 1. HUSO HORARIO DEL SALÓN Y CORTES DIARIOS DE TIEMPO

1. **Conversión Oficial de Zona Horaria (`TimeZoneInfo`):**
   * Toda persistencia en SQL Server ocurre en `DateTime.UtcNow`[cite: 29].
   * Para determinar el día actual del salón (`DashboardService.TurnosPendientesHoy`, descarte de slots pasados en `ObtenerHorariosDisponiblesAsync`), se convierte `DateTime.UtcNow` al `ConfiguracionSalon.TimeZoneId` (`"America/Argentina/Cordoba"` por defecto)[cite: 29]:
     ```csharp
     TimeZoneInfo tz;
     try { tz = TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId); }
     catch { tz = TimeZoneInfo.CreateCustomTimeZone("AR", TimeSpan.FromHours(-3), "Argentina", "Argentina"); }
     DateTime ahoraLocalSalon = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
     ```

---

## 2. MOTOR MATEMÁTICO DE AGENDA, TURNOS, BLOQUEOS Y WEBHOOKS

### 2.1 Algoritmo de Disponibilidad (`TurnoService.ObtenerHorariosDisponiblesAsync`)
Dado un `peluqueroId`, una `fecha` y una lista opcional `serviciosIds`[cite: 29]:
1. **Duración Total Requerida ($D$):** Suma de `DuracionMinutos` de los servicios activos seleccionados (cargados en 1 sola consulta con `GetByIdsDictionaryAsync`), o `ConfiguracionSalon.IntervaloTurnosMinutos` (default $30$ min) si no se especifican servicios[cite: 29].
2. **Jornada Laboral y Días Francos:** Si existe `HorarioLaboral` para `fecha.DayOfWeek` con `Activo == false`, retorna inmediatamente disponibilidad vacía[cite: 29]. Si no existe configuración previa, aplica jornada por defecto `09:00` a `20:00`[cite: 29].
3. **Paso de Iteración ($\Delta$):** $\Delta = \text{ConfiguracionSalon.IntervaloTurnosMinutos}$[cite: 29].
4. **Regla de Exclusión de Slots ($[S_{\text{ini}}, S_{\text{fin}}]$ donde $S_{\text{fin}} = S_{\text{ini}} + D$):**
   Un bloque se descarta si:
   * **a) Horario Pasado:** `fecha.Date == ahoraLocalSalon.Date` y $S_{\text{ini}} \le \text{ahoraLocalSalon}$[cite: 29].
   * **b) Descanso de Mediodía:** Existe `HoraInicioDescanso` ($R_{\text{ini}}$) y `HoraFinDescanso` ($R_{\text{fin}}$) tal que $(S_{\text{ini}} < R_{\text{fin}}) \land (S_{\text{fin}} > R_{\text{ini}})$[cite: 29].
   * **c) Bloqueo Horario:** Existe `BloqueoHorario` tal que $(S_{\text{ini}} < B_{\text{FechaFin}}) \land (S_{\text{fin}} > B_{\text{FechaInicio}})$[cite: 29].
   * **d) Turno Ocupado:** Existe `Turno` activo (`EstadoId != Cancelado && EstadoId != NoAsistio`) tal que $(S_{\text{ini}} < T_{\text{FechaHoraFin}}) \land (S_{\text{fin}} > T_{\text{FechaHoraInicio}})$[cite: 29].

### 2.2 Creación y Reprogramación con Aislamiento `Serializable` (`CreateAsync` y `ReprogramarAsync`)
1. Ambas operaciones se ejecutan dentro de `_uow.ExecuteInTransactionAsync(..., IsolationLevel.Serializable, ct)` para impedir condiciones de carrera (*Phantom Reads*) entre dos clientes que reserven el mismo horario en el mismo milisegundo.
2. Por cada servicio del turno se persiste un `TurnoServicio` con `PrecioHistorico = serv.Precio` y `DuracionMinutosHistorica = serv.DuracionMinutos`[cite: 29].
3. Las llamadas externas a pasarelas de pago (`IPaymentGatewayService`) o correo (`IEmailService`) se ejecutan **fuera** del bloque `ExecuteInTransactionAsync` una vez confirmado el `COMMIT` en SQL Server.

### 2.3 Regla Permisiva con Alerta al Crear un `BloqueoHorario`
Al invocar `BloqueoHorarioService.CrearBloqueoAsync(dto)`, el bloqueo se guarda siempre, pero si existen turnos `Pendientes` o `Confirmados` que se solapen con ese rango, retorna `200 OK` incluyendo la advertencia detallada en `ApiResponse.Warnings` con los IDs y horarios de los turnos afectados[cite: 29].

### 2.4 Máquina de Estados del Turno y Compensación en el "Cruce de la Muerte" (Webhook vs. Worker)
1. **Transiciones Estándar Permitidas:**
   * `Pendiente (1)` $\longrightarrow$ `Confirmado (2)` | `EnProceso (3)` | `Cancelado (5)` | `NoAsistio (6)`[cite: 29]
   * `Confirmado (2)` $\longrightarrow$ `EnProceso (3)` | `Completado (4)` | `Cancelado (5)` | `NoAsistio (6)`[cite: 29]
   * `EnProceso (3)` $\longrightarrow$ `Completado (4)` | `Cancelado (5)`[cite: 29]
   * `Completado (4)`, `Cancelado (5)` y `NoAsistio (6)` son estados finales inmutables para operaciones manuales[cite: 29].
2. **Excepción de Transacción Compensatoria por Webhook de Pago (`WebhooksController`):**
   * Si llega una notificación de pago con firma `HMAC-SHA256` válida y estado `"approved"` / `"paid"`:
     * **Idempotencia:** Si el turno ya tiene `SenaPagada == true` y `ReferenciaPagoExterno == payload.Id`, retorna `200 OK` inmediatamente sin duplicar cambios.
     * **Cruce con Cancelación Automática:** Si el turno había sido pasado a `Cancelado (5)` por el `RecordatorioTurnosBackgroundService` minutos antes:
       1. Se verifica `ExisteSolapamientoAsync` para ese rango horario.
       2. **Si el horario sigue libre:** Se reactiva el turno pasándolo a `Confirmado (2)`, se marca `SenaPagada = true`, `FechaPagoSena = DateTime.UtcNow`, `ReferenciaPagoExterno = payload.Id` y se asienta en `HistorialEstadosTurno`.
       3. **Si otro cliente ocupó ese horario en el ínterin:** Se mantiene `EstadoId = Cancelado (5)`, pero se actualiza `SenaPagada = true`, `MontoSena = payload.TransactionAmount`, `ReferenciaPagoExterno = payload.Id` y `Observaciones += " | [ALERTA CRÍTICA: Seña acreditada tras expiración con horario ocupado. Reprogramar o acreditar saldo a favor]"`, retornando `200 OK` a la pasarela.

### 2.5 Ventana de Protección de Agenda en Cancelaciones (`HorasAnticipacionCancelacion`)
* El staff interno (`Administrador`, `Recepcionista`, `Cajero`, `Peluquero`) puede cancelar o reprogramar en cualquier momento[cite: 29].
* Los usuarios con rol `Cliente` solo pueden autogestionar la cancelación/reprogramación online si faltan al menos `ConfiguracionSalon.HorasAnticipacionCancelacion` horas para `FechaHoraInicio`[cite: 29].

---

## 3. REGLAS DE STOCK, RECETAS DE GABINETE Y TRAZABILIDAD DE PRECIOS

1. **Venta en Mostrador con Control de Stock y Batch Loading:** Se permite vender cualquier producto activo siempre que `producto.StockActual >= item.Cantidad`[cite: 29]. Todos los productos se cargan en memoria con `GetByIdsDictionaryAsync` en 1 sola consulta SQL. Todo movimiento persiste `SaldoResultante = producto.StockActual`[cite: 29].
2. **Consumo de Gabinete Idempotente y No Bloqueante:**
   * Antes de descontar insumos por el `Turno #{turnoId}`, verifica en `MovimientosStock` que no exista ya un movimiento `ConsumoInternoSalon` para ese `TurnoId`[cite: 29].
   * Si un insumo de la receta tiene `StockActual < CantidadUnidadesDescontar`, **no se bloquea** el turno ni la venta: se descuenta hasta `0` (`Math.Max(0, ...)`) y se agrega la alerta correspondiente en `ApiResponse.Warnings` para que el Administrador/Cajero/Peluquero esté informado[cite: 29].
3. **Auditoría Automática de Precios:** Cualquier cambio de precio en `ServicioService.UpdateAsync` o `ProductoService.UpdateAsync` inserta automáticamente el registro histórico en `HistorialPreciosServicio` o `HistorialPreciosProducto` dentro de la misma transacción[cite: 29].

---

## 4. REGLAS FINANCIERAS: CAJA, POS IDEMPOTENTE, COMISIONES Y LIQUIDACIONES

1. **Arqueo de Caja Físico vs. Digital (`CajaService`):**
   $$\text{MontoFinalEsperado} = \text{MontoInicial} + \text{TotalVentasEfectivo} + \text{TotalIngresosManuales} - \text{TotalEgresosManuales}$$
   Donde `TotalVentasEfectivo` suma exclusivamente pagos con `MedioPago.EsEfectivoFisico == true`[cite: 29]. Los medios electrónicos se acumulan por separado en `TotalVentasDigital`[cite: 29].
2. **Facturación POS Idempotente, Herencia de Profesional y Deducción de Seña (`VentaService.RegistrarVentaAsync`):**
   * **Idempotencia (`X-Idempotency-Key`):** Si `dto.IdempotencyKey` no es nulo, se verifica al inicio si ya existe una `Venta` activa en ese local con esa misma `IdempotencyKey`. Si existe, se retorna `200 OK` con el `VentaResponseDto` de la venta existente sin volver a ejecutar el cobro ni descontar stock.
   * **Herencia de `PeluqueroId`:** Si la venta tiene `TurnoId`, cualquier renglón de `Detalles` con `PeluqueroId == null` hereda `turno.PeluqueroId`[cite: 29].
   * **Deducción de Seña:** Si el turno vinculado tiene `SenaPagada == true` y aún no fue descontada en otra venta activa, se aplica $\text{DescuentoSenaAplicado} = \min(\text{Subtotal}, \text{turno.MontoSena})$ y $\text{Total} = \text{Subtotal} - \text{DescuentoSenaAplicado}$[cite: 29].
   * **Autocompletado del Turno:** Pasa el turno vinculado a `Completado (4)` dentro de la misma transacción `ExecuteInTransactionAsync`[cite: 29].
3. **Protección Contable en Anulación de Ventas (`AnularVentaAsync`):**
   * Prohibido anular ventas de una `SesionCaja` ya cerrada (`EstadoAbierta == false`) o ventas cuyos renglones ya figuren en una `Liquidacion` no anulada[cite: 29].
4. **Jerarquía de Comisiones y Anulación de Liquidaciones (`LiquidacionService`):**
   * Prioridad de cálculo: 1°) Porcentaje específico `(PeluqueroId, ServicioId)`, 2°) Porcentaje general `(PeluqueroId, ServicioId == null)`, 3°) Default `50%` servicio / `10%` producto[cite: 29].
   * `AnularLiquidacionAsync` solo permite anular liquidaciones en estado `Pendiente (1)`, marcando sus `DetallesLiquidacion` como `IsDeleted = true` para liberar los renglones[cite: 29].

---

## 5. LÍMITES ANTI-ABUSO E INTEGRIDAD DE BAJAS LÓGICAS

1. **Tope Técnico Anti-Abuso:** Se aplica un límite de seguridad de infraestructura de `50` peluqueros activos por sucursal y `20` sucursales por Tenant[cite: 29].
2. **Protección en Baja de `Peluquero` y `Servicio`:**
   * No se permite dar de baja un `Peluquero` con turnos futuros `Pendientes`/`Confirmados` o liquidaciones `Pendientes`[cite: 29].
   * Al dar de baja un `Servicio` con turnos futuros, se desactiva (`Activo = false`, `IsDeleted = true`) preservando intactos los `TurnoServicios` ya agendados[cite: 29].
```