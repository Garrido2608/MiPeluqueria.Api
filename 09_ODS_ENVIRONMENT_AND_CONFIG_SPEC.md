# 09 - ESPECIFICACIÓN OPERATIVA DE DATOS (ODS), CONFIGURACIÓN, SECRETOS Y RUNBOOK SQL (CAPA 9 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)
**Documentos Padres Vinculados:** `01_SYSTEM_CONSTRAINTS.md`, `02_ARCHITECTURE.md`, `03_DATA_MODEL.md`, `06_INTEGRATION_AND_ODD_PLAN.md`
**Estándar de Ingeniería:** Twelve-Factor App (Config & Admin Processes) + .NET 8 Resilient Options Pattern
**Estado:** INMUTABLE - ESPECIFICACIÓN DE ENTORNO Y CICLO DE VIDA DE BASE DE DATOS

---

## 1. CONTRATO COMPLETO DE `appsettings.json` Y `appsettings.Development.json` (BACKEND)

Todo servicio de infraestructura o negocio (`AuthService`, `SmtpEmailService`, `PaymentGatewayService`, `RecordatorioTurnosBackgroundService`, `PeluqueroService`, `SaaSService` y `Program.cs`) debe encontrar la totalidad de sus claves jerárquicas declaradas en `appsettings.json` y `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=radaed;Database=MiPeluqueriaDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Key": "MiPeluqueria_Super_Secret_Key_Tesis_Cervantes_Edu_2026_Segura!",
    "Issuer": "MiPeluqueriaApi",
    "Audience": "MiPeluqueriaFrontend",
    "AccessTokenExpirationHours": 8,
    "RefreshTokenExpirationDays": 7
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:5173",
      "https://localhost:3000"
    ]
  },
  "RateLimiting": {
    "StrictAuthOtpPolicy": {
      "PermitLimit": 5,
      "WindowMinutes": 1
    }
  },
  "Smtp": {
    "Host": "",
    "Port": 587,
    "EnableSsl": true,
    "User": "",
    "Password": "",
    "FromEmail": "no-reply@mipeluqueria.com",
    "FromName": "Mi Peluquería SaaS",
    "TimeoutMilliseconds": 5000
  },
  "Payments": {
    "MercadoPago": {
      "AccessToken": "",
      "WebhookSecret": "mp_webhook_secret_dev_2026"
    },
    "Stripe": {
      "SecretKey": "",
      "WebhookSecret": "whsec_stripe_secret_dev_2026"
    }
  },
  "BackgroundJobs": {
    "RecordatoriosIntervaloMinutos": 15,
    "CancelarReservasOnlineImpagas": true,
    "MinutosExpiracionSenaOnline": 30
  },
  "SaaSLimits": {
    "MaxPeluquerosSeguridadPorSucursal": 50,
    "MaxSucursalesSeguridadPorTenant": 20
  }
}
(Nota operativa: En appsettings.Development.json, la cadena DefaultConnection debe preservar el nombre de instancia SQL Server que ya tenga configurado el entorno local del desarrollador, ej. Server=radaed;... o Server=localhost;...).

2. REGLAS DE CORTOCIRCUITO DEFENSIVO ANTE SECRETOS EXTERNOS VACÍOS EN DESARROLLO
Para evitar excepciones en tiempo de ejecución cuando el proyecto se ejecuta localmente sin servidor SMTP externo ni cuenta real de MercadoPago/Stripe conectada:

Cortocircuito en SmtpEmailService.cs:

Antes de instanciar SmtpClient, verificar if (string.IsNullOrWhiteSpace(host)).

Si el Host está vacío, no instanciar SmtpClient (evitando ArgumentException): registrar un LogWarning("SMTP no configurado en entorno local. Se omite el envío real de correo a {Destinatario}.", destinatario) y retornar false de inmediato para que el servicio llamador agregue el aviso no bloqueante en ApiResponse.Warnings.

Validación Criptográfica Determinista en PaymentGatewayService.cs:

Mantener un valor semilla no vacío en Payments:MercadoPago:WebhookSecret ("mp_webhook_secret_dev_2026") para poder probar la firma criptográfica HMAC-SHA256 en los tests de aceptación locales.

Si en algún proveedor el WebhookSecret estuviera vacío en configuración, ValidarFirmaWebhook debe retornar false por defecto (Fail-Closed Security) y jamás true.

Fallback de Parámetros en RecordatorioTurnosBackgroundService.cs:

Leer RecordatoriosIntervaloMinutos con fallback 15 (Math.Max(1, valor)) y MinutosExpiracionSenaOnline con fallback 30 para evitar bucles infinitos (Task.Delay(0)) si se borra accidentalmente la sección del JSON.

3. CONTRATO COMPLETO DE .env.local (FRONTEND NEXT.JS 14)
El archivo .env.local en la raíz de mipeluqueria-saas-web debe exponer exclusivamente variables públicas prefijadas con NEXT_PUBLIC_:

Fragmento de código
# URL Base del Monolito Modular .NET 8
NEXT_PUBLIC_API_URL=https://localhost:7132/api

# Contexto Multi-Tenant por Defecto para el Portal Público (/reservar)
NEXT_PUBLIC_DEFAULT_TENANT_ID=1
NEXT_PUBLIC_DEFAULT_SUCURSAL_ID=1

# Almacenamiento Multimedia Direct-to-Cloud (Fotos de Fichas Técnicas de Gabinete)
NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME=demo
NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET=mipeluqueria_preset

# Telemetría Opcional
NEXT_PUBLIC_GA_MEASUREMENT_ID=G-XXXXXXXXXX
Fallback Defensivo en src/lib/cloudinary.ts: Si NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME es "demo" o está vacío y la petición a Cloudinary falla en entorno local, el helper debe permitir ingresar una URL de imagen manual o retornar un placeholder controlado mostrando un toast.warning informativo, sin bloquear el guardado de la FichaTecnica.

4. RUNBOOK DE MIGRACIÓN LIMPIA Y REGLAS DE DATA SEEDING EN EF CORE 8 (HARD GATE 1)
Al llegar a la Tarea 1.4 (Hard Gate 1), dado que las 35 tablas operativas pasan a heredar de AuditableTenantEntity (TenantId, SucursalId, RowVersion) y las tablas puente PeluqueroEspecialidad y TurnoServicio unifican su clave primaria en Id:

4.1 Regla Técnica Innegociable para HasData en ApplicationDbContext.cs
Prohibición de Valores Dinámicos en HasData: Como AuditableEntity inicializa CreatedAt = DateTime.UtcNow, toda entidad insertada mediante .HasData(...) en el Seeding DEBE especificar explícitamente una constante estática fija:CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) e IsDeleted = false.

Prohibición de BCrypt.HashPassword Dinámico en OnModelCreating: Como BCrypt.HashPassword genera un salt aleatorio en cada compilación, ensucia el ModelSnapshot de EF Core. El usuario semilla admin@tesis.com debe declararse en HasData con un hash BCrypt precalculado fijo válido para la contraseña "Admin123!" (o inicializarse de forma idempotente en el arranque).

4.2 Procedimiento Exacto de Reconstrucción de Base de Datos (Gate 1)
Una vez otorgada la autorización humana en el Hard Gate 1:

Eliminar la base de datos previa de desarrollo (para evitar colisión de tablas existentes con la nueva migración unificada):

PowerShell
dotnet ef database drop --force
Limpiar la carpeta Migrations/ eliminando la migración inicial obsoleta y su snapshot.

Generar la nueva migración maestra Multi-Tenant:

PowerShell
dotnet ef migrations add InicialMultiTenantSaaS_v2
Impactar el esquema y el Data Seeding de las 41 tablas en SQL Server:

PowerShell
dotnet ef database update
4.3 Credenciales Semilla Oficiales Garantizadas tras database update
Tenant #1: Id = 1, NombreComercial = "Mi Peluquería Central", SubdominioSlug = "mi-peluqueria-central", Activo = true.

Sucursal #1: Id = 1, TenantId = 1, Nombre = "Sucursal Casa Matriz (Córdoba)", TimeZoneId = "America/Argentina/Cordoba", MonedaIso = "ARS", Activa = true.

Usuario Administrador #1: Id = 1, TenantId = 1, SucursalId = 1, Username = "admin", Email = "admin@tesis.com", Password = "Admin123!", RolId = 1 (Administrador), EmailVerificado = true, Activo = true.
