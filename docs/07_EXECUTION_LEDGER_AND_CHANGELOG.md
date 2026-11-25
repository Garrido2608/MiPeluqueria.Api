# 07 - LIBRO MAYOR DE ESTADO EN VIVO, BITÁCORA DE CAMBIOS Y MEMORIA PERSISTENTE (CAPA 7 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)
**Propósito Crítico:** Memoria persistente en disco para el Agente de IA. Elimina la dependencia del historial de chat y previene alucinaciones por desfase entre especificaciones y código real.
**Regla de Uso:** El Agente DEBE leer este archivo ANTES de iniciar cualquier tarea y DEBE actualizarlo ANTES de dar por finalizada cualquier tarea.

---

## 1. ESTADO ACTUAL DEL PROYECTO (PUNTERO DE EJECUCIÓN)

* **Estado Global de Ingeniería:** `ETAPA 8 EN CURSO (v1.1.0 — FASE 8C EN CURSO — TAREA 8.9 COMPLETADA)`
* **Última Tarea Completada:** `TAREA 8.9 - Configuración/Seguridad (/admin/configuracion-seguridad) + Separación SuperAdminSaaS (/saas-admin, /admin/diagnostico-api)`
* **Tarea Actual en Curso:** `TAREA 8.10 - Limpieza de Datos E2E, Suite Playwright Responsive (1280x720, 1366x768, 390x844), Certificación Integral y Cierre v1.1.0`
* **Estado de Compilación, Sonda y Testing Backend (`dotnet build --warnaserror` + `probe-api-22.mjs` + `dotnet test`):** `0 Errores / 0 Advertencias — Sonda Runtime 22/22 200 OK (27 ms prom. en caliente, máx. 75 ms) — Health Checks /health/live y /health/ready 200 Healthy — Serilog JSON Zero-PII activo (0 errores) — xUnit 40/40 Tests PASS (10 BDD + 22 Contratos + 3 Observabilidad + 5 Etapa 8 Fixes, 0 FAIL, 0 SKIP)`
* **Estado de Migración SQL Server:** `MiPeluqueriaDB_Tests migrada y validada (Up + Down) con 20261125014902_AddSuperAdminSaaSRole; comando exacto documentado para aplicar sobre MiPeluqueriaDB al liberar conexiones locales.`
* **Estado de Compilación, Frontera UI, E2E y CI/CD (`tsc --noEmit` + `next build` + Playwright E2E + GitHub Actions):** `0 Errores de TypeScript / 0 Errores de Build Producción (24/24 páginas con Plus Jakarta Sans + JetBrains Mono y Design System Dual) / 0 imports de dominio en src/components/ui / 0 ocurrencias de any / Playwright E2E 5/5 PASS (v1.0.0) / Pipeline .github/workflows/ci.yml validado`

---

## 2. REGISTRO DE DESVIACIONES, RENOMBRES Y DECISIONES APROBADAS EN VIVO (OVERRIDE LOG)

> **DIRECTIVA PARA EL AGENTE:** Toda modificación de nombres de propiedades, firmas de métodos, tipos de datos o reglas de negocio que se decida durante la programación DEBE registrarse en esta tabla y sincronizarse en el archivo `01` a `05` correspondiente. **Lo registrado en esta tabla tiene prioridad sobre cualquier texto anterior.**

| Fecha / Tarea | Archivo `.md` Original Afectado | Especificación Original | Nuevo Valor / Cambio Implementado | Motivo Técnico / Autorización |
| :--- | :--- | :--- | :--- | :--- |
| *Inicial* | `03_DATA_MODEL.md` | `ITentantEntity.cs` | `ITenantEntity.cs` + `AuditableTenantEntity.cs` | Corrección de typo e inclusión de herencia universal (`TenantId`, `SucursalId`)[cite: 30, 35]. |
| Tarea 5.1 | `02_ARCHITECTURE.md` (Sección 3.2, pasos 2 y 3) | Validación del par Tenant/Sucursal consultando `ApplicationDbContext.Sucursales` directo desde el middleware; caché solo con `SlidingExpiration = 10 min`. | Consulta vía `IUnitOfWork.SaaS.EsSucursalValidaYActivaAsync(...)`; caché válida con `SlidingExpiration = 10 min` + tope `AbsoluteExpirationRelativeToNow = 60 min`; resultados inválidos cacheados `1 min`. | Regla "0 DbContext directo" (`01` Sección 4.2). El tope absoluto impide que el tráfico continuo mantenga válida para siempre una sucursal desactivada (OWASP A01). **Ratificado por el Arquitecto y SPEC-SYNC aplicado en `02`.** |
| Tarea 5.1 / 5.2 | `06_INTEGRATION_AND_ODD_PLAN.md` (Tarea 5.1, ítem 5) / `02_ARCHITECTURE.md` (árbol) | `Services/Background/RecordatorioTurnosBackgroundService.cs` | Renombrado en Tarea 5.2 de `Services/BackGround/RecordatorioTurnoBackgroundService.cs` a `Services/Background/RecordatorioTurnosBackgroundService.cs` (1:1 con `02_ARCHITECTURE.md`). | **Resuelto en Tarea 5.2 por directiva expresa del Arquitecto.** |
| Tarea 5.1 / 5.2 | `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (Sección 2) | Sin valor por defecto especificado para `BackgroundJobs:CancelarReservasOnlineImpagas` si falta la clave. | Valor por defecto `false` si la clave no existe (el JSON la declara `true`). La expiración aplica **solo** a turnos con `RequiereSena && !SenaPagada` cuyo campo `Observaciones` empiece con `"[Reserva Online]"`, prefijo que `PortalReservasController.ReservarTurnoPublico` sella automáticamente en Tarea 5.2. | `TurnoService.CreateAsync` marca `RequiereSena = true` también en los turnos cargados por el staff; el prefijo `"[Reserva Online]"` sincroniza el portal público con el worker. **Ratificado por el Arquitecto.** |
| Tarea 7.1 | `01_SYSTEM_CONSTRAINTS.md` (Sección 2.1) | Único proyecto `.csproj` sin proyectos adicionales. | Se autoriza exclusivamente el proyecto satélite de pruebas `MiPeluqueria.Api.Tests.csproj` (xUnit + `WebApplicationFactory<Program>`), manteniendo intacto el monolito modular de producción `MiPeluqueria.Api.csproj`. | Habilitar la suite BDD automatizada y cercada de la Etapa 7 sin dividir el código de producción. **Ratificado en `11_QA_HARDENING_OBSERVABILITY_AND_REUSABILITY_PLAN.md`.** |
| Tarea 7.1 | `01_SYSTEM_CONSTRAINTS.md` (Secciones 2.3 y 5.1), `02_ARCHITECTURE.md` (Sección 5.2) | Stack NuGet y `package.json` base sin librerías de observabilidad estructurada ni testing E2E. | Inclusión oficial de `Serilog.AspNetCore`, `Serilog.Formatting.Compact`, `Serilog.Sinks.File`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions` y `@playwright/test`. | Observabilidad Zero-PII en JSON, Health Checks de liveness/readiness y suites automatizadas xUnit + Playwright E2E. |
| Tarea 7.1 | `01_SYSTEM_CONSTRAINTS.md` (Sección 4.4), `04_API_CONTRACTS.md`, `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md` (Test 04 y Sec. 3), `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Sec. 2 y 4.5) | Referencias residuales a `/api/configuracion-salon` en la documentación. | Unificación documental en **`/api/configuracion`** para coincidir 1:1 con `[Route("api/configuracion")]` compilado en `ConfiguracionSalonController.cs` y consumido en `common.service.ts`. | Eliminar cualquier contradicción documental entre los archivos `01`, `04`, `08`, `10` y el código real compilado. |
| Tarea 7.1 | `02_ARCHITECTURE.md` (Secciones 1.1 y 3.1) | Pipeline HTTP iniciando en `GlobalExceptionMiddleware` y terminando en `MapControllers()`. | Orden actualizado: `CorrelationIdMiddleware -> UseSerilogRequestLogging() -> GlobalExceptionMiddleware -> Swagger(dev) -> HttpsRedirection -> Cors -> RateLimiter -> Authentication -> TenantResolutionMiddleware -> Authorization -> MapHealthChecks("/health/live") + MapHealthChecks("/health/ready") -> MapControllers()`. | Trazabilidad por `X-Correlation-Id` de punta a punta y sondas de salud desacopladas del bloqueo multi-tenant. |
| Tarea 7.1 | `01_SYSTEM_CONSTRAINTS.md` (Sección 5.1), `04_API_CONTRACTS.md` (Secciones 2, 3.2 y 4), `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (Sección 1), `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Sección 5) | Sin contrato de ingesta de telemetría frontend, sección `Serilog` en JSON ni frontera formal de agnosticismo en `src/components/ui/`. | Contrato `POST /api/telemetry/client-errors` (`ClientErrorReportDto <= 4 KB`, Zero-PII, `202 Accepted`), bloque `"Serilog"` en `09`, Error Boundaries (`error.tsx`, `(dashboard)/error.tsx`, `global-error.tsx`) y regla de **0 imports de dominio** en `src/components/ui/*.tsx`. | Convergencia de errores Frontend/Backend en `Logs/errors-YYYYMMDD.json` y reutilización pura del Design System. |
| Tarea 8.1 | `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`, `tailwind.config.js` | Tema claro único con fuente `Inter` sin tokens duales y archivo `tailwind.config.js`. | Design System Dual `v1.1.0` (*Studio Clean* Claro por defecto + *Obsidian & Indigo* `.dark` con `darkMode: 'class'`, persistencia `mipeluqueria_theme` sin FOUC), tipografías `Plus Jakarta Sans` + `JetBrains Mono` (`tabular-nums`), `tailwind.config.ts` (reemplazando `.js`), mapa `src/lib/semanticColors.ts` y 4 componentes agnósticos (`NumericInput`, `BrandLogo`, `SearchCombobox`, `ThemeToggle`). En `Button` se mantienen alias legacy temporales (`editorial` -> `primary`, `danger` -> `destructive`) hasta finalizar Tareas 8.4–8.9. | Garantizar estándar comercial `v1.1.0` (WCAG 2.2 AA, viewport `1280x720`/`1366x768`, logo negro sobre placa clara) con cero regresiones en las 19 pantallas vigentes. **SPEC-SYNC aplicado en `docs/10` y `AGENTS.md`.** |
| Tarea 8.3 | `03_DATA_MODEL.md`, `04_API_CONTRACTS.md`, `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`, `DTOs/Ventas/ComisionConfigDto.cs`, `Controllers/SuscripcionesController.cs` | Catálogo `Rol` con IDs `1..5`; `SuscripcionesController.CambiarPlan` accesible para `Administrador`; `ComisionConfigDto` solo con `NombrePeluquero` y `NombreServicio`. | Incorporación de `Rol { Id = 6, Nombre = "SuperAdminSaaS" }` y usuario semilla `superadmin@mipeluqueria.com` (`"SuperAdmin123!"`, `RolId = 6`, fecha fija `2026-01-01T00:00:00Z` y hash BCrypt estático) mediante migración aditiva `20261125014902_AddSuperAdminSaaSRole`; restricción de `POST /api/suscripciones/cambiar-plan` exclusivamente a `[Authorize(Roles = "SuperAdminSaaS")]` (manteniendo `GET mi-suscripcion` para `Administrador,SuperAdminSaaS` y `GET planes` público); adición no disruptiva de `PeluqueroNombre` y `ServicioNombre` en `ComisionConfigDto` preservando `NombrePeluquero` y `NombreServicio`. | Separación real RBAC de plataforma SaaS frente a dueños de salón y alineación aditiva 1:1 del contrato `ComisionConfigDto` con el Frontend sin romper contratos previos. **SPEC-SYNC aplicado en `docs/03`, `docs/04` y `docs/09`.** |

---

## 3. INVENTARIO DE PUENTES DE COMPATIBILIDAD TEMPORAL (`[NotMapped]` / Obsoletos)

> **DIRECTIVA PARA EL AGENTE:** Si en una tarea temprana se deja una propiedad o método puente para no romper la compilación de capas superiores, anotalo acá y tácharlo (`~~así~~`) cuando lo elimines en la tarea correspondiente.

* ~~`[ACTIVO - CREADO EN TAREA 1.2 -> ELIMINAR EN TAREA 4.1]`: Propiedades `[NotMapped] public DateTime Fecha { get => FechaRegistro; set => FechaRegistro = value; }` y `[NotMapped] public string? DetalleFormulaTintura { get => FormulaAplicada; set => FormulaAplicada = value ?? string.Empty; }` en `Models/Clientes/FichaTecnica.cs` para sostener la compilación de `ClientesModuloConfiguration.cs` (hasta Tarea 1.4) y `FichaTecnicaService.cs` (hasta Tarea 4.1).~~ `[ELIMINADO DEFINITIVAMENTE EN TAREA 4.1]`
* ~~`[ACTIVO - CREADO EN TAREA 3.1 -> ELIMINAR EN TAREA 4.4]`: Métodos transaccionales manuales `BeginTransactionAsync`, `CommitTransactionAsync` y `RollbackTransactionAsync` en `Repositories/Interfaces/IUnitOfWork.cs` y `Repositories/Implementations/UnitOfWork.cs` conservados temporalmente para sostener la compilación de `VentaService.cs` y `OrdenCompraService.cs` hasta su migración a `ExecuteInTransactionAsync` en la Tarea 4.4.~~ `[ELIMINADO DEFINITIVAMENTE EN TAREA 4.4]`

---

## 4. DICCIONARIO DE SÍMBOLOS Y FIRMAS CRÍTICAS CONSOLIDADAS EN CÓDIGO

*(El agente debe ir completando esta sección al cerrar cada Etapa con las firmas exactas que quedaron compiladas en C# para no adivinarlas en etapas posteriores)*

* **Interfaz Multi-Tenant (`Models/Common/ITenantEntity.cs`):**
  ```csharp
  public interface ITenantEntity
  {
      int TenantId { get; set; }
      int? SucursalId { get; set; }
  }
  ```
* **Clase Base de Auditoría (`Models/Common/AuditableEntity.cs`):**
  ```csharp
  public abstract class AuditableEntity : ISoftDelete
  {
      [Key]
      public int Id { get; set; }
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
      public int? CreatedByUserId { get; set; }
      public DateTime? UpdatedAt { get; set; }
      public int? UpdatedByUserId { get; set; }
      public bool IsDeleted { get; set; } = false;
      public DateTime? DeletedAt { get; set; }
      public int? DeletedByUserId { get; set; }
  }
  ```
* **Clase Base Operativa (`Models/Common/AuditableTenantEntity.cs`):**
  ```csharp
  public abstract class AuditableTenantEntity : AuditableEntity, ITenantEntity
  {
      public int TenantId { get; set; } = 1;
      public int? SucursalId { get; set; } = 1;
  }
  ```
* **Excepción de Acceso Prohibido (`Exceptions/ForbiddenAccessException.cs`):**
  ```csharp
  public class ForbiddenAccessException : Exception
  {
      public ForbiddenAccessException(string message) : base(message) { }
  }
  ```
* **Entidades Consolidadas en Tarea 1.2 (`AuditableTenantEntity`):**
  - `ConfiguracionSalon`: incluye `NombreComercial = "Mi Peluquería SaaS"` y `HorarioAtencionTexto = string.Empty`.
  - `AuditLog`: hereda de `AuditableTenantEntity`.
  - `Usuario`: `RolId = 4` (default Cliente) y `[Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();`.
  - `HistorialSesion`, `RefreshToken`: heredan de `AuditableTenantEntity`.
  - `TipoCabello`, `TipoRostro`: heredan de `AuditableTenantEntity`.
  - `Cliente`: hereda de `AuditableTenantEntity` + `[Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();`.
  - `FichaTecnica`: hereda de `AuditableTenantEntity`, define `ClienteId`, `PeluqueroId`, `TurnoId`, `FechaRegistro`, `FormulaAplicada`, `Observaciones`, `NotasSensibilidadAlergias`, `ProductosUtilizados`, `FotoResultadoUrl` (+ puentes `[NotMapped] Fecha` y `DetalleFormulaTintura`).
  - `CategoriaProducto`: hereda de `AuditableTenantEntity` + `string? Descripcion`.
  - `Proveedor`: hereda de `AuditableTenantEntity` + `string? Cuit` y `RowVersion`.
  - `Producto`, `OrdenCompra`: heredan de `AuditableTenantEntity` + `RowVersion` (`DetalleOrdenCompra` también hereda de `AuditableTenantEntity`).
  - `MovimientoStock`: hereda de `AuditableTenantEntity` + `public int SaldoResultante { get; set; }`.
  - `HistorialPrecioProducto`: hereda de `AuditableTenantEntity`.
* **Entidades Consolidadas en Tarea 1.3 (`AuditableTenantEntity`):**
  - `Especialidad` y `CategoriaServicio`: heredan de `AuditableTenantEntity` + `string? Descripcion` (`[StringLength(250)]`).
  - `Peluquero` y `Servicio`: heredan de `AuditableTenantEntity` + `[Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();`.
  - `PeluqueroEspecialidad` y `TurnoServicio`: heredan ahora de `AuditableTenantEntity` (con `Id`, auditoría y `TenantId`/`SucursalId`).
  - `HorarioLaboral`: hereda de `AuditableTenantEntity` + `TimeSpan? HoraInicioDescanso`, `TimeSpan? HoraFinDescanso`, `bool Activo = true`.
  - `BloqueoHorario`, `ServicioInsumoReceta`, `HistorialPrecioServicio`, `MotivoCancelacion`, `HistorialEstadoTurno`: heredan de `AuditableTenantEntity`.
  - `Turno`: hereda de `AuditableTenantEntity` + `bool RequiereSena = false`, `decimal MontoSena = 0m`, `bool SenaPagada = false`, `DateTime? FechaPagoSena`, `string? ReferenciaPagoExterno`, `RowVersion`.
  - `MedioPago`: hereda de `AuditableTenantEntity` + `string? Descripcion`, `bool EsEfectivoFisico = false`.
  - `SesionCaja`: hereda de `AuditableTenantEntity` + `TotalVentasEfectivo`, `TotalVentasDigital`, `TotalIngresosManuales`, `TotalEgresosManuales`, `string? ObservacionesCierre`, `RowVersion`.
  - `MovimientoCaja`, `DetalleVenta`, `ComisionesConfig`, `DetalleLiquidacion`: heredan de `AuditableTenantEntity`.
  - `Venta`: hereda de `AuditableTenantEntity` + `decimal Subtotal`, `decimal DescuentoSenaAplicado`, `string? IdempotencyKey` (`[StringLength(64)]`), `RowVersion`.
  - `Pago`: hereda de `AuditableTenantEntity` + `string? ReferenciaOperacion` (`[StringLength(100)]`).
  - `Liquidacion`: hereda de `AuditableTenantEntity` + `DateTime FechaEmision`, `decimal Bonificaciones`, `decimal Descuentos`, `string? Observaciones`, `int? UsuarioLiquidacionId`, `RowVersion`.
* **Persistencia y Esquema Relacional Consolidado en Tarea 1.4 (`ApplicationDbContext` + 12 `Configurations`):**
  - Constructor: `public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider? tenantProvider = null)` exponiendo `public int CurrentTenantId` y `public int? CurrentSucursalId`.
  - Filtros Globales: 6 entidades globales (`Rol`, `EstadoTurno`, `Tenant`, `Sucursal`, `PlanSuscripcion`, `SuscripcionTenant`) filtradas por `!e.IsDeleted`; 38 entidades operativas (`AuditableTenantEntity`, incluidas `PeluqueroEspecialidad` y `TurnoServicio`) filtradas por `!e.IsDeleted && e.TenantId == CurrentTenantId && (CurrentSucursalId == null || e.SucursalId == null || e.SucursalId == CurrentSucursalId)`.
  - Claves e Índices: `PeluqueroEspecialidad` y `TurnoServicio` usan PK `Id` con índices únicos filtrados `UX_PeluqueroEspecialidades_Activa` y `UX_TurnoServicios_Turno_Servicio`. Todos los catálogos, entidades maestras, `ConfiguracionSalon` (`UX_ConfiguracionSalon_Tenant_Sucursal`), `ComisionesConfig` (`UX_ComisionesConfig_Local_Peluquero_Servicio`) y `Ventas` (`UX_Ventas_Local_IdempotencyKey`) poseen índices únicos compuestos por `(TenantId, SucursalId, ...)` filtrados con `[IsDeleted] = 0`. Eliminadas las 6 FKs sombra históricas (`TipoCabelloId1`, `TipoRostroId1`, `RolId1`, `CategoriaProductoId`, `CategoriaServicioId`, `EstadoTurnoId`).
  - `SaveChangesAsync`: Sella automáticamente `TenantId` y `SucursalId` en `EntityState.Added`, asigna `UpdatedByUserId` / `DeletedByUserId` y registra `AuditLog` Multi-Tenant excluyendo `PasswordHash` y `RowVersion`.
  - `HasData`: Semilla determinista con `seedDate = 2026-01-01T00:00:00Z`, hash BCrypt estático verificado para `admin@tesis.com` / `"Admin123!"` y catálogos/maestros completos.
* **Contratos DTO Consolidados en Tarea 2.1 (`Common`, `Seguridad`, `Clientes`, `SaaS`):**
  - `ApiResponse<T>` (`DTOs/Common/ApiResponse.cs`): `Success`, `Message`, `Data`, `Errors`, `FieldErrors` (`Dictionary<string, string[]>?`), `Warnings` (`List<string>?`) + métodos de fábrica `Ok`, `Fail`, `Exito`, `Falla`, `SuccessResponse`, `ErrorResponse`.
  - `PaginationFilterDto` (`DTOs/Common/PaginationFilterDto.cs`): `PageNumber = 1`, `PageSize` (default `15`, max `100`), `Search`, `SortBy`, `SortDescending = true`, `DateTime? FechaDesde`, `DateTime? FechaHasta`.
  - `CatalogoItemDto`, `CrearCatalogoItemDto`, `ActualizarCatalogoItemDto` (`DTOs/Common/CatalogoItemDto.cs`): incluyen `bool? EsEfectivoFisico`.
  - `ConfiguracionSalonDto` y `ActualizarConfiguracionSalonDto` (`DTOs/Common/ConfiguracionSalonDto.cs`): incluyen `Id`, `NombreSalon`, `NombreComercial`, `RazonSocial`, `Cuit`, `Direccion`, `Telefono`, `EmailContacto`, `MonedaIso`, `Moneda`, `TimeZoneId`, `HorarioAtencionTexto`, `IntervaloTurnosMinutos`, `MinutosToleranciaEspera`, `HorasAnticipacionCancelacion`, `PorcentajeSenaReserva`, `NotificacionesWhatsAppActivas`, `NotificacionesEmailActivas`.
  - `DTOs/Seguridad/SeguridadDto.cs`: `RegistroUsuarioDto` (`RolId = 4` por defecto), `RefreshTokenRequestDto` (`Token`, `RefreshToken`), `CrearUsuarioStaffDto` (`Username`, `Email`, `Password`, `RolId`, `PeluqueroId`), `CambiarRolUsuarioDto` (`RolId` y alias `NuevoRolId`).
  - `CrearFichaTecnicaDto`, `ActualizarFichaTecnicaDto`, `FichaTecnicaResponseDto` (`DTOs/Clientes/`): incluyen `public string? NotasSensibilidadAlergias { get; set; }`.
  - `PlanSuscripcionResponseDto` (`DTOs/SaaS/SaaSDto.cs`): `Id`, `Nombre`, `PrecioMensualUsd`, `MaxSucursales`, `MaxPeluqueros`, `IncluyeWhatsAppRecordatorios`, `IncluyePortalReservasOnline`, `Activo`.
* **Contratos DTO, Validadores y AutoMapper Consolidados en Tarea 2.2 (`Turnos`, `Stock`, `Ventas`, `Validators`, `MappingProfile`):**
  - `TurnoResponseDto` y `TurnoServicioDetalleDto` (`DTOs/Turnos/TurnoResponseDto.cs`): incluye `Id`, `ClienteId`, `ClienteNombre`, `ClienteTelefono`, `PeluqueroId`, `PeluqueroNombre`, `ColorAgendaHex`, `FechaHoraInicio`, `FechaHoraFin`, `DuracionTotalMinutos`, `EstadoId`, `EstadoNombre`, `MotivoCancelacionId`, `MotivoCancelacionDescripcion`, `MontoTotalEstimado`, `RequiereSena`, `MontoSena`, `SenaPagada`, `FechaPagoSena`, `ReferenciaPagoExterno`, `SaldoPendienteCobro`, `Observaciones`, `List<int> ServiciosIds`, `List<string> Servicios`, `List<TurnoServicioDetalleDto> ServiciosDetalle` (`ServicioId`, `ServicioNombre`, `PrecioHistorico`, `DuracionMinutosHistorica`).
  - `DisponibilidadHorarioResponseDto` (`DTOs/Turnos/DisponibilidadHorarioResponseDto.cs`): incluye `PeluqueroId`, `PeluqueroNombre`, `Fecha`, `IntervaloMinutos = 30`, `DuracionTotalMinutos`, `List<string> HorariosDisponibles` y `List<SlotHorarioDisponibleDto> Slots`.
  - `BloqueoHorarioResponseDto` (`DTOs/Turnos/BloqueoHorarioDto.cs`): `Id`, `PeluqueroId`, `PeluqueroNombre`, `FechaInicio`, `FechaFin`, `Motivo`.
  - `ProveedorDto`, `CrearProveedorDto`, `ActualizarProveedorDto` (`DTOs/Stock/`): incluyen `public string? Cuit { get; set; }`.
  - `SesionCajaResponseDto` (`DTOs/Ventas/SesionCajaResponseDto.cs`) y `MovimientoCajaResponseDto` (`DTOs/Ventas/MovimientoCajaDto.cs`): `SesionCajaResponseDto` expone `Id`, `UsuarioAperturaId`, `UsuarioAperturaNombre`, `FechaApertura`, `MontoInicial`, `UsuarioCierreId`, `UsuarioCierreNombre`, `FechaCierre`, `TotalVentasEfectivo`, `TotalVentasDigital`, `TotalIngresosManuales`, `TotalEgresosManuales`, `MontoFinalEsperado`, `MontoFinalReal`, `Diferencia`, `ObservacionesCierre`, `EstadoAbierta`, `List<MovimientoCajaResponseDto> Movimientos`.
  - `CierreCajaDto` (`DTOs/Ventas/CierreCajaDto.cs`): agrega `string? ObservacionesCierre`.
  - `CrearVentaDto` (`DTOs/Ventas/CrearVentaDto.cs`): agrega `string? IdempotencyKey`.
  - `PagoRequestDto` y `VentaPagoItemResponseDto` (`DTOs/Ventas/`): agregan `string? ReferenciaOperacion`. `VentaResponseDto` agrega `decimal Subtotal`, `decimal DescuentoSenaAplicado` y `string? IdempotencyKey`.
  - `LiquidacionResponseDto` y `DetalleLiquidacionItemDto` (`DTOs/Ventas/LiquidacionResponseDto.cs`): agregan `DateTime FechaEmision`, `decimal Bonificaciones`, `decimal Descuentos`, `string? Observaciones` y `string ConceptoNombre`.
  - `Validators/` y `Mappings/MappingProfile.cs`: corregido `CrearProductoDtoValidator.cs` (reemplazando el duplicado erróneo de `MovimientoStockDto`), agregados validadores para todos los DTOs de entrada nuevos (`ActualizarCatalogoItemDtoValidator`, `ConfiguracionSalonDtoValidator`, `RefreshTokenRequestDtoValidator`, `CrearUsuarioStaffDtoValidator`, `CambiarRolUsuarioDtoValidator`, `ActualizarFichaTecnicaDtoValidator`, `ActualizarPeluqueroDtoValidator`, `ActualizarServicioDtoValidator`, `ReprogramarTurnoDtoValidator`, `ReservaPublicaRequestDtoValidator`, `ConfigurarRecetaServicioDtoValidator`) y registrados todos los perfiles en `MappingProfile.cs`.
* **Firma Transaccional (`Repositories/Interfaces/IUnitOfWork.cs`):**
  ```csharp
  public interface IUnitOfWork : IDisposable
  {
      IClienteRepository Clientes { get; }
      IFichaTecnicaRepository FichasTecnicas { get; }
      IPeluqueroRepository Peluqueros { get; }
      IServicioRepository Servicios { get; }
      IRecetaServicioRepository Recetas { get; }
      IBloqueoHorarioRepository BloqueosHorarios { get; }
      ITurnoRepository Turnos { get; }
      IProductoRepository Productos { get; }
      IProveedorRepository Proveedores { get; }
      IOrdenCompraRepository OrdenesCompra { get; }
      ISesionCajaRepository SesionesCaja { get; }
      IVentaRepository Ventas { get; }
      IComisionConfigRepository ComisionesConfig { get; }
      ILiquidacionRepository Liquidaciones { get; }
      IUsuarioRepository Usuarios { get; }
      ICatalogoRepository Catalogos { get; }
      IConfiguracionSalonRepository ConfiguracionSalon { get; }
      IDashboardRepository Dashboard { get; }
      ISaaSRepository SaaS { get; }

      Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
      Task<int> CommitAsync(CancellationToken cancellationToken = default);

      Task ExecuteInTransactionAsync(
          Func<CancellationToken, Task> operation,
          IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
          CancellationToken ct = default);
  }
  ```
* **Firma Batch Anti N+1 (`Repositories/Interfaces/IGenericRepository.cs`):**
  ```csharp
  public interface IGenericRepository<T> where T : AuditableEntity
  {
      Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
      Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
      Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
      Task<Dictionary<int, T>> GetByIdsDictionaryAsync(IEnumerable<int> ids, bool tracking = false, CancellationToken ct = default);
      Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
      Task AddAsync(T entity, CancellationToken ct = default);
      void Update(T entity);
      void Delete(T entity);
      IQueryable<T> AsQueryable();
  }
  ```
* **Nuevos Repositorios Consolidados en Tarea 3.1 (`Recetas` y `SaaS`):**
  - `IRecetaServicioRepository : IGenericRepository<ServicioInsumoReceta>` (`GetByServicioIdWithDetailsAsync`, `GetByServiciosIdsAsync`, `AddRangeAsync`, `ExisteConsumoGabinetePorTurnoAsync`).
  - `ISaaSRepository : IGenericRepository<Sucursal>` (`GetTenantByIdAsync`, `GetTenantBySubdominioAsync`, `GetSucursalesByTenantAsync`, `GetSucursalByIdAsync`, `CountSucursalesActivasByTenantAsync`, `EsSucursalValidaYActivaAsync`, `AddSucursalAsync`, `UpdateSucursal`, `GetPlanesActivosAsync`, `GetPlanByIdAsync`, `GetSuscripcionActualByTenantAsync`, `AddSuscripcionTenantAsync`, `UpdateSuscripcionTenant`).
* **Firmas Consolidadas de los 17 Repositorios Refactorizados en Tarea 3.2 (Todos heredando de `IGenericRepository<T>`):**
  - `IUsuarioRepository : IGenericRepository<Usuario>`: `GetByIdWithRolAsync`, `GetByUsernameOrEmailAsync`, `GetByEmailAsync`, `GetAllWithRolesAsync`, `ExistsUsernameOrEmailAsync`, `GetRolByIdAsync`, `GetRolByNombreAsync`, `RegistrarHistorialSesionAsync`, `GetHistorialSesionesAsync`, `GetHistorialSesionesPagedAsync(PaginationFilterDto filter, int? usuarioId = null, CancellationToken ct = default)`, `AddRefreshTokenAsync`, `GetRefreshTokenAsync`, `UpdateRefreshToken`, `RevocarRefreshTokensUsuarioAsync`.
  - `IClienteRepository : IGenericRepository<Cliente>`: `GetAllQueryable`, `GetPagedAsync(PaginationFilterDto filter, CancellationToken ct = default)`, `GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken ct = default)`, `ExistsEmailAsync(string email, int? excludeId = null, CancellationToken ct = default)`, `UpdateAsync`, `DeleteAsync(int id, CancellationToken ct = default)`.
  - `IFichaTecnicaRepository : IGenericRepository<FichaTecnica>`: `GetByIdWithDetailsAsync`, `GetByClienteIdAsync` (ordenado por `FechaRegistro` descendente).
  - `ICatalogoRepository : IGenericRepository<Especialidad>`: `GetTiposCabelloAsync`, `GetTiposRostroAsync`, `GetCategoriasProductoAsync`, `GetCategoriasServicioAsync`, `GetEspecialidadesAsync`, `GetEstadosTurnoAsync`, `GetMotivosCancelacionAsync`, `GetMediosPagoAsync`, `GetRolesAsync`, `GetTipoCabelloByIdAsync`, `GetTipoRostroByIdAsync`, `GetCategoriaProductoByIdAsync`, `GetCategoriaServicioByIdAsync`, `GetEspecialidadByIdAsync`, `GetMotivoCancelacionByIdAsync`, `GetMedioPagoByIdAsync`, `GetCatalogEntityByIdAsync<TEntity>`, `AddEntityAsync<TEntity>`, `UpdateEntity<TEntity>`, `DeleteEntity<TEntity>`.
  - `IConfiguracionSalonRepository : IGenericRepository<ConfiguracionSalon>`: `GetConfiguracionActualAsync`.
  - `IPeluqueroRepository : IGenericRepository<Peluquero>`: `GetAllWithDetailsAsync(bool soloActivos = false, CancellationToken ct = default)`, `GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken ct = default)`, `ExistsEmailAsync(string email, int? excludeId = null, CancellationToken ct = default)`, `CountActivosAsync`, `ReemplazarEspecialidadesAsync`, `GetHorariosByPeluqueroIdAsync`, `GetHorarioDiaAsync`, `ReemplazarHorariosLaboralesAsync`, `TieneTurnosFuturosActivosAsync`, `TieneLiquidacionesPendientesAsync`, `UpdateAsync`, `DeleteAsync(int id, CancellationToken ct = default)`.
  - `IServicioRepository : IGenericRepository<Servicio>`: `GetAllWithCategoriaAsync(bool soloActivos = false, CancellationToken ct = default)`, `GetByIdWithCategoriaAsync(int id, bool tracking = false, CancellationToken ct = default)`, `GetActivosByIdsAsync`, `AddHistorialPrecioAsync(HistorialPrecioServicio historial, CancellationToken ct = default)`, `GetHistorialPreciosAsync`, `TieneTurnosFuturosActivosAsync`, `UpdateAsync`, `DeleteAsync(int id, CancellationToken ct = default)`.
  - `IBloqueoHorarioRepository : IGenericRepository<BloqueoHorario>`: `GetByPeluqueroIdAsync`, `ExisteSolapamientoAsync`.
  - `ITurnoRepository : IGenericRepository<Turno>`: `GetTurnosPeluqueroEnFechaAsync`, `GetTurnosActivosEnRangoAsync`, `ExisteSolapamientoAsync`, `GetByIdConDetallesAsync(int id, bool tracking = true, CancellationToken ct = default)`, `GetPagedAsync(PaginationFilterDto filter, DateTime? fecha = null, int? peluqueroId = null, int? estadoId = null, int? clienteId = null, CancellationToken ct = default)`, `GetByClienteIdAsync`, `AddTurnoServicioAsync`, `AddTurnoServiciosRangeAsync`, `ReemplazarTurnoServiciosAsync`, `AddHistorialEstadoAsync`, `GetHistorialEstadosByTurnoIdAsync`, `ExisteConsumoGabinetePorTurnoAsync`, `UpdateAsync`.
  - `IProductoRepository : IGenericRepository<Producto>`: `GetAllWithDetailsAsync(bool soloBajoStock = false, CancellationToken ct = default)`, `GetPagedAsync(PaginationFilterDto filter, bool soloBajoStock = false, int? categoriaId = null, int? proveedorId = null, CancellationToken ct = default)`, `GetStockCriticoAsync`, `GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken ct = default)`, `ExistsCodigoBarraAsync(string codigoBarra, int? excludeId = null, CancellationToken ct = default)`, `RegistrarMovimientoAsync`, `GetKardexPagedAsync(int productoId, PaginationFilterDto filter, CancellationToken ct = default)`, `GetKardexByProductoIdAsync`, `AddHistorialPrecioAsync(HistorialPrecioProducto historial, CancellationToken ct = default)`, `GetHistorialPreciosAsync`, `ExisteConsumoGabinetePorTurnoAsync`, `UpdateAsync`, `DeleteAsync(int id, CancellationToken ct = default)`.
  - `IProveedorRepository : IGenericRepository<Proveedor>`: `GetPagedAsync(PaginationFilterDto filter, CancellationToken ct = default)`, `ExistsByRazonSocialAsync(string razonSocial, int? excludeId = null, CancellationToken ct = default)`, `ExistsByCuitAsync(string cuit, int? excludeId = null, CancellationToken ct = default)`, `GetCantidadProductosPorProveedorAsync(IEnumerable<int>? proveedorIds = null, CancellationToken ct = default)`, `CountProductosAsociadosAsync`, `TieneProductosAsociadosAsync`.
  - `IOrdenCompraRepository : IGenericRepository<OrdenCompra>`: `GetAllAsync(int? proveedorId, EstadoOrdenCompraEnum? estado = null, CancellationToken ct = default)`, `GetPagedAsync(PaginationFilterDto filter, int? proveedorId = null, EstadoOrdenCompraEnum? estado = null, CancellationToken ct = default)`, `GetByIdCompletaAsync`.
  - `ISesionCajaRepository : IGenericRepository<SesionCaja>`: `GetCajaAbiertaActualAsync`, `GetByIdWithMovimientosYVentasAsync`, `GetHistorialCajasAsync` (sin over-fetching de `MovimientosCaja`), `GetHistorialCajasPagedAsync(PaginationFilterDto filter, CancellationToken ct = default)`, `AbrirSesionCajaAsync`, `ActualizarSesionCaja`, `RegistrarMovimientoCajaAsync`, `GetMovimientosBySesionIdAsync`.
  - `IVentaRepository : IGenericRepository<Venta>`: `GetByIdCompletaAsync`, `GetByIdempotencyKeyAsync`, `ExisteVentaActivaConSenaPorTurnoAsync`, `TieneRenglonesEnLiquidacionActivaAsync`, `GetBySesionCajaIdAsync`, `GetBySesionCajaIdPagedAsync(int sesionCajaId, PaginationFilterDto filter, CancellationToken cancellationToken = default)`, `GetVentasParaLiquidacionAsync`, `GetDatosServicioAsync` (0 Reflection), `GetNombrePeluqueroAsync` (0 Reflection).
  - `IComisionConfigRepository : IGenericRepository<ComisionesConfig>`: `GetAllWithDetailsAsync`, `GetByPeluqueroIdWithDetailsAsync`, `GetByIdWithDetailsAsync`, `GetExactaByPeluqueroYServicioAsync`, `GetVigenteByPeluqueroYServicioAsync` (prioriza `ServicioId == servicioId` con fallback a `ServicioId == null`).
  - `ILiquidacionRepository : IGenericRepository<Liquidacion>`: `GetByIdWithDetallesAsync(int id, bool tracking = true, CancellationToken cancellationToken = default)`, `GetAllWithPeluqueroAsync`, `GetPagedAsync(PaginationFilterDto filter, int? peluqueroId = null, EstadoLiquidacionEnum? estado = null, CancellationToken ct = default)`, `GetByPeluqueroIdAsync`, `ExisteLiquidacionEnPeriodoAsync`, `GetDetallesVentaPendientesLiquidacionAsync`, `GetDetallesVentaByIdsDictionaryAsync`, `AnularDetallesLiquidacion`.
  - `IDashboardRepository : IGenericRepository<Venta>`: `GetVentasEnRangoAsync`, `GetTurnosEnRangoAsync`, `CountTurnosPendientesEnRangoAsync`, `CountNuevosClientesEnRangoAsync`, `CountProductosStockCriticoAsync`.
* **Servicios Consolidados en Tarea 4.1 (`Seguridad`, `Usuarios`, `Catálogos`, `Configuración`, `SaaS` y `Clientes`):**
  - `IAuthService`: `LoginAsync`, `RefreshTokenAsync(RefreshTokenRequestDto dto, string ip = "127.0.0.1", CancellationToken ct = default)`, `RegistrarUsuarioAsync` (`RolId = 4` fijo + tolerancia a fallos SMTP con `Warnings`), `VerificarEmailAsync`, `CambiarPasswordAsync`, `SolicitarRecuperacionPasswordAsync` (tolerancia a fallos SMTP con `Warnings`), `RestablecerPasswordConOtpAsync`, `ObtenerUsuariosAsync`, `ActualizarEstadoUsuarioAsync`, `ObtenerHistorialSesionesAsync`.
  - `IUsuarioService`: `ObtenerTodosAsync`, `ObtenerPorIdAsync`, `CrearUsuarioStaffAsync(CrearUsuarioStaffDto dto, CancellationToken cancellationToken = default)`, `ActualizarEstadoAsync` (revoca `RefreshTokens` al desactivar), `DesbloquearUsuarioAsync`, `CambiarRolAsync`, `ObtenerHistorialSesionesAsync(PaginationFilterDto filter, int? usuarioId = null, CancellationToken cancellationToken = default)` retornando `ApiResponse<PagedResult<HistorialSesionResponseDto>>`.
  - `ICatalogoService`: `ObtenerTodosLosCatalogosAsync` (caché Multi-Tenant `$"SaaS_Catalogos_T{tenantId}_S{sucursalId}"`) y CRUD tipado completo (`Obtener*Async`, `Crear*Async(CrearCatalogoItemDto)`, `Actualizar*Async(int id, ActualizarCatalogoItemDto)`, `Eliminar*Async(int id)`) para los 7 catálogos del salón (`TiposCabello`, `TiposRostro`, `CategoriasServicio`, `Especialidades`, `MotivosCancelacion`, `CategoriasProducto`, `MediosPago` con `EsEfectivoFisico`), más `ObtenerEstadosTurnoAsync` y `ObtenerRolesAsync` (`0 DbContext`, `0 Reflection`).
  - `IConfiguracionSalonService`: `ObtenerConfiguracionAsync`, `ActualizarConfiguracionAsync(ConfiguracionSalonDto, CancellationToken)` y `ActualizarConfiguracionAsync(ActualizarConfiguracionSalonDto, CancellationToken)` con caché Multi-Tenant `$"SaaS_Config_T{tenantId}_S{sucursalId}"`.
  - `ISaaSService`: `ObtenerSucursalesAsync`, `CrearSucursalAsync` (valida tope anti-abuso de 20 sucursales e invalida caché `$"SaaS_Valid_T{tenantId}_S{nueva.Id}"`), `ObtenerPlanesActivosAsync`, `ObtenerMiSuscripcionAsync`, `CambiarPlanAsync`.
  - `IClienteService`: `ObtenerTodosAsync(PaginationFilterDto filter, CancellationToken ct = default)`, `ObtenerTodosAsync(int pageNumber = 1, int pageSize = 15, string? search = null)`, `ObtenerPorIdAsync(int id, CancellationToken ct = default)`, `CrearClienteAsync(ClienteRequestDto dto, CancellationToken ct = default)`, `ActualizarClienteAsync(int id, ClienteRequestDto dto, CancellationToken ct = default)`, `EliminarClienteAsync(int id, CancellationToken ct = default)`.
  - `IFichaTecnicaService`: `ObtenerPorIdAsync(int id, CancellationToken ct = default)`, `ObtenerHistorialClienteAsync(int clienteId, CancellationToken ct = default)`, `CrearFichaAsync(CrearFichaTecnicaDto dto, CancellationToken ct = default)`, `ActualizarFichaAsync(int id, ActualizarFichaTecnicaDto dto, CancellationToken ct = default)`, `ActualizarFichaAsync(int id, CrearFichaTecnicaDto dto)`, `EliminarFichaAsync(int id, CancellationToken ct = default)` (`0 DbContext`, `0 Reflection`).
* **Servicios Consolidados en Tarea 4.2 (`Peluqueros`, `Servicios`, `Recetas`, `Bloqueos` y `Motor de Turnos`):**
  - `IPeluqueroService`: `GetAllAsync(bool soloActivos = false, CancellationToken ct = default)`, `GetByIdAsync(int id, CancellationToken ct = default)`, `CreateAsync(CrearPeluqueroDto dto, CancellationToken ct = default)`, `UpdateAsync(int id, ActualizarPeluqueroDto dto, CancellationToken ct = default)`, `UpdateAsync(int id, CrearPeluqueroDto dto)`, `DeleteAsync(int id, CancellationToken ct = default)`, `ObtenerHorariosAsync(int peluqueroId, CancellationToken ct = default)`, `ConfigurarHorariosAsync(int peluqueroId, GuardarHorariosPeluqueroDto dto, CancellationToken ct = default)`.
  - `IServicioService`: `GetAllAsync(bool soloActivos = false, CancellationToken ct = default)`, `GetByIdAsync(int id, CancellationToken ct = default)`, `CreateAsync(CrearServicioDto dto, CancellationToken ct = default)`, `UpdateAsync(int id, ActualizarServicioDto dto, int? usuarioModificacionId = null, CancellationToken ct = default)`, `UpdateAsync(int id, CrearServicioDto dto)`, `DeleteAsync(int id, CancellationToken ct = default)`.
  - `IRecetaServicioService`: `ObtenerRecetaPorServicioAsync(int servicioId, CancellationToken ct = default)`, `GuardarRecetaServicioAsync(ConfigurarRecetaServicioDto dto, int usuarioId, CancellationToken ct = default)`, `AplicarConsumoGabinetePorTurnoAsync(int turnoId, int usuarioId, CancellationToken ct = default)`.
  - `IBloqueoHorarioService`: `ObtenerPorPeluqueroAsync(int peluqueroId, CancellationToken ct = default)`, `CrearBloqueoAsync(BloqueoHorarioDto dto, CancellationToken ct = default)` (retorna `ApiResponse<BloqueoHorarioResponseDto>` con `Warnings`), `EliminarBloqueoAsync(int id, CancellationToken ct = default)`.
  - `ITurnoService` (`Services/Interfaces/ITurnoService.cs`):
    ```csharp
    public interface ITurnoService
    {
        Task<ApiResponse<PagedResult<TurnoResponseDto>>> GetAllAsync(
            PaginationFilterDto filter,
            DateTime? fecha = null,
            int? peluqueroId = null,
            int? estadoId = null,
            int? clienteId = null,
            CancellationToken ct = default);

        Task<ApiResponse<PagedResult<TurnoResponseDto>>> GetAllAsync(
            DateTime? fecha = null,
            int? peluqueroId = null,
            CancellationToken ct = default);

        Task<ApiResponse<TurnoResponseDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ApiResponse<TurnoResponseDto>> CreateAsync(CrearTurnoDto dto, int usuarioId = 1, CancellationToken ct = default);
        Task<ApiResponse<TurnoResponseDto>> ReprogramarAsync(int id, ReprogramarTurnoDto dto, int usuarioId = 1, string? rolEjecutor = null, CancellationToken ct = default);
        Task<ApiResponse<bool>> CambiarEstadoAsync(int id, CambiarEstadoTurnoDto dto, int usuarioId = 1, string? rolEjecutor = null, CancellationToken ct = default);
        Task<ApiResponse<bool>> ProcesarPagoSenaWebhookAsync(int turnoId, WebhookPagoNotificacionDto payload, string proveedor = "MercadoPago", CancellationToken ct = default);
        Task<ApiResponse<DisponibilidadHorarioResponseDto>> ObtenerHorariosDisponiblesAsync(int peluqueroId, DateTime fecha, List<int>? serviciosIds = null, CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<TurnoResponseDto>>> ObtenerTurnosPorClienteAsync(int clienteId, CancellationToken ct = default);
        Task<ApiResponse<IReadOnlyList<HistorialEstadoTurnoResponseDto>>> ObtenerHistorialEstadoTurnoAsync(int turnoId, CancellationToken ct = default);
    }
    ```
* **Servicios Consolidados en Tarea 4.3 (`Stock`, `Proveedores` y `Órdenes de Compra`):**
  - `IProductoService`: `GetAllAsync(PaginationFilterDto filter, bool soloBajoStock = false, int? categoriaId = null, int? proveedorId = null, CancellationToken ct = default)`, `GetAllAsync(bool soloBajoStock = false, CancellationToken ct = default)` (retornando `ApiResponse<PagedResult<ProductoDto>>`), `GetStockCriticoAsync(CancellationToken ct = default)`, `GetByIdAsync(int id, CancellationToken ct = default)`, `CreateAsync(CrearProductoDto dto, int usuarioId = 1, CancellationToken ct = default)`, `UpdateAsync(int id, ActualizarProductoDto dto, int? usuarioModificacionId = null, CancellationToken ct = default)`, `UpdateAsync(int id, CrearProductoDto dto)`, `DeleteAsync(int id, CancellationToken ct = default)`, `RegistrarMovimientoStockAsync(MovimientoStockDto dto, int usuarioId = 1, CancellationToken ct = default)`, `ObtenerKardexProductoAsync(int productoId, PaginationFilterDto filter, CancellationToken ct = default)` y `ObtenerKardexProductoAsync(int productoId, CancellationToken ct = default)` (retornando `ApiResponse<PagedResult<KardexItemResponseDto>>`).
  - `IProveedorService`: `GetPagedAsync(PaginationFilterDto filter, CancellationToken ct = default)` (retornando `ApiResponse<PagedResult<ProveedorDto>>`), `GetAllAsync(CancellationToken ct = default)` (conteo batch `GROUP BY` anti N+1), `GetByIdAsync(int id, CancellationToken ct = default)`, `CreateAsync(CrearProveedorDto dto, CancellationToken ct = default)`, `UpdateAsync(int id, ActualizarProveedorDto dto, CancellationToken ct = default)`, `UpdateAsync(int id, CrearProveedorDto dto)`, `DeleteAsync(int id, CancellationToken ct = default)`.
  - `IOrdenCompraService`: `ObtenerTodasAsync(PaginationFilterDto filter, int? proveedorId = null, EstadoOrdenCompraEnum? estado = null, CancellationToken ct = default)` y `ObtenerTodasAsync(int? proveedorId = null, EstadoOrdenCompraEnum? estado = null, CancellationToken ct = default)` (retornando `ApiResponse<PagedResult<OrdenCompraResponseDto>>`), `ObtenerPorIdAsync(int id, CancellationToken ct = default)`, `CrearOrdenAsync(CrearOrdenCompraDto dto, int usuarioId, CancellationToken ct = default)`, `RecepcionarOrdenAsync(int id, RecepcionarOrdenCompraDto dto, int usuarioId, CancellationToken ct = default)`, `CancelarOrdenAsync(int id, string motivo, int usuarioId, CancellationToken ct = default)`.
* **Servicios Consolidados en Tarea 4.4 (`Caja`, `POS Idempotente`, `Comisiones`, `Liquidaciones` y `Dashboard` - `[HARD GATE 2]`):**
  - `ICajaService`: `ObtenerEstadoCajaActualAsync(CancellationToken ct = default)` (`ApiResponse<SesionCajaResponseDto?>` con `Movimientos`), `ObtenerPorIdAsync(int id, CancellationToken ct = default)` (`ApiResponse<SesionCajaResponseDto>` con `Movimientos`), `ObtenerHistorialCajasAsync(PaginationFilterDto filter, CancellationToken ct = default)` y sobrecarga `(DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default)` (`ApiResponse<PagedResult<SesionCajaResponseDto>>` sin *Over-fetching* de `Movimientos`), `AbrirCajaAsync(AperturaCajaDto dto, int? usuarioEjecutorId = null, CancellationToken ct = default)`, `RegistrarIngresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "IngresoManual", int? usuarioEjecutorId = null, CancellationToken ct = default)`, `RegistrarEgresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "EgresoMenor", int? usuarioEjecutorId = null, CancellationToken ct = default)`, `CerrarCajaAsync(CierreCajaDto dto, int? usuarioEjecutorId = null, CancellationToken ct = default)`.
  - `IVentaService`: `ObtenerVentaPorIdAsync(int id, CancellationToken ct = default)`, `ObtenerVentasPorSesionAsync(int sesionCajaId, PaginationFilterDto filter, CancellationToken ct = default)` y sobrecarga `(int sesionCajaId, CancellationToken ct = default)` (`ApiResponse<PagedResult<VentaResponseDto>>` con mapeo batch $O(1)$ anti N+1), `RegistrarVentaAsync(CrearVentaDto dto, int usuarioId, CancellationToken ct = default)` (idempotencia por `IdempotencyKey`, precarga batch `GetByIdsDictionaryAsync`, herencia de `turno.PeluqueroId`, descuento `DescuentoSenaAplicado`, separación efectivo/digital, autocompletado del turno a `Completado` y consumo de gabinete en `ExecuteInTransactionAsync`), `AnularVentaAsync(int ventaId, string motivo, int usuarioId, CancellationToken ct = default)` (bloqueo ante caja cerrada o liquidación activa).
  - `IComisionConfigService`: `ObtenerTodasAsync(CancellationToken ct = default)`, `ObtenerPorPeluqueroAsync(int peluqueroId, CancellationToken ct = default)`, `GuardarConfiguracionAsync(GuardarComisionConfigDto dto, CancellationToken ct = default)`, `ActualizarConfiguracionAsync(int id, GuardarComisionConfigDto dto, CancellationToken ct = default)`, `EliminarConfiguracionAsync(int id, CancellationToken ct = default)`.
  - `ILiquidacionService`: `LiquidarPeriodoAsync(GenerarLiquidacionDto dto, int? usuarioLiquidacionId = null, CancellationToken ct = default)`, `RegistrarPagoLiquidacionAsync(int liquidacionId, int? sesionCajaId = null, int? usuarioId = null, CancellationToken ct = default)`, `AnularLiquidacionAsync(int liquidacionId, string? motivo = null, int? usuarioId = null, CancellationToken ct = default)`, `ObtenerPorPeluqueroAsync(int peluqueroId, CancellationToken ct = default)`, `ObtenerTodasAsync(PaginationFilterDto filter, int? peluqueroId = null, EstadoLiquidacionEnum? estado = null, CancellationToken ct = default)` y sobrecarga `(int? peluqueroId = null, DateTime? desde = null, DateTime? hasta = null, CancellationToken ct = default)` (`ApiResponse<PagedResult<LiquidacionResponseDto>>`), `ObtenerPorIdAsync(int id, CancellationToken ct = default)` (poblando `ConceptoNombre` en lote).
  - `IDashboardService`: `ObtenerMetricasMesActualAsync(CancellationToken ct = default)`, `ObtenerKpisAsync(DateTime? fechaDesde = null, DateTime? fechaHasta = null, CancellationToken ct = default)` (`0 DbContext`, operando vía `IUnitOfWork.Dashboard` y `TimeZoneInfo` del salón).
* **Infraestructura Transversal Consolidada en Tarea 5.1 (`Middlewares`, `Integrations`, `Background`, `Program.cs`):**
  - `GlobalExceptionMiddleware`: `NotFoundException`/`KeyNotFoundException` → `404`; `UnauthorizedAppException`/`UnauthorizedAccessException` → `401`; `ForbiddenAccessException` → `403`; `DbUpdateConcurrencyException` → `409` (*"El registro fue modificado simultáneamente por otro usuario. Recargue la pantalla e intente nuevamente."*); `BusinessRuleException`/`InvalidOperationException`/`ArgumentException` → `400`; el resto → `500` con mensaje genérico. Siempre responde `ApiResponse<object>.Fail(message)` en camelCase, sin exponer detalles internos en `Errors`.
  - `TenantResolutionMiddleware.InvokeAsync(HttpContext context, ITenantProvider tenantProvider, IUnitOfWork unitOfWork)`: con usuario autenticado manda el claim `TenantId` (los headers de tenant se ignoran); con usuario anónimo lee el header o la QueryString (`X-Tenant-Id`/`tenantId`, `X-Sucursal-Id`/`sucursalId`) o `?subdominioSlug`; si no hay nada, usa `1/1`. Valida contra la caché `$"SaaS_Valid_T{tenantId}_S{sucursalId}"` usando `IUnitOfWork.SaaS.EsSucursalValidaYActivaAsync` y, si el par no es válido, lanza `ForbiddenAccessException("La sucursal indicada no pertenece al salón actual o se encuentra inactiva.")`.
  - `IPaymentGatewayService.ValidarFirmaWebhook(string proveedor, string? signatureHeader, string rawBody)` (firma sin cambios): usa `HMACSHA256.HashData` + `CryptographicOperations.FixedTimeEquals` con política Fail-Closed (devuelve `false` si falta el secreto, falta la firma o el hex es inválido). Formatos de header aceptados: `<hex>`, `sha256=<hex>`, `ts=..,v1=<hex>` (manifiesto `id:{rawBody};ts:{ts};`) y `t=..,v1=<hex>` (manifiesto `{t}.{rawBody}`).
  - `SmtpEmailService.EnviarEmailAsync`: si `Smtp:Host` está vacío → `LogWarning` + `return false` sin crear `SmtpClient`. `Timeout = Smtp:TimeoutMilliseconds` (máximo y valor por defecto `5000`) + `CancellationTokenSource(timeout)`. Lee también `Port`, `EnableSsl`, `FromEmail` y `FromName`. Cualquier excepción → `LogWarning` + `false`.
  - `RecordatorioTurnosBackgroundService`: recorre las sucursales activas de tenants activos (`IUnitOfWork.SaaS.AsQueryable().IgnoreQueryFilters()`) y crea un scope DI por sucursal con `ITenantProvider.SetTenantContext(tenantId, sucursalId)`. Lee la config vía `IConfiguracionSalonService`, expira señas online a través de `ITurnoService.CambiarEstadoAsync` (con un motivo de cancelación válido del tenant) y calcula los recordatorios de 24h/2h en hora local del salón. Valores por defecto si faltan claves: intervalo `Math.Max(1, valor ?? 15)` min, expiración `Math.Max(1, valor ?? 30)` min.
  - `Program.cs`: registra `IGenericRepository<>` abierto, `IUnitOfWork`, 19 repositorios, `ITenantProvider`, 20 servicios de negocio, 3 integraciones y el worker. `AddRateLimiter` define la política `"StrictAuthOtpPolicy"` (ventana fija por IP; valores de `RateLimiting:StrictAuthOtpPolicy`, por defecto 5/1 min) y `OnRejected` responde `429` con `ApiResponse<object>.Fail`. `InvalidModelStateResponseFactory` responde `400` con `ApiResponse<object>.Fail("Error de validación de datos.", errors, fieldErrors)` y claves en camelCase. Orden del pipeline: `GlobalException → Swagger(dev) → HttpsRedirection → Cors → RateLimiter → Authentication → TenantResolution → Authorization → MapControllers`.
* **Capa de Presentación HTTP Consolidada en Tarea 5.2 (22 Controladores en 21 archivos `.cs` + `MiPeluqueria.Api.http` - `[HARD GATE 3]`):**
  - `AuthController` (`/api/auth`): `[EnableRateLimiting("StrictAuthOtpPolicy")]`, expone `POST login`, `POST refresh-token`, `POST registro`, `POST verificar-email`, `POST olvide-password`, `POST restablecer-password` (`[AllowAnonymous]`) y `POST cambiar-password` (`[Authorize]`).
  - `UsuariosController` (`/api/usuarios`): inyecta `IUsuarioService`, protegido con `[Authorize(Roles = "Administrador,SuperAdminSaaS")]`, expone `GET`, `GET {id:int}`, `POST` (`CrearUsuarioStaffDto`), `PUT {id:int}/estado`, `PATCH {id:int}/rol`, `POST {id:int}/desbloquear` y `GET historial-sesiones` paginado.
  - `CatalogosController` (`/api/catalogos`): `0 DbContext`, inyecta `ICatalogoService`, expone `GET todos`/`GET maestros`, `GET estados-turno`, `GET roles` (`Administrador,SuperAdminSaaS`) y CRUD completo (`GET`, `POST`, `PUT {id:int}`, `DELETE {id:int}`) para los 7 catálogos del salón (`tipos-cabello`, `tipos-rostro`, `categorias-servicio`, `especialidades`, `motivos-cancelacion`, `categorias-producto`, `medios-pago`).
  - `ConfiguracionSalonController` (`/api/configuracion`): `0 DbContext` y `0 RAM estática`, inyecta `IConfiguracionSalonService`, expone `GET` (`[AllowAnonymous]`) y `PUT` (`[Authorize(Roles = "Administrador,SuperAdminSaaS")]`).
  - `SucursalesController` (`/api/sucursales`) y `SuscripcionesController` (`/api/suscripciones`): `0 DbContext`, inyectan `ISaaSService`, protegidos a nivel clase con `[Authorize(Roles = "Administrador,SuperAdminSaaS")]` (`[AllowAnonymous]` en `GET /api/suscripciones/planes` y `[Authorize(Roles = "SuperAdminSaaS")]` exclusivo en `POST /api/suscripciones/cambiar-plan`).
  - `ClientesController` (`/api/clientes`) y `FichasTecnicasController` (`/api/fichas-tecnicas` y `/api/fichastecnicas`): protegidos con `[Authorize(Roles = "Administrador,Recepcionista,Cajero,Peluquero,SuperAdminSaaS")]`, incluyen `GET` paginado en clientes, `GET {id:int}` y `PUT {id:int}` con `ActualizarFichaTecnicaDto` en fichas técnicas.
  - `PeluquerosController` (`/api/peluqueros`), `ServiciosController` (`/api/servicios`), `RecetasServicioController` (`/api/recetas-servicio`) y `BloqueosController` (`/api/bloqueos`): protegidos según Matriz RBAC, usando `ActualizarPeluqueroDto` y `ActualizarServicioDto` en `PUT {id:int}` y extrayendo `UsuarioId` del claim JWT.
  - `TurnosController` (`/api/turnos`): protegido con `[Authorize]`, expone `GET` paginado (`PaginationFilterDto`), `GET {id:int}`, `GET cliente/{clienteId:int}`, `GET disponibilidad`, `GET {id:int}/historial`, `POST`, `PUT {id:int}/reprogramar` (`ReprogramarTurnoDto`) y `PATCH {id:int}/estado`, propagando `UsuarioId` y `Rol` del claim JWT.
  - `PortalReservasController` (`/api/public`) y `WebhooksController` (`/api/webhooks/pagos`): `0 DbContext` y `0 IUnitOfWork`, operando exclusivamente vía `IClienteService`, `ITurnoService`, `IServicioService`, `IPeluqueroService`, `IConfiguracionSalonService` e `IPaymentGatewayService`. Prefija `Observaciones` con `"[Reserva Online]"` en `POST /api/public/reservar` y habilita buffering (`[EnableRequestBuffering]`) en `WebhooksController` para validar `rawBody` con `HMAC-SHA256` e invocar `_turnoService.ProcesarPagoSenaWebhookAsync`.
  - `ProductosController` (`/api/productos`), `ProveedoresController` (`/api/proveedores`) y `OrdenesCompraController` (`/api/ordenes-compra`): protegidos según Matriz RBAC, con `GET` paginados, `GET {id:int}/kardex` paginado y recepción de `ActualizarProductoDto` / `ActualizarProveedorDto`.
  - `CajaController` (`/api/caja`), `VentasController` (`/api/ventas`), `ComisionesController` (`/api/comisiones`), `LiquidacionesController` (`/api/liquidaciones`) y `DashboardController` (`/api/dashboard`): extraen `UsuarioId` exclusivamente del claim JWT `ClaimTypes.NameIdentifier`, incluyen `GET /api/caja/{id:int}`, lectura de header `X-Idempotency-Key` en `POST /api/ventas` y `POST /api/liquidaciones/{id:int}/anular`.
* **Cimientos Frontend Consolidados en Tarea 6.1 (`package.json`, `tsconfig.json`, `.env.local`, `src/app/layout.tsx`, `src/context/AppProviders.tsx`):**
  - `mipeluqueria-saas-web/package.json`: stack comercial Pro instalado (`@tanstack/react-query@^5.59.0`, `react-hook-form@^7.53.0`, `zod@^3.23.8`, `@hookform/resolvers@^3.9.0`, `sonner@^1.5.0`, `date-fns@^4.1.0`, `recharts@^2.13.0`, `lucide-react@^0.454.0`, `clsx@^2.1.1`, `axios@^1.7.7`, `next@14.2.15`).
  - `mipeluqueria-saas-web/.env.local` y `.env.example`: `NEXT_PUBLIC_API_URL=https://localhost:7132/api`, `NEXT_PUBLIC_DEFAULT_TENANT_ID=1`, `NEXT_PUBLIC_DEFAULT_SUCURSAL_ID=1`, `NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME=demo`, `NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET=mipeluqueria_preset`, `NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX`.
  - `mipeluqueria-saas-web/src/context/AppProviders.tsx`: integra `QueryClientProvider` (`staleTime: 60s`, `retry: 1`, `refetchOnWindowFocus: false`), `<Toaster position="top-right" richColors closeButton expand />` (`sonner`), invalidación automática `queryClient.invalidateQueries()` en `switchSucursal`, limpieza `queryClient.clear()` en `logout`, y preserva todos los hooks de contexto existentes (`useAuth`, `useCatalogos`, `useCajaPos`, `useCookieConsent`).
* **Infraestructura Cliente HTTP, Tipos y Servicios API Consolidados en Tareas 6.2 y 6.3 (`src/lib/`, `src/types/`, `src/services/`):**
  - `src/lib/axios.ts`: exporta `apiClient`, `STORAGE_KEYS`, `ApiErrorPayload` y `withIdempotencyKey`, con inyección de `Authorization`, `X-Tenant-Id`, `X-Sucursal-Id`, `X-Idempotency-Key` y cola `isRefreshing` + `failedQueue` contra `POST /api/auth/refresh-token`.
  - `src/lib/cloudinary.ts`: exporta `uploadImageToCloudinary`, `subirImagenCloudinary`, `isCloudinaryConfigured`, `validateImageFile` y `DEFAULT_FICHA_TECNICA_PLACEHOLDER_URL` con fallback defensivo (`toast.warning` en `sonner`).
  - `src/types/` (8 módulos + `api.types.ts` Barrel File): `common.types.ts` (`ApiResponse<T>` con `fieldErrors` y `warnings`, `PagedResult<T>`, `PaginationFilterDto`, `ConfiguracionSalonDto`, `CatalogoItemDto`, `ActualizarCatalogoItemDto`, `TipoCatalogoSalon`), `seguridad.types.ts`, `clientes.types.ts`, `turnos.types.ts`, `public.types.ts`, `stock.types.ts`, `ventas.types.ts` y `saas.types.ts`.
  - `src/services/` (8 módulos + `api.services.ts` Barrel File): `seguridad.service.ts` (`authService`, `usuariosService`), `common.service.ts` (`configuracionSalonService` contra `/api/configuracion`, `catalogosService` con CRUD de los 7 catálogos), `clientes.service.ts` (`clientesService`, `fichasTecnicasService`), `turnos.service.ts` (`turnosService`, `peluquerosService`, `bloqueosService`, `serviciosService`, `recetasServicioService`), `public.service.ts` (`portalPublicoService`), `stock.service.ts` (`productosService`, `proveedoresService`, `ordenesCompraService` contra `/api/ordenes-compra/{id}/recibir`), `ventas.service.ts` (`cajaService`, `ventasService`, `comisionesService`, `liquidacionesService`, `dashboardService`) y `saas.service.ts` (`sucursalesService`, `suscripcionesService`).
* **Esquemas Zod, Hooks TanStack Query v5 y 19 Pantallas Consolidadas en Tarea 6.4 (`src/schemas/`, `src/hooks/`, `src/components/`, `src/app/` - `[HARD GATE 4]`):**
  - `src/schemas/` (`auth.schema.ts`, `clientes.schema.ts`, `turnos.schema.ts`, `stock.schema.ts`, `ventas.schema.ts`, `common.schema.ts`, `index.ts`): espejan las reglas de `FluentValidation` del Backend (`passwordRegex`, teléfonos `7..25`, `duracionMinutos` `5..480`, `porcentajeComision` `0..100`, `precioVenta > precioCosto`, `stockActual` excluido de `actualizarProductoSchema`, validación de `idempotencyKey` UUID y equilibrio financiero $\sum \text{Pagos} = \text{TotalNeto}$ en `crearVentaPosSchema`).
  - `src/hooks/` (`queryKeys.ts`, `useApiFeedback.ts`, `useDomainQueries.ts`, `index.ts`): matriz canónica de `Query Keys` (Sección 2 de `10`), `notifyApiSuccess` / `notifyApiError` / `useApiMutation` con inyección automática de `fieldErrors` en `react-hook-form`, disparo de `toast.warning(w, { duration: 8000 })` por cada elemento en `res.warnings`, e invalidación automática de queries ante `409 Conflict`.
  - `src/app/` (19 pantallas operativas + páginas legales): reconstrucción íntegra de `recepcion/pos/page.tsx` (pre-carga por `?turnoId={id}`, descuento de `senaPagada`, multipago, `idempotencyKey` UUID v4 que se conserva ante fallos de red y rota tras `200 OK`, e impresión térmica en `PrintReceiptModal`) y `recepcion/clientes/page.tsx` (padrón paginado, insignia `esCumpleaneroDelMes` y Fichas Técnicas con subida Direct-to-Cloud a Cloudinary), separación física vs digital en `recepcion/caja/page.tsx`, reprogramación y `colorAgendaHex` en `recepcion/calendario/page.tsx`, gráficos `recharts` en `admin/dashboard/page.tsx`, `cuit` y `saldoResultante` en `admin/inventario/page.tsx`, anulación de liquidaciones pendientes en `admin/staff-liquidaciones/page.tsx`, y CRUD de los 7 catálogos + alta de staff + historial de sesiones paginado en `admin/configuracion-seguridad/page.tsx`.
* **Observabilidad Backend, Health Checks y Telemetría Consolidados en Tarea 7.3 (`CorrelationIdMiddleware`, `TelemetryController`, `Program.cs` - `[HARD GATE 5A]`):**
  - `CorrelationIdMiddleware`: lee/valida `X-Correlation-Id` (`<= 64` chars alfanuméricos/`-`/`_`/`.`, o genera UUID), asigna `HttpContext.TraceIdentifier`, enriquece `LogContext` e `IDiagnosticContext` con `CorrelationId` y sella el header de respuesta `X-Correlation-Id`.
  - `TenantResolutionMiddleware.InvokeAsync(HttpContext context, ITenantProvider tenantProvider, IUnitOfWork unitOfWork, IDiagnosticContext diagnosticContext)`: omite `/health*` y `/swagger*`, y enriquece `LogContext` e `IDiagnosticContext` con `TenantId`, `SucursalId` y `UserId` (Zero-PII).
  - `ClientErrorReportDto` (`DTOs/Common/ClientErrorReportDto.cs`) y `TelemetryController` (`POST /api/telemetry/client-errors`): `[AllowAnonymous]`, protegido con política Rate Limiting `"ClientTelemetryPolicy"` (30 req/min por IP) y `[RequestSizeLimit(4096)]`, redacta automáticamente JWTs, emails y pares clave-valor sensibles antes de emitir `[FRONTEND_ERROR]` / `[FRONTEND_WARN]` en Serilog con `Source = "Frontend"`, retornando `202 Accepted` con `ApiResponse<object>`.
  - `Program.cs`: Bootstrap Logger Serilog con `try / catch (Log.Fatal) / finally (Log.CloseAndFlush)`, `Directory.SetCurrentDirectory(builder.Environment.ContentRootPath)`, `UseSerilogRequestLogging`, `AddHealthChecks().AddDbContextCheck<ApplicationDbContext>("sqlserver_db")`, `MapHealthChecks("/health/live")` (`Predicate = _ => false`) y `MapHealthChecks("/health/ready")`, más `public partial class Program { }` para `WebApplicationFactory<Program>`.

---

## 5. HISTORIAL DE AUDITORÍA PASO A PASO (TASK EXECUTION LOG)

### Plantilla de Registro por Tarea (El Agente debe agregar un bloque por cada tarea terminada):
```text
### [TAREA X.X] - Nombre de la Tarea
- Estado: COMPLETADA / EN REVISIÓN
- Archivos Creados: [lista]
- Archivos Modificados: [lista]
- Archivos Eliminados: [lista]
- Cambios o Decisiones Especiales: [detalle de cualquier ajuste fino]
- Sincronización de MD (Spec-Sync): [indicar si hubo que actualizar algún archivo 01-05]
- Resultado dotnet build: 0 Errores / X Advertencias
```

### [TAREA 1.1] - Jerarquía Base Multi-Tenant y Excepciones de Dominio
- **Estado:** COMPLETADA
- **Archivos Creados:**
  - `Models/Common/ITenantEntity.cs`
  - `Models/Common/AuditableTenantEntity.cs`
  - `Exceptions/ForbiddenAccessException.cs`
- **Archivos Modificados:**
  - `Models/Common/AuditableEntity.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:**
  - `Models/Common/ITentantEntity.cs`
- **Cambios o Decisiones Especiales:**
  - Se eliminó `Models/Common/ITentantEntity.cs` (typo en nombre de archivo) y se creó `Models/Common/ITenantEntity.cs`.
  - Se agregaron las propiedades `public int? UpdatedByUserId { get; set; }` y `public int? DeletedByUserId { get; set; }` en `AuditableEntity`, conservando intacto el comentario al pie del archivo y todas las propiedades previas.
  - Se creó `AuditableTenantEntity` heredando de `AuditableEntity` e implementando `ITenantEntity` con defaults `TenantId = 1` y `SucursalId = 1`.
  - Se creó `ForbiddenAccessException` en `Exceptions/` heredando de `Exception`.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `01_SYSTEM_CONSTRAINTS.md`, `02_ARCHITECTURE.md` y `03_DATA_MODEL.md`).
- **Resultado dotnet build:** 0 Errores / 3 Advertencias (preexistentes en `Data/Context/ApplicationDbContext.cs`, programadas para Tarea 1.4).

### [TAREA 1.2] - Refactorización de Entidades de Common, Seguridad, Clientes y Stock
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `Models/Common/ConfiguracionSalon.cs`
  - `Models/Common/AuditLog.cs`
  - `Models/Seguridad/Usuario.cs`
  - `Models/Seguridad/HistorialSesion.cs`
  - `Models/Seguridad/RefreshToken.cs`
  - `Models/Clientes/TipoCabello.cs`
  - `Models/Clientes/TipoRostro.cs`
  - `Models/Clientes/Cliente.cs`
  - `Models/Clientes/FichaTecnica.cs`
  - `Models/Stock/CategoriaProducto.cs`
  - `Models/Stock/Proveedor.cs`
  - `Models/Stock/Producto.cs`
  - `Models/Stock/MovimientoStock.cs`
  - `Models/Stock/HistorialPrecioProducto.cs`
  - `Models/Stock/OrdenCompra.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - Todas las entidades de `Common`, `Seguridad` (excepto `Rol`), `Clientes` y `Stock` (incluyendo `DetalleOrdenCompra` dentro de `OrdenCompra.cs`) pasaron a heredar de `AuditableTenantEntity`.
  - En `ConfiguracionSalon.cs` se agregaron `NombreComercial` y `HorarioAtencionTexto`.
  - En `Usuario.cs` se fijó `RolId = 4` por defecto y se agregó `[Timestamp] public byte[] RowVersion`.
  - En `Cliente.cs`, `Proveedor.cs`, `Producto.cs` y `OrdenCompra.cs` se agregó `[Timestamp] public byte[] RowVersion`.
  - En `FichaTecnica.cs` se definieron `FechaRegistro`, `FormulaAplicada`, `Observaciones`, `NotasSensibilidadAlergias`, `ProductosUtilizados`, `FotoResultadoUrl` y los puentes `[NotMapped] Fecha` y `DetalleFormulaTintura`.
  - En `CategoriaProducto.cs` se agregó `string? Descripcion` (`[StringLength(250)]`) conforme a `03_DATA_MODEL.md` (Sección 2.6).
  - En `Proveedor.cs` se agregó `string? Cuit` (`[StringLength(20)]`).
  - En `MovimientoStock.cs` se agregó `public int SaldoResultante { get; set; }`.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `03_DATA_MODEL.md`).
- **Resultado dotnet build:** 0 Errores / 3 Advertencias (preexistentes en `Data/Context/ApplicationDbContext.cs`, programadas para Tarea 1.4).

### [TAREA 1.3] - Refactorización de Entidades de Turnos, Agenda y Ventas
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `Models/Turnos/Especialidad.cs`
  - `Models/Turnos/Peluquero.cs`
  - `Models/Turnos/PeluqueroEspecialidad.cs`
  - `Models/Turnos/HorarioLaboral.cs`
  - `Models/Turnos/BloqueoHorario.cs`
  - `Models/Turnos/CategoriaServicio.cs`
  - `Models/Turnos/Servicio.cs`
  - `Models/Turnos/ServicioInsumoReceta.cs`
  - `Models/Turnos/HistorialPrecioServicio.cs`
  - `Models/Turnos/MotivoCancelacion.cs`
  - `Models/Turnos/Turno.cs`
  - `Models/Turnos/TurnoServicio.cs`
  - `Models/Turnos/HistorialEstadoTurno.cs`
  - `Models/Ventas/MedioPago.cs`
  - `Models/Ventas/SesionCaja.cs`
  - `Models/Ventas/MovimientoCaja.cs`
  - `Models/Ventas/Venta.cs`
  - `Models/Ventas/DetalleVenta.cs`
  - `Models/Ventas/Pago.cs`
  - `Models/Ventas/ComisionesConfig.cs`
  - `Models/Ventas/Liquidacion.cs`
  - `Models/Ventas/DetalleLiquidacion.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - Las 13 entidades operativas de `Models/Turnos/` (incluyendo `PeluqueroEspecialidad` y `TurnoServicio`; `EstadoTurno` conserva `AuditableEntity`) y las 9 entidades de `Models/Ventas/` pasaron a heredar de `AuditableTenantEntity`.
  - Se incorporaron todas las propiedades de concurrencia (`RowVersion`), señas (`RequiereSena`, `MontoSena`, `SenaPagada`, `FechaPagoSena`, `ReferenciaPagoExterno`), descansos (`HoraInicioDescanso`, `HoraFinDescanso`, `Activo`), arqueo físico/digital (`EsEfectivoFisico`, `TotalVentasEfectivo`, `TotalVentasDigital`, `TotalIngresosManuales`, `TotalEgresosManuales`, `ObservacionesCierre`), idempotencia POS (`Subtotal`, `DescuentoSenaAplicado`, `IdempotencyKey`, `ReferenciaOperacion`) y liquidaciones (`FechaEmision`, `Bonificaciones`, `Descuentos`, `Observaciones`, `UsuarioLiquidacionId`).
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `03_DATA_MODEL.md`).
- **Resultado dotnet build:** 0 Errores / 3 Advertencias (preexistentes en `Data/Context/ApplicationDbContext.cs`, programadas para Tarea 1.4).

### [TAREA 1.4] - Configuraciones Fluent API, ApplicationDbContext Multi-Tenant y Migración [HARD GATE 1]
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `Data/Configurations/ClientesModuloConfiguration.cs`
  - `Data/Configurations/DetalleVentaConfiguration.cs`
  - `Data/Configurations/OrdenCompraConfiguration.cs`
  - `Data/Configurations/PagoConfiguration.cs`
  - `Data/Configurations/PeluqueroEspecialidadConfiguration.cs`
  - `Data/Configurations/SaasModuloConfiguration.cs`
  - `Data/Configurations/SeguridadyCommonConfiguration.cs`
  - `Data/Configurations/ServicioInsumoRecetaConfiguration.cs`
  - `Data/Configurations/StockModuloConfiguration.cs`
  - `Data/Configurations/TurnoServicioConfiguration.cs`
  - `Data/Configurations/TurnosModuloConfiguration.cs`
  - `Data/Configurations/VentasModuloConfiguration.cs`
  - `Data/Context/ApplicationDbContext.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - Se actualizaron las 12 clases de configuración Fluent API agregando `.IsRowVersion()`, precisión `decimal(18,2)` / `decimal(5,2)` / `decimal(10,2)`, `.Ignore()` para los puentes `[NotMapped]` de `FichaTecnica`, clave primaria `Id` en `PeluqueroEspecialidad` y `TurnoServicio`, e índices únicos compuestos por `(TenantId, SucursalId, ...)` filtrados por `[IsDeleted] = 0` (incluyendo `UX_Ventas_Local_IdempotencyKey`).
  - Se corrigieron además 6 navegaciones inversas (`.WithMany(...)`) que en la migración histórica habían generado columnas sombra (`TipoCabelloId1`, `TipoRostroId1`, `RolId1`, `CategoriaProductoId`, `CategoriaServicioId`, `EstadoTurnoId`).
  - En `ApplicationDbContext.cs` se inyectó `ITenantProvider? tenantProvider = null`, se aplicó el filtro global compuesto Multi-Tenant, se resolvieron las 3 advertencias de nulabilidad `CS8602` previas, se implementó el sellado de `TenantId`/`SucursalId` y `UpdatedByUserId`/`DeletedByUserId` en `SaveChangesAsync`, y se cargó el Data Seeding completo con `seedDate = 2026-01-01T00:00:00Z` y hash BCrypt estático verificado contra `"Admin123!"` (`BCrypt.Verify == true`).
  - Se validó preventivamente el modelo EF Core con `dotnet ef dbcontext info` y generación de script DDL en memoria (`scratch`), confirmando 0 errores de validación y 0 FKs sombra.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `01_SYSTEM_CONSTRAINTS.md`, `03_DATA_MODEL.md` y `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 2.1] - Actualización de DTOs de Common, Seguridad, Clientes y SaaS
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno (nuevas clases DTO definidas dentro de los archivos modulares permitidos)
- **Archivos Modificados:**
  - `DTOs/Common/ApiResponse.cs`
  - `DTOs/Common/PaginationFilterDto.cs`
  - `DTOs/Common/CatalogoItemDto.cs`
  - `DTOs/Common/ConfiguracionSalonDto.cs`
  - `DTOs/Seguridad/SeguridadDto.cs`
  - `DTOs/Clientes/CrearFichaTecnicaDto.cs`
  - `DTOs/Clientes/ActualizarFichaTecnicaDto.cs`
  - `DTOs/Clientes/FichaTecnicaResponseDto.cs`
  - `DTOs/SaaS/SaaSDto.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - En `ApiResponse.cs` se agregaron `FieldErrors` (`Dictionary<string, string[]>?`) y `Warnings` (`List<string>?`), extendiendo `Ok`, `Fail`, `Exito`, `Falla`, `SuccessResponse` y `ErrorResponse`.
  - En `PaginationFilterDto.cs` se actualizaron `_pageSize = 15`, `SortDescending = true` y se agregaron `DateTime? FechaDesde` y `DateTime? FechaHasta`.
  - En `CatalogoItemDto.cs` se agregó `ActualizarCatalogoItemDto` y la propiedad `bool? EsEfectivoFisico` en `CatalogoItemDto`, `CrearCatalogoItemDto` y `ActualizarCatalogoItemDto`.
  - En `ConfiguracionSalonDto.cs` se unificaron todas las propiedades de la Sección 3.2 de `04_API_CONTRACTS.md` (`Id`, `NombreSalon`, `NombreComercial`, `RazonSocial`, `Cuit`, `Direccion`, `Telefono`, `EmailContacto`, `MonedaIso`, `Moneda`, `TimeZoneId`, `HorarioAtencionTexto`, `IntervaloTurnosMinutos`, `MinutosToleranciaEspera`, `HorasAnticipacionCancelacion`, `PorcentajeSenaReserva`, `NotificacionesWhatsAppActivas`, `NotificacionesEmailActivas`).
  - En `SeguridadDto.cs` se cambió el default de `RegistroUsuarioDto.RolId` a `4` (`Cliente`) y se agregaron `RefreshTokenRequestDto`, `CrearUsuarioStaffDto` y `CambiarRolUsuarioDto`.
  - En `CrearFichaTecnicaDto.cs`, `ActualizarFichaTecnicaDto.cs` y `FichaTecnicaResponseDto.cs` se agregó `public string? NotasSensibilidadAlergias { get; set; }`.
  - En `SaaSDto.cs` se agregó `PlanSuscripcionResponseDto`.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `04_API_CONTRACTS.md` y `06_INTEGRATION_AND_ODD_PLAN.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 2.2] - Actualización de DTOs de Turnos, Stock y Ventas + Validadores + MappingProfile
- **Estado:** COMPLETADA
- **Archivos Creados:**
  - `DTOs/Ventas/SesionCajaResponseDto.cs`
- **Archivos Modificados:**
  - `DTOs/Turnos/TurnoResponseDto.cs`
  - `DTOs/Turnos/DisponibilidadHorarioResponseDto.cs`
  - `DTOs/Turnos/BloqueoHorarioDto.cs`
  - `DTOs/Stock/ProveedorDto.cs`
  - `DTOs/Stock/CrearProveedorDto.cs`
  - `DTOs/Stock/ActualizarProveedorDto.cs`
  - `DTOs/Ventas/AperturaCajaDto.cs`
  - `DTOs/Ventas/CierreCajaDto.cs`
  - `DTOs/Ventas/MovimientoCajaDto.cs`
  - `DTOs/Ventas/CrearVentaDto.cs`
  - `DTOs/Ventas/PagoRequestDto.cs`
  - `DTOs/Ventas/VentaResponseDto.cs`
  - `DTOs/Ventas/LiquidacionResponseDto.cs`
  - `Validators/Common/CrearCatalogoItemDtoValidator.cs`
  - `Validators/Common/ActualizarConfiguracionSalonDtoValidator.cs`
  - `Validators/Seguridad/SeguridadValidator.cs`
  - `Validators/Clientes/CrearFichaTecnicaDtoValidator.cs`
  - `Validators/Stock/CrearProductoDtoValidator.cs`
  - `Validators/Stock/CrearProveedorDtoValidator.cs`
  - `Validators/Stock/ActualizarStockValidator.cs`
  - `Validators/Turnos/CrearPeluqueroDtoValidator.cs`
  - `Validators/Turnos/CrearServicioDtoValidator.cs`
  - `Validators/Turnos/CrearTurnoDtoValidator.cs`
  - `Validators/Ventas/CajayVentaValidators.cs`
  - `Mappings/MappingProfile.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - Se implementó `TurnoResponseDto` enriquecido (incluyendo señas, `ColorAgendaHex`, `DuracionTotalMinutos`, `SaldoPendienteCobro`, `ServiciosIds`, `Servicios` y `ServiciosDetalle`) y `TurnoServicioDetalleDto` (`ServicioId`, `ServicioNombre`, `PrecioHistorico`, `DuracionMinutosHistorica`).
  - Se agregaron `IntervaloMinutos` y `List<string> HorariosDisponibles` en `DisponibilidadHorarioResponseDto` y `BloqueoHorarioResponseDto` en `BloqueoHorarioDto.cs`.
  - Se agregó `string? Cuit` en `ProveedorDto`, `CrearProveedorDto` y `ActualizarProveedorDto`.
  - Se crearon `SesionCajaResponseDto` (con `MontoFinalEsperado`, `TotalVentasEfectivo`, `TotalVentasDigital`, `TotalIngresosManuales`, `TotalEgresosManuales`, `ObservacionesCierre` y `Movimientos`) y `MovimientoCajaResponseDto`, más `ObservacionesCierre` en `CierreCajaDto`.
  - Se agregaron `IdempotencyKey` en `CrearVentaDto` y `VentaResponseDto`, `ReferenciaOperacion` en `PagoRequestDto` y `VentaPagoItemResponseDto`, `Subtotal` y `DescuentoSenaAplicado` en `VentaResponseDto`, y `FechaEmision`, `Bonificaciones`, `Descuentos`, `Observaciones` y `ConceptoNombre` en `LiquidacionResponseDto` / `DetalleLiquidacionItemDto`.
  - Se reparó `Validators/Stock/CrearProductoDtoValidator.cs` (que contenía por error un validador duplicado de `MovimientoStockDto` en vez de `CrearProductoDto`) y se incorporaron los validadores de FluentValidation para todos los DTOs de entrada nuevos y actualizados.
  - Se registraron los perfiles completos de todos los módulos en `Mappings/MappingProfile.cs`.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `04_API_CONTRACTS.md` y `06_INTEGRATION_AND_ODD_PLAN.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 3.1] - GenericRepository<T>, Nuevos Repositorios (Recetas y SaaS) y UnitOfWork Resiliente
- **Estado:** COMPLETADA
- **Archivos Creados:**
  - `Repositories/Interfaces/IRecetaServicioRepository.cs`
  - `Repositories/Implementations/RecetaServicioRepository.cs`
  - `Repositories/Interfaces/ISaaSRepository.cs`
  - `Repositories/Implementations/SaaSRepository.cs`
- **Archivos Modificados:**
  - `Repositories/Interfaces/IGenericRepository.cs`
  - `Repositories/Implementations/GenericRepository.cs`
  - `Repositories/Interfaces/IUnitOfWork.cs`
  - `Repositories/Implementations/UnitOfWork.cs`
  - `Program.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - Se extendió `IGenericRepository<T>` / `GenericRepository<T>` incorporando `GetByIdsDictionaryAsync` (carga en lote anti N+1 con opción de `tracking`) y `AsQueryable()`, además de `CancellationToken` en todas las firmas asíncronas.
  - Se crearon `IRecetaServicioRepository` / `RecetaServicioRepository` (lecturas con `Include`, carga masiva por múltiples servicios, `AddRangeAsync` e idempotencia `ExisteConsumoGabinetePorTurnoAsync`) e `ISaaSRepository` / `SaaSRepository` (gestión de `Tenant`, `Sucursal`, `PlanSuscripcion` y `SuscripcionTenant`).
  - Se actualizaron `IUnitOfWork` y `UnitOfWork` para exponer los **19 repositorios** (`OrdenesCompra`, `Recetas` y `SaaS` incluidos) e implementar `ExecuteInTransactionAsync` sobre `_context.Database.CreateExecutionStrategy().ExecuteAsync(...)`, con control de reentrancia `_transactionDepth` y limpieza `_context.ChangeTracker.Clear()` en caso de rollback. Se mantuvieron temporalmente `BeginTransactionAsync`, `CommitTransactionAsync` y `RollbackTransactionAsync` como puente de transición hasta la Tarea 4.4 (`VentaService` y `OrdenCompraService`).
  - Se normalizó `Program.cs` a UTF-8 sin BOM preservando todos los acentos y se registraron en DI `IRecetaServicioRepository` e `ISaaSRepository` (`IOrdenCompraRepository` ya se encontraba registrado).
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `02_ARCHITECTURE.md` y `06_INTEGRATION_AND_ODD_PLAN.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 3.2] - Refactorización de los 17 Repositorios Existentes (Cero Reflection, Herencia y Paginación)
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `Repositories/Interfaces/IUsuarioRepository.cs`
  - `Repositories/Implementations/UsuarioRepository.cs`
  - `Repositories/Interfaces/IClienteRepository.cs`
  - `Repositories/Implementations/ClienteRepository.cs`
  - `Repositories/Interfaces/IFichaTecnicaRepository.cs`
  - `Repositories/Implementations/FichaTecnicaRepository.cs`
  - `Repositories/Interfaces/ICatalogoRepository.cs`
  - `Repositories/Implementations/CatalogoRepository.cs`
  - `Repositories/Interfaces/IConfiguracionSalonRepository.cs`
  - `Repositories/Implementations/ConfiguracionSalonRepository.cs`
  - `Repositories/Interfaces/IPeluqueroRepository.cs`
  - `Repositories/Implementations/PeluqueroRepository.cs`
  - `Repositories/Interfaces/IServicioRepository.cs`
  - `Repositories/Implementations/ServicioRepository.cs`
  - `Repositories/Interfaces/IBloqueoHorarioRepository.cs`
  - `Repositories/Implementations/BloqueoHorarioRepository.cs`
  - `Repositories/Interfaces/ITurnoRepository.cs`
  - `Repositories/Implementations/TurnoRepository.cs`
  - `Repositories/Interfaces/IProductoRepository.cs`
  - `Repositories/Implementations/ProductoRepository.cs`
  - `Repositories/Interfaces/IProveedorRepository.cs`
  - `Repositories/Implementations/ProveedorRepository.cs`
  - `Repositories/Interfaces/IOrdenCompraRepository.cs`
  - `Repositories/Implementations/OrdenCompraRepository.cs`
  - `Repositories/Interfaces/ISesionCajaRepository.cs`
  - `Repositories/Implementations/SesionCajaRepository.cs`
  - `Repositories/Interfaces/IVentaRepository.cs`
  - `Repositories/Implementations/VentaRepository.cs`
  - `Repositories/Interfaces/IComisionConfigRepository.cs`
  - `Repositories/Implementations/ComisionConfigRepository.cs`
  - `Repositories/Interfaces/ILiquidacionRepository.cs`
  - `Repositories/Implementations/LiquidacionRepository.cs`
  - `Repositories/Interfaces/IDashboardRepository.cs`
  - `Repositories/Implementations/DashboardRepository.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - Se estandarizaron los 17 repositorios existentes para heredar de `IGenericRepository<T>` y `GenericRepository<T>`, habilitando `GetByIdsDictionaryAsync` y `AsQueryable()` de forma transversal.
  - En `VentaRepository.cs` (`[BUG-01]`) se erradicó el 100% de `entry.Metadata` / Reflection en `GetDatosServicioAsync` y `GetNombrePeluqueroAsync`, y se agregaron `GetByIdempotencyKeyAsync`, `ExisteVentaActivaConSenaPorTurnoAsync`, `TieneRenglonesEnLiquidacionActivaAsync` y `GetBySesionCajaIdPagedAsync`.
  - En `ComisionConfigRepository.cs` (`[BUG-09]`) se corrigió `GetVigenteByPeluqueroYServicioAsync` para priorizar `ServicioId == servicioId` con fallback a `ServicioId == null`, además de agregar `GetByPeluqueroIdWithDetailsAsync`, `GetByIdWithDetailsAsync` y `GetExactaByPeluqueroYServicioAsync`.
  - Se agregaron los métodos de soporte para CRUD de los 7 catálogos en `CatalogoRepository`, inserción de `HistorialPrecios` en `ProductoRepository` y `ServicioRepository`, verificación de consumo idempotente de gabinete en `TurnoRepository` y `ProductoRepository`, conteo en lote de productos por proveedor (`GetCantidadProductosPorProveedorAsync`) en `ProveedorRepository`, y consultas paginadas (`GetPagedAsync` / `GetKardexPagedAsync` / `GetHistorialSesionesPagedAsync` / `GetHistorialCajasPagedAsync`) en todos los repositorios operativos.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `00_AUDITORIA_Y_DEUDA_TECNICA.md`, `02_ARCHITECTURE.md`, `05_BUSINESS_RULES.md` y `06_INTEGRATION_AND_ODD_PLAN.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 4.1] - Servicios de Seguridad, Usuarios, Catálogos, Configuración, SaaS y Clientes
- **Estado:** COMPLETADA
- **Archivos Creados:**
  - `Services/Interfaces/ISaaSService.cs`
  - `Services/Implementations/SaaSService.cs`
- **Archivos Modificados:**
  - `Services/Interfaces/IAuthService.cs`
  - `Services/Implementations/AuthService.cs`
  - `Services/Interfaces/IUsuarioService.cs`
  - `Services/Implementations/UsuarioService.cs`
  - `Services/Interfaces/ICatalogoService.cs`
  - `Services/Implementations/CatalogoService.cs`
  - `Services/Interfaces/IConfiguracionSalonService.cs`
  - `Services/Implementations/ConfiguracionSalonService.cs`
  - `Services/Interfaces/IClienteService.cs`
  - `Services/Implementations/ClienteService.cs`
  - `Services/Interfaces/IFichaTecnicaService.cs`
  - `Services/Implementations/FichaTecnicaService.cs`
  - `Models/Clientes/FichaTecnica.cs`
  - `Data/Configurations/ClientesModuloConfiguration.cs`
  - `Program.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - En `AuthService.cs` se implementó `RefreshTokenAsync` con rotación segura de tokens, se fijó estrictamente `RolId = 4` (Cliente) en `RegistrarUsuarioAsync` (`[BUG-08]`) y se blindaron los envíos de correo OTP en `RegistrarUsuarioAsync` y `SolicitarRecuperacionPasswordAsync` para devolver advertencias en `ApiResponse.Warnings` sin abortar la transacción ante fallos o ausencia de SMTP.
  - En `UsuarioService.cs` (`[BUG-09]`) se envolvió toda respuesta en `ApiResponse<T>`, se agregó `CrearUsuarioStaffAsync(CrearUsuarioStaffDto)`, se paginó `ObtenerHistorialSesionesAsync` con `PaginationFilterDto` y se agregó la revocación automática de `RefreshTokens` al desactivar cuentas (`dto.Activo == false`).
  - En `CatalogoService.cs` (`[BUG-01]`, `[BUG-09]`) se erradicaron `ApplicationDbContext` y los métodos de Reflection (`MapToCatalogoDto`, `SetCatalogProperties`), implementando CRUD tipado completo para los 7 catálogos del salón (incluyendo `EsEfectivoFisico` en `MediosPago`) sobre `IUnitOfWork.Catalogos` con caché Multi-Tenant `$"SaaS_Catalogos_T{tenantId}_S{sucursalId}"`.
  - En `ConfiguracionSalonService.cs` (`[BUG-05]`) se particionó la caché en memoria con la clave `$"SaaS_Config_T{tenantId}_S{sucursalId}"` y se mapearon todas las propiedades comerciales y operativas de `ConfiguracionSalonDto`.
  - Se crearon `ISaaSService` y `SaaSService` (registrado en `Program.cs`) implementando la validación del tope anti-abuso de 20 sucursales por Tenant e invalidación de caché `$"SaaS_Valid_T{tenantId}_S{nueva.Id}"`.
  - En `FichaTecnicaService.cs` (`[BUG-01]`) se eliminaron `ApplicationDbContext` y el 100% de Reflection (`SetOptionalProp`, `GetOptionalProp`, `SetOptionalDate`, `GetOptionalDate`), se agregó `ObtenerPorIdAsync`, se incorporó la sobrecarga con `ActualizarFichaTecnicaDto` y se eliminaron definitivamente las propiedades puente `[NotMapped]` (`Fecha` y `DetalleFormulaTintura`) de `Models/Clientes/FichaTecnica.cs` (junto con sus dos líneas `builder.Ignore` asociadas en `ClientesModuloConfiguration.cs`), tachando el puente en la Sección 3 de este Libro Mayor.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `01_SYSTEM_CONSTRAINTS.md`, `02_ARCHITECTURE.md`, `04_API_CONTRACTS.md` y `06_INTEGRATION_AND_ODD_PLAN.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 4.2] - Servicios de Peluqueros, Servicios, Recetas, Bloqueos y Motor de Turnos
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `Services/Interfaces/IPeluqueroService.cs`
  - `Services/Implementations/PeluqueroService.cs`
  - `Services/Interfaces/IServicioService.cs`
  - `Services/Implementations/ServicioService.cs`
  - `Services/Interfaces/IRecetaServicioService.cs`
  - `Services/Implementations/RecetaServicioService.cs`
  - `Services/Interfaces/IBloqueoHorarioService.cs`
  - `Services/Implementations/BloqueoHorarioService.cs`
  - `Services/Interfaces/ITurnoService.cs`
  - `Services/Implementations/TurnoService.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - En `PeluqueroService.cs` (`[BUG-01]`) se erradicaron `ApplicationDbContext` y el 100% de Reflection (`GetEspecialidadesIdsFromDto`, `GetOptionalProp`, `GetTimeSpanProp`, `SetOptionalProp`, `SetDiaProp`), operando vía `IUnitOfWork` con recepción tipada de `ActualizarPeluqueroDto`, tope anti-abuso de 50 profesionales activos, persistencia directa de `HoraInicioDescanso`, `HoraFinDescanso` y `Activo`, y bloqueo de baja/desactivación si existen turnos futuros activos o liquidaciones pendientes.
  - En `ServicioService.cs` se eliminó `ApplicationDbContext`, se incorporó `UpdateAsync(..., ActualizarServicioDto, ...)` insertando automáticamente en `HistorialPreciosServicio` dentro de `ExecuteInTransactionAsync` cuando cambia el `Precio`, y se blindó `DeleteAsync` para desactivar preservando `TurnoServicios` históricos/futuros con advertencia en `Warnings`.
  - En `RecetaServicioService.cs` se eliminó `ApplicationDbContext`, se aplicó carga en lote de insumos con `GetByIdsDictionaryAsync`, idempotencia por `"Turno #{turnoId}"` (`ExisteConsumoGabinetePorTurnoAsync`), persistencia de `SaldoResultante` y descuento no bloqueante hasta `0` (`Math.Max(0, ...)`) retornando alertas en `ApiResponse.Warnings`.
  - En `BloqueoHorarioService.cs` se eliminó `ApplicationDbContext` y `object`, retornando `BloqueoHorarioResponseDto` e informando en `Warnings` los turnos activos solapados sin bloquear el guardado.
  - En `TurnoService.cs` (`[BUG-01]`, `[BUG-06]`, `[BUG-07]`, `[BUG-08]`) se eliminaron `ApplicationDbContext` y todo Reflection, implementando paginación con `PagedResult<TurnoResponseDto>` en `GetAllAsync`, transacciones `ExecuteInTransactionAsync(..., IsolationLevel.Serializable)` en `CreateAsync`, `ReprogramarAsync` y `ProcesarPagoSenaWebhookAsync`, carga batch de servicios con `GetByIdsDictionaryAsync`, sellado tipado de `PrecioHistorico` y `DuracionMinutosHistorica` en `TurnoServicio`, Máquina de Estados Estricta, ventana `HorasAnticipacionCancelacion` para rol `Cliente`, transacción compensatoria para el "Cruce de la Muerte" en Webhooks (`ProcesarPagoSenaWebhookAsync`), motor matemático de disponibilidad con `TimeZoneInfo` retornando `DisponibilidadHorarioResponseDto` y trazabilidad tipada con `HistorialEstadoTurnoResponseDto`.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `04_API_CONTRACTS.md`, `05_BUSINESS_RULES.md` y `06_INTEGRATION_AND_ODD_PLAN.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 4.3] - Servicios de Stock, Proveedores y Órdenes de Compra
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `Services/Interfaces/IProductoService.cs`
  - `Services/Implementations/ProductoService.cs`
  - `Services/Interfaces/IProveedorService.cs`
  - `Services/Implementations/ProveedorService.cs`
  - `Services/Interfaces/IOrdenCompraService.cs`
  - `Services/Implementations/OrdenCompraService.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - En `ProductoService.cs` (`[BUG-01]`, `[BUG-06]`) se eliminaron `ApplicationDbContext` y el método de Reflection `SetOptionalProp`. Se migró 100% a `IUnitOfWork`, se paginó `GetAllAsync` retornando `PagedResult<ProductoDto>`, se implementó `UpdateAsync` con `ActualizarProductoDto` auditando automáticamente en `HistorialPreciosProducto` dentro de `ExecuteInTransactionAsync` cuando varía `PrecioCosto` o `PrecioVenta`, se selló `SaldoResultante` tipado en `MovimientoStock` (tanto en inventario inicial como en `RegistrarMovimientoStockAsync`), y se paginó `ObtenerKardexProductoAsync` retornando `PagedResult<KardexItemResponseDto>`.
  - En `ProveedorService.cs` (`[BUG-06]`) se eliminaron `ApplicationDbContext` y el bucle $N+1$ de `CountAsync` por proveedor, reemplazándolo por una única consulta `GROUP BY` en SQL Server (`GetCantidadProductosPorProveedorAsync`), se agregó `GetPagedAsync(PaginationFilterDto)`, validación/mapeo completo de `Cuit` y recepción de `ActualizarProveedorDto` en `UpdateAsync`.
  - En `OrdenCompraService.cs` se eliminó `ApplicationDbContext`, se paginó `ObtenerTodasAsync` retornando `PagedResult<OrdenCompraResponseDto>`, se reemplazó la validación individual de ítems por carga en lote (`GetByIdsDictionaryAsync`), y se migró `RecepcionarOrdenAsync` a `_uow.ExecuteInTransactionAsync` persistiendo `SaldoResultante` en `MovimientoStock`, `HistorialPreciosProducto` vía repositorio y actualización sincronizada de `TotalEgresosManuales` en `SesionCaja` cuando `ImputarEgresoEnCajaAbierta == true`.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `04_API_CONTRACTS.md`, `05_BUSINESS_RULES.md` y `06_INTEGRATION_AND_ODD_PLAN.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 4.4] - Servicios Financieros: Caja, POS Idempotente, Comisiones, Liquidaciones y Dashboard [HARD GATE 2]
- **Estado:** COMPLETADA (EN ESPERA DE AUTORIZACIÓN HUMANA EN HARD GATE 2)
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `Services/Interfaces/ICajaService.cs`
  - `Services/Implementations/CajaService.cs`
  - `Services/Interfaces/IVentaService.cs`
  - `Services/Implementations/VentaService.cs`
  - `Services/Interfaces/IComisionConfigService.cs`
  - `Services/Implementations/ComisionConfigService.cs`
  - `Services/Interfaces/ILiquidacionService.cs`
  - `Services/Implementations/LiquidacionService.cs`
  - `Services/Interfaces/IDashboardService.cs`
  - `Services/Implementations/DashboardService.cs`
  - `Repositories/Interfaces/IUnitOfWork.cs`
  - `Repositories/Implementations/UnitOfWork.cs`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - En `CajaService.cs` (`[BUG-06]`, `[BUG-10]`) se reemplazó el retorno de entidades crudas EF Core por `SesionCajaResponseDto` y `MovimientoCajaResponseDto`, se incorporó `ObtenerPorIdAsync`, se paginó `ObtenerHistorialCajasAsync` omitiendo `Movimientos` para prevenir *Over-fetching* con precarga batch de usuarios (`GetByIdsDictionaryAsync`), se persistió `ObservacionesCierre` y se separó `TotalVentasEfectivo` (`EsEfectivoFisico == true`) de `TotalVentasDigital`, calculando exactamente $\text{MontoFinalEsperado} = \text{MontoInicial} + \text{TotalVentasEfectivo} + \text{TotalIngresosManuales} - \text{TotalEgresosManuales}$.
  - En `VentaService.cs` (`[BUG-02]`, `[BUG-06]`, `[BUG-07]`, `[BUG-10]`) se implementó control de idempotencia POS por `IdempotencyKey` (tanto al inicio como ante colisión concurrente de índice único), precarga en lote $O(1)$ de `Productos`, `Servicios` y `Peluqueros` con `GetByIdsDictionaryAsync`, ejecución transaccional con `_unitOfWork.ExecuteInTransactionAsync` heredando `turno.PeluqueroId`, descontando `DescuentoSenaAplicado`, persistiendo `SaldoResultante` en `MovimientoStock`, autocompletando el turno a `Completado (4)` e invocando el consumo idempotente de gabinete con propagación de `Warnings`. En `AnularVentaAsync` se blindó el bloqueo ante `SesionCaja` cerrada o renglones incluidos en una `Liquidacion` activa, y se paginó `ObtenerVentasPorSesionAsync` sin consultas $N+1$.
  - En `ComisionConfigService.cs` y `LiquidacionService.cs` (`[BUG-09]`, `[BUG-10]`) se erradicó `ApplicationDbContext` migrando 100% a `IUnitOfWork` y `ExecuteInTransactionAsync`, aplicando la jerarquía de comisiones (`Específica -> General -> 50%/10%`), persistiendo `FechaEmision`, `Bonificaciones`, `Descuentos`, `Observaciones` y `UsuarioLiquidacionId`, poblando `ConceptoNombre` en lote, paginando `ObtenerTodasAsync` e implementando `AnularLiquidacionAsync` (solo para estado `Pendiente`, liberando los renglones en `DetallesLiquidacion` con `IsDeleted = true`).
  - En `DashboardService.cs` se erradicó `ApplicationDbContext`, operando exclusivamente vía `IUnitOfWork.Dashboard` y calculando `TurnosPendientesHoy` convirtiendo `DateTime.UtcNow` al huso horario configurado del salón (`TimeZoneInfo` con fallback a `-03:00`).
  - En `IUnitOfWork.cs` y `UnitOfWork.cs` se eliminaron definitivamente los métodos transaccionales manuales obsoletos (`BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync`), dejando exclusivamente `ExecuteInTransactionAsync` y tachando el último puente temporal en la Sección 3 de este Libro Mayor (**0 puentes temporales activos**).
  - **Auditorías PowerShell ejecutadas:**
    1. `Select-String -Path .\Services\**\*.cs, .\Repositories\**\*.cs -Pattern "GetProperty|SetOptionalProp|GetOptionalProp|Metadata"` -> **0 coincidencias**.
    2. `Select-String -Path .\Services\Implementations\*.cs -Pattern "ApplicationDbContext"` -> **0 coincidencias**.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `04_API_CONTRACTS.md`, `05_BUSINESS_RULES.md` y `06_INTEGRATION_AND_ODD_PLAN.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.


### [TAREA 5.1] - Middlewares, Integraciones (HMAC + SMTP Guard), Background Worker, Configuración ODS y Program.cs
- **Estado:** COMPLETADA (APROBADA POR EL ARQUITECTO)
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `Middlewares/GlobalExceptionMiddleware.cs`
  - `Middlewares/TenantResolutionMiddleware.cs`
  - `Integrations/Payments/PaymentGatewayService.cs`
  - `Integrations/Email/SmtpEmailService.cs`
  - `Services/BackGround/RecordatorioTurnoBackgroundService.cs` (renombrado en Tarea 5.2 a `Services/Background/RecordatorioTurnosBackgroundService.cs`)
  - `appsettings.json`
  - `appsettings.Development.json`
  - `Program.cs`
  - `02_ARCHITECTURE.md` (SPEC-SYNC Sección 3.2)
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **`GlobalExceptionMiddleware.cs`:** se agregó el mapeo `ForbiddenAccessException` → `403` y `DbUpdateConcurrencyException` → `409 Conflict` con el mensaje oficial de `02` Sección 3.3. Se eliminó la fuga de `exception.Message` en `Errors` para los errores `500`. Se agregó una protección por si `Response.HasStarted` y se reutiliza un único `JsonSerializerOptions` estático.
  - **`TenantResolutionMiddleware.cs` (`[BUG-03]`):** con usuario autenticado, los claims del JWT mandan y se ignora `X-Tenant-Id` (Test 01). Las rutas públicas resuelven el contexto por header, QueryString o `subdominioSlug`. El par se valida en `IMemoryCache` vía `IUnitOfWork.SaaS.EsSucursalValidaYActivaAsync`, y un par inválido o mal formado lanza `ForbiddenAccessException` (`403`), sin acceso directo a `ApplicationDbContext`. Solo después se fija el contexto con `ITenantProvider.SetTenantContext`.
  - **`PaymentGatewayService.cs` (`[BUG-08]`):** se eliminó el `return true` de "modo sandbox". Ahora se calcula `HMAC-SHA256` real y se compara en tiempo constante con `CryptographicOperations.FixedTimeEquals`, con política Fail-Closed.
  - **`SmtpEmailService.cs` (deuda táctica SMTP):** si `Smtp:Host` está vacío, se corta antes de crear `SmtpClient` y devuelve `false` (antes simulaba el envío y devolvía `true`, ocultando el aviso en `Warnings`). Se configuró `Timeout = 5000` + `CancellationToken` de 5s, y remitente/SSL/puerto tomados de la configuración.
  - **`RecordatorioTurnosBackgroundService`:** se eliminaron `ApplicationDbContext` y el `MotivoCancelacionId = 1` fijo (que podía no existir en otros tenants). Ahora recorre todas las sucursales activas por Tenant con `.IgnoreQueryFilters()`, en un scope aislado por sucursal y con manejo de errores por sucursal. Aplica los valores por defecto de `09` Sección 2 y calcula las ventanas de recordatorio en hora local del salón, igual que `TurnoService`, que guarda `FechaHoraInicio` en hora local. La expiración automática se limita a reservas `"[Reserva Online]"` con `RequiereSena && !SenaPagada` (ver Sección 2).
  - **`appsettings.json` / `appsettings.Development.json`:** contrato completo de `09` Sección 1 (`Jwt`, `Cors`, `RateLimiting`, `Smtp`, `Payments`, `BackgroundJobs`, `SaaSLimits`). Se eliminó el comentario `// filepath:` inválido en JSON de `appsettings.Development.json` y se conservó su `Server=localhost`. La sintaxis de ambos se validó con `ConvertFrom-Json`.
  - **`Program.cs` (`[BUG-04]`):** `AddRateLimiter` + `UseRateLimiter` con la política `"StrictAuthOtpPolicy"` (5 req/min por IP, `429` con `ApiResponse`), `InvalidModelStateResponseFactory` con `FieldErrors`, registro reordenado de los 19 repositorios + 20 servicios, y orden de middlewares según `02` Sección 3.1 (corrige que `TenantResolution` corría **después** de `UseAuthorization`).
  - **Auditoría PowerShell:** `Select-String` sobre `Middlewares`, `Integrations/Email`, `Integrations/Payments`, `Services/BackGround` y `Services/Implementations` con el patrón `ApplicationDbContext|GetProperty|SetValue\(|SetOptionalProp|GetOptionalProp|System.Reflection` -> **0 coincidencias**.
- **Sincronización de MD (Spec-Sync):** `02_ARCHITECTURE.md` Sección 3.2 actualizado (consulta vía `IUnitOfWork.SaaS` + políticas de caché). Desviaciones registradas en la Sección 2 de este Libro Mayor.
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 5.2] - Refactorización de los 22 Controladores, Suite HTTP y Verificación BDD [HARD GATE 3]
- **Estado:** COMPLETADA (HARD GATE 3 APROBADO POR EL ARQUITECTO)
- **Archivos Creados:**
  - `Services/Background/RecordatorioTurnosBackgroundService.cs` (renombrado desde `Services/BackGround/RecordatorioTurnoBackgroundService.cs`)
- **Archivos Modificados:**
  - `Controllers/AuthController.cs`
  - `Controllers/UsuariosController.cs`
  - `Controllers/CatalogosController.cs`
  - `Controllers/ClientesController.cs`
  - `Controllers/FichasTecnicasController.cs`
  - `Controllers/PeluquerosController.cs`
  - `Controllers/ServiciosController.cs`
  - `Controllers/RecetasController.cs`
  - `Controllers/BloqueosController.cs`
  - `Controllers/TurnosController.cs`
  - `Controllers/PortalReservasController.cs` (`PortalReservasController` y `WebhooksController`)
  - `Controllers/ProductosController.cs`
  - `Controllers/ProveedoresController.cs`
  - `Controllers/OrdenesCompraController.cs`
  - `Controllers/CajaController.cs`
  - `Controllers/VentasController.cs`
  - `Controllers/ComisionesController.cs`
  - `Controllers/LiquidacionesController.cs`
  - `Controllers/ConfiguracionSalonController.cs`
  - `Controllers/DashboardController.cs`
  - `Controllers/SucursalesController.cs`
  - `Controllers/SuscripcionesController.cs`
  - `MiPeluqueria.Api.http`
  - `07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:**
  - `Services/BackGround/RecordatorioTurnoBackgroundService.cs` (movido a `Services/Background/RecordatorioTurnosBackgroundService.cs`)
- **Cambios o Decisiones Especiales:**
  - **Renombre 1:1 del Background Worker:** Se renombró el directorio y archivo a `Services/Background/RecordatorioTurnosBackgroundService.cs` para coincidir 1:1 con `02_ARCHITECTURE.md`.
  - **Erradicación total de `ApplicationDbContext` y estado estático en Controladores:** Se eliminó el 100% de referencias a `ApplicationDbContext` (e `IUnitOfWork`) en `CatalogosController`, `ConfiguracionSalonController` (eliminando también la clase `ConfiguracionSalonDto` duplicada y la variable estática `_configActual`), `SucursalesController`, `SuscripcionesController` y `PortalReservasController`.
  - **Blindaje de `PortalReservasController` y `WebhooksController`:**
    - `PortalReservasController` opera exclusivamente mediante `IClienteService`, `ITurnoService`, `IServicioService`, `IPeluqueroService`, `IConfiguracionSalonService` e `IPaymentGatewayService`, sellando el prefijo `"[Reserva Online]"` en `Observaciones` al crear el turno en `POST /api/public/reservar`.
    - `WebhooksController` incorpora `[EnableRequestBuffering]` (`IResourceFilter` sin Reflection) para habilitar `Request.EnableBuffering()` antes del Model Binding, leyendo el `rawBody` real del stream HTTP, verificando la firma criptográfica con `_paymentGateway.ValidarFirmaWebhook(proveedor, signature, rawBody)` y delegando la confirmación/compensación a `_turnoService.ProcesarPagoSenaWebhookAsync(turnoId, payload, proveedor, ct)`.
  - **Completitud de Contratos y Matriz RBAC (`04_API_CONTRACTS.md`):**
    - `AuthController`: agregado `POST /api/auth/refresh-token`.
    - `UsuariosController`: migrado a `IUsuarioService` con `[Authorize(Roles = "Administrador,SuperAdminSaaS")]` y endpoints `GET`, `GET {id:int}`, `POST` (`CrearUsuarioStaffDto`), `PUT {id:int}/estado`, `PATCH {id:int}/rol`, `POST {id:int}/desbloquear` y `GET historial-sesiones` paginado.
    - `CatalogosController`: expone el CRUD completo (`GET`, `POST`, `PUT {id:int}`, `DELETE {id:int}`) para los 7 catálogos del salón.
    - `TurnosController`: agregados `PUT {id:int}/reprogramar`, paginación en `GET` y propagación del `rolEjecutor` desde `ClaimTypes.Role`.
    - `CajaController`, `VentasController`, `LiquidacionesController`: extracción exclusiva de `UsuarioId` desde `ClaimTypes.NameIdentifier`, agregado `GET /api/caja/{id:int}`, lectura de header `X-Idempotency-Key` en `POST /api/ventas` y agregado `POST /api/liquidaciones/{id:int}/anular`.
  - **Suite Ejecutable BDD (`MiPeluqueria.Api.http`):** Actualizada con los 14 pasos de prueba extremo a extremo (`"sesionCajaId"`, `"detalles"` e `"idempotencyKey"` en `POST /api/ventas`).
  - **Auditorías PowerShell ejecutadas:**
    1. `Select-String -Path .\Controllers\*.cs -Pattern "ApplicationDbContext"` -> **0 coincidencias**.
    2. `Select-String -Path .\Controllers\*.cs, .\Services\**\*.cs, .\Middlewares\*.cs -Pattern "ApplicationDbContext"` -> **0 coincidencias**.
    3. `Select-String -Path .\Controllers\*.cs, .\Services\**\*.cs, .\Repositories\**\*.cs, .\Middlewares\*.cs -Pattern "GetProperty|SetValue\(|SetOptionalProp|GetOptionalProp|System.Reflection"` -> **0 coincidencias**.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `02_ARCHITECTURE.md`, `04_API_CONTRACTS.md`, `06_INTEGRATION_AND_ODD_PLAN.md` y `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md`).
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.

### [TAREA 6.1] - Manifiesto package.json, Variables .env.local, Layout y Providers Globales
- **Estado:** COMPLETADA (APROBADA POR EL ARQUITECTO)
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/package.json`
  - `mipeluqueria-saas-web/package-lock.json`
  - `mipeluqueria-saas-web/tsconfig.json`
  - `mipeluqueria-saas-web/.env.local`
  - `mipeluqueria-saas-web/.env.example`
  - `mipeluqueria-saas-web/src/app/layout.tsx`
  - `mipeluqueria-saas-web/src/context/AppProviders.tsx`
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **`package.json` y `tsconfig.json`:** Se restauró el manifiesto con el stack Pro completo (`@tanstack/react-query@^5.59.0`, `react-hook-form@^7.53.0`, `zod@^3.23.8`, `@hookform/resolvers@^3.9.0`, `sonner@^1.5.0`, `date-fns@^4.1.0`, `recharts@^2.13.0`, `lucide-react@^0.454.0`, `clsx@^2.1.1`) y se limpió el comentario inválido `// filepath:` de `tsconfig.json` manteniendo `"strict": true` y `"paths": { "@/*": ["./src/*"] }`.
  - **`.env.local` y `.env.example`:** Configuradas las variables públicas de la Sección 3 de `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (`NEXT_PUBLIC_API_URL=https://localhost:7132/api`, `NEXT_PUBLIC_DEFAULT_TENANT_ID=1`, `NEXT_PUBLIC_DEFAULT_SUCURSAL_ID=1`, `NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME=demo`, `NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET=mipeluqueria_preset`, `NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX`).
  - **`src/app/layout.tsx` y `src/context/AppProviders.tsx`:** Se incorporó `QueryClientProvider` (con instancia estable en `useState`, `staleTime = 60s`, `retry = 1`, `refetchOnWindowFocus = false`), `<Toaster position="top-right" richColors closeButton expand />` de `sonner`, lectura de defaults desde `NEXT_PUBLIC_DEFAULT_TENANT_ID` / `NEXT_PUBLIC_DEFAULT_SUCURSAL_ID`, invalidación global `queryClient.invalidateQueries()` en `switchSucursal` y limpieza `queryClient.clear()` en `logout`.
  - **Instalación y Verificación:** `npm --prefix mipeluqueria-saas-web install` (175 paquetes auditados) y `npm --prefix mipeluqueria-saas-web run typecheck` (`tsc --noEmit` con **0 errores**).
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `01_SYSTEM_CONSTRAINTS.md`, `02_ARCHITECTURE.md`, `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` y `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`).
- **Resultado TypeScript (`tsc --noEmit`):** 0 Errores.

### [TAREA 6.2] - Cliente HTTP Resiliente (axios.ts con Cola RefreshToken) y Helper Multimedia (cloudinary.ts)
- **Estado:** COMPLETADA (APROBADA POR EL ARQUITECTO)
- **Archivos Creados:**
  - `mipeluqueria-saas-web/src/lib/cloudinary.ts`
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/lib/axios.ts`
  - `mipeluqueria-saas-web/src/lib/formatters.ts`
  - `mipeluqueria-saas-web/src/middleware.ts`
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **`src/lib/axios.ts`:**
    - **Request Interceptor:** Inyecta automáticamente `Authorization: Bearer ${token}` (cuando existe sesión), `X-Tenant-Id` y `X-Sucursal-Id` (resolviendo prioridad `localStorage` -> `USER_SESSION` -> `NEXT_PUBLIC_DEFAULT_TENANT_ID` / `NEXT_PUBLIC_DEFAULT_SUCURSAL_ID` -> `'1'`), y soporta `idempotencyKey` en `AxiosRequestConfig` + helper `withIdempotencyKey(...)` para inyectar `X-Idempotency-Key`.
    - **Response Interceptor (`isRefreshing` + `failedQueue`):** Implementa la cola de concurrencia para prevenir *Refresh Token Storm* ante respuestas `401 Unauthorized` fuera de endpoints de autenticación/webhooks. La primera petición marca `isRefreshing = true` e invoca una única vez `POST /api/auth/refresh-token` enviando `{ token, refreshToken }` junto con `X-Tenant-Id` y `X-Sucursal-Id`; las peticiones concurrentes aguardan en `failedQueue`. Si el refresh responde `200 OK`, persiste el nuevo par `token`/`refreshToken` y cookies (`mipeluqueria_token`, `mipeluqueria_role`), resuelve `failedQueue` y reintenta las peticiones originales; si falla, rechaza `failedQueue`, limpia la sesión y redirige a `/login`. Propaga además `fieldErrors`, `warnings`, `errors` y `statusCode` en `ApiErrorPayload`.
  - **`src/lib/cloudinary.ts`:** Implementa subida Direct-to-Cloud (`uploadImageToCloudinary` y `subirImagenCloudinary`) con validación de MIME/tamaño (10 MB) y el fallback defensivo de la Sección 3 de `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`: si `NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME` es `"demo"` o está vacío y la subida falla en entorno local, emite un `toast.warning` (`sonner`, `duration: 8000`) y retorna una URL manual o placeholder controlado (`DEFAULT_FICHA_TECNICA_PLACEHOLDER_URL`) sin bloquear el guardado de la `FichaTecnica`.
  - **`src/lib/formatters.ts`:** Tipado estricto (`0 any`) y blindaje contra fechas inválidas (`Number.isNaN(date.getTime())`) en `formatCurrency`, `formatDateTime`, `formatDateOnly`, `formatTimeOnly`, `formatPercentage` y `calcularHorasHastaTurno`.
  - **`src/middleware.ts`:** Protección de rutas por rol (`ROLE_ROUTE_MAP`) tipada con `RolNombreType`, permitiendo `SuperAdminSaaS` y `Administrador` en `/saas-admin` y redirigiendo accesos sin permiso a la ruta de inicio correspondiente al rol (`getDefaultHomeForRole`).
  - **Auditorías ejecutadas:**
    1. `npm --prefix mipeluqueria-saas-web run typecheck` (`tsc --noEmit`) -> **0 Errores**.
    2. `Select-String -Path .\mipeluqueria-saas-web\src\lib\*.ts, .\mipeluqueria-saas-web\src\middleware.ts -Pattern "\bany\b"` -> **0 coincidencias**.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `06_INTEGRATION_AND_ODD_PLAN.md`, `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` y `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`).
- **Resultado TypeScript (`tsc --noEmit`):** 0 Errores.

### [TAREA 6.3] - Modularización de Tipos TypeScript (src/types/) y Servicios API (src/services/) con Barrel Files
- **Estado:** COMPLETADA (EN REVISIÓN HUMANA)
- **Archivos Creados:**
  - `mipeluqueria-saas-web/src/types/common.types.ts`
  - `mipeluqueria-saas-web/src/types/seguridad.types.ts`
  - `mipeluqueria-saas-web/src/types/clientes.types.ts`
  - `mipeluqueria-saas-web/src/types/turnos.types.ts`
  - `mipeluqueria-saas-web/src/types/public.types.ts`
  - `mipeluqueria-saas-web/src/types/stock.types.ts`
  - `mipeluqueria-saas-web/src/types/ventas.types.ts`
  - `mipeluqueria-saas-web/src/types/saas.types.ts`
  - `mipeluqueria-saas-web/src/services/seguridad.service.ts`
  - `mipeluqueria-saas-web/src/services/common.service.ts`
  - `mipeluqueria-saas-web/src/services/clientes.service.ts`
  - `mipeluqueria-saas-web/src/services/turnos.service.ts`
  - `mipeluqueria-saas-web/src/services/public.service.ts`
  - `mipeluqueria-saas-web/src/services/stock.service.ts`
  - `mipeluqueria-saas-web/src/services/ventas.service.ts`
  - `mipeluqueria-saas-web/src/services/saas.service.ts`
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/types/api.types.ts` (*Barrel File*)
  - `mipeluqueria-saas-web/src/services/api.services.ts` (*Barrel File*)
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **Modularización en 8 Dominios (`src/types/*.types.ts` + `api.types.ts`):** Se espejaron 1:1 contra el Backend .NET 8 `ApiResponse<T>` (con `fieldErrors?: Record<string, string[]>` y `warnings?: string[]`), `PagedResult<T>`, `PaginationFilterDto`, todos los `Actualizar*Dto` (`ActualizarCatalogoItemDto`, `ActualizarFichaTecnicaDto`, `ActualizarPeluqueroDto`, `ActualizarServicioDto`, `ActualizarProductoDto`, `ActualizarProveedorDto`), `ReprogramarTurnoDto`, `CrearUsuarioStaffDto`, `CambiarRolUsuarioDto`, `RefreshTokenRequestDto`, `DisponibilidadHorarioResponseDto` (`horariosDisponibles` y `slots`), `KardexItemResponseDto` (`saldoResultante`), `SesionCajaDto` (`montoFinalEsperado`, `totalVentasEfectivo`, `totalVentasDigital`, `observacionesCierre`), `VentaResponseDto` (`subtotal`, `descuentoSenaAplicado`, `idempotencyKey`), `LiquidacionDto` (`fechaEmision`, `bonificaciones`, `descuentos`, `observaciones`, `conceptoNombre`) y `DashboardKpisDto` (`ingresosPorMedioPago`, `rankingPeluqueros`).
  - **Modularización de Servicios HTTP (`src/services/*.service.ts` + `api.services.ts`):**
    - Se conectaron los 22 controladores corrigiendo rutas y métodos que antes discrepaban del Backend: `configuracionSalonService` contra `/api/configuracion`, `ordenesCompraService.recepcionar` contra `POST /api/ordenes-compra/{id}/recibir`, `usuariosService.cambiarRol` contra `PATCH /api/usuarios/{id}/rol`, `usuariosService.crearStaff` contra `POST /api/usuarios`, `comisionesService.getByPeluquero` contra `GET /api/comisiones?peluqueroId={id}` y `liquidacionesService.getPorPeluquero` contra `GET /api/liquidaciones?peluqueroId={id}`.
    - Se agregaron todos los nuevos endpoints del contrato (`authService.refreshToken`, CRUD completo de los 7 catálogos en `catalogosService`, `turnosService.reprogramar`, `turnosService.getDisponibilidadCompleta`, `portalPublicoService.getDisponibilidadCompleta`, `cajaService.getById`, `ventasService.registrar` con `withIdempotencyKey`, `liquidacionesService.anular` y métodos `*Paged` que retornan `ApiResponse<PagedResult<TDto>>` junto con sus wrappers de compatibilidad).
  - **Auditorías ejecutadas:**
    1. `npm --prefix mipeluqueria-saas-web run typecheck` (`tsc --noEmit`) -> **0 Errores**.
    2. `Select-String -Path .\mipeluqueria-saas-web\src\types\*.ts, .\mipeluqueria-saas-web\src\services\*.ts -Pattern "\bany\b"` -> **0 coincidencias**.
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `02_ARCHITECTURE.md`, `04_API_CONTRACTS.md`, `06_INTEGRATION_AND_ODD_PLAN.md` y `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`).
- **Resultado TypeScript (`tsc --noEmit`):** 0 Errores.

### [TAREA 6.4] - Esquemas Zod, Hooks TanStack Query y Conexión de las 19 Pantallas (src/app/) [HARD GATE 4 - FIN DEL PROYECTO]
- **Estado:** COMPLETADA (PROYECTO FINALIZADO - EN ESPERA DE REVISIÓN FINAL HUMANA)
- **Archivos Creados:**
  - `mipeluqueria-saas-web/src/schemas/auth.schema.ts`
  - `mipeluqueria-saas-web/src/schemas/clientes.schema.ts`
  - `mipeluqueria-saas-web/src/schemas/turnos.schema.ts`
  - `mipeluqueria-saas-web/src/schemas/stock.schema.ts`
  - `mipeluqueria-saas-web/src/schemas/ventas.schema.ts`
  - `mipeluqueria-saas-web/src/schemas/common.schema.ts`
  - `mipeluqueria-saas-web/src/schemas/index.ts`
  - `mipeluqueria-saas-web/src/hooks/queryKeys.ts`
  - `mipeluqueria-saas-web/src/hooks/useApiFeedback.ts`
  - `mipeluqueria-saas-web/src/hooks/useDomainQueries.ts`
  - `mipeluqueria-saas-web/src/hooks/index.ts`
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/components/ui/DesignSystem.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/pos/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/clientes/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/caja/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/calendario/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/peluquero/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/dashboard/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/inventario/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/staff-liquidaciones/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/configuracion-seguridad/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/diagnostico-api/page.tsx`
  - `mipeluqueria-saas-web/src/app/(public)/reservar/page.tsx`
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **Esquemas Zod (`src/schemas/*.ts`):** Se espejaron todas las reglas de `FluentValidation` del Backend (`loginSchema`, `registroClienteSchema`, `solicitarOtpSchema`, `restablecerPasswordOtpSchema`, `cambiarPasswordSchema`, `crearUsuarioStaffSchema`, `clienteSchema`, `fichaTecnicaSchema`, `peluqueroSchema`, `servicioSchema`, `configurarRecetaServicioSchema`, `crearTurnoSchema`, `reprogramarTurnoSchema`, `bloqueoHorarioSchema`, `crearProductoSchema`, `actualizarProductoSchema` sin `stockActual`, `movimientoStockSchema`, `proveedorSchema` con `cuit`, `crearOrdenCompraSchema`, `recepcionarOrdenCompraSchema`, `aperturaCajaSchema`, `movimientoCajaManualSchema`, `cierreCajaSchema`, `crearVentaPosSchema` con validación de equilibrio financiero y `idempotencyKey`, `guardarComisionConfigSchema`, `generarLiquidacionSchema`, `catalogoItemSchema`, `configuracionSalonSchema` y `crearSucursalSchema`).
  - **Hooks TanStack Query v5 y Feedback Unificado (`src/hooks/*.ts`):**
    - `queryKeys.ts`: implementa las 16+ claves jerárquicas canónicas de la Sección 2 de `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`.
    - `useApiFeedback.ts`: centraliza el contrato de experiencia de usuario de la Sección 3 de `10`: `notifyApiSuccess` emite `toast.success(res.message)` e itera sobre `res.warnings` disparando `toast.warning(w, { duration: 8000 })`; `notifyApiError` y `useApiMutation` inyectan `fieldErrors` en `react-hook-form` (`setError`), invalidan automáticamente las queries afectadas ante un `409 Conflict` (`DbUpdateConcurrencyException`) y notifican `403 Forbidden` / `429 Too Many Requests`.
    - `useDomainQueries.ts`: expone los hooks `useQuery` y `useMutation` tipados con invalidación cruzada automática para todos los dominios.
  - **Reparación y Conexión de las 19 Pantallas (`src/app/`):**
    - **`recepcion/pos/page.tsx`:** Reconstruido íntegramente (había sido sobrescrito en el repositorio original con una copia de `caja/page.tsx`), implementando pre-carga automática por `?turnoId={id}` (`GET /api/turnos/{id}`), visualización y deducción de `"Seña Online Abonada: -$X"` cuando `turno.senaPagada === true`, multipago con bloqueador visual hasta que $\text{Restante a Pagar} = \$0.00$, gestión de `idempotencyKey` (`crypto.randomUUID()`) que se preserva ante caídas de red y se regenera tras `200 OK`, comprobante térmico (`PrintReceiptModal`) y anulación de ventas de la sesión.
    - **`recepcion/clientes/page.tsx`:** Reconstruido íntegramente (había sido sobrescrito con una copia de `admin/dashboard/page.tsx`), implementando el padrón paginado de clientes, insignia de `esCumpleaneroDelMes`, CRUD completo y modal de Fichas Técnicas Capilares con `notasSensibilidadAlergias` y subida Direct-to-Cloud a Cloudinary (`uploadImageToCloudinary`).
    - **`recepcion/caja/page.tsx`:** Separación explícita en dos tarjetas (**1. Efectivo Físico Esperado en Cajón** leyendo `montoFinalEsperado` vs **2. Recaudación Digital Acumulada** leyendo `totalVentasDigital`), campo `observacionesCierre`, historial paginado e inspección bajo demanda (`GET /api/caja/{id}`).
    - **`recepcion/calendario/page.tsx` y `peluquero/page.tsx`:** Borde lateral con `colorAgendaHex`, modal de reprogramación (`PUT /api/turnos/{id}/reprogramar`), subida de foto a Cloudinary en fichas técnicas y disparo de `toast.warning` ante alertas de insumos de gabinete (`res.warnings`).
    - **`admin/dashboard/page.tsx`, `admin/inventario/page.tsx`, `admin/staff-liquidaciones/page.tsx`, `admin/configuracion-seguridad/page.tsx`, `admin/diagnostico-api/page.tsx` y `(public)/reservar/page.tsx`:** Gráficos `recharts` (`PieChart` para `ingresosPorMedioPago` y `BarChart` para `rankingPeluqueros`), `cuit` en proveedores y `saldoResultante` en el Kardex, botón de anulación de liquidaciones pendientes (`POST /api/liquidaciones/{id}/anular`), ABM completo de los 7 catálogos del salón + alta de staff (`POST /api/usuarios`) + historial de sesiones paginado, y consumo de `DisponibilidadHorarioResponseDto` en el portal público.
  - **Auditorías de cierre ejecutadas:**
    1. `npm --prefix mipeluqueria-saas-web run typecheck` (`npx tsc --noEmit`) -> **0 Errores**.
    2. `Select-String -Path "mipeluqueria-saas-web\src\**\*.ts","mipeluqueria-saas-web\src\**\*.tsx" -Pattern "\bany\b"` -> **0 coincidencias**.
    3. `npm --prefix mipeluqueria-saas-web run build` (`next build`) -> **0 Errores** (24 rutas compiladas y empaquetadas para producción).
- **Sincronización de MD (Spec-Sync):** No requerida (100% alineado con `04_API_CONTRACTS.md`, `05_BUSINESS_RULES.md`, `06_INTEGRATION_AND_ODD_PLAN.md`, `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` y `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`).
- **Resultado Compilación y Build Frontend (`tsc --noEmit` + `next build`):** 0 Errores / 0 Advertencias.

### [TAREA 7.1] - SPEC-SYNC Documental + Mapa AGENTS.md
- **Estado:** COMPLETADA (EN ESPERA DE APROBACIÓN HUMANA)
- **Archivos Creados:**
  - `AGENTS.md` (en la raíz del repositorio, 57 líneas `< 80` líneas)
  - `.gitignore` (en la raíz del repositorio)
- **Archivos Modificados:**
  - `MiPeluqueria.Api/01_SYSTEM_CONSTRAINTS.md`
  - `MiPeluqueria.Api/02_ARCHITECTURE.md`
  - `MiPeluqueria.Api/04_API_CONTRACTS.md`
  - `MiPeluqueria.Api/08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md`
  - `MiPeluqueria.Api/09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`
  - `MiPeluqueria.Api/10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **SPEC-SYNC y Overrides en Sección 2 de `07`:** Se asentaron las 5 decisiones arquitectónicas de la Etapa 7 y se sincronizaron en `01`, `02`, `04`, `08`, `09` y `10`:
    1. Autorización de `MiPeluqueria.Api.Tests.csproj` sin dividir el monolito de producción (`01` Sec. 2.1).
    2. Inclusión oficial de paquetes de Observabilidad y Testing (`Serilog.AspNetCore`, `Serilog.Formatting.Compact`, `Serilog.Sinks.File`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions`, `@playwright/test`) en `01` y `02`.
    3. Unificación de la ruta `/api/configuracion` en `01`, `04`, `08` y `10` (0 referencias residuales a `/api/configuracion-salon`).
    4. Orden actualizado del pipeline HTTP en `02` (`CorrelationIdMiddleware` + `UseSerilogRequestLogging()` + `MapHealthChecks("/health/live")` y `MapHealthChecks("/health/ready")`).
    5. Contrato de `POST /api/telemetry/client-errors` (`ClientErrorReportDto`, `202 Accepted`) y Health Checks en `04`, bloque `"Serilog"` JSON en `09`, y regla de Error Boundaries + frontera de componentes reutilizables `src/components/ui/` (0 imports de dominio) en `01` y `10`.
  - **Mapa Operativo `AGENTS.md` y `.gitignore`:** Creado `AGENTS.md` en la raíz del repositorio (57 líneas) resumiendo el flujo Harness Engineering, la regla de cercado de tests, la política `try-catch`, privacidad Zero-PII en logs y sensores determinísticos obligatorios. Actualizado `.gitignore` excluyendo `MiPeluqueria.Api/Logs/`, `mipeluqueria-saas-web/playwright-report/` y `mipeluqueria-saas-web/test-results/`.
- **Sincronización de MD (Spec-Sync):** Aplicada íntegramente en `01`, `02`, `04`, `08`, `09` y `10`.
- **Sensor Determinístico Ejecutado:**
  1. `Select-String` sobre `01`, `08` y `10` buscando `/api/configuracion-salon` -> **0 coincidencias** (todas unificadas en `/api/configuracion`).
  2. Validación sintáctica del bloque JSON de `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` con `ConvertFrom-Json` -> **JSON válido (OK)**.

### [TAREA 7.2] - Sonda Runtime de 22 Controladores + Erradicación de los 5 Timeouts
- **Estado:** COMPLETADA (EN ESPERA DE REVISIÓN HUMANA)
- **Archivos Creados:**
  - `scripts/probe-api-22.mjs`
- **Archivos Modificados:**
  - `MiPeluqueria.Api/Controllers/ConfiguracionSalonController.cs`
  - `MiPeluqueria.Api/Controllers/ClientesController.cs`
  - `MiPeluqueria.Api/Services/Interfaces/IClienteService.cs`
  - `MiPeluqueria.Api/Services/Implementations/ClienteService.cs`
  - `MiPeluqueria.Api/Repositories/Implementations/ClienteRepository.cs`
  - `MiPeluqueria.Api/Controllers/FichasTecnicasController.cs`
  - `MiPeluqueria.Api/Services/Interfaces/IFichaTecnicaService.cs`
  - `MiPeluqueria.Api/Services/Implementations/FichaTecnicaService.cs`
  - `MiPeluqueria.Api/Repositories/Implementations/FichaTecnicaRepository.cs`
  - `MiPeluqueria.Api/Repositories/Implementations/RecetaServicioRepository.cs`
  - `MiPeluqueria.Api/Repositories/Implementations/ProductoRepository.cs`
  - `MiPeluqueria.Api/Repositories/Implementations/ProveedorRepository.cs`
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **Sensor Determinístico Secuencial (`scripts/probe-api-22.mjs`):** Construido en Node 20+ ESM activando `NODE_TLS_REJECT_UNAUTHORIZED = "0"` únicamente para `https://localhost:7132`, autenticando contra `POST /api/auth/login` (`admin@tesis.com` / `Admin123!`, `X-Tenant-Id: 1`, `X-Sucursal-Id: 1`) y recorriendo de forma estrictamente secuencial los 22 endpoints GET de diagnóstico con umbral duro `< 2000 ms`, objetivo en caliente `< 500 ms` y clasificación de payload (`PagedResult` / `Array` / `Object`).
  - **Diagnóstico y Optimización de Raíz en los 5 Módulos (`Clientes`, `FichasTecnicas`, `RecetasServicio`, `Productos`, `Proveedores`):**
    1. **Desacoplamiento de `Include` y Cortocircuito de `CountAsync` en Paginación (`ClienteRepository`, `ProductoRepository`, `ProveedorRepository`):** Se separó `baseQuery` (`AsNoTracking()`) de los `.Include(...)` para evitar `LEFT JOIN` innecesarios durante `CountAsync`, se acotaron `pageNumber >= 1` y `pageSize` (`1..100`), y se omitió el segundo roundtrip SQL `SELECT COUNT(*)` cuando `pageNumber == 1 && items.Count < pageSize`.
    2. **Eliminación de Funciones No-SARGables (`LOWER(...)`):** Se removieron las conversiones `.ToLower()` en filtros SQL (`Search`, `ExistsEmailAsync`, `ExistsCodigoBarraAsync`, `ExistsByRazonSocialAsync`, `ExistsByCuitAsync`) permitiendo que SQL Server aproveche directamente los índices únicos filtrados con su colación case-insensitive nativa.
    3. **Poda de Joins Innecesarios en `FichaTecnicaRepository` y `AsNoTracking()`:** Se eliminaron `.Include(f => f.Turno)` y `.ThenInclude(TipoCabello/TipoRostro)` que no eran proyectados en `FichaTecnicaResponseDto`, incorporando `.AsNoTracking()` en `GetByIdWithDetailsAsync` y en `RecetaServicioRepository.ExisteConsumoGabinetePorTurnoAsync`.
    4. **Propagación Completa de `CancellationToken`:** Se propagó `CancellationToken ct = default` de punta a punta en `ClientesController`/`IClienteService`/`ClienteService` y `FichasTecnicasController`/`IFichaTecnicaService`/`FichaTecnicaService` para abortar inmediatamente comandos SQL ante desconexiones HTTP y prevenir agotamiento del pool de conexiones.
    5. **Alias de Compatibilidad en `ConfiguracionSalonController`:** Se agregó `[Route("api/configuracion-salon")]` junto a `[Route("api/configuracion")]`.
  - **Nota de Observabilidad Preventiva para Tarea 7.3:** En el log de Kestrel se identificaron advertencias `MultipleCollectionIncludeWarning` (`EventId 20504`) en repositorios con múltiples colecciones (`PeluqueroRepository`, `VentaRepository`, `SesionCajaRepository`, `DashboardRepository`, `TurnoRepository`) que podrán evaluarse con `AsSplitQuery()` en fases subsiguientes si se autoriza su alcance.
- **Sincronización de MD (Spec-Sync):** Firmas opcionales `CancellationToken ct = default` reflejadas en la Sección 4 de este Libro Mayor (`04_API_CONTRACTS.md` no cambia).
- **Sensor Determinístico Ejecutado:**
  1. `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.csproj` -> **0 Errores / 0 Advertencias**.
  2. `node scripts/probe-api-22.mjs` (1ra corrida post-arranque): **22/22 `200 OK`**, promedio `79 ms`, máximo `188 ms`.
  3. `node scripts/probe-api-22.mjs` (2da corrida en caliente post-JIT): **22/22 `200 OK`**, **22/22 `< 500 ms`**, promedio **`27 ms`**, máximo **`75 ms`** (`Clientes: 23 ms`, `FichasTecnicas: 15 ms`, `RecetasServicio: 18 ms`, `Productos: 21 ms`, `Proveedores: 37 ms`).

### [TAREA 7.3] - Observabilidad Backend (Serilog JSON + CorrelationId + Health Checks + Telemetría + Auditoría try-catch) [HARD GATE 5A]
- **Estado:** COMPLETADA (EN ESPERA DE REVISIÓN HUMANA EN HARD GATE 5A)
- **Archivos Creados:**
  - `MiPeluqueria.Api/Middlewares/CorrelationIdMiddleware.cs`
  - `MiPeluqueria.Api/DTOs/Common/ClientErrorReportDto.cs`
  - `MiPeluqueria.Api/Controllers/TelemetryController.cs`
- **Archivos Modificados:**
  - `MiPeluqueria.Api/MiPeluqueria.Api.csproj`
  - `MiPeluqueria.Api/Program.cs`
  - `MiPeluqueria.Api/appsettings.json`
  - `MiPeluqueria.Api/appsettings.Development.json`
  - `MiPeluqueria.Api/Middlewares/GlobalExceptionMiddleware.cs`
  - `MiPeluqueria.Api/Middlewares/TenantResolutionMiddleware.cs`
  - `MiPeluqueria.Api/Services/Implementations/AuthService.cs`
  - `MiPeluqueria.Api/Services/Implementations/DashboardService.cs`
  - `MiPeluqueria.Api/Services/Implementations/TurnoService.cs`
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **Serilog Estructurado y Sinks Rotativos Diarios:** Se instalaron `Serilog.AspNetCore` (`8.0.3`), `Serilog.Formatting.Compact` (`3.0.0`), `Serilog.Sinks.File` (`6.0.0`) y `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` (`8.0.8`). Se configuraron en `appsettings.json` y `appsettings.Development.json` los 3 sinks: Consola, `Logs/app-.log` (`Information`, 14 días) y `Logs/errors-.json` (`Warning+` con `CompactJsonFormatter`, 30 días), fijando `Directory.SetCurrentDirectory(builder.Environment.ContentRootPath)` en `Program.cs` para garantizar que los archivos se escriban siempre dentro de `MiPeluqueria.Api/Logs/`.
  - **Estabilidad del Pool SQL Server (`MultipleActiveResultSets=false`):** Gracias al nuevo sink `Logs/errors-.json`, se detectó que `MultipleActiveResultSets=true` provocaba bloqueos intermitentes de 14s en la fase `[Post-Login]` (`TdsParserStateObject.ReadSniSyncOverAsync` en `CompleteLogin`) al negociar sesiones SMUX durante el arranque concurrente. Se ajustó `MultipleActiveResultSets=false;Pooling=true;Min Pool Size=1;Max Pool Size=100;Connect Timeout=30` en `DefaultConnection` (dado que el sistema tiene 0 lazy loading y 0 data readers anidados).
  - **Trazabilidad y Health Checks:**
    - `CorrelationIdMiddleware` propaga `X-Correlation-Id` en `HttpContext.TraceIdentifier`, `LogContext`, `IDiagnosticContext` y el header de respuesta (expuesto además en CORS `WithExposedHeaders("X-Correlation-Id")`).
    - `TenantResolutionMiddleware` exime `/health*` y `/swagger*` del bloqueo multi-tenant e inyecta `TenantId`, `SucursalId` y `UserId` en `LogContext` e `IDiagnosticContext`.
    - Mapeados `GET /health/live` (`Predicate = _ => false`) y `GET /health/ready` (`AddDbContextCheck<ApplicationDbContext>`).
  - **Telemetría Frontend Zero-PII (`POST /api/telemetry/client-errors`):** `TelemetryController` valida `<= 4 KB`, sanitiza patrones de JWT, emails y secretos con expresiones regulares compiladas, registra el evento con `Source = "Frontend"` en Serilog y responde `202 Accepted`.
  - **Auditoría `try-catch` y `public partial class Program`:** Confirmados **0 bloques `try-catch`** en los 23 controladores. En `AuthService.cs` se eliminaron 2 bloques `try-catch` vacíos redundantes alrededor de `_emailService` (ya encapsulados en `SmtpEmailService`), y en `DashboardService.cs` y `TurnoService.cs` se reemplazó `try { TimeZoneInfo.FindSystemTimeZoneById(...) } catch` por `TimeZoneInfo.TryFindSystemTimeZoneById(...)` nativo de .NET 8. Se expuso `public partial class Program { }` al final de `Program.cs`.
- **Sincronización de MD (Spec-Sync):** Completada previamente en Tarea 7.1 (`01`, `02`, `04`, `09`).
- **Sensor Determinístico Ejecutado (HARD GATE 5A):**
  1. `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.csproj` -> **0 Errores / 0 Advertencias**.
  2. `GET /health/live` -> **`200 OK` (`Healthy`)** con header `X-Correlation-Id: corr-health-live-73`.
  3. `GET /health/ready` -> **`200 OK` (`Healthy`)** con header `X-Correlation-Id: 9a1b00b5-4c86-44ea-a43e-0b0dfdb9c826`.
  4. `POST /api/telemetry/client-errors` -> **`202 Accepted`** con `{"success":true,"message":"Reporte de error registrado.","data":{"accepted":true,"correlationId":"fe-crash-corr-20261124-001","source":"Frontend"}}`.
  5. Verificación física de `MiPeluqueria.Api/Logs/errors-20261124.json` -> línea JSON válida con `"Source":"Frontend"`, `"CorrelationId":"fe-crash-corr-20261124-001"`, `"TenantId":1`, `"SucursalId":1`.
  6. `node scripts/probe-api-22.mjs` -> **22/22 `200 OK`**, **22/22 `< 500 ms`** (promedio `128 ms`, máx `472 ms`).

### [TAREA 7.4] - Proyecto MiPeluqueria.Api.Tests + Tests BDD 01 al 05 y Contract Tests 01–11 (Cercados)
- **Estado:** COMPLETADA (EN ESPERA DE REVISIÓN HUMANA)
- **Archivos Creados:**
  - `MiPeluqueria.Api.Tests/MiPeluqueria.Api.Tests.csproj`
  - `MiPeluqueria.Api.Tests/CustomWebApplicationFactory.cs`
  - `MiPeluqueria.Api.Tests/BddTests01To05.cs`
  - `MiPeluqueria.Api.Tests/ContractTests01To11.cs`
- **Archivos Modificados:**
  - `MiPeluqueria.Api/MiPeluqueria.Api.sln`
  - `MiPeluqueria.Api/Program.cs` (resolución de rutas relativas de Serilog contra `ContentRootPath` sin mutar `Directory.GetCurrentDirectory`, preservando `MvcTestingAppManifest.json` en `WebApplicationFactory`)
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **`CustomWebApplicationFactory` y Base de Datos Aislada (`MiPeluqueriaDB_Tests`):** Configura `ApplicationDbContext` exclusivamente contra `MiPeluqueriaDB_Tests` (con guardia de seguridad que lanza excepción si el nombre difiere), aplica las migraciones reales de EF Core en `CreateHost` y ejecuta limpieza SQL determinista de datos transaccionales preservando el Seeding maestro.
  - **Tests BDD 01 al 05 Cercados (`BddTests01To05.cs`):**
    1. `Test01_MultiTenantIsolation_And_CrossSucursalCache_403`: Verifica `403 Forbidden` ante `X-Sucursal-Id: 999` y prioridad absoluta del claim JWT `TenantId = 1` sobre el header malicioso `X-Tenant-Id: 2`.
    2. `Test02_RateLimiting_429_And_RoleEscalationBlocked_RolId4`: Verifica que `POST /api/auth/registro` enviando `rolId: 1` persista `RolId = 4` (Cliente) en SQL Server, y que la 6ta petición consecutiva a `POST /api/auth/login` retorne `429 Too Many Requests`.
    3. `Test03_ZeroReflection_Turnos_HorariosDescanso_FichasTecnicas`: Verifica persistencia del descanso `13:00-14:00` excluyendo `"13:00"` y `"13:30"` en `/api/turnos/disponibilidad`, sellado exacto de `precioHistorico = 12000.00` y `duracionMinutosHistorica = 30` en `GET /api/turnos/{id}`, y persistencia íntegra de `formulaAplicada`, `observaciones`, `notasSensibilidadAlergias`, `productosUtilizados` y `fotoResultadoUrl` en `GET /api/fichastecnicas/cliente/1`.
    4. `Test04_ConfiguracionSalon_PersistsInSqlServer`: Verifica que `PUT /api/configuracion` persista en SQL Server e invalide la caché en `GET /api/configuracion`.
    5. `Test05_SerializableTurnos_And_RowVersion_409Conflict`: Verifica prevención de doble reserva simultánea bajo `IsolationLevel.Serializable` y respuesta `409 Conflict` ante colisión de `RowVersion` (`DbUpdateConcurrencyException`).
  - **Contract Tests 01 al 11 (`ContractTests01To11.cs`):** Validan `200 OK` y deserialización tipada `ApiResponse<T>` en los primeros 11 controladores (`Auth`, `Usuarios`, `Catalogos`, `Clientes`, `FichasTecnicas`, `Peluqueros`, `Servicios`, `RecetasServicio`, `Bloqueos`, `Turnos`, `PortalReservas`).
- **Sensor Determinístico Ejecutado:**
  - `dotnet test MiPeluqueria.Api.Tests --no-build --verbosity normal` -> **16/16 PASS (`100% PASS, 0 FAILED, 0 SKIPPED`)** en `15.91 s`.

### [TAREA 7.5] - Tests BDD 06 al 10 + Contract Tests 12–22 + Observabilidad Tests (Cercados) [HARD GATE 5B]
- **Estado:** COMPLETADA (EN ESPERA DE REVISIÓN HUMANA EN HARD GATE 5B)
- **Archivos Creados:**
  - `MiPeluqueria.Api.Tests/BddTests06To10.cs`
  - `MiPeluqueria.Api.Tests/ContractTests12To22.cs`
  - `MiPeluqueria.Api.Tests/ObservabilityTests.cs`
- **Archivos Modificados:**
  - `MiPeluqueria.Api.Tests/CustomWebApplicationFactory.cs` (ampliación de limpieza determinista para `ComisionesConfig`, `Productos`, `Peluqueros` de test)
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **Tests BDD 06 al 10 Cercados (`BddTests06To10.cs`) — 0 modificaciones necesarias en producción:**
    1. `Test06_WebhookHmacSha256_And_DeathCrossCompensation`: Firma inválida rechazada (`401`), firma `HMAC-SHA256` válida en Escenario A reactiva turno cancelado a `Confirmado (2)` con `SenaPagada = true`, `ReferenciaPagoExterno` e historial `5 -> 2`; Escenario B con slot ocupado mantiene `Cancelado (5)`, marca `SenaPagada = true` (`5000.00`), agrega alerta crítica en `Observaciones` y retorna `200 OK`.
    2. `Test07_PosIdempotencyKey_SenaDeduction_PhysicalVsDigitalCash`: Caja abierta con `$10.000`, servicio `$20.000` con seña abonada `$5.000`, cobro POS digital `$15.000` con `X-Idempotency-Key` -> `Subtotal = 20000`, `DescuentoSenaAplicado = 5000`, `Total = 15000`, herencia de `PeluqueroId = 1`, turno en `Completado (4)`, reenvío idempotente retorna misma venta sin duplicar, y `GET /api/caja/estado-actual` confirma `totalVentasDigital = 15000`, `totalVentasEfectivo = 0` y `montoFinalEsperado = 10000`.
    3. `Test08_GabineteConsumption_NonBlockingWarnings_And_Kardex`: Receta de `2` unid. con stock `1` descuenta hasta `0`, devuelve `200 OK` con `Warnings`, segunda invocación es idempotente (`data == 0`) y `GET /api/productos/{id}/kardex` devuelve `PagedResult` con `saldoResultante = 0`.
    4. `Test09_CommissionHierarchy_And_AccountingVoidProtection`: Jerarquía `60%` específica (`$6.000`) + `40%` general (`$4.000`) = `$10.000`, rechazo `400 Bad Request` al anular venta en liquidación activa o caja cerrada, y `POST /api/liquidaciones/{id}/anular` marca `DetallesLiquidacion.IsDeleted = true`.
    5. `Test10_BloqueosOverlapWarnings_And_SmtpFailSafe`: Bloqueo `15:00-18:00` solapado con turno `16:00` guarda con `Warnings` detallados; `POST /api/auth/olvide-password` con `Smtp:Host = ""` completa `< 5000 ms` devolviendo `200 OK` y `Warnings`.
  - **Contract Tests 12 al 22 (`ContractTests12To22.cs`):** Validan `200 OK` y `ApiResponse<T>` / `PagedResult<T>` en `Productos`, `Proveedores`, `OrdenesCompra`, `Caja`, `Ventas`, `Comisiones`, `Liquidaciones`, `ConfiguracionSalon` (`/api/configuracion`), `Dashboard`, `Sucursales` y `Suscripciones`.
  - **Observability Tests (`ObservabilityTests.cs`):** Validan `GET /health/live` (`200 Healthy` + `X-Correlation-Id`), `GET /health/ready` (`200 Healthy`) y `POST /api/telemetry/client-errors` (`202 Accepted` con redacción Zero-PII verificada en log `[REDACTED_EMAIL]`).
- **Sensor Determinístico Ejecutado (HARD GATE 5B):**
  - `dotnet build MiPeluqueria.Api.Tests` -> **0 Errores / 0 Advertencias**.
  - `dotnet test MiPeluqueria.Api.Tests --no-build --verbosity normal` -> **35/35 PASS (`100% PASS, 0 FAILED, 0 SKIPPED`)**.

### [TAREA 7.6] - Kit UI Reutilizable Agnóstico + Error Boundaries + Telemetría Frontend
- **Estado:** COMPLETADA (EN ESPERA DE REVISIÓN HUMANA)
- **Archivos Creados:**
  - `mipeluqueria-saas-web/src/lib/errorLogger.ts`
  - `mipeluqueria-saas-web/src/app/error.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/error.tsx`
  - `mipeluqueria-saas-web/src/app/global-error.tsx`
  - `mipeluqueria-saas-web/src/components/ui/ErrorFallbackCard.tsx`
  - `mipeluqueria-saas-web/src/components/ui/AsyncStateGuard.tsx`
  - `mipeluqueria-saas-web/src/components/ui/DataTable.tsx`
  - `mipeluqueria-saas-web/src/components/ui/PaginationBar.tsx`
  - `mipeluqueria-saas-web/src/components/ui/ConfirmModal.tsx`
  - `mipeluqueria-saas-web/src/components/ui/index.ts`
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/lib/axios.ts`
  - `mipeluqueria-saas-web/src/context/AppProviders.tsx`
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **Telemetría Zero-PII (`src/lib/errorLogger.ts`):** `reportClientError` envía reportes inesperados hacia `${NEXT_PUBLIC_API_URL}/telemetry/client-errors` priorizando `navigator.sendBeacon` con fallback a `fetch(..., { keepalive: true })`, redactando JWTs/emails/credenciales antes de serializar, truncando stack traces (`<= 2400` caracteres, payload `< 4 KB`) y aplicando cortocircuito anti-bucle (ventana de deduplicación de `10s`, exclusión explícita de `/telemetry/client-errors` y cero reintentos).
  - **Propagación de `X-Correlation-Id` (`src/lib/axios.ts`):** Generación de UUID v4 (`crypto.randomUUID` con fallback seguro) en cada request saliente y extracción de `X-Correlation-Id` de respuesta hacia `ApiErrorPayload.correlationId`.
  - **Listeners Globales (`src/context/AppProviders.tsx`):** Registro con cleanup en `useEffect` para `window.error` y `window.unhandledrejection` (ignorando rechazos HTTP de negocio `< 500` ya gestionados por TanStack Query / `sonner` y reportando `>= 500` o errores nativos de ejecución).
  - **Error Boundaries Jerárquicos (`app/error.tsx`, `app/(dashboard)/error.tsx`, `app/global-error.tsx`):** Captura aislada en App Router (`'use client'`) delegando a `reportClientError` y renderizando `ErrorFallbackCard` con opción `reset()` sin tumbar el Sidebar en `(dashboard)` e incluyendo `<html>`/`<body>` propios en `global-error.tsx`.
  - **Kit UI 100% Agnóstico de Dominio (`src/components/ui/*`):** `ErrorFallbackCard`, `AsyncStateGuard`, `DataTable<T>`, `PaginationBar`, `ConfirmModal` y Barrel File `index.ts` documentados con JSDoc (`@param`, `@example`), composables (`children`, `className`, genéricos `<T>`) y con `0` dependencias hacia servicios, hooks o tipos de negocio.
- **Sensores Determinísticos Ejecutados:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` -> **`0` errores (`EXIT 0`)**.
  2. `Select-String -Path "mipeluqueria-saas-web\src\components\ui\*.tsx" -Pattern "services/|useDomainQueries|clientes\.types|turnos\.types|ventas\.types|stock\.types|saas\.types"` -> **`0` coincidencias**.
  3. Auditoría `\bany\b` en `mipeluqueria-saas-web/src/**/*.{ts,tsx}` -> **`0` coincidencias**.

### [TAREA 7.7] - Barrido Global Frontend (`PagedResult<T>`, Null-Safety y Diagnóstico en Lotes)
- **Estado:** COMPLETADA (EN ESPERA DE REVISIÓN HUMANA)
- **Archivos Creados:**
  - `mipeluqueria-saas-web/src/lib/apiHelpers.ts`
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/services/clientes.service.ts`
  - `mipeluqueria-saas-web/src/services/common.service.ts`
  - `mipeluqueria-saas-web/src/services/public.service.ts`
  - `mipeluqueria-saas-web/src/services/saas.service.ts`
  - `mipeluqueria-saas-web/src/services/seguridad.service.ts`
  - `mipeluqueria-saas-web/src/services/stock.service.ts`
  - `mipeluqueria-saas-web/src/services/turnos.service.ts`
  - `mipeluqueria-saas-web/src/services/ventas.service.ts`
  - `mipeluqueria-saas-web/src/hooks/useDomainQueries.ts`
  - `mipeluqueria-saas-web/src/app/(dashboard)/layout.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/clientes/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/calendario/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/pos/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/caja/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/peluquero/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/dashboard/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/inventario/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/servicios-recetas/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/staff-liquidaciones/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/configuracion-seguridad/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/diagnostico-api/page.tsx`
  - `mipeluqueria-saas-web/src/app/(dashboard)/saas-admin/page.tsx`
  - `mipeluqueria-saas-web/src/app/(public)/reservar/page.tsx`
  - `mipeluqueria-saas-web/src/app/(public)/mi-cuenta/turnos/page.tsx`
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **Causa Raíz de Desfase `PagedResult<T>` Resuelta (`src/lib/apiHelpers.ts`):** El DTO `PagedResult<T>` de .NET 8 serializa `{ data: T[], totalRecords: number, pageNumber, pageSize, totalPages }`, mientras el Frontend esperaba `{ items: T[], totalCount: number }`, lo que provocaba `clientes === undefined` (`Cannot read properties of undefined (reading 'length')` en `recepcion/clientes/page.tsx:374`) y listas vacías al recargar Proveedores, Productos, Kardex, Órdenes de Compra, Turnos, Historial de Caja, Ventas de Sesión, Liquidaciones e Historial de Sesiones. Se crearon `extractPagedItems<T>`, `extractPaginationMeta` y `toNormalizedPagedResult<T>` soportando ambas estructuras y arreglos planos, retornando siempre `T[]` seguro.
  - **Blindaje de los 8 Servicios y `useDomainQueries.ts`:** Todos los endpoints paginados y de colección desempaquetan con `toNormalizedPagedResult<T>` y `extractPagedItems<T>`.
  - **Null-Safety Integral y Adopción de `<AsyncStateGuard />` / `<PaginationBar />`:** Se eliminaron accesos desprotegidos a `.length`/`.map()`/`.filter()`, se cerraron bloques `try...finally` sin `catch` que podían causar `unhandledrejection`, y se estandarizaron los estados de carga, error (con `X-Correlation-Id` y reintento) y colección vacía en las pantallas principales.
  - **Consola de Diagnóstico en Lotes (`admin/diagnostico-api/page.tsx`):** Refactorizada para ejecutar los 22 controladores en lotes controlados de **4 requests en paralelo** (`BATCH_SIZE = 4`) con progreso en vivo, clasificación de forma de payload (`PagedResult` / `Array` / `Object`) y tarjeta superior de Health Checks consumiendo `GET /health/live` y `GET /health/ready` sobre el host raíz sin `/api`.
- **Sensores Determinísticos Ejecutados:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` -> **`0` errores (`TYPECHECK_EXIT=0`)**.
  2. `Select-String -Path "mipeluqueria-saas-web\src\**\*.ts","mipeluqueria-saas-web\src\**\*.tsx" -Pattern "\bany\b"` -> **`0` coincidencias (`EXACT_SELECT_STRING_ANY=0`)**.
  3. `npm --prefix mipeluqueria-saas-web run build` -> **`0` errores (`BUILD_EXIT=0`, `24/24` rutas compiladas para producción)**.

### [TAREA 7.8] - Harness E2E con Playwright (Crawler 19 Rutas + 3 Flujos Críticos de Dinero + Login Único `storageState`) [HARD GATE 5C]
- **Estado:** COMPLETADA (EN ESPERA DE REVISIÓN HUMANA EN HARD GATE 5C)
- **Archivos Creados:**
  - `mipeluqueria-saas-web/playwright.config.ts`
  - `mipeluqueria-saas-web/e2e/auth.setup.ts`
  - `mipeluqueria-saas-web/e2e/smoke-19-routes.spec.ts`
  - `mipeluqueria-saas-web/e2e/critical-journeys.spec.ts`
- **Archivos Modificados:**
  - `.gitignore` (inclusión de `mipeluqueria-saas-web/e2e/.auth/`)
  - `mipeluqueria-saas-web/package.json` (`@playwright/test` + script `test:e2e`)
  - `MiPeluqueria.Api/Middlewares/TenantResolutionMiddleware.cs` (single-flight con `ConcurrentDictionary<string, SemaphoreSlim>` para eliminar cache stampede en `EsSucursalValidaYActivaAsync`)
  - `MiPeluqueria.Api/Middlewares/GlobalExceptionMiddleware.cs` (manejo limpio HTTP `499` ante cancelaciones explícitas del cliente vía `context.RequestAborted`)
  - `MiPeluqueria.Api/Services/Implementations/AuthService.cs` (evitar `UPDATE Usuarios` + `AuditLog` innecesarios cuando `IntentosFallidos == 0 && BloqueadoHasta == null` y consolidar `HistorialSesion` + `RefreshToken` en un único `CommitAsync()`)
  - `MiPeluqueria.Api/Services/Background/RecordatorioTurnosBackgroundService.cs` (retardo inicial de `15 s` para evitar contención en arranque, pre-filtro ligero por IDs antes de `Include(Cliente).Include(Peluquero)` y reemplazo de `try-catch` por `TimeZoneInfo.TryFindSystemTimeZoneById`)
  - `MiPeluqueria.Api/Repositories/Implementations/TurnoRepository.cs` (paginación `GetPagedAsync` por IDs sin `Include`s + carga de página con `.AsSingleQuery()`, eliminando el timeout de `20 s` en `PopulateSplitIncludeCollectionAsync`)
  - `MiPeluqueria.Api/Program.cs` (`sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` para evitar explosión cartesiana)
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Diagnóstico de Causa Raíz y Correcciones Aplicadas:**
  - **Evidencia Real en Logs (`Logs/errors-20261124.json`):** El `500` de `30748.6597 ms` en `POST /api/auth/login` (`TenantResolutionMiddleware.cs:line 159`) correspondió a `SqlException (-2): Execution Timeout Expired` en `INSERT INTO [AuditLogs]` / `UPDATE [Usuarios]` (`AuthService.LoginAsync` ejecutaba `Update(user)` y dos `CommitAsync()` por login), seguido de otro timeout de `33188.872 ms` en `SELECT TOP(1) ... FROM [Usuarios]` y de `35028 ms` en `RecordatorioTurnosBackgroundService`.
  - **Causa Raíz en SQL Server 2025 Express:** Ambas bases (`MiPeluqueriaDB` y `MiPeluqueriaDB_Tests`) tenían `AUTO_CLOSE = ON`, y la instancia SQL Server tenía `max server memory (MB) = 2147483647` sin cota, con el 75% de su memoria paginada a disco (`WorkingSet = 61.7 MB`, `9.6M` page faults), provocando stalls de `45.5 s` en consultas de 0 filas. Se configuró `AUTO_CLOSE OFF` en ambas bases y `min/max server memory = 128/512 MB` (reduciendo la consulta de `45.5 s` a `0 ms`).
  - **Blindaje de Concurrencia y Rendimiento Backend:** Single-flight en `TenantResolutionMiddleware`, un único `CommitAsync()` sin lock de `Usuarios` en `AuthService.LoginAsync`, arranque diferido + pre-filtro por IDs en `RecordatorioTurnosBackgroundService`, y paginación por IDs + `AsSingleQuery()` en `TurnoRepository.GetPagedAsync` (erradicando el timeout de `20 s` en `GET /api/turnos` durante `/admin/diagnostico-api`).
  - **Refactor Playwright a Login Único (`storageState`):** `e2e/auth.setup.ts` inicia sesión una única vez con `admin@tesis.com` / `Admin123!`, espera hidratación cliente y persiste `e2e/.auth/admin.json`. `playwright.config.ts` ejecuta el proyecto `setup` y encadena `chromium` con `storageState`, evitando múltiples logins contra `StrictAuthOtpPolicy` (5 req/min). **Journey 2 (`/reservar`)** corre explícitamente sin sesión (`test.use({ storageState: { cookies: [], origins: [] } })`). Se corrigieron exclusivamente los selectores estrictos (`inputByLabel` con `following-sibling::input[1]`, tarjetas `div.bg-white` en Caja, `CAJA CERRADA` exacto y `div.border-t` en POS) manteniendo intactas todas las aserciones y sensores `pageerror`/`console.error`/`HTTP >= 500` (Regla de Cercado).
- **Sensores Determinísticos Ejecutados (HARD GATE 5C):**
  1. `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.sln --no-restore --warnaserror` -> **`0 Errores / 0 Advertencias`**.
  2. `dotnet test MiPeluqueria.Api.Tests --no-build --verbosity normal` -> **`35/35 PASS` (`0 FAILED, 0 SKIPPED`) en `17.92 s`**.
  3. `npm --prefix mipeluqueria-saas-web run typecheck` + auditoría `\bany\b` (`src/**` + `e2e/**`) -> **`0 errores` / `0 ocurrencias de any`**.
  4. `npm --prefix mipeluqueria-saas-web run build` -> **`0 errores` (`24/24` rutas compiladas)**.
  5. Servidores levantados (`GET https://localhost:7132/health/live` -> `200 Healthy`, `GET https://localhost:7132/health/ready` -> `200 Healthy`, `http://localhost:3000/login` -> `200`) + `npm --prefix mipeluqueria-saas-web run test:e2e`:
     - `[setup] e2e/auth.setup.ts` -> **PASS (`2.7 s`)**
     - `Journey 1 (Proveedores post-reload + Cliente CRM 360° + Ficha Técnica post-reload)` -> **PASS (`8.7 s`)**
     - `Journey 3 (Flujo del Dinero: Caja $10.000 -> Turno -> POS Digital Idempotente -> Arqueo Físico Intacto -> Dashboard BI)` -> **PASS (`5.3 s`)**
     - `Journey 2 (Portal Público /reservar sin sesión)` -> **PASS (`13.0 s`)**
     - `Smoke Crawler E2E (19/19 pantallas SDD + /admin/diagnostico-api OK: 22/22, ERROR: 0)` -> **PASS (`5.2 s`)**
     - **Total:** **`5 passed (37.6 s)`, `0 failed`, `0 skipped`**, **`0` errores `[ERR]` en stdout de Backend** y puertos `7132`/`3000` verificados limpios (`OPEN_PORTS=0`) tras el apagado.

### [TAREA 7.9] - Pipeline CI/CD (GitHub Actions) y Cierre Definitivo Etapa 7 — Release Candidate `v1.0.0`
- **Estado:** COMPLETADA — **ETAPA 7 CERRADA AL 100% (`v1.0.0`)**
- **Archivos Creados:**
  - `.github/workflows/ci.yml`
- **Archivos Modificados:**
  - `MiPeluqueria.Api.Tests/CustomWebApplicationFactory.cs` (`TestConnectionString` prioriza `Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")` para soportar el contenedor SQL Server 2022 en `ubuntu-latest` conservando el fallback local `Trusted_Connection=True` hacia `MiPeluqueriaDB_Tests`)
  - `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Cambios o Decisiones Especiales:**
  - **Pipeline de Integración Continua (`.github/workflows/ci.yml`):** Disparado en `push` y `pull_request` sobre `develop` y `main`, con cancelación automática de corridas redundantes (`concurrency`), permisos mínimos (`contents: read`) y 2 jobs independientes en paralelo:
    1. **`backend-ci` (`ubuntu-latest`):** Aprovisiona contenedor de servicio `mcr.microsoft.com/mssql/server:2022-latest` (`ACCEPT_EULA: "Y"`, `MSSQL_PID: "Developer"`, puerto `1433` y healthcheck nativo `sqlcmd`), instala `.NET 8.0.x` (`actions/setup-dotnet@v4`), restaura dependencias (`dotnet restore MiPeluqueria.Api/MiPeluqueria.Api.sln`), compila con tolerancia cero a advertencias (`dotnet build MiPeluqueria.Api/MiPeluqueria.Api.sln --no-restore --configuration Release --warnaserror`) y ejecuta la suite cercada (`dotnet test MiPeluqueria.Api.Tests/MiPeluqueria.Api.Tests.csproj --no-build --configuration Release --verbosity normal`) inyectando `ConnectionStrings__DefaultConnection` hacia `MiPeluqueriaDB_Tests`.
    2. **`frontend-ci` (`ubuntu-latest`):** Configura Node.js `20` (`actions/setup-node@v4` con cache `npm` sobre `mipeluqueria-saas-web/package-lock.json`), ejecuta `npm --prefix mipeluqueria-saas-web ci`, `npm --prefix mipeluqueria-saas-web run typecheck`, auditoría determinista en Bash verificando `0` ocurrencias de `\bany\b` en `mipeluqueria-saas-web/src` y `0` imports de dominio (`services/`, `useDomainQueries`, `*.types`) en `mipeluqueria-saas-web/src/components/ui`, y finalmente compila la aplicación productiva (`npm --prefix mipeluqueria-saas-web run build` con `NEXT_PUBLIC_API_URL=https://localhost:7132/api`).
- **Tabla Consolidada de Métricas Reales Verificadas — Release Candidate `v1.0.0`:**

| Dimensión / Sensor Determinístico | Comando / Herramienta | Resultado Real Verificado | Umbral / Meta SDD |
| :--- | :--- | :--- | :--- |
| **Compilación Solución Backend (.NET 8)** | `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.sln --warnaserror` | **`0 Errores / 0 Advertencias`** | `0 Errores / 0 Advertencias` |
| **Sonda Runtime Secuencial Backend** | `node scripts/probe-api-22.mjs` | **`22/22` controladores `200 OK`** (`27 ms` prom. en caliente, máx. `75 ms`) | `22/22` `200 OK` y `< 500 ms` en caliente |
| **Suite Integración & Contratos Backend** | `dotnet test MiPeluqueria.Api.Tests` | **`35/35 PASS` en `17.92 s`** (`10` BDD + `22` Contratos + `3` Observabilidad, `0 FAIL`, `0 SKIP`) | `100% PASS / 0 FAIL / 0 SKIP` |
| **Observabilidad, Correlación & Salud** | Serilog JSON (`CompactJsonFormatter`) + `/health/live` & `/health/ready` + `/api/telemetry/client-errors` | **Activos (`200 Healthy` / `202 Accepted`)**, `X-Correlation-Id` end-to-end, Zero-PII y **`0` errores en Serilog** | Logs estructurados Zero-PII + Health `200` |
| **Tipado Estricto & Build Frontend (Next.js 14)** | `npm run typecheck` + `npm run build` | **`0 Errores` TypeScript / `24/24` rutas compiladas / `0 any`** | `0 Errores` y `0 any` |
| **Frontera Agnóstica Design System UI** | Auditoría sobre `mipeluqueria-saas-web/src/components/ui/*` | **`0` imports de dominio** (`services/`, `useDomainQueries`, `*.types`) + JSDoc completo | `0` dependencias de negocio |
| **Harness E2E Frontend + Backend Vivo** | `npm --prefix mipeluqueria-saas-web run test:e2e` (Playwright Chromium) | **`5/5 PASS` en `37.6 s`** (`auth.setup` + `19/19` rutas SDD + `22/22` consola API + `3/3` Journeys críticos, `0 FAIL`, `0 SKIP`) | `19/19` rutas + `3/3` Journeys verdes |
| **Pipeline CI/CD Automatizado** | `.github/workflows/ci.yml` (`yaml.parseDocument` strict) | **YAML válido (`0` errores / `0` warnings)** — `backend-ci` + `frontend-ci` en paralelo | Sintaxis válida y gates bloqueantes |

- **Sensores Determinísticos Ejecutados (Cierre Tarea 7.9):**
  1. Validación sintáctica estricta YAML (`yaml.parseDocument` con `strict: true` sobre `.github/workflows/ci.yml`) -> **`YAML_PARSE_OK` (`0` errores, `0` warnings)**.
  2. `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.sln --no-restore --warnaserror -m:1` -> **`0 Errores / 0 Advertencias` (`EXIT 0`)**.
  3. `npm --prefix mipeluqueria-saas-web run typecheck` -> **`0 Errores` (`EXIT 0`)**.
  4. Auditoría `Select-String` en `src/**/*.{ts,tsx}` y `src/components/ui/*` -> **`AUDIT_ANY_COUNT=0 | AUDIT_UI_DOMAIN_IMPORTS=0`**.
  5. Verificación de limpieza de entorno -> **`0` puertos colgados (`7132`/`3000` libres) y `0` archivos temporales sueltos**.

### [TAREA 8.1] - SPEC-SYNC + Tokens Studio Clean (Claro/Oscuro) + Tipografía + Kit UI Base
- **Estado:** COMPLETADA
- **Archivos Creados:**
  - `mipeluqueria-saas-web/tailwind.config.ts`
  - `mipeluqueria-saas-web/src/context/ThemeProvider.tsx`
  - `mipeluqueria-saas-web/src/lib/semanticColors.ts`
  - `mipeluqueria-saas-web/src/components/ui/NumericInput.tsx`
  - `mipeluqueria-saas-web/src/components/ui/BrandLogo.tsx`
  - `mipeluqueria-saas-web/src/components/ui/SearchCombobox.tsx`
  - `mipeluqueria-saas-web/src/components/ui/ThemeToggle.tsx`
- **Archivos Modificados:**
  - `AGENTS.md` (referencia explícita al plan activo Etapa 8 `docs/12_...`)
  - `docs/10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (SPEC-SYNC Sección 6: tokens duales, tipografía, mapa semántico y viewport base `1280x720`/`1366x768` + móvil `390x844`)
  - `mipeluqueria-saas-web/src/app/globals.css` (3 capas CSS Custom Properties `:root` *Studio Clean* y `.dark` *Obsidian & Indigo* + `@media print` limpio en 1 hoja)
  - `mipeluqueria-saas-web/src/app/layout.tsx` (`Plus_Jakarta_Sans` + `JetBrains_Mono` vía `next/font/google`, `suppressHydrationWarning` y script inline anti-FOUC con default `'light'`)
  - `mipeluqueria-saas-web/src/context/AppProviders.tsx` (integración de `ThemeProvider` y `<Toaster />` sincronizado con el tema activo)
  - `mipeluqueria-saas-web/src/components/ui/DesignSystem.tsx` (4 variantes estrictas de `Button`: `primary`, `secondary`, `destructive` [`solid`/`soft`], `ghost`, alturas `h-9`/`h-10`, contraste WCAG 2.2 AA dual y compatibilidad legacy temporal para `editorial`/`danger`)
  - `mipeluqueria-saas-web/src/components/ui/index.ts` (re-exportación completa del kit UI y tipos)
  - `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:**
  - `mipeluqueria-saas-web/tailwind.config.js` (reemplazado limpiamente por `tailwind.config.ts`)
- **Decisiones Especiales:**
  - **Cero Regresiones en Pantallas Existentes:** Se auditaron los usos de `Button` en todo `src/` (`11` usos de `editorial` y `3` de `danger` que serán migrados en Tareas 8.4–8.9), manteniendo alias temporales tipados hacia `primary` y `destructive` (`solid`) para que el 100% de las 19 pantallas compile sin errores.
  - **Frontera UI 100% Agnóstica:** Todos los componentes nuevos (`NumericInput`, `BrandLogo`, `SearchCombobox`, `ThemeToggle`) y `DesignSystem.tsx` cuentan con JSDoc (`@param`, `@example`), composición limpia y `0` imports de dominio.
- **Sensores Determinísticos Ejecutados:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` -> **`0` errores (`TYPECHECK_EXIT=0`)**.
  2. Auditoría `\bany\b` en `mipeluqueria-saas-web/src/**/*.{ts,tsx}` -> **`0` coincidencias (`AUDIT_ANY_COUNT=0`)**.
  3. Auditoría de frontera agnóstica en `mipeluqueria-saas-web/src/components/ui/*.{ts,tsx}` (`services/|useDomainQueries|\.types`) -> **`0` coincidencias (`AUDIT_UI_DOMAIN_IMPORTS=0`)**.
  4. `npm --prefix mipeluqueria-saas-web run build` -> **`0` errores (`24/24` rutas compiladas con `Plus Jakarta Sans` + `JetBrains Mono`)**.

### [TAREA 8.2] - Fixes Críticos Backend #1 (Cálculo de `HistorialCajas` Abierta + Rango Semanal de Turnos) + Tests xUnit
- **Estado:** COMPLETADA
- **Archivos Creados:**
  - `MiPeluqueria.Api.Tests/Etapa8BackendFixesTests.cs`
- **Archivos Modificados:**
  - `MiPeluqueria.Api/Repositories/Implementations/SesionCajaRepository.cs` (`GetHistorialCajasPagedAsync` y `GetHistorialCajasAsync` incorporan `.AsSplitQuery()` con carga de `MovimientosCaja` y `Ventas -> Pagos -> MedioPago` solo sobre la página solicitada y normalización de borde final `23:59:59.9999999` cuando `FechaHasta` tiene `TimeOfDay == TimeSpan.Zero`)
  - `MiPeluqueria.Api/Services/Implementations/CajaService.cs` (`ObtenerHistorialCajasAsync` recalcula `TotalVentasEfectivo`, `TotalVentasDigital`, `TotalIngresosManuales`, `TotalEgresosManuales` y `MontoFinalEsperado` mediante `CalcularTotalesColecciones` para sesiones abiertas o con movimientos/ventas activas, garantizando coincidencia exacta al centavo con `GET /api/caja/estado-actual` y `GET /api/caja/{id}`)
  - `MiPeluqueria.Api/Repositories/Implementations/TurnoRepository.cs` (`GetPagedAsync` aplica filtro por `FechaDesde` y `FechaHasta` cuando no se envía `fecha` puntual, expandiendo `FechaHasta` con `TimeOfDay == TimeSpan.Zero` hasta `Date.AddDays(1).AddTicks(-1)` para incluir íntegramente el último día de la semana y preservando el 100% del comportamiento cuando se envía `fecha`)
  - `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Decisiones Especiales:**
  - **Cero Ruptura de Contratos API:** Se mantuvieron intactos todos los nombres de propiedades de `SesionCajaResponseDto`, `TurnoResponseDto`, `PaginationFilterDto` y las rutas HTTP vigentes.
  - **Causa Raíz Resuelta en Historial de Cajas:** En `ObtenerHistorialCajasAsync`, la lectura previa con `recalcularDesdeColecciones: false` exponía acumuladores desfasados en sesiones abiertas cuando concurrían cobros POS y egresos de órdenes de compra. Al cargar las colecciones de la página con `.AsSplitQuery()` y aplicar `CalcularTotalesColecciones`, los 3 endpoints (`GET /api/caja/estado-actual`, `GET /api/caja/{id}` y `GET /api/caja/historial`) ejecutan exactamente la misma fórmula de la Regla 4.1 (`05_BUSINESS_RULES.md`).
- **Sensores Determinísticos Ejecutados:**
  1. `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.sln --no-restore --warnaserror -m:1 -p:UseSharedCompilation=false` -> **`0 Errores / 0 Advertencias` (`11.25 s`)**.
  2. `dotnet test MiPeluqueria.Api.Tests/MiPeluqueria.Api.Tests.csproj --no-build --verbosity normal` -> **`37/37 PASS` (`35` previos + `2` nuevos en `Etapa8BackendFixesTests`, `0 FAIL`, `0 SKIP` en `49.66 s`)**.

### [TAREA 8.3] - Fixes Críticos Backend #2 (Liquidaciones Duplicadas, `Pagar`, `PeluqueroNombre` en Comisiones) + Rol Real `SuperAdminSaaS` (`RolId = 6`)
- **Estado:** COMPLETADA — 🛑 **HARD GATE 6A ALCANZADO**
- **Archivos Creados:**
  - `MiPeluqueria.Api/Migrations/20261125014902_AddSuperAdminSaaSRole.cs`
  - `MiPeluqueria.Api/Migrations/20261125014902_AddSuperAdminSaaSRole.Designer.cs`
- **Archivos Modificados:**
  - `MiPeluqueria.Api/Repositories/Implementations/LiquidacionRepository.cs` (`GetDetallesVentaPendientesLiquidacionAsync` normaliza bordes horarios `00:00:00`..`23:59:59.9999999` y consulta `_context.DetallesLiquidacion.IgnoreQueryFilters()` acotado por `TenantId` para impedir que cualquier renglón incluido en una liquidación activa `Pendiente`, `Pagado` o `Atrasado` vuelva a liquidarse, cubriendo también registros históricos por `TurnoId`; `GetPagedAsync` normaliza `FechaHasta` inclusiva)
  - `MiPeluqueria.Api/Services/Implementations/LiquidacionService.cs` (`LiquidarPeriodoAsync` opera bajo `IsolationLevel.Serializable` deduplicando `itemsPendientes` por `Id` y retornando `400 Bad Request` con `"No existen ventas pendientes de liquidar para este profesional en el período seleccionado."` cuando no hay nuevos renglones; `RegistrarPagoLiquidacionAsync` soporta pago con o sin imputación en caja abierta actualizando `Estado = Pagado` + `FechaPago = DateTime.UtcNow` y recalculando acumuladores de `SesionCaja` sin ensuciar el grafo de navegación de EF Core; eliminación de `try-catch` de flujo esperado conforme a `AGENTS.md` §3)
  - `MiPeluqueria.Api/Controllers/LiquidacionesController.cs` (`POST /api/liquidaciones/{id:int}/pagar` normaliza `sesionCajaId <= 0` a `null` permitiendo pago limpio con o sin caja abierta)
  - `MiPeluqueria.Api/DTOs/Ventas/ComisionConfigDto.cs` (incorporación estrictamente aditiva de `PeluqueroNombre` y `ServicioNombre` preservando `NombrePeluquero` y `NombreServicio`)
  - `MiPeluqueria.Api/Services/Implementations/ComisionConfigService.cs` (`ObtenerTodasAsync`, `ObtenerPorPeluqueroAsync` y `MapToDto` pueblan `PeluqueroNombre` y `ServicioNombre` con fallback batch de profesionales)
  - `MiPeluqueria.Api/Data/Context/ApplicationDbContext.cs` (`HasData` agrega `Rol { Id = 6, Nombre = "SuperAdminSaaS" }` y usuario semilla `superadmin@mipeluqueria.com` / `"SuperAdmin123!"` con hash BCrypt fijo verificado y `seedDate = 2026-01-01T00:00:00Z`)
  - `MiPeluqueria.Api/Migrations/ApplicationDbContextModelSnapshot.cs`
  - `MiPeluqueria.Api/Controllers/SuscripcionesController.cs` (`POST /api/suscripciones/cambiar-plan` restringido exclusivamente a `[Authorize(Roles = "SuperAdminSaaS")]`, manteniendo `GET mi-suscripcion` para `Administrador,SuperAdminSaaS` y `GET planes` `[AllowAnonymous]`)
  - `MiPeluqueria.Api.Tests/CustomWebApplicationFactory.cs` (preservación determinista del usuario semilla `superadmin@mipeluqueria.com` y del esquema semilla `ComisionesConfig` `Id = 1` de Carlos Gómez `50% / 10%` en `MiPeluqueriaDB_Tests`)
  - `MiPeluqueria.Api.Tests/Etapa8BackendFixesTests.cs` (3 nuevos tests cercados de regresión para Liquidaciones sin duplicados + Pago con/sin caja, `PeluqueroNombre == "Carlos Gómez"` en Comisiones, y RBAC `403`/`200` de `SuperAdminSaaS` con login real)
  - `docs/03_DATA_MODEL.md`, `docs/04_API_CONTRACTS.md`, `docs/09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Decisiones Especiales y Hallazgos de Auditoría:**
  - **Causa Raíz en `Comisiones` (`Peluquero #1` y `55% / 0%` en `/peluquero`):** El DTO backend exponía únicamente `nombrePeluquero` y `nombreServicio`, mientras que `ventas.service.ts` leía `peluqueroNombre` y `servicioNombre`, provocando el fallback `Peluquero #1`. En `/peluquero`, los valores `55% / 0%` provenían de una edición previa sobre la fila #1 de `ComisionesConfig` en `MiPeluqueriaDB` (afectada además por el `0` pegajoso del antiguo input numérico en `% Venta Productos`); la semilla oficial (`50.00%` servicios / `10.00%` productos) quedó blindada en `ComisionConfigService` y `Etapa8BackendFixesTests`.
  - **Causa Raíz en `POST /api/liquidaciones/{id}/pagar`:** La llamada previa a `_unitOfWork.SesionesCaja.ActualizarSesionCaja(cajaAbierta)` y `_unitOfWork.Liquidaciones.Update(liq)` invocaba `_dbSet.Update(...)` sobre entidades ya trackeadas con colecciones incluidas (`Ventas`, `Pagos`, `MediosPago`, `Peluquero`, `DetallesLiquidacion`), marcando todo el grafo como `Modified`. Al mutar únicamente las propiedades escalares de las entidades ya trackeadas y recalcular los totales desde las colecciones activas de `SesionCaja`, el pago con o sin caja abierta persiste de forma limpia y consistente al centavo.
  - **Migración `20261125014902_AddSuperAdminSaaSRole` Idempotente y Libre de Colisiones:** El método `Up` verifica existencia previa de `RolId = 6` y `superadmin@mipeluqueria.com`, e inserta con `Id = 2` cuando está libre o usa el próximo `IDENTITY` disponible si `Id = 2` ya estuviera ocupado por un usuario previo en `MiPeluqueriaDB`. Se validó ciclo completo `Down` + `Up` en `MiPeluqueriaDB_Tests`. Para impactar `MiPeluqueriaDB` (una vez cerradas conexiones/transacciones abiertas en SSMS o API local):
    `dotnet ef database update --project MiPeluqueria.Api --startup-project MiPeluqueria.Api`
- **Sincronización de MD (Spec-Sync):** Actualizados `docs/03_DATA_MODEL.md`, `docs/04_API_CONTRACTS.md` y `docs/09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` con `RolId = 6 (SuperAdminSaaS)`, usuario semilla `superadmin@mipeluqueria.com`, RBAC de `SuscripcionesController` y propiedades aditivas de `ComisionConfigDto`.
- **Sensores Determinísticos Ejecutados:**
  1. `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.sln --no-restore --warnaserror -m:1 -p:UseSharedCompilation=false` -> **`0 Errores / 0 Advertencias` (`10.30 s`)**.
  2. Verificación de migración `Down` + `Up` (`20261125014902_AddSuperAdminSaaSRole`) sobre `MiPeluqueriaDB_Tests` -> **`EXIT 0` (`Done.`)**.
  3. `dotnet test MiPeluqueria.Api.Tests/MiPeluqueria.Api.Tests.csproj --no-build --verbosity normal` -> **`40/40 PASS` (`35` previos + `2` Tarea 8.2 + `3` Tarea 8.3, `0 FAIL`, `0 SKIP`)**.

### [TAREA 8.4] - App Shell Compacto (`1280x720` / `1366x768`): Sidebar Agrupado, Topbar, Switch Claro/Oscuro y Caja con Fecha
- **Estado:** COMPLETADA (FASE 8B iniciada tras aprobación de HARD GATE 6A)
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/app/(dashboard)/layout.tsx` (rediseño integral del App Shell sobre Design System Dual *Studio Clean* / *Obsidian & Indigo*: Sidebar compacto `w-56` / `224px` en desktop con drawer colapsable en `< lg`, `<BrandLogo />` con placa clara para logo negro y badge discreto de Rol sin `TENANT #1` ni prefijos numéricos `1..10`, navegación agrupada con íconos `lucide-react` 16px en *Mi Espacio*, *Operación*, *Gestión* y *Plataforma* exclusivo `SuperAdminSaaS`, footer con indicador semántico `Caja #N • DD/MM` / `Caja Cerrada` con fecha real `fechaApertura`, Topbar fino `h-14` con breadcrumb de sección, fecha del día del salón en español `Mar, 24 Nov 2026`, selector compacto de sucursal y `<ThemeToggle />`, área principal `min-w-0 flex-1 overflow-x-hidden px-4 py-4 lg:px-6 lg:py-5` garantizando `~1056px` libres y `0px` de desborde horizontal a `1280x720`, y modal de cambio de contraseña con copywriting humano libre de jerga técnica)
  - `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Decisiones Especiales:**
  - El formateo de fecha del salón en el Topbar se ejecuta en cliente (`useEffect`) con `Intl.DateTimeFormat('es-AR')` y fallback seguro ante identificadores Windows de zona horaria (`TimeZoneId`), evitando desajustes de hidratación SSR y excepciones `RangeError`.
  - No se requirieron cambios en `DesignSystem.tsx`, preservando compatibilidad 100% con las vistas existentes y manteniendo la frontera agnóstica intacta.
- **Sensores Determinísticos Ejecutados:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`tsc --noEmit`) -> **`0` errores (`EXIT 0`)**.
  2. Auditoría `\bany\b` en `mipeluqueria-saas-web/src/**` -> **`ANY_MATCHES=0`**.
  3. Auditoría frontera agnóstica (`services/|useDomainQueries|\.types`) en `mipeluqueria-saas-web/src/components/ui/*.tsx` -> **`UI_DOMAIN_IMPORTS=0`**.
  4. `npm --prefix mipeluqueria-saas-web run build` (`next build`) -> **`0` errores (`24/24` páginas estáticas generadas, Middleware `26.8 kB`, `EXIT 0`)**.

### [TAREA 8.5] - Mi Puesto Profesional (`/peluquero`) + Calendario Master (`/recepcion/calendario`)
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/services/turnos.service.ts` (agregado `turnosService.getByRange(fechaDesde, fechaHasta, peluqueroId, estadoId, clienteId)` consumiendo `GET /api/turnos?fechaDesde=...&fechaHasta=...` consolidado en Tarea 8.2, sin alterar firmas previas)
  - `mipeluqueria-saas-web/src/hooks/useDomainQueries.ts` (extensión retrocompatible de `useTurnosAgendaQuery` para soportar consulta por rango `fechaDesde`/`fechaHasta` además de `fecha` puntual)
  - `mipeluqueria-saas-web/src/app/(dashboard)/peluquero/page.tsx` (rediseño integral sobre Design System Dual *Studio Clean* / *Obsidian & Indigo* y `semanticColors.ts`: cabecera sin textos técnicos ni prefijos numéricos, *Mi Agenda* con segmented control `Hoy | Mañana | Esta semana` + datepicker + timeline vertical `09:00–20:00` con franja sombreada de descanso laboral, bloqueos horarios, línea de hora actual y tarjetas proporcionales + vista semanal Lunes a Sábado; *Fichas Técnicas Capilares* en layout Master-Detail responsive y mobile-first `390x844` con buscador instantáneo, lista vertical con avatares e iniciales, alternancia lista/detalle con `← Volver a clientes`, banner rojo destacado de alergias y `+ Nueva Ficha Técnica` siempre accesible; *Comisiones y Recibos* mostrando porcentajes reales con `font-mono-tabular` y recibo imprimible; *Horarios y Ausencias* con jerarquía estricta de botones y copywriting limpio)
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/calendario/page.tsx` (rediseño integral estilo Fresha: switch `Día | Semana`, navegación `Hoy` + `<` / `>` + datepicker + filtro de estado sin prefijos numéricos + filtro por profesional en vista semanal; Vista Día con grilla horaria vertical `09:00–20:00` en slots de 30 min clicables para agendar preseleccionando profesional y horario, columnas por profesional con avatar y `colorAgendaHex`, descansos y bloqueos sombreados, línea de hora actual y tarjetas proporcionales con modal de acciones rápidas *Confirmar*, *En proceso*, *Completar*, *Cancelar*, *Cobrar en POS*, *Reprogramar* e *Historial*; modal *Nuevo Turno* usando `<SearchCombobox />` para clientes con acción integrada a Registro Express y las 4 variantes estrictas de botón)
  - `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Decisiones Especiales:**
  - Todas las fechas de agenda y rangos semanales se calculan en hora local (`toLocalDateIso` / `parseLocalDateIso`) para evitar saltos de día por desfase UTC en horarios nocturnos locales, y el posicionamiento de tarjetas en la grilla vertical resuelve solapamientos en carriles paralelos sin deformar columnas.
  - En Vista Día de `/recepcion/calendario`, la grilla multi-columna mantiene `overflow-x-auto` confinado dentro del contenedor del calendario cuando haya muchos estilistas, garantizando `0px` de desborde horizontal en el documento a `1280x720` y `1366x768`.
- **Sensores Determinísticos Ejecutados:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`tsc --noEmit`) -> **`0` errores (`EXIT 0`)**.
  2. Auditoría `\bany\b` en `mipeluqueria-saas-web/src/**` -> **`ANY_MATCHES=0`**.
  3. Auditoría frontera agnóstica (`services/|useDomainQueries|\.types`) en `mipeluqueria-saas-web/src/components/ui/*.tsx` -> **`UI_DOMAIN_IMPORTS=0`**.
  4. `npm --prefix mipeluqueria-saas-web run build` (`next build`) -> **`0` errores (`24/24` páginas estáticas generadas, `/peluquero` `16.4 kB`, `/recepcion/calendario` `12.6 kB`, `EXIT 0`)**.

### [TAREA 8.6] - Punto de Venta POS (`/recepcion/pos`), Caja Diaria (`/recepcion/caja`), Clientes CRM (`/recepcion/clientes`) y `PrintReceiptModal.tsx`
- **Estado:** COMPLETADA — 🛑 **HARD GATE 6B ALCANZADO**
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/components/ui/PrintReceiptModal.tsx` (rediseño agnóstico de ticket/acta con `<BrandLogo />`, tabla tabular `font-mono-tabular`, JSDoc completo y portal dedicado `#mipeluqueria-print-portal` en `document.body` con `@media print` estricto ocultando `body > *:not(#mipeluqueria-print-portal)` para imprimir en **1 sola hoja** sin borde de modal, título de vista previa, botón `X`, botones ni sidebar)
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/pos/page.tsx` (header limpio con badge semántico `Caja del DD/MM/YYYY (#N)` y sin textos técnicos de `Idempotency-Key` en UI manteniendo 100% intacta la lógica interna de `idempotencyKey`, `<NumericInput />` sin `0` pegajoso, atajos de cobro coloreados con `semanticColors.ts` — Efectivo esmeralda / Digital azul cielo —, jerarquía estricta de 4 variantes de botón con único `primary` para emitir ticket, y layout 60/40 adaptado al Design System Dual con `0px` de desborde horizontal a `1280x720`)
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/caja/page.tsx` (header limpio sin fórmula matemática ni `CajaController`, 2 tarjetas KPI con colores semánticos fijos Esmeralda = Efectivo en Cajón y Azul Cielo = Recaudación Digital, `<NumericInput />` en apertura, movimientos manuales y arqueo físico, tabla de historial mostrando `Sesión #N — DD/MM/YYYY` con `<PaginationBar />` real sin scroll interno atrapado y modal de inspección sobre tokens duales)
  - `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/clientes/page.tsx` (header limpio `"Clientes"` + badge total + único botón `primary` `+ Nuevo Cliente` sin textos de controladores, tabla con avatares de iniciales, badges suaves y acciones suavizadas con `destructiveTone="soft"`, `<PaginationBar />` real y modal de Fichas Técnicas con banner destacado de alergias, vista previa de foto e indicador de estado Cloudinary activo vs modo demo local con URL manual)
  - `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Decisiones Especiales:**
  - En `PrintReceiptModal.tsx` se utilizó `createPortal(..., document.body)` montado solo tras hidratación (`useEffect`) junto con una regla `@media print` acotada a `body > *:not(#mipeluqueria-print-portal) { display: none !important; }`, garantizando 1 sola hoja limpia sin necesidad de tocar `globals.css` ni `DesignSystem.tsx` (respetando estrictamente la lista blanca de TAREA 8.6).
- **Sensores Determinísticos Ejecutados:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`tsc --noEmit`) -> **`0` errores (`EXIT 0`)**.
  2. Auditoría `\bany\b` en `mipeluqueria-saas-web/src/**` -> **`ANY_MATCHES=0`**.
  3. Auditoría frontera agnóstica (`services/|useDomainQueries|\.types`) en `mipeluqueria-saas-web/src/components/ui/*.tsx` -> **`UI_DOMAIN_IMPORTS=0`**.
  4. `npm --prefix mipeluqueria-saas-web run build` (`next build`) -> **`0` errores (`24/24` páginas estáticas generadas, `/recepcion/pos` `11.1 kB`, `/recepcion/caja` `5.5 kB`, `/recepcion/clientes` `10.3 kB`, Middleware `26.8 kB`, `EXIT 0`)**.

### [TAREA 8.7] - Dashboard Ejecutivo BI (`/admin/dashboard`) + Inventario, Proveedores y OC Pro (`/admin/inventario`)
- **Estado:** COMPLETADA (FASE 8C iniciada tras aprobación de HARD GATE 6B)
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/dashboard/page.tsx` (rediseño sobre Design System Dual *Studio Clean* / *Obsidian & Indigo*: filtros rápidos `Hoy | Esta semana | Este mes` en hora local + datepickers compactos y único CTA `primary` `Actualizar`; 6 tarjetas KPI sin prefijos numéricos `1.`–`6.` con íconos semánticos y cifras en `font-mono-tabular`; coherencia cromática absoluta vía `getPaymentMethodColor(nombre, theme)` entre Donut Chart y barras horizontales de *Ingresos por Medio de Pago*; gráfico de barras *Ranking por Estilista* pintado con `colorAgendaHex` por profesional o `--brand-primary` y tooltips legibles en ambos temas; alerta de stock crítico estilizada sin fórmulas técnicas y acceso directo a `/admin/inventario`)
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/inventario/page.tsx` (rediseño Opción 2 Pro sobre Design System Dual sin prefijos numéricos ni jerga técnica: pestaña *Productos* con buscador instantáneo por nombre/código, filtro por categoría, toggle `Solo stock crítico`, `<PaginationBar />`, renombrado `Kardex` -> `Historial` con tooltip auditado, `<NumericInput />` en precios/stock y reemplazo de `"CAMPO PROTEGIDO POR KARDEX"` por badge `"Se ajusta desde Movimientos"` con acceso directo al modal de movimiento; pestaña *Proveedores* con buscador por razón social/CUIT/contacto y badge clicable `N artículo(s)` que abre modal de productos vinculados consultando `GET /api/productos?proveedorId={id}`; pestaña *Órdenes de Compra* con modal multi-ítem con columnas `Producto / Cantidad / Costo Unit. / Subtotal`, `+ Agregar renglón` y total en vivo, acción `Generar OC con faltantes` consultando `GET /api/productos/stock-critico` agrupado por proveedor para precarga en 1 clic, vista previa/PDF en 1 hoja vía `PrintReceiptModal` y acción `WhatsApp` vía `https://wa.me/{telefono}?text=...`; confirmaciones destructivas con `<ConfirmModal />`)
  - `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Decisiones Especiales:**
  - En `/admin/dashboard` se cruza `peluquerosService.getAll()` en paralelo (con fallback silencioso) para colorear cada barra y fila del ranking de estilistas con su `colorAgendaHex` real, manteniendo `0` cambios en contratos API.
  - En `/admin/inventario`, cuando `Generar OC con faltantes` detecta artículos críticos de un único proveedor precarga directamente el modal de nueva OC; si existen múltiples proveedores, despliega un selector agrupado por proveedor para emitir cada pedido en 1 clic.
- **Sensores Determinísticos Ejecutados:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`tsc --noEmit`) -> **`0` errores (`EXIT 0`)**.
  2. Auditoría `\bany\b` en `mipeluqueria-saas-web/src/**` -> **`ANY_MATCHES=0`**.
  3. Auditoría frontera agnóstica (`services/|useDomainQueries|\.types`) en `mipeluqueria-saas-web/src/components/ui/*.tsx` -> **`UI_DOMAIN_IMPORTS=0`**.
  4. `npm --prefix mipeluqueria-saas-web run build` (`next build`) -> **`0` errores (`24/24` páginas estáticas generadas, `/admin/dashboard` `115 kB`, `/admin/inventario` `14.5 kB`, Middleware `26.8 kB`, `EXIT 0`)**.

### [TAREA 8.8] - Servicios/Recetas (`/admin/servicios-recetas`) + Staff, Comisiones y Liquidaciones (`/admin/staff-liquidaciones`)
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/services/ventas.service.ts` (`normalizeComision` prioriza `peluqueroNombre` con fallback seguro a `nombrePeluquero` / `peluqueroNombreCompleto` y `servicioNombre` / `nombreServicio`; `liquidacionesService.pagar` acepta `sesionCajaId?: number | null` y omite el query param cuando es `null` o `<= 0` alineado al contrato de Tarea 8.3)
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/servicios-recetas/page.tsx` (rediseño Opción 1 sobre Design System Dual *Studio Clean* / *Obsidian & Indigo* sin prefijos numéricos ni textos técnicos/rutas `POST /api/...` ni jerga de `Kardex`: Catálogo con buscador, filtro por categoría, `<NumericInput />` en precio/duración y `<ConfirmModal />` para baja; Editor de Recetas con encabezados explícitos `Insumo / Unid. a descontar / Medida técnica / Unidad`, `<NumericInput />` sin `0` pegajoso, **Margen Neto Real** en esmeralda junto al **porcentaje de rentabilidad** en `font-mono-tabular`; acordeón colapsado `"Ajuste manual de consumo por turno"` reemplazando el input numérico crudo por `<SearchCombobox />` cargado con turnos recientes)
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/staff-liquidaciones/page.tsx` (rediseño Opción 1 sobre Design System Dual con 4 variantes estrictas de botón y copywriting humano: Profesionales con avatar `colorAgendaHex` y chips colapsables de especialidades `+N más` con popover ordenado evitando desbordes a `1280x720`; Esquemas de Comisión mostrando nombre real del profesional, fila en edición resaltada y `<NumericInput />` en porcentajes sin `(PUT)`; Liquidaciones con bloqueo `isLoading` en `"Calcular y Emitir Liquidación"` contra doble emisión, fix de **`Recibo PDF`** normalizando fechas ISO con milisegundos de SQL Server antes de abrir `PrintReceiptModal` en 1 hoja, y fix de **`Pagar`** mediante `<ConfirmModal />` con opción para imputar el egreso en la Caja Abierta actual o marcar pagada fuera de caja invalidando queries de `liquidaciones` y `caja` + `refrescarEstadoCaja`)
  - `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Decisiones Especiales:**
  - Se respetó estrictamente la lista blanca de TAREA 8.8 definida en `docs/12`, dejando `/admin/configuracion-seguridad`, `/saas-admin`, `/admin/diagnostico-api`, `middleware.ts` y `formatters.ts` para **TAREA 8.9** (confirmado por el usuario).
  - La normalización segura de fechas de SQL Server (`normalizeSafeIsoForReceipt` y `formatSafeDateOnly`) se implementó localmente en `staff-liquidaciones/page.tsx` para recortar fracciones de 7 dígitos (`.1234567`) y evitar corrimientos UTC-3 sin tocar archivos fuera de la lista blanca.
- **Sensores Determinísticos Ejecutados:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`tsc --noEmit`) -> **`0` errores (`TYPECHECK_EXIT=0`)**.
  2. Auditoría `\bany\b` en `mipeluqueria-saas-web/src/**` -> **`ANY_MATCHES=0`**.
  3. Auditoría frontera agnóstica (`services/|useDomainQueries|\.types`) en `mipeluqueria-saas-web/src/components/ui/*.tsx` -> **`UI_DOMAIN_IMPORTS=0`**.
  4. `npm --prefix mipeluqueria-saas-web run build` (`next build`) -> **`0` errores (`24/24` páginas estáticas generadas, `/admin/servicios-recetas` `9.87 kB`, `/admin/staff-liquidaciones` `12.5 kB`, Middleware `26.8 kB`, `BUILD_EXIT=0`)**.

### [TAREA 8.9] - Configuración/Seguridad (`/admin/configuracion-seguridad`) + Separación `SuperAdminSaaS` (`/saas-admin`, `/admin/diagnostico-api`)
- **Estado:** COMPLETADA
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `mipeluqueria-saas-web/src/middleware.ts` (restringidas `/saas-admin` y `/admin/diagnostico-api` exclusivamente al rol `SuperAdminSaaS` con precedencia de prefijo sobre `/admin`, redirigiendo a `Administrador` a `/admin/dashboard`)
  - `mipeluqueria-saas-web/src/lib/formatters.ts` (nuevo helper puro `formatUserAgentSummary` que traduce strings User-Agent a etiquetas legibles como `Chrome · Windows`, `Safari · iOS`, `Cliente Node`, `Playwright · Automatizado`)
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/configuracion-seguridad/page.tsx` (rediseño Opción 1 sobre Design System Dual *Studio Clean* / *Obsidian & Indigo* con 4 variantes estrictas de botón y `font-mono-tabular`: Ajustes del Salón con `<NumericInput />`; Catálogos Maestros sin textos técnicos `(CRUD Completo)/(POST)/(PUT)`, resaltado de fila activa con borde `--brand-primary` + badge `"Editando"`, foco automático y `<ConfirmModal />` para bajas; Usuarios y Sesiones con modal *Crear Cuenta de Personal* blindado contra autofill de Chrome vía `autoComplete="off"` / `autoComplete="new-password"` y desplegable de profesionales existentes en reemplazo de `ID LEGAJO PELUQUERO`; Auditoría de Accesos sin scroll interno atrapado con `<PaginationBar />` real de 10 eventos/página y `formatUserAgentSummary` con tooltip del User-Agent completo; nueva pestaña `"Mis Sucursales y Plan"` con gestión de sucursales y vista de plan actual estrictamente en solo lectura para `Administrador`)
  - `mipeluqueria-saas-web/src/app/(dashboard)/saas-admin/page.tsx` (eliminada pestaña mockeada `AuditLogs JSON Diff`, limitado `Simulador Webhooks` a `process.env.NODE_ENV === 'development'`, migración completa a tokens duales *Studio Clean* / *Obsidian & Indigo*, `SEMANTIC_COLORS`, `font-mono-tabular`, `<NumericInput />` y 4 variantes de botón sin scroll horizontal a `1280x720`)
  - `mipeluqueria-saas-web/src/app/(dashboard)/admin/diagnostico-api/page.tsx` (conservada 100% la sonda en lotes x4 de los 22 controladores y los Health Checks `/health/live` + `/health/ready`, adaptada íntegramente a tokens duales, `SEMANTIC_COLORS`, `font-mono-tabular` y 4 variantes de botón sin scroll horizontal a `1280x720`)
  - `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
- **Archivos Eliminados:** Ninguno
- **Decisiones Especiales:**
  - En `middleware.ts`, la regla `'/admin/diagnostico-api': ['SuperAdminSaaS']` se ubicó antes de `'/admin'` para garantizar la evaluación por prefijo más específico y redirigir inmediatamente a `Administrador` hacia `/admin/dashboard`.
  - En `configuracion-seguridad/page.tsx`, los usuarios con rol `Administrador` no pueden asignar ni modificar cuentas con rol `SuperAdminSaaS` (`RolId = 6`) y visualizan su suscripción vigente en modo estrictamente lectura.
- **Sensores Determinísticos Ejecutados:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`tsc --noEmit`) -> **`0` errores (`TYPECHECK_EXIT=0`)**.
  2. Auditoría `\bany\b` en `mipeluqueria-saas-web/src/**` -> **`ANY_MATCHES=0`**.
  3. Auditoría frontera agnóstica (`services/|useDomainQueries|\.types`) en `mipeluqueria-saas-web/src/components/ui/*.tsx` -> **`UI_DOMAIN_IMPORTS=0`**.
  4. `npm --prefix mipeluqueria-saas-web run build` (`next build`) -> **`0` errores (`24/24` páginas estáticas generadas, `/admin/configuracion-seguridad` `11.2 kB`, `/saas-admin` `5.71 kB`, `/admin/diagnostico-api` `6.43 kB`, Middleware `26.8 kB`, `BUILD_EXIT=0`)**.




