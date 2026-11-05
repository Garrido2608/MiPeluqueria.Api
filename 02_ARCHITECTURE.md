# 02 - ARQUITECTURA FÍSICA, GRAFO DE DEPENDENCIAS E INFRAESTRUCTURA TRANSVERSAL (CAPA 2 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)[cite: 26]
**Documentos Padres Vinculados:** `00_AUDITORIA_Y_DEUDA_TECNICA.md` y `01_SYSTEM_CONSTRAINTS.md`[cite: 26]
**Estado:** INMUTABLE - ESPECIFICACIÓN ESTRUCTURAL Y DE PIPELINE[cite: 26]

---

## 1. TOPOLOGÍA FÍSICA DEL MONOLITO MODULAR BACKEND (`MiPeluqueria.Api`)

El backend se mantiene como un único proyecto `.csproj` (`MiPeluqueria.Api.csproj`) bajo el patrón **Modular Monolith con Clean Layered Separation**[cite: 26].

### 1.1 Árbol de Directorios Objetivo y Responsabilidades
```text
MiPeluqueria.Api/
├── 00_AUDITORIA_Y_DEUDA_TECNICA.md
├── 01_SYSTEM_CONSTRAINTS.md
├── 02_ARCHITECTURE.md
├── 03_DATA_MODEL.md
├── 04_API_CONTRACTS.md
├── 05_BUSINESS_RULES.md
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── MiPeluqueria.Api.csproj
│
├── Controllers/                                  # CAPA DE PRESENTACIÓN HTTP (Cero acceso a DbContext)
│   ├── AuthController.cs                         # Inyecta IAuthService ([EnableRateLimiting("StrictAuthOtpPolicy")])
│   ├── UsuariosController.cs                     # Inyecta IUsuarioService ([Authorize(Roles = "Administrador,SuperAdminSaaS")])
│   ├── ClientesController.cs                     # Inyecta IClienteService ([Authorize])
│   ├── FichasTecnicasController.cs               # Inyecta IFichaTecnicaService ([Authorize])
│   ├── PeluquerosController.cs                   # Inyecta IPeluqueroService ([Authorize])
│   ├── ServiciosController.cs                    # Inyecta IServicioService ([Authorize])
│   ├── RecetasController.cs                      # Inyecta IRecetaServicioService ([Authorize])
│   ├── BloqueosController.cs                     # Inyecta IBloqueoHorarioService ([Authorize])
│   ├── TurnosController.cs                       # Inyecta ITurnoService ([Authorize])
│   ├── PortalReservasController.cs               # Inyecta Servicios vía fachada limpia ([AllowAnonymous])
│   ├── ProductosController.cs                    # Inyecta IProductoService ([Authorize])
│   ├── ProveedoresController.cs                  # Inyecta IProveedorService ([Authorize])
│   ├── OrdenesCompraController.cs                # Inyecta IOrdenCompraService ([Authorize])
│   ├── CajaController.cs                         # Inyecta ICajaService ([Authorize])
│   ├── VentasController.cs                       # Inyecta IVentaService ([Authorize]) + Lee Header X-Idempotency-Key
│   ├── ComisionesController.cs                   # Inyecta IComisionConfigService ([Authorize])
│   ├── LiquidacionesController.cs                # Inyecta ILiquidacionService ([Authorize])
│   ├── CatalogosController.cs                    # Inyecta ICatalogoService (Eliminado DbContext directo)
│   ├── ConfiguracionSalonController.cs           # Inyecta IConfiguracionSalonService (Eliminada RAM estática)
│   ├── DashboardController.cs                    # Inyecta IDashboardService ([Authorize(Roles = "Administrador,SuperAdminSaaS")])
│   ├── SucursalesController.cs                   # Inyecta ISaaSService (Eliminado DbContext directo)
│   └── SuscripcionesController.cs                # Inyecta ISaaSService (Eliminado DbContext directo)
│
├── Services/                                     # CAPA DE APLICACIÓN Y LÓGICA DE NEGOCIO (Solo inyecta IUnitOfWork)
│   ├── Background/
│   │   └── RecordatorioTurnosBackgroundService.cs# Worker Multi-Tenant iterativo por Sucursal activa
│   ├── Interfaces/
│   │   ├── ITenantProvider.cs
│   │   ├── IAuthService.cs
│   │   ├── IUsuarioService.cs
│   │   ├── IClienteService.cs
│   │   ├── IFichaTecnicaService.cs
│   │   ├── IPeluqueroService.cs
│   │   ├── IServicioService.cs
│   │   ├── IRecetaServicioService.cs
│   │   ├── IBloqueoHorarioService.cs
│   │   ├── ITurnoService.cs
│   │   ├── IProductoService.cs
│   │   ├── IProveedorService.cs
│   │   ├── IOrdenCompraService.cs
│   │   ├── ICajaService.cs
│   │   ├── IVentaService.cs
│   │   ├── IComisionConfigService.cs
│   │   ├── ILiquidacionService.cs
│   │   ├── ICatalogoService.cs
│   │   ├── IConfiguracionSalonService.cs
│   │   ├── IDashboardService.cs
│   │   └── ISaaSService.cs                       # [NUEVO] Gestión de Tenants, Sucursales y Suscripciones
│   └── Implementations/
│       └── (Implementaciones 1:1 con Interfaces, CERO Reflection, CERO N+1 y CERO ApplicationDbContext directo)
│
├── Repositories/                                 # CAPA DE ACCESO A DATOS (Heredan de GenericRepository<T>)
│   ├── Interfaces/
│   │   ├── IGenericRepository.cs                 # Incluye GetByIdsDictionaryAsync para prevenir N+1
│   │   ├── IUnitOfWork.cs                        # Expone los 19 Repositorios + ExecuteInTransactionAsync
│   │   ├── IUsuarioRepository.cs
│   │   ├── IClienteRepository.cs
│   │   ├── IFichaTecnicaRepository.cs
│   │   ├── IPeluqueroRepository.cs
│   │   ├── IServicioRepository.cs
│   │   ├── IRecetaServicioRepository.cs          # [NUEVO] Encapsula ServicioInsumoReceta
│   │   ├── IBloqueoHorarioRepository.cs
│   │   ├── ITurnoRepository.cs
│   │   ├── IProductoRepository.cs
│   │   ├── IProveedorRepository.cs
│   │   ├── IOrdenCompraRepository.cs
│   │   ├── ISesionCajaRepository.cs
│   │   ├── IVentaRepository.cs
│   │   ├── IComisionConfigRepository.cs
│   │   ├── ILiquidacionRepository.cs
│   │   ├── ICatalogoRepository.cs
│   │   ├── IConfiguracionSalonRepository.cs
│   │   ├── IDashboardRepository.cs
│   │   └── ISaaSRepository.cs                    # [NUEVO] Encapsula Tenant, Sucursal, PlanSuscripcion, SuscripcionTenant
│   └── Implementations/
│       └── (Implementaciones heredando de GenericRepository<T> + UnitOfWork.cs)
│
├── Data/                                         # CAPA DE PERSISTENCIA Y CONFIGURACIÓN EF CORE
│   ├── Context/
│   │   └── ApplicationDbContext.cs               # Inyecta ITenantProvider, QueryFilters Multi-Tenant + Auditoría JSON
│   └── Configurations/
│       └── (12 archivos IEntityTypeConfiguration<T>)
│
├── Models/                                       # ENTIDADES DE DOMINIO
│   ├── Common/
│   │   ├── ISoftDelete.cs
│   │   ├── ITenantEntity.cs                      # [RENOMBRADO de ITentantEntity.cs]
│   │   ├── AuditableEntity.cs
│   │   ├── AuditableTenantEntity.cs              # [NUEVO] Hereda de AuditableEntity e ITenantEntity
│   │   ├── AuditLog.cs
│   │   └── ConfiguracionSalon.cs
│   ├── Clientes/
│   ├── Turnos/
│   ├── Stock/
│   ├── Ventas/
│   ├── Seguridad/
│   └── SaaS/
│
├── DTOs/                                         # CONTRATOS DE DATOS (Request / Response)
├── Validators/                                   # VALIDACIÓN DE ENTRADA (FluentValidation)
├── Mappings/
│   └── MappingProfile.cs
├── Middlewares/
│   ├── GlobalExceptionMiddleware.cs
│   └── TenantResolutionMiddleware.cs
├── Integrations/
│   ├── Email/
│   ├── Notifications/
│   └── Payments/
└── Exceptions/
    ├── BusinessRuleException.cs
    ├── ForbiddenAccessException.cs               # [NUEVO] Mapeado a 403 Forbidden
    ├── NotFoundException.cs
    └── UnauthorizedAppException.cs
```

---

## 2. GRAFO DE DEPENDENCIAS, `GenericRepository<T>` ANTI N+1 Y `IUnitOfWork` RESILIENTE

### 2.1 Regla de Flujo Unidireccional
$$\text{Controller} \longrightarrow \text{IService} \longrightarrow \text{IUnitOfWork} \longrightarrow \text{IRepository} \longrightarrow \text{ApplicationDbContext}$$

### 2.2 Contrato Base `IGenericRepository<T>` (Con Batch Loading $O(1)$ Anti N+1)
Para erradicar las consultas $O(N)$ dentro de bucles `foreach`, `IGenericRepository<T>` incorpora `GetByIdsDictionaryAsync`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
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
}
```

Implementación en `GenericRepository<T>`:
```csharp
public virtual async Task<Dictionary<int, T>> GetByIdsDictionaryAsync(
    IEnumerable<int> ids,
    bool tracking = false,
    CancellationToken ct = default)
{
    var distinctIds = ids.Distinct().ToList();
    if (distinctIds.Count == 0) return new Dictionary<int, T>();

    var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
    return await query
        .Where(e => distinctIds.Contains(e.Id))
        .ToDictionaryAsync(e => e.Id, ct);
}
```

### 2.3 Contrato Consolidado `IUnitOfWork` y Patrón `ExecuteInTransactionAsync`
Dado que `Program.cs` utiliza `EnableRetryOnFailure` (`SqlServerRetryingExecutionStrategy`)[cite: 4, 26], toda transacción explícita debe ejecutarse a través de `ExecuteInTransactionAsync`, el cual encapsula `CreateExecutionStrategy().ExecuteAsync(...)` y soporta anidamiento reentrante entre servicios:

```csharp
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
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
}
```

### 2.4 Límites Transaccionales Estrictos
1. **Operaciones que exigen `ExecuteInTransactionAsync`:**
   * `TurnoService.CreateAsync` y `ReprogramarAsync` (con `IsolationLevel.Serializable` para impedir *Phantom Reads* de doble reserva).
   * `TurnoService.CambiarEstadoAsync` (al pasar a `Completado`, envuelve el cambio de estado y el descuento de insumos en `RecetaServicioService`).
   * `VentaService.RegistrarVentaAsync` y `AnularVentaAsync`.
   * `OrdenCompraService.RecepcionarOrdenAsync`.
   * `LiquidacionService.LiquidarPeriodoAsync`, `RegistrarPagoLiquidacionAsync` y `AnularLiquidacionAsync`.
2. **Prohibición de I/O de Red Externo dentro de Transacciones SQL:**
   * Las llamadas a `IPaymentGatewayService`, `IEmailService` e `IWhatsAppService` deben ejecutarse **siempre fuera** del delegado `ExecuteInTransactionAsync` (después del `COMMIT` en base de datos) para no bloquear latches ni conexiones de SQL Server.

---

## 3. PIPELINE HTTP, SEGURIDAD MULTI-TENANT EN CACHÉ Y MANEJO DE EXCEPCIONES

### 3.1 Orden Inmutable de Middlewares en `Program.cs`
```csharp
var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### 3.2 Validación Cruzada `TenantId <-> SucursalId` en `TenantResolutionMiddleware` con `IMemoryCache`
Para cerrar el vector OWASP A01 sin penalizar la base de datos en cada request:
1. `TenantResolutionMiddleware` extrae `tenantId` y `sucursalId` de los claims del JWT (si está autenticado, los claims tienen prioridad absoluta sobre los headers) o de los headers `X-Tenant-Id` / `X-Sucursal-Id` / QueryString en rutas públicas[cite: 11, 28].
2. Verifica en `IMemoryCache` la clave `$"SaaS_Valid_T{tenantId}_S{sucursalId}"` (`SlidingExpiration = TimeSpan.FromMinutes(10)`).
3. Si no existe en caché, consulta en `ApplicationDbContext` (`Sucursales.AsNoTracking().AnyAsync(s => s.Id == sucursalId && s.TenantId == tenantId && s.Activa && !s.IsDeleted)`).
4. Si la combinación es inválida, lanza `ForbiddenAccessException("La sucursal indicada no pertenece al salón actual o se encuentra inactiva.")` devolviendo `403 Forbidden`.

### 3.3 Mapeo Exhaustivo de Códigos HTTP en `GlobalExceptionMiddleware.cs`
`GlobalExceptionMiddleware` nunca debe filtrar detalles de `SqlException` al cliente. El switch de estado HTTP debe mapear:
* `NotFoundException` o `KeyNotFoundException` $\rightarrow$ `404 Not Found`[cite: 11].
* `UnauthorizedAppException` o `UnauthorizedAccessException` $\rightarrow$ `401 Unauthorized`[cite: 11].
* `ForbiddenAccessException` $\rightarrow$ `403 Forbidden`.
* `DbUpdateConcurrencyException` $\rightarrow$ `409 Conflict` (Mensaje: *"El registro fue modificado simultáneamente por otro usuario. Recargue la pantalla e intente nuevamente."*).
* `BusinessRuleException`, `InvalidOperationException` o `ArgumentException` $\rightarrow$ `400 Bad Request`[cite: 11].
* `_` (Excepciones no controladas / `SqlException`) $\rightarrow$ `500 Internal Server Error` (Mensaje genérico seguro en producción, logueando el stack trace completo en `ILogger`)[cite: 11].

---

## 4. TOLERANCIA A FALLOS EN INTEGRACIONES EXTERNAS (SMTP, WHATSAPP Y WEBHOOKS)

1. **Tolerancia a Fallos en `SmtpEmailService` (Timeout 5000ms):**
   * `SmtpClient` debe configurarse con `Timeout = 5000` (5 segundos) y ejecutarse con `CancellationTokenSource(TimeSpan.FromSeconds(5))`.
   * Si el servidor SMTP externo falla o excede el timeout, `EnviarEmailAsync` captura la excepción, registra un `LogWarning` y retorna `false` sin lanzar excepción hacia arriba, permitiendo que el servicio llamador agregue un `Warning` en `ApiResponse` sin abortar el registro del usuario o la reserva del turno.
2. **Validación Criptográfica `HMAC-SHA256` en `PaymentGatewayService`:**
   * Cuando `Payments:{proveedor}:WebhookSecret` esté configurado, `ValidarFirmaWebhook` debe calcular el hash `HMACSHA256` sobre el payload/manifiesto recibido y compararlo en tiempo constante (`CryptographicOperations.FixedTimeEquals`) contra la firma del header `x-signature`.

---

## 5. ARQUITECTURA MODULAR DEL FRONTEND (`mipeluqueria-saas-web`)

### 5.1 Árbol de Directorios (`src/`) con *Barrel Files* e Idempotencia
```text
mipeluqueria-saas-web/
├── .env.local
├── package.json
├── tsconfig.json
└── src/
    ├── middleware.ts
    ├── context/
    │   └── appproviders.tsx                      # QueryClientProvider + Toaster (Sonner) + AuthContext
    ├── lib/
    │   ├── axios.ts                              # Interceptores JWT, Tenant, RefreshToken y X-Idempotency-Key
    │   ├── cloudinary.ts                         # Subida Direct-to-Cloud de fotos de Fichas Técnicas
    │   └── formatters.ts
    ├── types/                                    # 8 módulos + api.types.ts (Barrel File)
    ├── services/                                 # 8 módulos + api.services.ts (Barrel File)
    ├── schemas/                                  # Esquemas Zod espejados con FluentValidation
    ├── hooks/                                    # Custom Hooks con TanStack Query
    ├── components/ui/
    └── app/
```

### 5.2 Especificación del Manifiesto `package.json`
```json
{
  "name": "mipeluqueria-saas-frontend",
  "version": "1.0.0",
  "private": true,
  "scripts": {
    "dev": "next dev",
    "build": "next build",
    "start": "next start",
    "lint": "next lint"
  },
  "dependencies": {
    "@hookform/resolvers": "^3.9.0",
    "@tanstack/react-query": "^5.59.0",
    "axios": "^1.7.7",
    "clsx": "^2.1.1",
    "date-fns": "^4.1.0",
    "lucide-react": "^0.454.0",
    "next": "14.2.15",
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "react-hook-form": "^7.53.0",
    "recharts": "^2.13.0",
    "sonner": "^1.5.0",
    "zod": "^3.23.8"
  },
  "devDependencies": {
    "@types/node": "^20.14.10",
    "@types/react": "^18.3.3",
    "@types/react-dom": "^18.3.0",
    "autoprefixer": "^10.4.20",
    "postcss": "^8.4.47",
    "tailwindcss": "^3.4.14",
    "typescript": "^5.6.3"
  }
}
