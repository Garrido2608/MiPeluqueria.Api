# 08 - PLAN DE PRUEBAS FUNCIONALES, MATRIZ BDD Y SUITE EJECUTABLE HTTP (CAPA 8 SDD/ODD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)
**Documentos Padres Vinculados:** `00_AUDITORIA_Y_DEUDA_TECNICA.md`, `04_API_CONTRACTS.md`, `05_BUSINESS_RULES.md`, `06_INTEGRATION_AND_ODD_PLAN.md`
**Estándar de Ingeniería:** ISO/IEC/IEEE 29119 & Specification by Example (BDD)
**Archivo Ejecutable Vinculado:** `MiPeluqueria.Api.http`
**Estado:** INMUTABLE - PUERTA DE CALIDAD OBLIGATORIA PARA HARD GATES 1, 2 Y 3

---

## 1. REGLA DE APROBACIÓN DE ETAPAS (ZERO-DEFECT GATE POLICY)

Que la solución compile con `dotnet build` (`0 Errores, 0 Advertencias CS86xx`) es condición necesaria pero **NO suficiente** para aprobar los Hard Gates. Antes de cerrar la **Tarea 5.2 (Hard Gate 3)**, el agente autónomo debe validar que el código satisfaga los **10 Criterios de Aceptación Innegociables** que erradican del `[BUG-01]` al `[BUG-10]`.

---

## 2. MATRIZ DE CRITERIOS DE ACEPTACIÓN INNEGOCIABLES (GIVEN / WHEN / THEN)

### Test 01: Aislamiento Multi-Tenant y Validación Cruzada en Caché (OWASP A01 - `[BUG-03]`)
* **Dado** que existen en base de datos `TenantId = 1, SucursalId = 1` activa, y una petición HTTP con headers manipulados `X-Tenant-Id: 1` y `X-Sucursal-Id: 999` (inexistente o perteneciente a otro Tenant).
* **Cuando** se invoca cualquier endpoint operativo o público de la API.
* **Entonces** `TenantResolutionMiddleware` valida el par en `IMemoryCache` / SQL Server, intercepta la petición antes del controlador y retorna **HTTP `403 Forbidden`** con:
  `{"success": false, "message": "La sucursal indicada no pertenece al salón actual o se encuentra inactiva.", "data": null}`.
* **Y Cuando** un usuario autenticado envía un JWT válido con claims `tenantId = 1, sucursalId = 1` pero inyecta en headers `X-Tenant-Id: 2`, el middleware ignora el header malicioso y prioriza estrictamente los claims del token JWT.

### Test 02: Escudo Rate Limiting y Prevención de Escalación de Privilegios (`[BUG-04]`, `[BUG-08]`, `[BUG-10]`)
* **Dado** el endpoint público `POST /api/auth/registro` y el endpoint `POST /api/auth/login`.
* **Cuando** un atacante envía `POST /api/auth/registro` intentando inyectar `"rolId": 1` (Administrador) o `"rolId": 5` (Cajero) en el payload JSON.
* **Entonces** `RegistroUsuarioDto` y `AuthService` ignoran o sobrescriben el valor forzando estrictamente **`RolId = 4` (Cliente)** en la tabla `Usuarios`.
* **Y Cuando** se envían 6 peticiones consecutivas en menos de 60 segundos desde la misma IP a `POST /api/auth/login`.
* **Entonces** las primeras 5 se procesan normalmente (`200 OK` o `401 Unauthorized`) y la 6ta petición es cortocircuitada por la política `"StrictAuthOtpPolicy"` retornando **HTTP `429 Too Many Requests`** con envoltorio `ApiResponse<object>.Fail(...)`.

### Test 03: Persistencia Tipada Cero-Reflection en Turnos, Horarios y Fichas Técnicas (`[BUG-01]`)
* **Dado** un peluquero con horario cortado configurado vía `PUT /api/peluqueros/{id}/horarios` (`09:00` a `20:00`, `HoraInicioDescanso = 13:00`, `HoraFinDescanso = 14:00`, `Activo = true`), un servicio de `$12.000` (`30 min`) y un cliente activo.
* **Cuando** se crea un `Turno` (`POST /api/turnos`) y una `FichaTecnica` (`POST /api/fichastecnicas` con `FormulaAplicada`, `Observaciones`, `NotasSensibilidadAlergias`, `ProductosUtilizados` y `FotoResultadoUrl`).
* **Entonces**:
  1. En `GET /api/turnos/disponibilidad`, los bloques `13:00` y `13:30` **no aparecen disponibles** (el descanso persistió en SQL Server).
  2. En `GET /api/turnos/{id}`, `serviciosDetalle[0].precioHistorico` es exactamente `12000.00` y `duracionMinutosHistorica` es `30` (jamás `0`).
  3. En `GET /api/fichastecnicas/cliente/{clienteId}`, `formulaAplicada`, `observaciones`, `notasSensibilidadAlergias`, `productosUtilizados` y `fotoResultadoUrl` devuelven exactamente los strings guardados (jamás `null` por pérdida de Reflection).

### Test 04: Persistencia Real de Configuración del Salón en SQL Server (`[BUG-05]`)
* **Dado** el endpoint `PUT /api/configuracion-salon` autenticado como `Administrador`.
* **Cuando** se actualiza `"intervaloTurnosMinutos": 45`, `"porcentajeSenaReserva": 25.00` y `"nombreComercial": "Salon Test SDD"`.
* **Entonces** `ConfiguracionSalonService` persiste los cambios en la tabla `ConfiguracionSalon` de SQL Server e invalida la clave de caché `$"SaaS_Config_T{tenantId}_S{sucursalId}"`, retornando los valores actualizados en `GET /api/configuracion-salon` incluso tras reiniciar el proceso de la API.

### Test 05: Prevención de Condiciones de Carrera (`Serializable`) y Concurrencia Optimista (`RowVersion`) (`[BUG-02]`, `[BUG-07]`)
* **Dado** un slot horario libre (`10:00` a `10:30`) para el `PeluqueroId = 1`.
* **Cuando** se invoca `POST /api/turnos` o `PUT /api/turnos/{id}/reprogramar`.
* **Entonces** la operación se ejecuta dentro de `_uow.ExecuteInTransactionAsync(..., IsolationLevel.Serializable, ct)` compatible con `SqlServerRetryingExecutionStrategy`, impidiendo reservas duplicadas simultáneas (*Phantom Reads*), y cualquier llamada a `SmtpEmailService` o `PaymentGatewayService` ocurre **fuera** de la transacción SQL.
* **Y Cuando** dos operadores editan simultáneamente el mismo `Cliente`, `Producto`, `Servicio` o `Turno` con un token `RowVersion` vencido, `GlobalExceptionMiddleware` captura `DbUpdateConcurrencyException` y retorna **HTTP `409 Conflict`** con estructura `ApiResponse<object>`.

### Test 06: Webhooks Criptográficos (`HMAC-SHA256`) y Transacción Compensatoria en "Cruce de la Muerte" (`[BUG-08]`)
* **Dado** un turno online con `RequiereSena = true` que fue pasado a `EstadoId = 5 (Cancelado)` por el `RecordatorioTurnosBackgroundService` al cumplirse los 30 minutos de expiración.
* **Cuando** llega una petición a `POST /api/webhooks/pagos/mercadopago` con firma `x-signature` inválida.
* **Entonces** `PaymentGatewayService.ValidarFirmaWebhook` rechaza la petición usando `CryptographicOperations.FixedTimeEquals` y devuelve **HTTP `400/401`**.
* **Y Cuando** llega un webhook con firma `HMAC-SHA256` válida y pago `"approved"` segundos después de la cancelación automática:
  1. **Escenario A (Slot aún libre):** El sistema reactiva automáticamente el turno a `EstadoId = 2 (Confirmado)`, marca `SenaPagada = true`, registra `ReferenciaPagoExterno` y asienta el movimiento en `HistorialEstadosTurno`.
  2. **Escenario B (Slot ocupado por otro cliente en el ínterin):** Mantiene `EstadoId = 5 (Cancelado)`, marca `SenaPagada = true`, asigna `MontoSena` como saldo a favor y concatena en `Observaciones` la alerta crítica para recepción, respondiendo `200 OK` a la pasarela.

### Test 07: Facturación POS Idempotente, Deducción de Seña y Arqueo Físico vs. Digital (`[BUG-02]`, `[BUG-07]`, `[BUG-10]`)
* **Dado** una `SesionCaja` abierta con `MontoInicial = $10.000`, y un `Turno` con servicios por `$20.000` que posee `SenaPagada = true` y `MontoSena = $5.000`.
* **Cuando** se invoca `POST /api/ventas` enviando `turnoId`, header `X-Idempotency-Key: "pos-uuid-001"` y un pago con Mercado Pago QR (`MedioPagoId` con `EsEfectivoFisico = false`) por `$15.000`:
* **Entonces**:
  1. La venta se persiste en una única transacción con `Subtotal = 20000.00`, `DescuentoSenaAplicado = 5000.00` y `Total = 15000.00`.
  2. Los renglones sin profesional explícito heredan `turno.PeluqueroId`, y el turno pasa a `EstadoId = 4 (Completado)`.
  3. Si se reenvía el mismo `POST /api/ventas` con `X-Idempotency-Key: "pos-uuid-001"`, la API retorna `200 OK` con el mismo `VentaResponseDto` existente sin duplicar cobro ni volver a descontar stock.
  4. Al consultar `GET /api/caja/estado-actual`, `totalVentasDigital` es `15000.00`, `totalVentasEfectivo` es `0.00` y **`montoFinalEsperado`** en cajón físico se mantiene exactamente en **`10000.00`**.

### Test 08: Consumo de Gabinete No Bloqueante con `Warnings` y Trazabilidad de Kardex (`[BUG-01]`, `[BUG-06]`)
* **Dado** un servicio con receta técnica (`ServicioInsumoReceta`) que requiere `2` unidades de un insumo cuyo `StockActual` en la tabla `Productos` es de solo `1` unidad.
* **Cuando** el turno pasa a `Completado (4)` o se invoca `POST /api/recetas-servicio/turno/{turnoId}/consumir-gabinete`.
* **Entonces**:
  1. La operación **no se bloquea**: descuenta el stock hasta `0` (`Math.Max(0, 1 - 2)`), registra el movimiento en `MovimientosStock` con **`SaldoResultante = 0`** y devuelve `200 OK` incluyendo la alerta en `ApiResponse.Warnings`.
  2. Si se invoca nuevamente el consumo para el mismo `turnoId`, la verificación de idempotencia evita un segundo descuento.
  3. Al consultar `GET /api/productos/{id}/kardex`, devuelve `ApiResponse<PagedResult<KardexItemResponseDto>>` informando `saldoResultante: 0`.

### Test 09: Jerarquía de Comisiones, Liquidaciones y Protección Contable de Anulación (`[BUG-09]`, `[BUG-10]`)
* **Dado** un peluquero con comisión general del `40%` (`ServicioId = null`) y comisión específica del `60%` para el `ServicioId = 2`.
* **Cuando** se genera una liquidación (`POST /api/liquidaciones/generar`) e intentan anularse (`DELETE /api/ventas/{id}` o anulación) ventas asociadas.
* **Entonces**:
  1. `ComisionConfigRepository` aplica el `60%` al `ServicioId = 2` y el `40%` al resto de los servicios (usando el fallback `50%` servicio / `10%` producto solo si no existe configuración para el profesional).
  2. El sistema **rechaza con `400 Bad Request`** cualquier intento de anular una venta cuya `SesionCaja` ya esté cerrada (`EstadoAbierta == false`) o cuyos renglones estén incluidos en una `Liquidacion` no anulada.
  3. Al invocar `POST /api/liquidaciones/{id}/anular` sobre una liquidación en estado `Pendiente (1)`, pasa a anulada y marca sus `DetallesLiquidacion` con `IsDeleted = true`, liberando los renglones para una nueva liquidación.

### Test 10: Alertas No Bloqueantes en Bloqueos Horarios y Tolerancia a Fallos SMTP (`[BUG-09]`, Deuda Táctica SMTP)
* **Dado** un peluquero que ya tiene un turno `Confirmado` a las `16:00` y un entorno local donde `Smtp:Host` está vacío o inaccesible.
* **Cuando** se registra un bloqueo (`POST /api/bloqueos`) de `15:00` a `18:00` o se crea un nuevo turno/usuario.
* **Entonces**:
  1. El bloqueo horario se guarda con éxito (`200 OK`), pero `ApiResponse.Warnings` contiene el detalle de los IDs y horarios de los turnos solapados afectados.
  2. El fallo o ausencia de servidor SMTP no demora más de `5000ms` ni aborta la transacción principal; retorna `200/201` con una advertencia en `ApiResponse.Warnings`.

---

## 3. ESTRUCTURA EJECUTABLE OBLIGATORIA PARA `MiPeluqueria.Api.http`

En la **Tarea 5.2**, el agente debe actualizar `MiPeluqueria.Api.http` con variables de entorno y la secuencia encadenada completa:

```http
@baseUrl = https://localhost:7132/api
@tenantId = 1
@sucursalId = 1
@idempotencyKey = 550e8400-e29b-41d4-a716-446655440000

### 1. Test Seguridad: Intento de Cruce Multi-Tenant Inválido (Debe retornar 403 Forbidden)
GET {{baseUrl}}/catalogos/todos
X-Tenant-Id: 1
X-Sucursal-Id: 999

### 2. Login Administrador Semilla (Captura de JWT)
# @name loginAdmin
POST {{baseUrl}}/auth/login
Content-Type: application/json
X-Tenant-Id: {{tenantId}}
X-Sucursal-Id: {{sucursalId}}

{
  "usernameOrEmail": "admin@tesis.com",
  "password": "Admin123!"
}

### 3. Persistencia Real de Configuración del Salón (Verifica BUG-05)
@authToken = {{loginAdmin.response.body.data.token}}
PUT {{baseUrl}}/configuracion-salon
Authorization: Bearer {{authToken}}
Content-Type: application/json
X-Tenant-Id: {{tenantId}}
X-Sucursal-Id: {{sucursalId}}

{
  "nombreSalon": "Mi Peluquería Central",
  "nombreComercial": "Mi Peluquería Pro SaaS",
  "razonSocial": "Mi Peluquería S.A.",
  "cuit": "30-71122334-9",
  "direccion": "Av. Colón 500, Córdoba",
  "telefono": "+5493515550000",
  "emailContacto": "contacto@tesis.com",
  "monedaIso": "ARS",
  "timeZoneId": "America/Argentina/Cordoba",
  "horarioAtencionTexto": "Lunes a Sábados de 09:00 a 20:00 hs",
  "intervaloTurnosMinutos": 30,
  "minutosToleranciaEspera": 15,
  "horasAnticipacionCancelacion": 24,
  "porcentajeSenaReserva": 25.00,
  "notificacionesWhatsAppActivas": true,
  "notificacionesEmailActivas": true
}

### 4. Apertura de Caja, Venta POS Idempotente y Verificación de montoFinalEsperado (Verifica BUG-02, BUG-07, BUG-10)
POST {{baseUrl}}/ventas
Authorization: Bearer {{authToken}}
Content-Type: application/json
X-Tenant-Id: {{tenantId}}
X-Sucursal-Id: {{sucursalId}}
X-Idempotency-Key: {{idempotencyKey}}

{
  "clienteId": 1,
  "items": [
    {
      "tipoItem": 1,
      "itemId": 1,
      "cantidad": 1,
      "precioUnitario": 12000.00,
      "peluqueroId": 1
    }
  ],
  "pagos": [
    {
      "medioPagoId": 5,
      "importe": 12000.00,
      "referenciaOperacion": "MP-QR-998877"
    }
  ]
}
