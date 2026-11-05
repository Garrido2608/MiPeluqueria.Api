# 07 - LIBRO MAYOR DE ESTADO EN VIVO, BITÁCORA DE CAMBIOS Y MEMORIA PERSISTENTE (CAPA 7 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)
**Propósito Crítico:** Memoria persistente en disco para el Agente de IA. Elimina la dependencia del historial de chat y previene alucinaciones por desfase entre especificaciones y código real.
**Regla de Uso:** El Agente DEBE leer este archivo ANTES de iniciar cualquier tarea y DEBE actualizarlo ANTES de dar por finalizada cualquier tarea.

---

## 1. ESTADO ACTUAL DEL PROYECTO (PUNTERO DE EJECUCIÓN)

* **Última Tarea Completada:** `TAREA 5.1 - Middlewares, Integraciones (HMAC + SMTP Guard), Background Worker, Configuración ODS y Program.cs`
* **Tarea Actual en Curso:** `EN ESPERA DE REVISIÓN HUMANA -> Próxima: TAREA 5.2 - Refactorización de los 22 Controladores, Suite HTTP y Verificación BDD [HARD GATE 3]`
* **Estado de Compilación Backend (`dotnet build`):** `0 Errores / 0 Advertencias`
* **Estado de Migración SQL Server:** `Esquema Fluent API y Seeding compilados y validados (DDL verificado sin FKs sombra).`
* **Estado de Compilación Frontend (`npm run build`):** `En espera hasta superar HARD GATE 3`

---

## 2. REGISTRO DE DESVIACIONES, RENOMBRES Y DECISIONES APROBADAS EN VIVO (OVERRIDE LOG)

> **DIRECTIVA PARA EL AGENTE:** Toda modificación de nombres de propiedades, firmas de métodos, tipos de datos o reglas de negocio que se decida durante la programación DEBE registrarse en esta tabla y sincronizarse en el archivo `01` a `05` correspondiente. **Lo registrado en esta tabla tiene prioridad sobre cualquier texto anterior.**

| Fecha / Tarea | Archivo `.md` Original Afectado | Especificación Original | Nuevo Valor / Cambio Implementado | Motivo Técnico / Autorización |
| :--- | :--- | :--- | :--- | :--- |
| *Inicial* | `03_DATA_MODEL.md` | `ITentantEntity.cs` | `ITenantEntity.cs` + `AuditableTenantEntity.cs` | Corrección de typo e inclusión de herencia universal (`TenantId`, `SucursalId`)[cite: 30, 35]. |
| Tarea 5.1 | `02_ARCHITECTURE.md` (Sección 3.2, pasos 2 y 3) | Validación del par Tenant/Sucursal consultando `ApplicationDbContext.Sucursales` directo desde el middleware; caché solo con `SlidingExpiration = 10 min`. | Consulta vía `IUnitOfWork.SaaS.EsSucursalValidaYActivaAsync(...)`; caché válida con `SlidingExpiration = 10 min` + tope `AbsoluteExpirationRelativeToNow = 60 min`; resultados inválidos cacheados `1 min`. | Regla "0 DbContext directo" (`01` Sección 4.2). El tope absoluto impide que el tráfico continuo mantenga válida para siempre una sucursal desactivada (OWASP A01). **SPEC-SYNC aplicado en `02`.** |
| Tarea 5.1 | `06_INTEGRATION_AND_ODD_PLAN.md` (Tarea 5.1, ítem 5) / `02_ARCHITECTURE.md` (árbol) | `Services/Background/RecordatorioTurnosBackgroundService.cs` | Se conserva la ruta física existente `Services/BackGround/RecordatorioTurnoBackgroundService.cs` (la clase sí se llama `RecordatorioTurnosBackgroundService`, namespace `MiPeluqueria.Api.Services.Background`). | Solo cambia el nombre del archivo, no el contrato. No se renombró para no alterar rutas fuera de la tarea. **Pendiente de decisión del Arquitecto.** |
| Tarea 5.1 | `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (Sección 2) | Sin valor por defecto especificado para `BackgroundJobs:CancelarReservasOnlineImpagas` si falta la clave. | Valor por defecto `false` si la clave no existe (el JSON la declara `true`). La expiración aplica **solo** a turnos con `RequiereSena && !SenaPagada` cuyo campo `Observaciones` empiece con `"[Reserva Online]"`. | `TurnoService.CreateAsync` marca `RequiereSena = true` también en los turnos cargados por el staff; sin este filtro el worker cancelaría turnos presenciales. |

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
  - `IClienteService`: `ObtenerTodosAsync(PaginationFilterDto filter, CancellationToken ct = default)`, `ObtenerTodosAsync(int pageNumber = 1, int pageSize = 15, string? search = null)`, `ObtenerPorIdAsync`, `CrearClienteAsync`, `ActualizarClienteAsync`, `EliminarClienteAsync`.
  - `IFichaTecnicaService`: `ObtenerPorIdAsync(int id, CancellationToken ct = default)`, `ObtenerHistorialClienteAsync(int clienteId)`, `CrearFichaAsync(CrearFichaTecnicaDto dto)`, `ActualizarFichaAsync(int id, ActualizarFichaTecnicaDto dto, CancellationToken ct = default)`, `ActualizarFichaAsync(int id, CrearFichaTecnicaDto dto)`, `EliminarFichaAsync(int id)` (`0 DbContext`, `0 Reflection`).
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
- **Estado:** COMPLETADA (EN REVISIÓN HUMANA)
- **Archivos Creados:** Ninguno
- **Archivos Modificados:**
  - `Middlewares/GlobalExceptionMiddleware.cs`
  - `Middlewares/TenantResolutionMiddleware.cs`
  - `Integrations/Payments/PaymentGatewayService.cs`
  - `Integrations/Email/SmtpEmailService.cs`
  - `Services/BackGround/RecordatorioTurnoBackgroundService.cs` (clase `RecordatorioTurnosBackgroundService`)
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
  - **Nota para Tarea 5.2:** `WebhooksController` hoy pasa `payload.ExternalReference` como `rawBody` y sigue llamando a `CambiarEstadoAsync` en lugar de `ProcesarPagoSenaWebhookAsync`. Además, `PortalReservasController` todavía inyecta `ApplicationDbContext`. Ambos se corrigen en la Tarea 5.2 (fuera del alcance de esta tarea).
- **Sincronización de MD (Spec-Sync):** `02_ARCHITECTURE.md` Sección 3.2 actualizado (consulta vía `IUnitOfWork.SaaS` + políticas de caché). Desviaciones registradas en la Sección 2 de este Libro Mayor.
- **Resultado dotnet build:** 0 Errores / 0 Advertencias.
