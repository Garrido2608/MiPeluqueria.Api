# 03 - MODELO DE DOMINIO, ESQUEMA RELACIONAL SQL SERVER Y FLUENT API (CAPA 3 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8)[cite: 27]
**Documentos Padres Vinculados:** `00_AUDITORIA_Y_DEUDA_TECNICA.md`, `01_SYSTEM_CONSTRAINTS.md`, `02_ARCHITECTURE.md`[cite: 27]
**Estado:** INMUTABLE - ESPECIFICACIÓN DE PERSISTENCIA, ENTIDADES E ÍNDICES[cite: 27]

---

## 1. JERARQUÍA DE CLASES BASE, AUDITORÍA Y MULTI-TENANCY UNIVERSAL

Ninguna tabla del sistema puede existir por fuera de la jerarquía de auditoría (`PeluqueroEspecialidad` y `TurnoServicio` heredan el estándar completo)[cite: 27].

### 1.1 Interfaces y Clases Abstractas Base (`Models/Common/`)

```csharp
// filepath: Models/Common/ISoftDelete.cs
using System;

namespace MiPeluqueria.Api.Models.Common
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
    }
}

// filepath: Models/Common/ITenantEntity.cs
namespace MiPeluqueria.Api.Models.Common
{
    public interface ITenantEntity
    {
        int TenantId { get; set; }
        int? SucursalId { get; set; }
    }
}

// filepath: Models/Common/AuditableEntity.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.Models.Common
{
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
}

// filepath: Models/Common/AuditableTenantEntity.cs
namespace MiPeluqueria.Api.Models.Common
{
    public abstract class AuditableTenantEntity : AuditableEntity, ITenantEntity
    {
        public int TenantId { get; set; } = 1;
        public int? SucursalId { get; set; } = 1;
    }
}
```

### 1.2 Regla de Herencia para las Entidades del Sistema
1. **Entidades de Plataforma / Catálogo Fijo de Código (Heredan de `AuditableEntity`):**
   * `Tenant`, `Sucursal`, `PlanSuscripcion`, `SuscripcionTenant`, `Rol` (IDs 1 al 5) y `EstadoTurno` (IDs 1 al 6)[cite: 27].
2. **Entidades Operativas y Catálogos del Local (Heredan de `AuditableTenantEntity`):**
   * Todas las demás entidades del sistema (incluyendo `Usuario`, `Cliente`, `FichaTecnica`, `Peluquero`, `PeluqueroEspecialidad`, `Turno`, `TurnoServicio`, `Producto`, `MovimientoStock`, `OrdenCompra`, `SesionCaja`, `Venta`, `Liquidacion`, `ConfiguracionSalon`, `AuditLog` y los 7 catálogos personalizables del salón)[cite: 27].

---

## 2. DICCIONARIO COMPLETO DE ENTIDADES Y ESQUEMA SQL SERVER

### 2.1 Módulo Common y Configuración (`Models/Common/`)
* **`ConfiguracionSalon` (`AuditableTenantEntity`):**
  * Columnas: `NombreSalon` (`nvarchar(150)`), `NombreComercial` (`nvarchar(150)`), `RazonSocial` (`nvarchar(150)`), `Cuit` (`nvarchar(20)`), `Direccion` (`nvarchar(250)`), `Telefono` (`nvarchar(50)`), `EmailContacto` (`nvarchar(150)`), `MonedaIso` (`nvarchar(3)`, default `"ARS"`), `TimeZoneId` (`nvarchar(100)`, default `"America/Argentina/Cordoba"`), `HorarioAtencionTexto` (`nvarchar(200)`), `IntervaloTurnosMinutos` (`int`, default `30`), `MinutosToleranciaEspera` (`int`, default `15`), `HorasAnticipacionCancelacion` (`int`, default `24`), `PorcentajeSenaReserva` (`decimal(5,2)`, default `0.00m`), `NotificacionesWhatsAppActivas` (`bit`, default `true`), `NotificacionesEmailActivas` (`bit`, default `true`)[cite: 27].
  * Índice Único: `UX_ConfiguracionSalon_Tenant_Sucursal` sobre `(TenantId, SucursalId)` con filtro `[IsDeleted] = 0`[cite: 27].
* **`AuditLog` (`AuditableTenantEntity`):**
  * Columnas: `UsuarioId` (`int?`), `EntidadNombre` (`nvarchar(100)`), `Accion` (`nvarchar(30)`), `ClavePrimaria` (`nvarchar(50)`), `ValoresAnterioresJson` (`nvarchar(max)?`), `ValoresNuevosJson` (`nvarchar(max)?`), `IpAddress` (`nvarchar(50)?`), `FechaUtc` (`datetime2`)[cite: 27].

### 2.2 Módulo SaaS (`Models/SaaS/`)
* **`Tenant` (`AuditableEntity`):** `NombreComercial` (`nvarchar(150)`), `SubdominioSlug` (`nvarchar(100)`), `IdentificacionFiscal` (`nvarchar(30)?`), `EmailPropietario` (`nvarchar(150)`), `PaisIso` (`nvarchar(10)`), `Activo` (`bit`)[cite: 27]. Índice único `UX_Tenants_SubdominioSlug` filtrado por `[IsDeleted] = 0`[cite: 27].
* **`Sucursal` (`AuditableEntity`):** `TenantId` (`int`), `Nombre` (`nvarchar(150)`), `Direccion` (`nvarchar(250)`), `Telefono` (`nvarchar(50)?`), `TimeZoneId` (`nvarchar(100)`), `MonedaIso` (`nvarchar(3)`), `EsCasaMatriz` (`bit`), `Activa` (`bit`)[cite: 27].
* **`PlanSuscripcion` (`AuditableEntity`):** `Nombre` (`nvarchar(100)`), `PrecioMensualUsd` (`decimal(18,2)`), `MaxSucursales` (`int`), `MaxPeluqueros` (`int`), `IncluyeWhatsAppRecordatorios` (`bit`), `IncluyePortalReservasOnline` (`bit`), `Activo` (`bit`)[cite: 27].
* **`SuscripcionTenant` (`AuditableEntity`):** `TenantId` (`int`), `PlanSuscripcionId` (`int`), `FechaInicio` (`datetime2`), `FechaProximoVencimiento` (`datetime2`), `Estado` (`EstadoSuscripcionEnum`), `ExternalSubscriptionId` (`nvarchar(150)?`)[cite: 27].

### 2.3 Módulo Seguridad (`Models/Seguridad/`)
* **`Rol` (`AuditableEntity`):** `Nombre` (`nvarchar(50)`). Valores: `1 = Administrador`, `2 = Recepcionista`, `3 = Peluquero`, `4 = Cliente`, `5 = Cajero`[cite: 27].
* **`Usuario` (`AuditableTenantEntity`):** `RolId` (`int`, default `4`), `Username` (`nvarchar(100)`), `PasswordHash` (`nvarchar(500)`), `Email` (`nvarchar(150)`), `EmailVerificado` (`bit`), `CodigoVerificacion` (`nvarchar(6)?`), `FechaExpiracionCodigo` (`datetime2?`), `AceptaTerminos` (`bit`), `FechaAceptacionTerminos` (`datetime2?`), `IntentosFallidos` (`int`), `BloqueadoHasta` (`datetime2?`), `PeluqueroId` (`int?`), `ClienteId` (`int?`), `Activo` (`bit`), `[Timestamp] public byte[] RowVersion`[cite: 27].
  * Índices Únicos por Local: `UX_Usuarios_Local_Username` sobre `(TenantId, SucursalId, Username)` y `UX_Usuarios_Local_Email` sobre `(TenantId, SucursalId, Email)` con filtro `[IsDeleted] = 0`[cite: 27].
* **`HistorialSesion` (`AuditableTenantEntity`):** `UsuarioId` (`int`), `DireccionIP` (`nvarchar(50)`), `Dispositivo` (`nvarchar(500)?`), `Exitoso` (`bit`), `MotivoFallo` (`MotivoFalloSesionEnum?`), `FechaIntento` (`datetime2`)[cite: 27].
* **`RefreshToken` (`AuditableTenantEntity`):** `UsuarioId` (`int`), `Token` (`nvarchar(256)`), `ExpiresAt` (`datetime2`), `CreatedByIp` (`nvarchar(50)?`), `RevokedAt` (`datetime2?`), `RevokedByIp` (`nvarchar(50)?`), `ReplacedByToken` (`nvarchar(256)?`), `ReasonRevoked` (`nvarchar(200)?`)[cite: 27].

### 2.4 Módulo Clientes y Gabinete (`Models/Clientes/`)
* **`TipoCabello` y `TipoRostro` (`AuditableTenantEntity`):** `Nombre` (`nvarchar(50)`), `Descripcion` (`nvarchar(250)?`). Índice único por `(TenantId, SucursalId, Nombre)` filtrado por `[IsDeleted] = 0`[cite: 27].
* **`Cliente` (`AuditableTenantEntity`):** `Nombre` (`nvarchar(100)`), `Apellido` (`nvarchar(100)`), `Telefono` (`nvarchar(30)`), `Email` (`nvarchar(150)?`), `FechaNacimiento` (`datetime2`), `FechaRegistro` (`datetime2`), `Activo` (`bit`), `TipoCabelloId` (`int?`), `TipoRostroId` (`int?`), `[Timestamp] public byte[] RowVersion`[cite: 27].
  * Índices Únicos por Local: `UX_Clientes_Local_Telefono` sobre `(TenantId, SucursalId, Telefono)` y `UX_Clientes_Local_Email` sobre `(TenantId, SucursalId, Email)`[cite: 27].
* **`FichaTecnica` (`AuditableTenantEntity`):** `ClienteId` (`int`), `PeluqueroId` (`int`), `TurnoId` (`int?`), `FechaRegistro` (`datetime2`), `FormulaAplicada` (`nvarchar(1000)`), `Observaciones` (`nvarchar(1000)?`), `NotasSensibilidadAlergias` (`nvarchar(1000)?`), `ProductosUtilizados` (`nvarchar(500)?`), `FotoResultadoUrl` (`nvarchar(500)?`)[cite: 27].

### 2.5 Módulo Turnos, Agenda y Staff (`Models/Turnos/`)
* **`Especialidad` y `CategoriaServicio` (`AuditableTenantEntity`):** `Nombre` (`nvarchar(100)`), `Descripcion` (`nvarchar(250)?`). Índice único por `(TenantId, SucursalId, Nombre)`[cite: 27].
* **`Peluquero` (`AuditableTenantEntity`):** `Nombre` (`nvarchar(100)`), `Apellido` (`nvarchar(100)`), `Telefono` (`nvarchar(30)?`), `Email` (`nvarchar(150)`), `ColorAgendaHex` (`nvarchar(7)`), `Activo` (`bit`), `[Timestamp] public byte[] RowVersion`[cite: 27]. Índice único `UX_Peluqueros_Local_Email`[cite: 27].
* **`PeluqueroEspecialidad` (`AuditableTenantEntity`):** `PeluqueroId` (`int`), `EspecialidadId` (`int`). Índice único `UX_PeluqueroEspecialidades_Activa` sobre `(TenantId, SucursalId, PeluqueroId, EspecialidadId)` con filtro `[IsDeleted] = 0`[cite: 27].
* **`HorarioLaboral` (`AuditableTenantEntity`):** `PeluqueroId` (`int`), `DiaSemana` (`DayOfWeek`), `HoraInicio` (`time`), `HoraFin` (`time`), `HoraInicioDescanso` (`time?`), `HoraFinDescanso` (`time?`), `Activo` (`bit`, default `true`)[cite: 27].
* **`BloqueoHorario` (`AuditableTenantEntity`):** `PeluqueroId` (`int`), `FechaInicio` (`datetime2`), `FechaFin` (`datetime2`), `Motivo` (`nvarchar(200)`)[cite: 27].
* **`Servicio` (`AuditableTenantEntity`):** `CategoriaId` (`int`), `Nombre` (`nvarchar(150)`), `Descripcion` (`nvarchar(500)?`), `Precio` (`decimal(18,2)`), `DuracionMinutos` (`int`), `Activo` (`bit`), `[Timestamp] public byte[] RowVersion`[cite: 27].
* **`ServicioInsumoReceta` (`AuditableTenantEntity`):** `ServicioId` (`int`), `ProductoId` (`int`), `CantidadUnidadesDescontar` (`int`), `CantidadMedidaEstandar` (`decimal(10,2)`), `UnidadMedida` (`nvarchar(20)`), `DescuentoAutomaticoAlCompletarTurno` (`bit`)[cite: 27].
* **`HistorialPrecioServicio` (`AuditableTenantEntity`):** `ServicioId` (`int`), `PrecioAnterior` (`decimal(18,2)`), `PrecioNuevo` (`decimal(18,2)`), `FechaCambio` (`datetime2`), `UsuarioModificacionId` (`int?`), `Motivo` (`nvarchar(250)?`)[cite: 27].
* **`EstadoTurno` (`AuditableEntity`):** `1 = Pendiente`, `2 = Confirmado`, `3 = En Proceso`, `4 = Completado`, `5 = Cancelado`, `6 = No Asistió`[cite: 27].
* **`MotivoCancelacion` (`AuditableTenantEntity`):** `Descripcion` (`nvarchar(150)`)[cite: 27].
* **`Turno` (`AuditableTenantEntity`):** `ClienteId` (`int`), `PeluqueroId` (`int`), `FechaHoraInicio` (`datetime2`), `FechaHoraFin` (`datetime2`), `EstadoId` (`int`), `MotivoCancelacionId` (`int?`), `MontoTotalEstimado` (`decimal(18,2)`), `RequiereSena` (`bit`, default `false`), `MontoSena` (`decimal(18,2)`, default `0m`), `SenaPagada` (`bit`, default `false`), `FechaPagoSena` (`datetime2?`), `ReferenciaPagoExterno` (`nvarchar(150)?`), `Observaciones` (`nvarchar(500)?`), `[Timestamp] public byte[] RowVersion`[cite: 27].
* **`TurnoServicio` (`AuditableTenantEntity`):** `TurnoId` (`int`), `ServicioId` (`int`), `PrecioHistorico` (`decimal(18,2)`), `DuracionMinutosHistorica` (`int`)[cite: 27].
* **`HistorialEstadoTurno` (`AuditableTenantEntity`):** `TurnoId` (`int`), `EstadoAnteriorId` (`int?`), `EstadoNuevoId` (`int`), `FechaCambio` (`datetime2`), `UsuarioId` (`int?`), `Observaciones` (`nvarchar(300)?`)[cite: 27].

### 2.6 Módulo Stock, Inventario y Compras (`Models/Stock/`)
* **`CategoriaProducto` (`AuditableTenantEntity`):** `Nombre` (`nvarchar(100)`), `Descripcion` (`nvarchar(250)?`)[cite: 27].
* **`Proveedor` (`AuditableTenantEntity`):** `RazonSocial` (`nvarchar(150)`), `Cuit` (`nvarchar(20)?`), `Contacto` (`nvarchar(100)?`), `Telefono` (`nvarchar(30)?`), `Email` (`nvarchar(150)?`), `[Timestamp] public byte[] RowVersion`[cite: 27].
* **`Producto` (`AuditableTenantEntity`):** `CategoriaId` (`int`), `ProveedorId` (`int?`), `CodigoBarras` (`nvarchar(50)?`), `Nombre` (`nvarchar(150)`), `PrecioCosto` (`decimal(18,2)`), `PrecioVenta` (`decimal(18,2)`), `StockActual` (`int`), `StockMinimoAlerta` (`int`), `EsParaVenta` (`bit`), `[Timestamp] public byte[] RowVersion`[cite: 27].
* **`MovimientoStock` (`AuditableTenantEntity`):** `ProductoId` (`int`), `TipoMovimiento` (`TipoMovimientoStockEnum`), `Cantidad` (`int`), `SaldoResultante` (`int`, Required), `Fecha` (`datetime2`), `UsuarioId` (`int?`), `Motivo` (`nvarchar(300)?`)[cite: 27].
* **`HistorialPrecioProducto` (`AuditableTenantEntity`):** `ProductoId` (`int`), `PrecioCostoAnterior` (`decimal(18,2)`), `PrecioCostoNuevo` (`decimal(18,2)`), `PrecioVentaAnterior` (`decimal(18,2)`), `PrecioVentaNuevo` (`decimal(18,2)`), `FechaCambio` (`datetime2`), `UsuarioModificacionId` (`int?`), `Motivo` (`nvarchar(250)?`)[cite: 27].
* **`OrdenCompra` (`AuditableTenantEntity`) y `DetalleOrdenCompra` (`AuditableTenantEntity`)**[cite: 27].

### 2.7 Módulo Ventas, Caja, Comisiones y Liquidaciones (`Models/Ventas/`)
* **`MedioPago` (`AuditableTenantEntity`):** `Nombre` (`nvarchar(50)`), `Descripcion` (`nvarchar(150)?`), `EsEfectivoFisico` (`bit`, default `false`)[cite: 27].
* **`SesionCaja` (`AuditableTenantEntity`):** `UsuarioAperturaId` (`int`), `FechaApertura` (`datetime2`), `MontoInicial` (`decimal(18,2)`), `UsuarioCierreId` (`int?`), `FechaCierre` (`datetime2?`), `TotalVentasEfectivo` (`decimal(18,2)`), `TotalVentasDigital` (`decimal(18,2)`), `TotalIngresosManuales` (`decimal(18,2)`), `TotalEgresosManuales` (`decimal(18,2)`), `MontoFinalEsperado` (`decimal(18,2)?`), `MontoFinalReal` (`decimal(18,2)?`), `Diferencia` (`decimal(18,2)?`), `ObservacionesCierre` (`nvarchar(500)?`), `EstadoAbierta` (`bit`), `[Timestamp] public byte[] RowVersion`[cite: 27].
* **`MovimientoCaja` (`AuditableTenantEntity`):** `SesionCajaId` (`int`), `Tipo` (`nvarchar(50)`), `Monto` (`decimal(18,2)`), `Concepto` (`nvarchar(250)`), `Fecha` (`datetime2`)[cite: 27].
* **`Venta` (`AuditableTenantEntity`):**
  * Columnas: `SesionCajaId` (`int`), `ClienteId` (`int?`), `TurnoId` (`int?`), `UsuarioId` (`int`), `Fecha` (`datetime2`), `Subtotal` (`decimal(18,2)`), `DescuentoSenaAplicado` (`decimal(18,2)`), `Total` (`decimal(18,2)`), `IdempotencyKey` (`nvarchar(64)?` -> **Clave única de operación contra reintentos de red en POS**), `[Timestamp] public byte[] RowVersion`[cite: 27].
  * Índice Único de Idempotencia: `UX_Ventas_Local_IdempotencyKey` sobre `(TenantId, SucursalId, IdempotencyKey)` con filtro `[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0`.
* **`DetalleVenta` (`AuditableTenantEntity`):** `VentaId` (`int`), `TipoItem` (`TipoItemVentaEnum`), `ItemId` (`int`), `Cantidad` (`int`), `PrecioUnitario` (`decimal(18,2)`), `Subtotal` (`decimal(18,2)`), `PeluqueroId` (`int?`)[cite: 27].
* **`Pago` (`AuditableTenantEntity`):** `VentaId` (`int`), `MedioPagoId` (`int`), `Importe` (`decimal(18,2)`), `ReferenciaOperacion` (`nvarchar(100)?`)[cite: 27].
* **`ComisionesConfig` (`AuditableTenantEntity`):** `PeluqueroId` (`int`), `ServicioId` (`int?`), `PorcentajeServicio` (`decimal(5,2)`), `PorcentajeVentaProducto` (`decimal(5,2)`)[cite: 27].
* **`Liquidacion` (`AuditableTenantEntity`):** `PeluqueroId` (`int`), `FechaEmision` (`datetime2`), `FechaDesde` (`datetime2`), `FechaHasta` (`datetime2`), `FechaVencimiento` (`datetime2?`), `FechaPago` (`datetime2?`), `TotalServicios` (`decimal(18,2)`), `TotalProductos` (`decimal(18,2)`), `Bonificaciones` (`decimal(18,2)`), `Descuentos` (`decimal(18,2)`), `TotalPagar` (`decimal(18,2)`), `Observaciones` (`nvarchar(500)?`), `Estado` (`EstadoLiquidacionEnum`), `UsuarioLiquidacionId` (`int?`), `[Timestamp] public byte[] RowVersion`[cite: 27].
* **`DetalleLiquidacion` (`AuditableTenantEntity`):** `LiquidacionId` (`int`), `TurnoId` (`int?`), `DetalleVentaId` (`int?`), `MontoComisionCalculado` (`decimal(18,2)`)[cite: 27].

---

## 3. FILTROS GLOBALES Y DATA SEEDING EN `ApplicationDbContext.cs`

1. **Inyección de `ITenantProvider` y Filtro Global Compuesto:**
   Para toda entidad que hereda de `AuditableTenantEntity`:
   ```csharp
   e => !e.IsDeleted 
        && e.TenantId == CurrentTenantId 
        && (CurrentSucursalId == null || e.SucursalId == null || e.SucursalId == CurrentSucursalId)
   ```
2. **Sellado Automático en `SaveChangesAsync`:**
   Al insertar cualquier `ITenantEntity` (`EntityState.Added`), si `TenantId <= 0` se asigna `CurrentTenantId`, y si `SucursalId == null || SucursalId <= 0` se asigna `CurrentSucursalId ?? 1`[cite: 27].
3. **Data Seeding Completo (`HasData`):**
   Incluye `Roles` (1-5), `EstadosTurno` (1-6), `PlanesSuscripcion` (1-3), `Tenant #1`, `Sucursal #1`, `SuscripcionTenant #1`, `ConfiguracionSalon #1`, `Usuario #1` (`admin`), `MediosPago` (1-7 con `EsEfectivoFisico = true` para Efectivo), `MotivosCancelacion` (1-4), `TiposCabello` (1-6), `TiposRostro` (1-5), `Especialidades` (1-5), `CategoriasServicio` (1-4), `CategoriasProducto` (1-4), `Peluquero #1` con `HorariosLaborales` y `ComisionesConfig`, `Servicios #1-#3`, `Proveedor #1` y `Productos #1-#2`[cite: 27].
```