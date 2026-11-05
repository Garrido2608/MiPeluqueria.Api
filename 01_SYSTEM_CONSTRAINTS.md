# 01 - RESTRICCIONES DEL SISTEMA, ROL ARQUITECTÓNICO Y STACK INNEGOCIABLE (CAPA 1 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` + `mipeluqueria-saas-web`)
**Metodología:** Specification-Driven Development (SDD) para Agentes Autónomos (Google Antigravity)
**Estado:** INMUTABLE - LEY SUPREMA DEL REPOSITORIO

---

## 1. DIRECTIVA PRIMARIA PARA EL AGENTE DE IA (SYSTEM ROLE)

Actuás como un **Principal Software Architect & Senior Full-Stack Engineer (.NET 8 / Next.js 14)** especializado en sistemas SaaS empresariales de alta concurrencia para gestión integral de peluquerías y barberías.

Toda generación, refactorización o corrección de código que ejecutes en este repositorio debe obedecer de forma estricta e innegociable las restricciones técnicas, patrones arquitectónicos y prohibiciones establecidas en este documento y en las capas subsiguientes (`00_AUDITORIA_Y_DEUDA_TECNICA.md`, `02_ARCHITECTURE.md`, `03_DATA_MODEL.md`, `04_API_CONTRACTS.md`, `05_BUSINESS_RULES.md`).

**Regla de Oro de Ejecución:** Tenés terminantemente prohibido improvisar tecnologías, cambiar convenciones de rutas, omitir validaciones o introducir "parches" basados en Reflection para ocultar discrepancias de tipado.

---

## 2. STACK TECNOLÓGICO INNEGOCIABLE — BACKEND (`MiPeluqueria.Api`)

### 2.1 Plataforma y Configuración de Compilación
* **SDK & Runtime:** .NET 8.0 (`<TargetFramework>net8.0</TargetFramework>`).
* **Lenguaje:** C# 12.
* **Tipado y Nulabilidad:** `<Nullable>enable</Nullable>` e `<ImplicitUsings>enable</ImplicitUsings>` obligatorios. El código debe compilar con **0 errores y 0 advertencias de nulabilidad (`CS8600` - `CS8625`)**.
* ** Paradigma de Proyecto:** **Monolito Modular en un único `.csproj` (`MiPeluqueria.Api.csproj`)**. Queda prohibido dividir la solución en múltiples proyectos de librería de clases (`.csproj`) a menos que se ordene explícitamente en una fase futura. La separación de responsabilidades se aplica mediante aislamiento estricto de carpetas y namespaces.

### 2.2 Motor de Base de Datos y ORM
* **Motor RDBMS:** Microsoft SQL Server (compatibilidad local y cloud con resiliencia activada: `EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null)`).
* **ORM:** Entity Framework Core 8 (`8.0.8+`) bajo enfoque **Code-First con Fluent API puro**.
* **Configuración de Entidades:** Toda definición de tablas, claves compuestas, índices filtrados, precisión decimal y comportamientos de borrado (`DeleteBehavior`) debe residir en clases `IEntityTypeConfiguration<T>` dentro de `Data/Configurations/`.

### 2.3 Paquetes NuGet Oficiales y Versiones
* `Microsoft.EntityFrameworkCore` (`8.0.8`)
* `Microsoft.EntityFrameworkCore.SqlServer` (`8.0.8`)
* `Microsoft.EntityFrameworkCore.Tools` (`8.0.8`)
* `Microsoft.AspNetCore.Authentication.JwtBearer` (`8.0.8`)
* `AutoMapper.Extensions.Microsoft.DependencyInjection` (`12.0.1`+)
* `FluentValidation` & `FluentValidation.DependencyInjectionExtensions` (`11.x` / `12.x`)
* `BCrypt.Net-Next` (`4.2.0`)
* `Swashbuckle.AspNetCore` (`6.6.2`)

---

## 3. ARQUITECTURA DE MULTI-TENANCY Y AISLAMIENTO POR LOCAL (MODELO 1 A 1)

El sistema opera bajo un modelo comercial donde el propietario de la plataforma (`SuperAdminSaaS`) alquila el software por suscripción a salones/peluquerías independientes, o administra múltiples locales de forma totalmente aislada:

1. **Interfaz Obligatoria (`ITenantEntity`):**
   * Ubicada en `Models/Common/ITenantEntity.cs` (corrigiendo el typo histórico `ITentantEntity.cs`).
   * Define `public int TenantId { get; set; }` y `public int? SucursalId { get; set; }`.
2. **Clasificación de Tablas:**
   * **Tablas Globales del Sistema (NO implementan `ITenantEntity`):**
     * `Rol` (IDs fijos inmutables: `1 = Administrador`, `2 = Recepcionista`, `3 = Peluquero`, `4 = Cliente`, `5 = Cajero`).
     * `EstadoTurno` (IDs fijos inmutables: `1 = Pendiente`, `2 = Confirmado`, `3 = En Proceso`, `4 = Completado`, `5 = Cancelado`, `6 = No Asistió`).
     * `PlanSuscripcion`, `Tenant`, `Sucursal`, `SuscripcionTenant`.
   * **Tablas Aisladas por Local/Salón (DEBEN implementar `ITenantEntity`):**
     * Absolutamente todas las entidades operativas y de catálogo configurable del salón (`Cliente`, `FichaTecnica`, `TipoCabello`, `TipoRostro`, `Peluquero`, `Especialidad`, `HorarioLaboral`, `BloqueoHorario`, `CategoriaServicio`, `Servicio`, `ServicioInsumoReceta`, `HistorialPrecioServicio`, `MotivoCancelacion`, `Turno`, `HistorialEstadoTurno`, `CategoriaProducto`, `Proveedor`, `Producto`, `MovimientoStock`, `HistorialPrecioProducto`, `OrdenCompra`, `DetalleOrdenCompra`, `SesionCaja`, `Venta`, `DetalleVenta`, `MedioPago`, `Pago`, `MovimientoCaja`, `ComisionesConfig`, `Liquidacion`, `DetalleLiquidacion`, `Usuario`, `HistorialSesion`, `RefreshToken`, `ConfiguracionSalon`, `AuditLog`).
3. **Filtro Global y Sello Automático en `ApplicationDbContext`:**
   * Toda consulta EF Core sobre una entidad `ITenantEntity` debe filtrarse automáticamente en `OnModelCreating` por:
     `!e.IsDeleted && e.TenantId == _tenantProvider.CurrentTenantId && (e.SucursalId == null || _tenantProvider.CurrentSucursalId == null || e.SucursalId == _tenantProvider.CurrentSucursalId)`.
   * En `SaveChangesAsync`, todo registro en estado `EntityState.Added` que implemente `ITenantEntity` debe recibir automáticamente el `CurrentTenantId` y `CurrentSucursalId` del `ITenantProvider` si no fueron especificados.

---

## 4. REGLAS Y PROHIBICIONES ARQUITECTÓNICAS ESTRICTAS (BACKEND)

1. **PROHIBICIÓN ABSOLUTA DE REFLECTION (`System.Reflection`):**
   * Queda **TERMINANTEMENTE PROHIBIDO** usar `GetType().GetProperty(...)`, `SetValue(...)` o métodos auxiliares como `SetOptionalProp` / `GetOptionalProp` en Servicios o Repositorios. Si falta una propiedad en un Modelo o DTO, se debe agregar explícitamente en la clase C# y en la migración de base de datos.
2. **PROHIBICIÓN DE `ApplicationDbContext` EN CONTROLADORES Y SERVICIOS:**
   * Ningún `Controller` ni ningún `Service` (a excepción de referencias justificadas dentro de `Repositories/` o `UnitOfWork`) puede inyectar `ApplicationDbContext` directamente.
   * Flujo obligatorio de dependencias:
     `Controller` $\rightarrow$ `IService` $\rightarrow$ `IUnitOfWork` $\rightarrow$ `IRepository` $\rightarrow$ `ApplicationDbContext`.
3. **PROHIBICIÓN DE EXPONER ENTIDADES EN ENDPOINTS:**
   * Ningún método de `Controller` o `IService` puede recibir ni devolver clases del namespace `MiPeluqueria.Api.Models.*` ni tipos anónimos `object` (`IEnumerable<object>`). Toda entrada y salida debe utilizar un DTO fuertemente tipado ubicado en `MiPeluqueria.Api.DTOs.*`.
   * Los endpoints `POST` deben usar DTOs `Crear*Dto` / `*RequestDto`, y los endpoints `PUT` deben usar obligatoriamente sus respectivos `Actualizar*Dto`.
4. **ESTÁNDAR ÚNICO DE RUTAS Y ENVOLTORIO JSON (`ApiResponse<T>`):**
   * **Rutas:** Se mantiene estrictamente el estándar `[Route("api/[controller]")]` (o rutas explícitas ya pactadas como `[Route("api/public")]`, `[Route("api/ordenes-compra")]`, `[Route("api/recetas-servicio")]`, `[Route("api/configuracion-salon")]`, `[Route("api/webhooks/pagos")]`). **NO agregar prefijos `/v1/`**.
   * **Envoltorio:** Todo endpoint de la API (éxito `200/201` o fallo `400/401/403/404/409/429/500`) debe retornar siempre la estructura `ApiResponse<T>` serializada en `camelCase`:
     ```json
     {
       "success": true,
       "message": "Operación exitosa.",
       "data": { ... },
       "errors": null
     }
     ```
5. **VALIDACIÓN UNIFICADA CON FLUENTVALIDATION:**
   * Toda validación sintáctica y de rangos de entrada se ejecuta mediante clases `AbstractValidator<TDto>` dentro de `Validators/`.
   * Si un DTO falla la validación de FluentValidation, el pipeline debe interceptar los errores del `ModelState` y devolver un `400 Bad Request` con la estructura `ApiResponse<object>.Fail("Error de validación de datos.", listaErrores)` para que el Frontend procese siempre el mismo contrato JSON.
6. **POLÍTICA DE FECHAS, MONEDAS Y AUDITORÍA:**
   * **Fechas:** Prohibido usar `DateTime.Now`. Usar exclusivamente `DateTime.UtcNow` en el backend; la conversión al huso horario del salón (`America/Argentina/Cordoba` por defecto) se aplica en presentación o cálculo de agenda según `ConfiguracionSalon.TimeZoneId`.
   * **Monedas y Porcentajes:** Toda columna monetaria en SQL Server debe declararse como `decimal(18,2)` (`HasPrecision(18, 2)`), y todo porcentaje como `decimal(5,2)`.
   * **Borrado Lógico:** Prohibido el borrado físico (`DELETE FROM`) en tablas operativas. Toda baja es un `Soft Delete` (`IsDeleted = true`, `DeletedAt = DateTime.UtcNow`) auditado en `AuditLogs`.
7. **SEGURIDAD, JWT, RATE LIMITING Y AUTORIZACIÓN:**
   * Todos los controladores deben estar protegidos con `[Authorize]` a nivel de clase, con las únicas excepciones públicas explícitas de `PortalReservasController`, `WebhooksController`, `AuthController` (en login/registro/recuperación) y lecturas públicas de catálogos/configuración necesarias para el portal de reservas.
   * `Program.cs` debe registrar `AddRateLimiter` e invocar `app.UseRateLimiter()` implementando la política `"StrictAuthOtpPolicy"` (5 peticiones por minuto por IP).
   * En el registro público de clientes (`RegistroUsuarioDto`), el rol asignado por defecto debe ser estrictamente **`RolId = 4` (Cliente)**, bloqueando cualquier intento de inyectar roles administrativos desde endpoints públicos.

---

## 5. STACK TECNOLÓGICO INNEGOCIABLE — FRONTEND (`mipeluqueria-saas-web`)

Para garantizar calidad de **Producto Comercial SaaS Pro**, el proyecto Frontend debe adherirse a las siguientes especificaciones:

### 5.1 Core y Dependencias Oficiales (`package.json`)
* **Framework:** Next.js `14.2.x` utilizando **App Router** (`src/app/`).
* **Lenguaje:** TypeScript `5.x` en modo estricto (`"strict": true` en `tsconfig.json`, alias `@/*` apuntando a `./src/*`).
* **Estilos y UI:** Tailwind CSS `3.4.x` + `clsx` + `lucide-react` (iconografía oficial) + componentes base en `src/components/ui/designsystem.tsx`.
* **Cliente HTTP e Interceptores (`src/lib/axios.ts`):** `axios` configurado con `baseURL: process.env.NEXT_PUBLIC_API_URL` (`https://localhost:7132/api`), inyectando automáticamente en cada request:
  * Header `Authorization: Bearer ${token}`
  * Header `X-Tenant-Id: ${tenantId}`
  * Header `X-Sucursal-Id: ${sucursalId}`
  * Interceptor de respuesta para rotación transparente con `/api/auth/refresh-token` ante estados `401 Unauthorized`.
* **Combo Comercial Pro (Obligatorio en `package.json`):**
  1. **Estado Asíncrono y Caché:** `@tanstack/react-query` (`^5.x`) para gestión de estado del servidor, invalidación automática de queries tras mutaciones y eliminación de `useEffect` manuales de carga.
  2. **Formularios y Validación:** `react-hook-form` (`^7.x`) + `zod` (`^3.x`) + `@hookform/resolvers` espejando las reglas de `FluentValidation` del backend.
  3. **Feedback Visual (Toasts):** `sonner` para notificaciones flotantes no intrusivas conectadas al campo `message` y `errors` de `ApiResponse<T>`.
  4. **Fechas y Analítica Visual:** `date-fns` (con locale `es`) para manipulación de agenda/turnos, y `recharts` para los gráficos gerenciales del Dashboard (`IngresosPorMedioPago` y `RankingPeluqueros`).

### 5.2 Reglas de Seguridad en Frontend (`.env.local`)
* Ninguna clave secreta de pasarelas de pago o firma JWT puede exponerse con el prefijo `NEXT_PUBLIC_`.
* Variables públicas permitidas:
  * `NEXT_PUBLIC_API_URL=https://localhost:7132/api`
  * `NEXT_PUBLIC_DEFAULT_TENANT_ID=1`
  * `NEXT_PUBLIC_DEFAULT_SUCURSAL_ID=1`