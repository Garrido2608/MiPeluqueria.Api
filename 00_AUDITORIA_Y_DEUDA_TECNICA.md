# 00 - REGISTRO MAESTRO DE AUDITORÍA, BUGS OCULTOS, DEUDA TÉCNICA Y PLAN DE EJECUCIÓN (v2.0)
**Sistema:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)[cite: 24]
**Arquitectura Objetivo:** Monolito Modular Multi-Tenant con Aislamiento Estricto por Local (`TenantId` + `SucursalId`)[cite: 24]
**Rol de este Documento:** Fuente de verdad innegociable de saneamiento arquitectónico y hoja de ruta de refactorización para el agente autónomo (Google Antigravity)[cite: 24].

---

## SECCIÓN 1: BOMBAS DE TIEMPO, CONCURRENCIA Y PÉRDIDA DE DATOS (PRIORIDAD CRÍTICA P0)

### [BUG-01] Pérdida silenciosa de datos por "Reflection Defensiva" (`SetOptionalProp` / `GetOptionalProp`)
* **Archivos afectados:** `TurnoService.cs`, `FichaTecnicaService.cs`, `PeluqueroService.cs`, `ProductoService.cs`, `CatalogoService.cs`, `VentaRepository.cs`[cite: 24].
* **Diagnóstico técnico:** Métodos privados buscan propiedades por `string` mediante Reflection y silencian fallos con `try/catch` vacíos cuando el nombre en el Servicio no coincide con la Entidad de EF Core[cite: 24].
* **Daño comprobado en base de datos:**
  1. **`TurnoServicio`:** `TurnoService.CreateAsync` busca `"PrecioAcordado"` y `"Precio"`, pero `TurnoServicio.cs` define `PrecioHistorico` y `DuracionMinutosHistorica`[cite: 24]. Todos los turnos se guardan con precio y duración histórica en `0`[cite: 24].
  2. **`FichaTecnica`:** `FichaTecnicaService` busca `"FormulaAplicada"`, `"Observaciones"`, `"ProductosUtilizados"` y `"FotoResultadoUrl"`, pero `FichaTecnica.cs` solo tiene `DetalleFormulaTintura` y `NotasSensibilidadAlergias`[cite: 24]. Se pierden todos los datos al guardar[cite: 24].
  3. **`HorarioLaboral`:** `PeluqueroService` y `TurnoService` buscan `"HoraInicioDescanso"`, `"HoraFinDescanso"` y `"Activo"`, pero `HorarioLaboral.cs` solo tiene `DiaSemana`, `HoraInicio` y `HoraFin`[cite: 24]. Los descansos y francos jamás persisten[cite: 24].
  4. **`MovimientoStock`:** `ProductoService` busca `"StockResultante"`, columna inexistente en `MovimientoStock.cs` (requerida como `SaldoResultante` en `KardexItemResponseDto`)[cite: 24].
  5. **`VentaRepository`:** Usa Reflection sobre `entry.Metadata` para leer `Nombre`, `Apellido` y `Precio` de `Servicio` y `Peluquero`[cite: 24].
* **Resolución obligatoria:** Erradicar el 100% de Reflection[cite: 24]. Sincronizar propiedades en Entidades, configurar Fluent API, generar migración y usar asignación estática fuertemente tipada[cite: 24].

### [BUG-02] Crash en Producción por Conflicto entre `EnableRetryOnFailure` y `BeginTransactionAsync`
* **Archivos afectados:** `Program.cs`, `UnitOfWork.cs`, `VentaService.cs`, `OrdenCompraService.cs`[cite: 4, 14].
* **Diagnóstico técnico:** `Program.cs` habilita `SqlServerRetryingExecutionStrategy` (`EnableRetryOnFailure`)[cite: 4, 11], la cual prohíbe llamar a `Database.BeginTransactionAsync()` manualmente fuera de un delegado `CreateExecutionStrategy().ExecuteAsync(...)`.
* **Resolución obligatoria:** Implementar en `IUnitOfWork` / `UnitOfWork` el método `ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, CancellationToken ct = default)` con contador de profundidad (`_transactionDepth`) para soportar transacciones anidadas entre servicios y limpieza del `ChangeTracker` ante reintentos.

### [BUG-03] Falso Aislamiento Multi-Tenant + Vulnerabilidad de Cruce de Sucursal (OWASP A01)
* **Archivos afectados:** `Models/Common/ITentantEntity.cs` (typo), todas las entidades operativas en `Models/`, `ApplicationDbContext.cs`, `TenantResolutionMiddleware.cs`[cite: 24].
* **Diagnóstico técnico:**
  1. Ninguna entidad implementa `ITenantEntity`; `HasQueryFilter` solo filtra por `!e.IsDeleted`[cite: 24].
  2. `TenantResolutionMiddleware` acepta headers `X-Tenant-Id` y `X-Sucursal-Id` sin verificar que la `SucursalId` pertenezca realmente a ese `TenantId`[cite: 11].
* **Resolución obligatoria:**
  1. Renombrar a `ITenantEntity.cs`, crear `AuditableTenantEntity` y aplicarla a las 35 entidades operativas/catálogos de salón[cite: 26, 27].
  2. Aplicar `HasQueryFilter` compuesto por `TenantId` y `SucursalId` y autocompletado en `SaveChangesAsync`[cite: 24, 27].
  3. Validar en `TenantResolutionMiddleware` mediante `IMemoryCache` (`SlidingExpiration = 10 min`) que el par `(TenantId, SucursalId)` exista y esté activo en la tabla `Sucursales`, devolviendo `403 Forbidden` ante discrepancias.

### [BUG-04] Crash en `AuthController` por Política de Rate Limiting No Registrada
* **Archivos afectados:** `Controllers/AuthController.cs`, `Program.cs`[cite: 24].
* **Diagnóstico técnico:** `AuthController` exige `[EnableRateLimiting("StrictAuthOtpPolicy")]`, pero `Program.cs` no registra `AddRateLimiter` ni `UseRateLimiter`[cite: 24].
* **Resolución obligatoria:** Registrar Rate Limiter nativo en `Program.cs` (5 peticiones/minuto por IP) devolviendo `429 Too Many Requests` con formato `ApiResponse<object>.Fail(...)`[cite: 24, 26].

### [BUG-05] `ConfiguracionSalonController` Opera en Memoria RAM Estática e Ignora SQL Server
* **Archivos afectados:** `Controllers/ConfiguracionSalonController.cs`, `Services/Implementations/ConfiguracionSalonService.cs`[cite: 24].
* **Diagnóstico técnico:** El controlador define un `ConfiguracionSalonDto` duplicado y guarda cambios en `private static ConfiguracionSalonDto _configActual = new();`, ignorando `IConfiguracionSalonService` y la base de datos[cite: 24].
* **Resolución obligatoria:** Eliminar clase duplicada y variable estática; inyectar `IConfiguracionSalonService` con caché particionada por `(TenantId, SucursalId)`[cite: 24, 26].

---

## SECCIÓN 2: RENDIMIENTO, ESCALABILIDAD, RED Y SEGURIDAD (PRIORIDAD ALTA P1)

### [BUG-06] Consultas $O(2N + 1)$ en `VentaService` y `ProveedorService` (Agotamiento de Connection Pool)
* **Archivos afectados:** `VentaService.cs`, `ProveedorService.cs`, `IProductoRepository.cs`, `IServicioRepository.cs`[cite: 14].
* **Diagnóstico técnico:** Se ejecutan consultas `SELECT` individuales dentro de bucles `foreach` al registrar ventas, al mapear `VentaResponseDto` en `ObtenerVentasPorSesionAsync` y al contar productos por proveedor en `ProveedorService.GetAllAsync`[cite: 14].
* **Resolución obligatoria:** Implementar `GetByIdsDictionaryAsync(IEnumerable<int> ids, bool tracking, CancellationToken ct)` usando `WHERE Id IN (...)` (`Contains`) para resolver todos los productos y servicios en 1 sola consulta SQL ($O(1)$), y proyectar lecturas con `.Select(...)` directo en SQL.

### [BUG-07] Ausencia de Idempotencia en POS (`Ventas`) y Race Condition en Reserva de Turnos (`TOCTOU`)
* **Archivos afectados:** `Venta.cs`, `CrearVentaDto.cs`, `VentasController.cs`, `VentaService.cs`, `TurnoService.cs`, `PortalReservasController.cs`[cite: 11, 14].
* **Diagnóstico técnico:**
  1. Un reintento de red o doble clic en `POST /api/ventas` duplica el cobro y descuenta el doble de stock[cite: 14].
  2. Dos reservas simultáneas para el mismo horario pasan el `AnyAsync` bajo aislamiento `ReadCommitted` generando doble reserva (`Phantom Read`)[cite: 14].
* **Resolución obligatoria:**
  1. Soportar el header `X-Idempotency-Key` (y propiedad `IdempotencyKey` en `CrearVentaDto` / tabla `Ventas` con índice único filtrado por local) retornando `200 OK` con la venta existente si la clave ya fue procesada.
  2. Ejecutar `TurnoService.CreateAsync` y `ReprogramarAsync` bajo `IsolationLevel.Serializable` dentro de `ExecuteInTransactionAsync`, manteniendo las llamadas externas a MercadoPago/SMTP **fuera** de la transacción SQL.

### [BUG-08] Falsificación de Firma en Webhooks (OWASP A08) y "Cruce de la Muerte" con Background Worker
* **Archivos afectados:** `PaymentGatewayService.cs`, `PortalReservasController.cs` (`WebhooksController`), `RecordatorioTurnosBackgroundService.cs`[cite: 11].
* **Diagnóstico técnico:**
  1. `ValidarFirmaWebhook` solo verifica `!string.IsNullOrWhiteSpace(signatureHeader)` sin calcular `HMAC-SHA256`[cite: 11].
  2. Si el worker cancela un turno impago a los 30 minutos y el webhook de pago aprobado llega a los 30m 05s, la máquina de estados bloquea la confirmación[cite: 11, 29].
* **Resolución obligatoria:**
  1. Implementar cálculo criptográfico `HMACSHA256` real sobre el manifiesto/payload del webhook.
  2. Aplicar **Patrón de Transacción Compensatoria** en el webhook: si el turno estaba `Cancelado` por expiración pero el slot sigue libre, reabrirlo y pasarlo a `Confirmado`; si el slot fue ocupado por otro cliente, dejar `SenaPagada = true` como saldo a favor y registrar alerta para recepción.

### [BUG-09] Bypass de `IUnitOfWork`, Código Huérfano y Exposición de Entidades Crudas / `object`
* **Archivos afectados:** 4 Controladores y 13 Servicios que inyectan `ApplicationDbContext` directo; `CajaService`, `SuscripcionesController`, `BloqueoHorarioService`, `ProductoService`, `TurnoService`[cite: 24].
* **Resolución obligatoria:**
  1. Prohibir `ApplicationDbContext` en Controladores y Servicios[cite: 24, 25]. Integrar `IOrdenCompraRepository`, `IRecetaServicioRepository` e `ISaaSRepository` dentro de `IUnitOfWork`[cite: 26].
  2. Conectar `UsuariosController` con `IUsuarioService` y `CatalogosController` con `ICatalogoService`[cite: 24, 26].
  3. Reemplazar retornos de `SesionCaja`, `MovimientoCaja`, `PlanSuscripcion` e `IEnumerable<object>` por sus DTOs tipados (`SesionCajaResponseDto`, `MovimientoCajaResponseDto`, `PlanSuscripcionResponseDto`, `BloqueoHorarioResponseDto`, `KardexItemResponseDto`, `HistorialEstadoTurnoResponseDto`)[cite: 24, 28].

### [BUG-10] Fallas en Reglas Financieras (Arqueo Físico/Digital, Señas en POS, Comisiones) y Seguridad
* **Archivos afectados:** `CajaService.cs`, `VentaService.cs`, `ComisionConfigRepository.cs`, `SeguridadDto.cs`, `ClientesController.cs`, `DashboardController.cs`, `FichasTecnicasController.cs`[cite: 24].
* **Resolución obligatoria:**
  1. Separar en `SesionCaja` el efectivo físico (`EsEfectivoFisico == true`) de los medios digitales[cite: 24, 27, 29].
  2. Descontar automáticamente en `VentaService.RegistrarVentaAsync` la seña abonada (`DescuentoSenaAplicado`) y heredar `turno.PeluqueroId` en los renglones sin profesional explícito[cite: 27, 29].
  3. Filtrar correctamente por `servicioId` con fallback a `ServicioId == null` en `ComisionConfigRepository`[cite: 24, 29].
  4. Fijar `RolId = 4` (Cliente) en `RegistroUsuarioDto` y proteger todos los controladores internos con `[Authorize]`[cite: 24, 25].

---

## SECCIÓN 3: DEUDA TÉCNICA TÁCTICA VS. ESTRATÉGICA (HOJA DE RUTA FUTURA)

1. **Servicio de Correo Electrónico (`SmtpEmailService.cs`):**
   * **Acción Táctica Inmediata (Fase Actual):** Configurar `Timeout = 5000` (5 segundos) en `SmtpClient` con `CancellationToken` y capturar cualquier excepción de red devolviendo un `Warning` en `ApiResponse` para evitar *Thread Starvation* si el servidor SMTP externo no responde.
   * **Deuda Técnica Estratégica (Fase Post-MVP):** Reemplazar `System.Net.Mail.SmtpClient` (API obsoleta) por **MailKit** y desacoplar el envío mediante una cola en memoria (`System.Threading.Channels.Channel<EmailMessage>`) procesada por un `BackgroundService`, logrando tiempos de respuesta HTTP inferiores a 50ms en registro y reservas.
2. **Saneamiento del Frontend (`mipeluqueria-saas-web`):**
   * Restaurar `package.json` con el stack Pro (`@tanstack/react-query`, `react-hook-form`, `zod`, `sonner`, `date-fns`, `recharts`)[cite: 24, 26].
   * Reparar `src/app/layout.tsx` (`TS2306`)[cite: 24, 26].
   * Modularizar `src/types/` y `src/services/` manteniendo *Barrel Files* retrocompatibles (`api.types.ts` y `api.services.ts`) e inyectar `X-Idempotency-Key` en los cobros de POS[cite: 26].

---

## SECCIÓN 4: PLAN DE EJECUCIÓN ORDENADO POR FASES (PARA GOOGLE ANTIGRAVITY)

Para evitar errores de compilación en cascada, el agente autónomo debe ejecutar los cambios respetando estrictamente estas **5 Fases Secuenciales**:

* **FASE 1 (Cimientos y Dominio - Capa 3):** Renombrar `ITenantEntity.cs`, crear `AuditableTenantEntity.cs`, actualizar las 41 entidades en `Models/` (agregando columnas faltantes, `RowVersion`, `IdempotencyKey` y herencia universal), actualizar las 12 clases en `Data/Configurations/`, refactorizar `ApplicationDbContext.cs` (filtros Multi-Tenant + Seed completo) y generar la migración EF Core.
* **FASE 2 (Contratos y Validaciones - Capa 4):** Actualizar `ApiResponse.cs` (`FieldErrors`, `Warnings`), `PaginationFilterDto.cs`, crear/actualizar los DTOs en `DTOs/`, actualizar `MappingProfile.cs` y corregir los validadores en `Validators/`.
* **FASE 3 (Acceso a Datos y Transacciones - Capa 2):** Actualizar `IGenericRepository.cs` y `GenericRepository.cs` (incluyendo `GetByIdsDictionaryAsync`), crear `IRecetaServicioRepository` e `ISaaSRepository`, estandarizar los 19 repositorios y refactorizar `IUnitOfWork` / `UnitOfWork` con `ExecuteInTransactionAsync`.
* **FASE 4 (Lógica de Negocio, Seguridad y Pipeline - Capa 5 y Capa 2):** Refactorizar los 19 Servicios en `Services/` (cero Reflection, cero `DbContext` directo, cero consultas $N+1$, reglas de caja/señas/turnos/webhooks), actualizar `GlobalExceptionMiddleware.cs` (`409 Conflict`, `403 Forbidden`), `TenantResolutionMiddleware.cs` (validación cruzada en caché), `RecordatorioTurnosBackgroundService.cs`, los 22 Controladores y `Program.cs`.
* **FASE 5 (Frontend Next.js 14 Pro):** Reparar `package.json` y `layout.tsx`, configurar interceptores en `src/lib/axios.ts` (JWT, Tenant, RefreshToken, `X-Idempotency-Key`) y estructurar módulos con *Barrel Files*.