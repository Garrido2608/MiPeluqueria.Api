# 06 - PLAN MAESTRO DE INTEGRACIÓN ODD, PROTOCOLO ANTI-ALUCINACIÓN Y ORQUESTACIÓN POR TAREAS (CAPA 6 SDD - v4.0)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)
**Suite Documental Vinculada (Lectura Obligatoria):**
* `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (**MEMORIA VIVA EN DISCO: Leer SIEMPRE en primer lugar y actualizar al cerrar cada tarea**)
* `00_AUDITORIA_Y_DEUDA_TECNICA.md` (Registro de los 10 Bugs Críticos P0/P1)
* `01_SYSTEM_CONSTRAINTS.md` (Leyes inmutables de compilación, stack y prohibiciones)
* `02_ARCHITECTURE.md` (Árbol físico, `IGenericRepository`, `IUnitOfWork.ExecuteInTransactionAsync` y Pipeline)
* `03_DATA_MODEL.md` (Diccionario exacto de las 41 tablas, índices Multi-Tenant y Data Seeding)
* `04_API_CONTRACTS.md` (Especificación de DTOs, paginación `PagedResult<T>` y matriz RBAC de los 22 Controladores)
* `05_BUSINESS_RULES.md` (Fórmulas matemáticas de agenda, caja física/digital, señas, comisiones e idempotencia)
* `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md` (Matriz BDD de los 10 criterios de aceptación innegociables y suite ejecutable `MiPeluqueria.Api.http`)
* `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (Contrato completo de `appsettings.json`, `.env.local`, cortocircuitos defensivos y Runbook de migración SQL Server)
* `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Cola de concurrencia JWT en Axios, matriz de Query Keys, rotación de idempotencia POS y mapeo de las 19 pantallas)

---

## 1. PROTOCOLO INQUEBRANTABLE DE MEMORIA EN DISCO Y SINCRONIZACIÓN BIDIRECCIONAL (`SPEC-SYNC`)

Los agentes de IA sufren degradación de contexto en conversaciones largas. Para garantizar **CERO ALUCINACIONES** desde la Tarea 1.1 hasta la Tarea 6.4, rigen las siguientes **5 Reglas de Hierro**:

1. **REGLA 1 - LECTURA PRIMARIA DEL LIBRO MAYOR (`07`):**
   * Al iniciar cualquier tarea, el primer archivo que el agente DEBE abrir y leer es `07_EXECUTION_LEDGER_AND_CHANGELOG.md` para conocer el estado exacto del repositorio, las firmas reales compiladas en tareas previas, los puentes `[NotMapped]` activos y cualquier cambio de diseño aprobado sobre la marcha.
2. **REGLA 2 - LECTURA FORZADA DE CAPAS SDD/ODD/ODS (`00` a `10`):**
   * Inmediatamente después de leer el archivo `07`, el agente DEBE leer las secciones de los archivos `00` a `10` indicadas en `[LECTURA OBLIGATORIA PREVIA]` de la tarea en curso. Queda prohibido programar de memoria.
3. **REGLA 3 - AISLAMIENTO DE ALCANCE (Lista Blanca de Archivos):**
   * El agente solo puede crear, editar o eliminar en cada paso los archivos enumerados en `[ARCHIVOS PERMITIDOS EN ESTA TAREA]` (más la actualización documental en `07` y en `01`-`10` si hubo cambios autorizados).
4. **REGLA 4 - SINCRONIZACIÓN BIDIRECCIONAL OBLIGATORIA (`SPEC-SYNC`):**
   * Si durante la ejecución de una tarea surge la necesidad técnica (o el Arquitecto ordena) de cambiar el nombre de una propiedad, parámetro, ruta, tabla o regla respecto a lo que decían los archivos `01` a `10`:
     * **a)** El agente debe pedir autorización si el cambio altera contratos o lógica de negocio.
     * **b)** En el mismo acto en que modifica el código `.cs` o `.ts`, el agente **DEBE editar el archivo `01`-`10` correspondiente** para que el documento refleje la nueva realidad, y **DEBE dejarlo asentado en la Sección 2 de `07_EXECUTION_LEDGER_AND_CHANGELOG.md`**. Jamás puede quedar un archivo `.md` contradiciendo al código compilado.
5. **REGLA 5 - ESCRITURA DE CIERRE EN EL LIBRO MAYOR (`07`) Y AUDITORÍA DE CONSOLA:**
   * Ninguna tarea se considera terminada hasta que:
     * `dotnet build` (o `npm run build` en Etapa 6) arroje **0 Errores**.
     * Se ejecuten los comandos de verificación de la tarea.
     * El agente actualice `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (Secciones 1, 2, 3, 4 y 5) dejando documentado todo lo que hizo para que en la siguiente tarea su "yo futuro" tenga el mapa exacto.

---

## 2. MATRIZ DE TRAZABILIDAD DE DEFECTOS (`00_AUDITORIA` <-> TAREAS <-> TESTS `08`)

| ID Defecto | Descripción del Problema Crítico | Tareas donde se Erradica al 100% | Test BDD (`08`) |
| :--- | :--- | :--- | :--- |
| **[BUG-01]** | Pérdida de datos por Reflection (`SetOptionalProp`) en Turnos, Fichas, Horarios, Stock y Ventas | `1.2`, `1.3`, `3.2`, `4.1`, `4.2`, `4.3` | `Test 03`, `Test 08` |
| **[BUG-02]** | Crash por conflicto entre `EnableRetryOnFailure` y `BeginTransactionAsync` manual | `3.1`, `4.2`, `4.3`, `4.4` | `Test 05`, `Test 07` |
| **[BUG-03]** | Falso aislamiento Multi-Tenant + Vulnerabilidad de cruce `TenantId <-> SucursalId` (OWASP A01) | `1.1`, `1.2`, `1.3`, `1.4`, `5.1` | `Test 01` |
| **[BUG-04]** | Crash en `AuthController` por política `StrictAuthOtpPolicy` no registrada en `Program.cs` | `5.1` | `Test 02` |
| **[BUG-05]** | `ConfiguracionSalonController` guarda en variable RAM estática e ignora SQL Server | `2.1`, `4.1`, `5.2` | `Test 04` |
| **[BUG-06]** | Consultas $O(2N+1)$ en `VentaService` / `ProveedorService` y exposición de Entidades / `object` | `2.2`, `3.1`, `4.2`, `4.3`, `4.4` | `Test 08` |
| **[BUG-07]** | Falta de Idempotencia en POS (`Ventas`), Race Condition en `Turnos` y DTOs `PUT` ignorados | `1.3`, `2.2`, `4.2`, `4.4`, `5.2`, `6.4` | `Test 05`, `Test 07` |
| **[BUG-08]** | Falsificación de firma en Webhooks, "Cruce de la Muerte", `RolId=5` en Registro y falta de `[Authorize]` | `2.1`, `4.2`, `5.1`, `5.2` | `Test 02`, `Test 06` |
| **[BUG-09]** | Bypass de `IUnitOfWork`, servicios huérfanos (`UsuarioService`, `CatalogoService`) y filtro de comisión roto | `3.1`, `3.2`, `4.1`, `5.2` | `Test 09`, `Test 10` |
| **[BUG-10]** | Arqueo de caja contaminado con pagos digitales y falta de descuento de seña online en POS | `1.3`, `2.2`, `4.4`, `6.4` | `Test 07`, `Test 09` |

---

## ETAPA 1: CIMIENTOS DE DOMINIO, ENTIDADES Y ESQUEMA SQL SERVER (CAPA 3)

### TAREA 1.1: Jerarquía Base Multi-Tenant y Excepciones de Dominio
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (Estado actual y overrides).
  * Leer `01_SYSTEM_CONSTRAINTS.md` (Secciones 2 y 3).
  * Leer `02_ARCHITECTURE.md` (Secciones 1.1 y 3.3).
  * Leer `03_DATA_MODEL.md` (Secciones 1.1 y 1.2).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. Eliminar `Models/Common/ITentantEntity.cs` y crear `Models/Common/ITenantEntity.cs`.
  2. Modificar `Models/Common/AuditableEntity.cs` (agregar `UpdatedByUserId` y `DeletedByUserId` conservando `Id`, `CreatedAt`, `CreatedByUserId`, `UpdatedAt`, `IsDeleted`, `DeletedAt`).
  3. Crear `Models/Common/AuditableTenantEntity.cs` (hereda de `AuditableEntity` e implementa `ITenantEntity` con `TenantId = 1` y `SucursalId = 1`).
  4. Crear `Exceptions/ForbiddenAccessException.cs` (hereda de `Exception`).
  5. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`).
  * Registrar en `07_EXECUTION_LEDGER_AND_CHANGELOG.md` las firmas exactas de `AuditableEntity`, `ITenantEntity` y `AuditableTenantEntity`, y avanzar el puntero a la `TAREA 1.2`.

---

### TAREA 1.2: Refactorización de Entidades de Common, Seguridad, Clientes y Stock
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `00_AUDITORIA_Y_DEUDA_TECNICA.md` (`[BUG-01]` y `[BUG-03]`).
  * Leer `03_DATA_MODEL.md` (Secciones 1.2, 2.1, 2.3, 2.4 y 2.6).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `Models/Common/ConfiguracionSalon.cs` y `Models/Common/AuditLog.cs`: Cambiar herencia a `AuditableTenantEntity`. En `ConfiguracionSalon` agregar `NombreComercial` y `HorarioAtencionTexto`.
  2. `Models/Seguridad/Usuario.cs`, `HistorialSesion.cs`, `RefreshToken.cs`: Cambiar herencia a `AuditableTenantEntity` (`Rol.cs` mantiene `AuditableEntity`). En `Usuario.cs` agregar `[Timestamp] public byte[] RowVersion { get; set; } = Array.Empty<byte>();` y fijar default `RolId = 4`.
  3. `Models/Clientes/TipoCabello.cs`, `TipoRostro.cs`, `Cliente.cs`, `FichaTecnica.cs`: Cambiar herencia a `AuditableTenantEntity`.
     * En `Cliente.cs`: agregar `RowVersion`.
     * En `FichaTecnica.cs`: definir `ClienteId`, `PeluqueroId`, `TurnoId`, `FechaRegistro`, `FormulaAplicada`, `Observaciones`, `NotasSensibilidadAlergias`, `ProductosUtilizados`, `FotoResultadoUrl`. Agregar puente temporal `[NotMapped] public DateTime Fecha { get => FechaRegistro; set => FechaRegistro = value; }` y `[NotMapped] public string? DetalleFormulaTintura { get => FormulaAplicada; set => FormulaAplicada = value ?? string.Empty; }`.
  4. `Models/Stock/CategoriaProducto.cs`, `Proveedor.cs`, `Producto.cs`, `MovimientoStock.cs`, `HistorialPrecioProducto.cs`, `OrdenCompra.cs`: Cambiar herencia a `AuditableTenantEntity`.
     * En `Proveedor.cs`: agregar `string? Cuit` y `RowVersion`.
     * En `Producto.cs` y `OrdenCompra.cs`: agregar `RowVersion`.
     * En `MovimientoStock.cs`: agregar `public int SaldoResultante { get; set; }`.
  5. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (y `03_DATA_MODEL.md` si hubo algún ajuste de tipos).
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`).
  * Documentar en la Sección 3 de `07_EXECUTION_LEDGER_AND_CHANGELOG.md` el puente `[NotMapped]` creado en `FichaTecnica.cs` y cualquier detalle de propiedades.

---

### TAREA 1.3: Refactorización de Entidades de Turnos, Agenda y Ventas
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `00_AUDITORIA_Y_DEUDA_TECNICA.md` (`[BUG-01]`, `[BUG-07]`, `[BUG-10]`).
  * Leer `03_DATA_MODEL.md` (Secciones 2.5 y 2.7).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `Models/Turnos/`: Cambiar herencia a `AuditableTenantEntity` en `Especialidad.cs`, `Peluquero.cs`, `PeluqueroEspecialidad.cs`, `HorarioLaboral.cs`, `BloqueoHorario.cs`, `CategoriaServicio.cs`, `Servicio.cs`, `ServicioInsumoReceta.cs`, `HistorialPrecioServicio.cs`, `MotivoCancelacion.cs`, `Turno.cs`, `TurnoServicio.cs`, `HistorialEstadoTurno.cs` (`EstadoTurno.cs` mantiene `AuditableEntity`).
     * En `Peluquero.cs` y `Servicio.cs`: agregar `RowVersion`. En `Especialidad.cs` y `CategoriaServicio.cs`: agregar `string? Descripcion`.
     * En `HorarioLaboral.cs`: agregar `TimeSpan? HoraInicioDescanso`, `TimeSpan? HoraFinDescanso` y `bool Activo { get; set; } = true`.
     * En `Turno.cs`: agregar `bool RequiereSena`, `decimal MontoSena`, `bool SenaPagada`, `DateTime? FechaPagoSena`, `string? ReferenciaPagoExterno` y `RowVersion`.
  2. `Models/Ventas/`: Cambiar herencia a `AuditableTenantEntity` en `MedioPago.cs`, `SesionCaja.cs`, `MovimientoCaja.cs`, `Venta.cs`, `DetalleVenta.cs`, `Pago.cs`, `ComisionesConfig.cs`, `Liquidacion.cs`, `DetalleLiquidacion.cs`.
     * En `MedioPago.cs`: agregar `string? Descripcion` y `bool EsEfectivoFisico { get; set; } = false`.
     * En `SesionCaja.cs`: agregar `TotalVentasEfectivo`, `TotalVentasDigital`, `TotalIngresosManuales`, `TotalEgresosManuales`, `string? ObservacionesCierre` y `RowVersion`.
     * En `Venta.cs`: agregar `decimal Subtotal`, `decimal DescuentoSenaAplicado`, `string? IdempotencyKey` y `RowVersion`.
     * En `Pago.cs`: agregar `string? ReferenciaOperacion`.
     * En `Liquidacion.cs`: agregar `DateTime FechaEmision`, `decimal Bonificaciones`, `decimal Descuentos`, `string? Observaciones`, `int? UsuarioLiquidacionId` y `RowVersion`.
  3. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`).
  * Registrar en `07_EXECUTION_LEDGER_AND_CHANGELOG.md` todas las nuevas columnas agregadas en `Models/Turnos/` y `Models/Ventas/`.

---

### TAREA 1.4: Configuraciones Fluent API, `ApplicationDbContext` Multi-Tenant y Migración [HARD GATE 1]
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (Revisar todas las propiedades agregadas en Tareas 1.1, 1.2 y 1.3).
  * Leer completo `03_DATA_MODEL.md` (Secciones 2 y 3).
  * Leer `01_SYSTEM_CONSTRAINTS.md` (Sección 3).
  * Leer `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (Sección 4 - Reglas innegociables de `HasData` con fechas estáticas UTC, hash BCrypt fijo y Runbook de Migración Limpia en SQL Server).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. Los 12 archivos dentro de `Data/Configurations/`:
     * Actualizar `ClientesModuloConfiguration.cs` (configurar `FormulaAplicada`, `Observaciones`, `NotasSensibilidadAlergias`, `ProductosUtilizados`, `FotoResultadoUrl` y `FechaRegistro` en `FichaTecnicaConfiguration`, ignorando cualquier propiedad `[NotMapped]`).
     * Actualizar `PeluqueroEspecialidadConfiguration.cs` y `TurnoServicioConfiguration.cs`: usar `builder.HasKey(x => x.Id)` y crear los índices únicos compuestos filtrados `UX_PeluqueroEspecialidades_Activa` y `UX_TurnoServicios_Turno_Servicio` con `.HasFilter("[IsDeleted] = 0")`.
     * Convertir todos los índices únicos de `Clientes`, `Usuarios`, `Peluqueros`, `Productos`, `Servicios`, `Categorias*`, `Especialidades`, `Tipos*`, `MediosPago`, `Proveedores`, `ConfiguracionSalon` y `ComisionesConfig` en **Índices Únicos Compuestos por Local** (`new { x.TenantId, x.SucursalId, x.Campo }`) con `.HasFilter("[IsDeleted] = 0")`.
     * Configurar `.IsRowVersion()` en todas las propiedades `RowVersion`, precisión `decimal(18,2)` en todas las columnas monetarias e índice único `UX_Ventas_Local_IdempotencyKey` sobre `(TenantId, SucursalId, IdempotencyKey)` con filtro `[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0`.
  2. `Data/Context/ApplicationDbContext.cs`:
     * Inyectar `ITenantProvider? tenantProvider = null` en el constructor exponiendo `CurrentTenantId` y `CurrentSucursalId`.
     * Aplicar el filtro global Multi-Tenant compuesto en las 35 entidades `AuditableTenantEntity`.
     * En `SaveChangesAsync`: sellar automáticamente `TenantId` y `SucursalId` en toda entidad `ITenantEntity` en estado `EntityState.Added`, y asignar `UpdatedByUserId` / `DeletedByUserId`.
     * Incorporar el **Data Seeding completo** especificado en la Sección 3 de `03_DATA_MODEL.md` y la Sección 4.1 de `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (usando `CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)` y hash BCrypt estático).
  3. Archivos dentro de `Migrations/` (únicamente tras recibir autorización en el Hard Gate 1 para ejecutar el Runbook de la Sección 4.2 de `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`).
  4. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`).
  * Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md` con el resumen exacto del esquema relacional.
* **🛑 [HARD GATE DE AUTORIZACIÓN HUMANA 1]:**
  * **DETENERSE AQUÍ.** Mostrar el estado de `07_EXECUTION_LEDGER_AND_CHANGELOG.md` y solicitar autorización al Arquitecto antes de ejecutar el Runbook de migración EF Core (`dotnet ef database drop --force`, `dotnet ef migrations add InicialMultiTenantSaaS_v2` y `dotnet ef database update`) definido en `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`.

---

## ETAPA 2: CONTRATOS DTO, VALIDACIONES FLUENTVALIDATION Y AUTOMAPPER (CAPA 4)

### TAREA 2.1: Actualización de DTOs de Common, Seguridad, Clientes y SaaS
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `04_API_CONTRACTS.md` (Secciones 1, 2, 3.1 y 3.2).
  * Leer `00_AUDITORIA_Y_DEUDA_TECNICA.md` (`[BUG-05]`, `[BUG-08]`).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `DTOs/Common/ApiResponse.cs`: Implementar estructura con `FieldErrors` y `Warnings`.
  2. `DTOs/Common/PaginationFilterDto.cs`: Implementar estructura con `FechaDesde` y `FechaHasta`.
  3. `DTOs/Common/CatalogoItemDto.cs`: Agregar `ActualizarCatalogoItemDto` y `bool? EsEfectivoFisico`.
  4. `DTOs/Common/ConfiguracionSalonDto.cs`: Unificar todas las propiedades según `04_API_CONTRACTS.md` (Sección 3.2).
  5. `DTOs/Seguridad/SeguridadDto.cs`: Cambiar default de `RegistroUsuarioDto.RolId` a `4` (`Cliente`). Agregar `RefreshTokenRequestDto`, `CrearUsuarioStaffDto` y `CambiarRolUsuarioDto`.
  6. `DTOs/Clientes/`: Agregar `string? NotasSensibilidadAlergias` en `CrearFichaTecnicaDto`, `ActualizarFichaTecnicaDto` y `FichaTecnicaResponseDto`.
  7. `DTOs/SaaS/SaaSDto.cs`: Agregar `PlanSuscripcionResponseDto`.
  8. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`). Documentar en `07` los nuevos nombres de DTOs creados.

---

### TAREA 2.2: Actualización de DTOs de Turnos, Stock y Ventas + Validadores + MappingProfile
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `04_API_CONTRACTS.md` (Secciones 3.3 y 3.4).
  * Leer `00_AUDITORIA_Y_DEUDA_TECNICA.md` (`[BUG-06]`, `[BUG-07]`, `[BUG-10]`).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `DTOs/Turnos/TurnoResponseDto.cs`: Implementar `TurnoResponseDto` enriquecido y `TurnoServicioDetalleDto`.
  2. `DTOs/Turnos/DisponibilidadHorarioResponseDto.cs`: Agregar `IntervaloMinutos` y `List<string> HorariosDisponibles`.
  3. `DTOs/Turnos/BloqueoHorarioDto.cs`: Agregar `BloqueoHorarioResponseDto`.
  4. `DTOs/Stock/ProveedorDto.cs`, `CrearProveedorDto.cs`, `ActualizarProveedorDto.cs`: Agregar `string? Cuit`.
  5. `DTOs/Ventas/`:
     * Agregar `SesionCajaResponseDto` (respetando la propiedad `MontoFinalEsperado`, `TotalVentasEfectivo` y `TotalVentasDigital`) y `MovimientoCajaResponseDto`, más `string? ObservacionesCierre` en `CierreCajaDto`.
     * En `CrearVentaDto.cs`: agregar `public string? IdempotencyKey { get; set; }`.
     * En `PagoRequestDto.cs` y `VentaResponseDto.cs`: agregar `string? ReferenciaOperacion`, `decimal Subtotal` y `decimal DescuentoSenaAplicado`.
     * En `LiquidacionResponseDto.cs`: agregar `DateTime FechaEmision`, `decimal Bonificaciones`, `decimal Descuentos`, `string? Observaciones`, y `string ConceptoNombre` en `DetalleLiquidacionItemDto`.
  6. `Validators/`: Corregir `Validators/Stock/CrearProductoDtoValidator.cs` y agregar validadores para los nuevos DTOs de entrada.
  7. `Mappings/MappingProfile.cs`: Registrar todos los perfiles AutoMapper nuevos y actualizados.
  8. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (y `04_API_CONTRACTS.md` si se adicionó alguna propiedad auxiliar).
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`). Documentar en `07` el estado completo del catálogo de DTOs.

---

## ETAPA 3: CAPA DE REPOSITORIOS, BATCH LOADING ANTI N+1 Y `UnitOfWork` (CAPA 2)

### TAREA 3.1: `GenericRepository<T>`, Nuevos Repositorios (`Recetas` y `SaaS`) y `UnitOfWork` Resiliente
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `02_ARCHITECTURE.md` (Secciones 2.1, 2.2, 2.3 y 2.4).
  * Leer `00_AUDITORIA_Y_DEUDA_TECNICA.md` (`[BUG-02]`, `[BUG-06]`, `[BUG-09]`).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `Repositories/Interfaces/IGenericRepository.cs` y `Repositories/Implementations/GenericRepository.cs`: Implementar `GetByIdsDictionaryAsync`, `AsQueryable()` y firmas estándar.
  2. Crear `Repositories/Interfaces/IRecetaServicioRepository.cs` y `Repositories/Implementations/RecetaServicioRepository.cs`.
  3. Crear `Repositories/Interfaces/ISaaSRepository.cs` y `Repositories/Implementations/SaaSRepository.cs`.
  4. `Repositories/Interfaces/IUnitOfWork.cs` y `Repositories/Implementations/UnitOfWork.cs`:
     * Exponer los **19 repositorios**.
     * Implementar `ExecuteInTransactionAsync` con `_context.Database.CreateExecutionStrategy().ExecuteAsync(...)`, soporte de profundidad `_transactionDepth` y `_context.ChangeTracker.Clear()` en rollback *(mantener métodos `BeginTransactionAsync`/`CommitTransactionAsync` delegando o marcados para transición hasta terminar la Tarea 4.4 para no romper la compilación de `VentaService` y `OrdenCompraService` en este paso)*.
  5. `Program.cs`: Registrar `IRecetaServicioRepository` e `ISaaSRepository`.
  6. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`). Registrar en la Sección 4 de `07` las firmas exactas de `IUnitOfWork` e `IGenericRepository<T>`.

---

### TAREA 3.2: Refactorización de los 17 Repositorios Existentes (Cero Reflection, Herencia y Paginación)
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `00_AUDITORIA_Y_DEUDA_TECNICA.md` (`[BUG-01]`, `[BUG-06]`, `[BUG-09]`).
  * Leer `02_ARCHITECTURE.md` (Sección 2.2) y `05_BUSINESS_RULES.md` (Sección 4.4).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. Todos los archivos en `Repositories/Interfaces/` y `Repositories/Implementations/`:
     * Asegurar herencia de `IGenericRepository<T>` / `GenericRepository<T>`.
     * **`VentaRepository.cs` [CRÍTICO - BUG-01]:** Eliminar por completo el uso de `entry.Metadata` / Reflection en `GetDatosServicioAsync` y `GetNombrePeluqueroAsync`. Agregar `GetByIdempotencyKeyAsync` y `ExisteVentaActivaConSenaPorTurnoAsync`.
     * **`ComisionConfigRepository.cs` [CRÍTICO - BUG-09]:** En `GetVigenteByPeluqueroYServicioAsync(int peluqueroId, int? servicioId)`, filtrar primero por `ServicioId == servicioId` con fallback a `ServicioId == null`.
     * **`CatalogoRepository.cs`, `ProductoRepository.cs`, `TurnoRepository.cs`, `PeluqueroRepository.cs`, `ServicioRepository.cs`:** Agregar los métodos de soporte para CRUD de los 7 catálogos, inserción en `HistorialPrecios`, verificación de consumo idempotente de gabinete y consultas paginadas.
  2. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`).
  * Documentar en `07` todos los métodos nuevos agregados en las interfaces de los repositorios para que los Servicios en la Etapa 4 los consuman con sus nombres exactos.

---

## ETAPA 4: REFACTORIZACIÓN DE SERVICIOS Y REGLAS DE NEGOCIO (CAPA 5)

### TAREA 4.1: Servicios de Seguridad, Usuarios, Catálogos, Configuración, SaaS y Clientes
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (Verificar nombres exactos de métodos en Repositorios y DTOs).
  * Leer `01_SYSTEM_CONSTRAINTS.md` (Sección 4), `02_ARCHITECTURE.md` (Secciones 2.4 y 4.1) y `04_API_CONTRACTS.md` (Secciones 4.1 a 4.5).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `IAuthService.cs` y `AuthService.cs`: Agregar `RefreshTokenAsync`, fijar `RolId = 4` en registro público y capturar fallos de `SmtpEmailService` devolviendo `Warnings` sin abortar el alta.
  2. `IUsuarioService.cs` y `UsuarioService.cs`: Retornar siempre `ApiResponse<T>`, agregar `CrearUsuarioStaffAsync` y paginación en `ObtenerHistorialSesionesAsync`, revocando `RefreshTokens` al desactivar cuentas.
  3. `ICatalogoService.cs` y `CatalogoService.cs`: Eliminar `ApplicationDbContext` y Reflection (`MapToCatalogoDto`, `SetCatalogProperties`). Usar `IUnitOfWork.Catalogos` y caché Multi-Tenant `$"SaaS_Catalogos_T{tenantId}_S{sucursalId}"` con CRUD completo para los 7 catálogos.
  4. `IConfiguracionSalonService.cs` y `ConfiguracionSalonService.cs`: Particionar caché por `(TenantId, SucursalId)`.
  5. Crear `ISaaSService.cs` y `SaaSService.cs` (y registrarlo en `Program.cs`).
  6. `IClienteService.cs`, `ClienteService.cs`, `IFichaTecnicaService.cs`, `FichaTecnicaService.cs` y `Models/Clientes/FichaTecnica.cs`:
     * En `FichaTecnicaService` **[CRÍTICO - BUG-01]**: Eliminar `ApplicationDbContext` y todos los métodos de Reflection (`SetOptionalProp`, `GetOptionalProp`, etc.). Usar `IUnitOfWork.FichasTecnicas`, agregar `ObtenerPorIdAsync`, recibir `ActualizarFichaTecnicaDto` en `ActualizarFichaAsync` y mapear las propiedades reales.
     * Eliminar de `FichaTecnica.cs` las propiedades puente `[NotMapped]` temporales creadas en la Tarea 1.2.
  7. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`). Tachado del puente `[NotMapped]` en la Sección 3 del archivo `07`.

---

### TAREA 4.2: Servicios de Peluqueros, Servicios, Recetas, Bloqueos y Motor de Turnos
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer completo `05_BUSINESS_RULES.md` (Secciones 1, 2 y 3).
  * Leer `04_API_CONTRACTS.md` (Secciones 3.3 y 4.6 a 4.11).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `IPeluqueroService.cs` y `PeluqueroService.cs` **[CRÍTICO - BUG-01]**: Eliminar `ApplicationDbContext` y Reflection. Usar `IUnitOfWork`, recibir `ActualizarPeluqueroDto` en `UpdateAsync`, validar tope de 50 profesionales, persistir `HoraInicioDescanso`, `HoraFinDescanso` y `Activo`, y bloquear `DeleteAsync` si tiene turnos futuros activos o liquidaciones pendientes.
  2. `IServicioService.cs` y `ServicioService.cs`: Eliminar `ApplicationDbContext`, usar `IUnitOfWork`, recibir `ActualizarServicioDto`, insertar automáticamente en `HistorialPreciosServicio` cuando cambie el `Precio` y desactivar en `DeleteAsync` preservando turnos históricos.
  3. `IRecetaServicioService.cs` y `RecetaServicioService.cs`: Eliminar `ApplicationDbContext`, usar `IUnitOfWork`, validar idempotencia por `"Turno #{turnoId}"` y descontar hasta `0` devolviendo `Warnings` cuando falte stock.
  4. `IBloqueoHorarioService.cs` y `BloqueoHorarioService.cs`: Eliminar `ApplicationDbContext`, usar `IUnitOfWork`, retornar `BloqueoHorarioResponseDto` y advertir en `Warnings` si existen turnos activos en el rango bloqueado.
  5. `ITurnoService.cs` y `TurnoService.cs` **[CRÍTICO - BUG-01, BUG-06, BUG-07, BUG-08]**:
     * Eliminar `ApplicationDbContext` y Reflection.
     * Implementar paginación en `GetAllAsync`, `ExecuteInTransactionAsync(..., IsolationLevel.Serializable)` en `CreateAsync` y `ReprogramarAsync`, carga en lote con `GetByIdsDictionaryAsync`, asignación tipada de `PrecioHistorico` y `DuracionMinutosHistorica`, Máquina de Estados Estricta, ventana `HorasAnticipacionCancelacion`, **Transacción Compensatoria para Webhooks (`ProcesarPagoSenaWebhookAsync`)**, cálculo con `TimeZoneInfo` en `ObtenerHorariosDisponiblesAsync` retornando `DisponibilidadHorarioResponseDto` y retorno de `HistorialEstadoTurnoResponseDto`.
  6. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`). Registrar en `07` las firmas actualizadas de `ITurnoService`.

---

### TAREA 4.3: Servicios de Stock, Proveedores y Órdenes de Compra
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `05_BUSINESS_RULES.md` (Sección 3) y `04_API_CONTRACTS.md` (Sección 4.12).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `IProductoService.cs` y `ProductoService.cs` **[CRÍTICO - BUG-01 y BUG-06]**: Eliminar `ApplicationDbContext` y `SetOptionalProp`. Usar `IUnitOfWork`, paginar `GetAllAsync`, usar `ActualizarProductoDto` en `UpdateAsync` (auditando en `HistorialPreciosProducto`), persistir `SaldoResultante` en `MovimientoStock` y retornar `PagedResult<KardexItemResponseDto>` en `ObtenerKardexProductoAsync`.
  2. `IProveedorService.cs` y `ProveedorService.cs` **[CRÍTICO - BUG-06]**: Eliminar `ApplicationDbContext` y el bucle $N+1$ de `CountAsync`, y recibir `ActualizarProveedorDto` en `UpdateAsync`.
  3. `IOrdenCompraService.cs` y `OrdenCompraService.cs`: Eliminar `ApplicationDbContext`, paginar `ObtenerTodasAsync` y usar `_uow.ExecuteInTransactionAsync` en `RecepcionarOrdenAsync` persistiendo `SaldoResultante`.
  4. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`). Actualizar `07`.

---

### TAREA 4.4: Servicios Financieros: Caja, POS Idempotente, Comisiones, Liquidaciones y Dashboard [HARD GATE 2]
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer completo `05_BUSINESS_RULES.md` (Secciones 1 y 4).
  * Leer `04_API_CONTRACTS.md` (Secciones 3.4 y 4.13).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `ICajaService.cs` y `CajaService.cs` **[CRÍTICO - BUG-06 y BUG-10]**: Retornar `SesionCajaResponseDto` y `MovimientoCajaResponseDto`, agregar `ObtenerPorIdAsync`, paginar `ObtenerHistorialCajasAsync` (sin *Over-fetching* de movimientos) y separar `TotalVentasEfectivo` (`EsEfectivoFisico == true`) de `TotalVentasDigital` calculando exactamente `MontoFinalEsperado`.
  2. `IVentaService.cs` y `VentaService.cs` **[CRÍTICO - BUG-02, BUG-06, BUG-07, BUG-10]**:
     * Validar `IdempotencyKey` al inicio de `RegistrarVentaAsync`.
     * Precargar productos y servicios con `GetByIdsDictionaryAsync` ($O(1)$ queries).
     * Ejecutar en `_uow.ExecuteInTransactionAsync`: heredar `turno.PeluqueroId`, descontar `DescuentoSenaAplicado`, autocompletar el turno a `Completado (4)` y persistir `SaldoResultante`.
     * En `AnularVentaAsync`: validar que la `SesionCaja` esté abierta y que ningún renglón esté en una `Liquidacion` activa.
     * Paginar `ObtenerVentasPorSesionAsync` sin consultas $N+1$.
  3. `IComisionConfigService.cs`, `ComisionConfigService.cs`, `ILiquidacionService.cs` y `LiquidacionService.cs`: Eliminar `ApplicationDbContext`, usar `IUnitOfWork` y `_uow.ExecuteInTransactionAsync`, persistir `FechaEmision`, `Bonificaciones`, `Descuentos` y `Observaciones`, poblar `ConceptoNombre` en lote, paginar `ObtenerTodasAsync` e implementar `AnularLiquidacionAsync`.
  4. `IDashboardService.cs` y `DashboardService.cs`: Eliminar `ApplicationDbContext`, usar `IUnitOfWork.Dashboard` y calcular `TurnosPendientesHoy` con `TimeZoneInfo` del salón.
  5. Limpiar de `IUnitOfWork` / `UnitOfWork` cualquier método transaccional manual obsoleto dejando exclusivamente `ExecuteInTransactionAsync`.
  6. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`).
  * **Auditorías Automáticas en Consola PowerShell:**
    1. `Select-String -Path .\Services\**\*.cs, .\Repositories\**\*.cs -Pattern "GetProperty|SetOptionalProp|GetOptionalProp|Metadata"` -> **0 coincidencias**.
    2. `Select-String -Path .\Services\Implementations\*.cs -Pattern "ApplicationDbContext"` -> **0 coincidencias**.
* **🛑 [HARD GATE DE AUTORIZACIÓN HUMANA 2]:**
  * **DETENERSE AQUÍ.** Presentar el estado de `07_EXECUTION_LEDGER_AND_CHANGELOG.md` y el resultado de las auditorías de consola. Esperar autorización antes de iniciar la Etapa 5.

---

## ETAPA 5: PIPELINE HTTP, SEGURIDAD, CONTROLADORES Y PRUEBAS DE ACEPTACIÓN (CAPAS 2, 4, 8 Y 9)

### TAREA 5.1: Middlewares, Integraciones (`HMAC` + `SMTP` Guard), Background Worker, Configuración ODS y `Program.cs`
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `02_ARCHITECTURE.md` (Secciones 3 y 4) y `00_AUDITORIA_Y_DEUDA_TECNICA.md` (`[BUG-03]`, `[BUG-04]`, `[BUG-08]`).
  * Leer `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (Secciones 1 y 2 - Esquema completo de `appsettings.json` y reglas de cortocircuito defensivo ante `Smtp:Host` vacío, `WebhookSecret` y fallbacks de `BackgroundJobs`).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `Middlewares/GlobalExceptionMiddleware.cs`: Mapear `ForbiddenAccessException` (`403`) y `DbUpdateConcurrencyException` (`409 Conflict`).
  2. `Middlewares/TenantResolutionMiddleware.cs`: Priorizar claims del JWT y validar en `IMemoryCache` (`10 min`) que `(TenantId, SucursalId)` exista y esté activa en `Sucursales`.
  3. `Integrations/Payments/PaymentGatewayService.cs`: Implementar validación criptográfica `HMACSHA256` real (`CryptographicOperations.FixedTimeEquals`) en `ValidarFirmaWebhook` con política *Fail-Closed*.
  4. `Integrations/Email/SmtpEmailService.cs`: Implementar cortocircuito defensivo si `Smtp:Host` está vacío (retornando `false` con `LogWarning` sin instanciar `SmtpClient`) y configurar `Timeout = 5000` (5s) con `CancellationToken`.
  5. `Services/Background/RecordatorioTurnosBackgroundService.cs`: Iterar sobre todas las sucursales activas con `.IgnoreQueryFilters()` seteando `tenantProvider.SetTenantContext` y aplicando fallbacks seguros de configuración.
  6. `appsettings.json` y `appsettings.Development.json`: Actualizar con el contrato completo definido en la Sección 1 de `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`.
  7. `Program.cs`: Configurar `AddRateLimiter("StrictAuthOtpPolicy")`, `InvalidModelStateResponseFactory` (`ApiResponse<object>.Fail` con `FieldErrors`), inyección de los 19 Repositorios y 19 Servicios, y orden estricto de middlewares.
  8. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores`). Actualizar `07`.

---

### TAREA 5.2: Refactorización de los 22 Controladores, Suite HTTP y Verificación BDD [HARD GATE 3]
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (Verificar todas las firmas de los 19 `IService`).
  * Leer completo `04_API_CONTRACTS.md` (Secciones 1, 2, 3 y 4).
  * Leer completo `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md` (Secciones 1, 2 y 3 - Matriz BDD de los 10 tests innegociables y estructura de `MiPeluqueria.Api.http`).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. Los 22 archivos en `Controllers/`:
     * Eliminar toda inyección de `ApplicationDbContext` y la clase/RAM estática de `ConfiguracionSalonController`.
     * Conectar cada controlador a su `IService` correspondiente (incluyendo `UsuariosController` -> `IUsuarioService`, `CatalogosController` -> `ICatalogoService`, `SucursalesController` y `SuscripcionesController` -> `ISaaSService`).
     * Exponer todos los endpoints nuevos y paginados definidos en `04_API_CONTRACTS.md` (`POST refresh-token`, `PUT {id}/reprogramar`, `POST {id}/anular` en liquidaciones, `GET {id}` en caja, CRUD completo en los 7 catálogos, `Actualizar*Dto` en los `PUT`, lectura de `X-Idempotency-Key` en `VentasController` y extracción de `UsuarioId` desde el JWT en `CajaController`).
     * Aplicar los atributos `[Authorize]` y `[Authorize(Roles = "...")]` según la Matriz RBAC de `04_API_CONTRACTS.md`.
  2. `MiPeluqueria.Api.http`: Actualizar con la batería automatizada de pruebas HTTP especificada en `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md`.
  3. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `dotnet build` (`0 Errores y 0 Advertencias`).
  * Ejecutar en consola: `Select-String -Path .\Controllers\*.cs -Pattern "ApplicationDbContext"` -> **0 coincidencias**.
  * Verificar el cumplimiento de los **Tests 01 al 10** de `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md` y documentar en `07_EXECUTION_LEDGER_AND_CHANGELOG.md` el cierre completo del Backend.
* **🛑 [HARD GATE DE AUTORIZACIÓN HUMANA 3 - CIERRE DEL BACKEND]:**
  * **DETENERSE AQUÍ.** Esperar a que el Arquitecto valide la API y los casos de `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md` en Swagger UI / `MiPeluqueria.Api.http` antes de iniciar la Etapa 6 (Frontend).

---

## ETAPA 6: MODERNIZACIÓN DEL FRONTEND PRO (`mipeluqueria-saas-web` - CAPAS 9 Y 10)

### TAREA 6.1: Manifiesto `package.json`, Variables `.env.local`, Layout y Providers Globales
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `01_SYSTEM_CONSTRAINTS.md` (Sección 5) y `02_ARCHITECTURE.md` (Sección 5).
  * Leer `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (Sección 3 - Contrato de `.env.local`).
  * Leer `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Secciones 1, 2 y 3).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `mipeluqueria-saas-web/package.json` y `tsconfig.json`: Restaurar manifiesto con el stack Pro (`@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`, `date-fns`, `recharts`, `lucide-react`, `clsx`).
  2. `mipeluqueria-saas-web/.env.local`: Configurar las variables públicas especificadas en `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`.
  3. `mipeluqueria-saas-web/src/app/layout.tsx` y `mipeluqueria-saas-web/src/context/appproviders.tsx`: Reparar error `TS2306`, configurar `QueryClientProvider`, `Toaster` (`sonner`) y `AuthContext`.
  4. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Ejecutar `npm install` y verificar compilación base de proveedores. Actualizar `07`.

---

### TAREA 6.2: Cliente HTTP Resiliente (`axios.ts` con Cola `RefreshToken`) y Helper Multimedia (`cloudinary.ts`)
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Sección 1 - Prevención de *Refresh Token Storm* con `isRefreshing` + `failedQueue`).
  * Leer `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (Sección 3 - Fallback defensivo en `cloudinary.ts`).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `mipeluqueria-saas-web/src/lib/axios.ts`: Implementar inyección automática de `Authorization`, `X-Tenant-Id`, `X-Sucursal-Id`, soporte de `X-Idempotency-Key` y cola de concurrencia `failedQueue` para rotación transparente con `POST /api/auth/refresh-token` ante respuestas `401`.
  2. `mipeluqueria-saas-web/src/lib/cloudinary.ts` y `mipeluqueria-saas-web/src/lib/formatters.ts`: Implementar subida Direct-to-Cloud con fallback controlado en desarrollo.
  3. `mipeluqueria-saas-web/src/middleware.ts`: Protección de rutas por rol en Next.js.
  4. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Verificar tipado estricto en `src/lib/` sin errores de TypeScript. Actualizar `07`.

---

### TAREA 6.3: Modularización de Tipos TypeScript (`src/types/`) y Servicios API (`src/services/`) con *Barrel Files*
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (Revisar firmas finales compiladas en Backend).
  * Leer `04_API_CONTRACTS.md` (Secciones 1, 3 y 4).
  * Leer `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Secciones 3 y 4).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `mipeluqueria-saas-web/src/types/*.types.ts` y `mipeluqueria-saas-web/src/types/api.types.ts` (*Barrel File*): Espejar 1:1 `ApiResponse<T>` (con `fieldErrors` y `warnings`), `PagedResult<T>`, `PaginationFilterDto` y todos los DTOs del Backend (incluyendo `montoFinalEsperado`, `totalVentasEfectivo`, `totalVentasDigital`, `descuentoSenaAplicado`, `idempotencyKey`, `saldoResultante`, `precioHistorico`, etc.).
  2. `mipeluqueria-saas-web/src/services/*.service.ts` y `mipeluqueria-saas-web/src/services/api.services.ts` (*Barrel File*): Conectar todos los métodos HTTP contra los 22 controladores.
  3. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN Y REGISTRO EN LEDGER `07`]:**
  * Verificar que ningún servicio invoque rutas inexistentes ni tipos `any`. Actualizar `07`.

---

### TAREA 6.4: Esquemas Zod, Hooks TanStack Query y Conexión de las 19 Pantallas (`src/app/`)
* **[LECTURA OBLIGATORIA PREVIA]:**
  * Leer `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  * Leer completo `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Secciones 2, 3 y 4 - Matriz de `Query Keys`, protocolo `sonner` + `409 Conflict`, rotación de `idempotencyKey` en POS, arqueo con `montoFinalEsperado` en Caja y mapeo de las 19 rutas).
* **[ARCHIVOS PERMITIDOS EN ESTA TAREA]:**
  1. `mipeluqueria-saas-web/src/schemas/*.ts`: Esquemas Zod espejados con FluentValidation.
  2. `mipeluqueria-saas-web/src/hooks/*.ts`: Custom hooks con TanStack Query v5 respetando la matriz de `Query Keys`, invalidación cruzada automática, inyección de `fieldErrors` en `react-hook-form`, disparo de `toast.warning` por cada ítem en `res.warnings` y recarga automática ante `409 Conflict`.
  3. Las 19 pantallas en `mipeluqueria-saas-web/src/app/**` y sus componentes en `mipeluqueria-saas-web/src/components/**` según el mapeo exacto de la Sección 4 de `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`.
  4. Actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[VERIFICACIÓN FINAL Y CIERRE EN LEDGER `07`]:**
  * Ejecutar `npm run build` (`0 Errores de compilación y 0 Errores de TypeScript`).
  * Documentar el cierre definitivo de la arquitectura SDD + ODD + ODS en `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
  