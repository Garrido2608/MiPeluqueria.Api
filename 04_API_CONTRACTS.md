
# 04 - CONTRATOS DE INTERFAZ, ENDPOINTS HTTP, PAGINACIÓN Y DTOs (CAPA 4 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)[cite: 28]
**Documentos Padres Vinculados:** `00_AUDITORIA_Y_DEUDA_TECNICA.md`, `01_SYSTEM_CONSTRAINTS.md`, `02_ARCHITECTURE.md`, `03_DATA_MODEL.md`[cite: 28]
**Estado:** INMUTABLE - ESPECIFICACIÓN DE CONTRATOS API Y SEGURIDAD RBAC[cite: 28]

---

## 1. ESTÁNDARES TRANSVERSALES DE COMUNICACIÓN HTTP

### 1.1 Envoltorio Universal `ApiResponse<T>` (`DTOs/Common/ApiResponse.cs`)
Absolutamente todos los endpoints de los 22 controladores retornan `ApiResponse<T>` serializado en `camelCase`[cite: 28].

```csharp
namespace MiPeluqueria.Api.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public Dictionary<string, string[]>? FieldErrors { get; set; }
        public List<string>? Warnings { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Operación exitosa.", List<string>? warnings = null) => new()
        {
            Success = true,
            Message = message,
            Data = data,
            Warnings = warnings
        };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null, Dictionary<string, string[]>? fieldErrors = null) => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string> { message },
            FieldErrors = fieldErrors
        };

        public static ApiResponse<T> Exito(T data, string message = "Operación exitosa.", List<string>? warnings = null) => Ok(data, message, warnings);
        public static ApiResponse<T> Falla(string message, List<string>? errors = null) => Fail(message, errors);
    }
}
```

### 1.2 Paginación Empresarial Obligatoria (`PagedResult<T>` y `PaginationFilterDto`)
Todos los endpoints de listados transaccionales e históricos (`Clientes`, `Turnos`, `Historial de Caja`, `Ventas`, `Liquidaciones`, `Órdenes de Compra`, `Kardex de Stock` e `Historial de Sesiones`) implementan paginación en base de datos (`Skip` / `Take`) retornando `ApiResponse<PagedResult<TDto>>`[cite: 28].

```csharp
namespace MiPeluqueria.Api.DTOs.Common
{
    public class PaginationFilterDto
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 15;

        public int PageNumber { get; set; } = 1;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value < 1 ? 15 : value);
        }
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}
```

### 1.3 Identidad JWT, Header `X-Idempotency-Key` y Contexto Público
1. **Identidad por Token JWT:** En `CajaController`, `VentasController`, `OrdenesCompraController`, `TurnosController` y `RecetasServicioController`, el `UsuarioId` ejecutor se extrae **exclusivamente** del claim `ClaimTypes.NameIdentifier` del token JWT[cite: 28].
2. **Idempotencia en POS (`X-Idempotency-Key`):** `POST /api/ventas` lee el header opcional `X-Idempotency-Key` (o la propiedad `IdempotencyKey` del `CrearVentaDto`). Si no se envía en el body pero viene en el header, el controlador lo asigna al DTO.
3. **Contexto en Endpoints Públicos (`/api/public/*`):** Resuelve el salón destino leyendo QueryString (`?sucursalId` / `?subdominioSlug`), Headers (`X-Tenant-Id` / `X-Sucursal-Id`) o fallback a `1`[cite: 28].

---

## 2. MATRIZ DE AUTORIZACIÓN RBAC POR ROL

| Nivel de Acceso | Roles Permitidos | Controladores / Endpoints Alcanzados |
| :--- | :--- | :--- |
| **Público (`[AllowAnonymous]`)** | Cualquier visitante | `PortalReservasController` (`/api/public/*`), `WebhooksController` (`/api/webhooks/pagos/*`), `AuthController` (`login`, `registro`, `verificar-email`, `olvide-password`, `restablecer-password`, `refresh-token`), `GET` de `CatalogosController` y `GET` de `ConfiguracionSalonController`. |
| **Portal Cliente (`[Authorize]`)** | `Cliente` + Roles Internos | `AuthController.CambiarPassword`, consulta y cancelación con anticipación de sus propios turnos. |
| **Operativo de Salón** | `Administrador`, `Recepcionista`, `Cajero`, `Peluquero`, `SuperAdminSaaS` | `TurnosController`, `ClientesController`, `FichasTecnicasController`, `BloqueosController`, `RecetasServicioController`, `GET` de `PeluquerosController`, `GET` de `ServiciosController`, `GET` de `ProductosController`. |
| **Caja, POS y Compras** | `Administrador`, `Recepcionista`, `Cajero`, `SuperAdminSaaS` | `CajaController`, `VentasController`, `OrdenesCompraController`, `ProveedoresController`, `ProductosController.RegistrarMovimiento`. |
| **Gerencial / Dueño** | `Administrador`, `SuperAdminSaaS` | `DashboardController`, `ComisionesController` (`POST/PUT/DELETE`), `LiquidacionesController`, `UsuariosController`, `PeluquerosController` (`POST/PUT/DELETE/Horarios`), `ServiciosController` (`POST/PUT/DELETE`), `CatalogosController` (`POST/PUT/DELETE`), `ConfiguracionSalonController` (`PUT`), `SucursalesController`, `SuscripcionesController`. |

---

## 3. CATÁLOGO DEFINITIVO DE DTOs CORREGIDOS Y UNIFICADOS (`DTOs/`)

### 3.1 Módulo Seguridad (`DTOs/Seguridad/`)
* **`RefreshTokenRequestDto.cs`:** `string Token`, `string RefreshToken`[cite: 28].
* **`CrearUsuarioStaffDto.cs`:** `string Username`, `string Email`, `string Password`, `int RolId`, `int? PeluqueroId`[cite: 28].
* **`RegistroUsuarioDto.cs`:** `RolId` por defecto fijado en `4` (`Cliente`)[cite: 28].

### 3.2 Módulo Common (`DTOs/Common/`)
* **`ConfiguracionSalonDto.cs` (Único DTO unificado):** Incluye `Id`, `NombreSalon`, `NombreComercial`, `RazonSocial`, `Cuit`, `Direccion`, `Telefono`, `EmailContacto`, `MonedaIso`, `Moneda`, `TimeZoneId`, `HorarioAtencionTexto`, `IntervaloTurnosMinutos`, `MinutosToleranciaEspera`, `HorasAnticipacionCancelacion`, `PorcentajeSenaReserva`, `NotificacionesWhatsAppActivas`, `NotificacionesEmailActivas`[cite: 28].
* **`ActualizarCatalogoItemDto.cs`:** `string Nombre`, `string? Descripcion`, `bool? EsEfectivoFisico`[cite: 28].

### 3.3 Módulo Turnos y Agenda (`DTOs/Turnos/`)
* **`TurnoResponseDto.cs`:** Incluye `Id`, `ClienteId`, `ClienteNombre`, `ClienteTelefono`, `PeluqueroId`, `PeluqueroNombre`, `ColorAgendaHex`, `FechaHoraInicio`, `FechaHoraFin`, `DuracionTotalMinutos`, `EstadoId`, `EstadoNombre`, `MotivoCancelacionId`, `MotivoCancelacionDescripcion`, `MontoTotalEstimado`, `RequiereSena`, `MontoSena`, `SenaPagada`, `FechaPagoSena`, `SaldoPendienteCobro`, `Observaciones`, `List<int> ServiciosIds`, `List<string> Servicios`, `List<TurnoServicioDetalleDto> ServiciosDetalle`[cite: 28].
* **`DisponibilidadHorarioResponseDto.cs`:** Incluye `PeluqueroId`, `PeluqueroNombre`, `Fecha`, `IntervaloMinutos`, `DuracionTotalMinutos`, `List<string> HorariosDisponibles` y `List<SlotHorarioDisponibleDto> Slots`[cite: 28].
* **`BloqueoHorarioResponseDto.cs`:** `Id`, `PeluqueroId`, `PeluqueroNombre`, `FechaInicio`, `FechaFin`, `Motivo`[cite: 28].

### 3.4 Módulo Caja, Ventas y Liquidaciones (`DTOs/Ventas/`)
* **`SesionCajaResponseDto.cs` y `MovimientoCajaResponseDto.cs`:**
  * En `GET /api/caja/estado-actual` y `GET /api/caja/{id:int}` se incluye la lista `Movimientos`[cite: 28].
  * En `GET /api/caja/historial` (listado paginado), `Movimientos` se retorna vacío para evitar *Over-fetching*.
* **`CrearVentaDto.cs`, `PagoRequestDto.cs` y `VentaResponseDto.cs`:**
  * `CrearVentaDto` agrega `public string? IdempotencyKey { get; set; }`.
  * `PagoRequestDto` y `VentaPagoItemResponseDto` agregan `public string? ReferenciaOperacion { get; set; }`.
  * `VentaResponseDto` agrega `public decimal Subtotal { get; set; }` y `public decimal DescuentoSenaAplicado { get; set; }`.
* **`LiquidacionResponseDto.cs` y `DetalleLiquidacionItemDto.cs`:**
  * `LiquidacionResponseDto` incluye `FechaEmision`, `Bonificaciones`, `Descuentos`, `Observaciones`[cite: 28].
  * `DetalleLiquidacionItemDto` incluye `string ConceptoNombre`[cite: 28].

---

## 4. ESPECIFICACIÓN DE ENDPOINTS POR CONTROLADOR (22 CONTROLADORES)

1. **`AuthController` (`/api/auth`):** `POST login`, `POST refresh-token`, `POST registro`, `POST verificar-email`, `POST cambiar-password`, `POST olvide-password`, `POST restablecer-password`[cite: 28].
2. **`UsuariosController` (`/api/usuarios`):** `GET`, `GET {id:int}`, `POST` (alta staff), `PUT {id:int}/estado`, `PATCH {id:int}/rol`, `POST {id:int}/desbloquear`, `GET historial-sesiones` (paginado)[cite: 28].
3. **`CatalogosController` (`/api/catalogos`):** `GET todos`, `GET maestros`, `GET estados-turno`, `GET roles`, y CRUD completo (`GET`, `POST`, `PUT {id:int}`, `DELETE {id:int}`) para `tipos-cabello`, `tipos-rostro`, `categorias-servicio`, `especialidades`, `motivos-cancelacion`, `categorias-producto` y `medios-pago`[cite: 28].
4. **`ClientesController` (`/api/clientes`):** `GET` (paginado), `GET {id:int}`, `POST`, `PUT {id:int}`, `DELETE {id:int}`[cite: 28].
5. **`FichasTecnicasController` (`/api/fichastecnicas`):** `GET cliente/{clienteId:int}`, `GET {id:int}`, `POST` (`CrearFichaTecnicaDto`), `PUT {id:int}` (`ActualizarFichaTecnicaDto`), `DELETE {id:int}`[cite: 28].
6. **`PeluquerosController` (`/api/peluqueros`):** `GET`, `GET {id:int}`, `POST` (`CrearPeluqueroDto`), `PUT {id:int}` (`ActualizarPeluqueroDto`), `DELETE {id:int}`, `GET {id:int}/horarios`, `PUT {id:int}/horarios`[cite: 28].
7. **`ServiciosController` (`/api/servicios`):** `GET`, `GET {id:int}`, `POST` (`CrearServicioDto`), `PUT {id:int}` (`ActualizarServicioDto`), `DELETE {id:int}`[cite: 28].
8. **`RecetasServicioController` (`/api/recetas-servicio`):** `GET servicio/{servicioId:int}`, `PUT`, `POST turno/{turnoId:int}/consumir-gabinete`[cite: 28].
9. **`BloqueosController` (`/api/bloqueos`):** `GET peluquero/{peluqueroId:int}`, `POST`, `DELETE {id:int}`[cite: 28].
10. **`TurnosController` (`/api/turnos`):** `GET` (paginado), `GET {id:int}`, `GET disponibilidad`, `GET cliente/{clienteId:int}`, `GET {id:int}/historial`, `POST`, `PUT {id:int}/reprogramar`, `PATCH {id:int}/estado`[cite: 28].
11. **`PortalReservasController` (`/api/public`) y `WebhooksController` (`/api/webhooks/pagos`):** `GET catalogo-servicios`, `GET peluqueros`, `GET disponibilidad`, `POST reservar`, `POST {proveedor}`[cite: 28].
12. **`ProductosController` (`/api/productos`), `ProveedoresController` (`/api/proveedores`) y `OrdenesCompraController` (`/api/ordenes-compra`):** CRUD con `ActualizarProductoDto` / `ActualizarProveedorDto`, `GET` paginados y `GET {id:int}/kardex` paginado con `KardexItemResponseDto`[cite: 28].
13. **`CajaController` (`/api/caja`), `VentasController` (`/api/ventas`), `ComisionesController` (`/api/comisiones`) y `LiquidacionesController` (`/api/liquidaciones`):** Incluye `GET /api/caja/{id:int}`, `POST /api/ventas` idempotente y `POST /api/liquidaciones/{id:int}/anular`[cite: 28].
14. **`ConfiguracionSalonController`, `DashboardController`, `SucursalesController` y `SuscripcionesController`:** Todos operando vía Servicios e `IUnitOfWork` devolviendo DTOs tipados[cite: 28].
