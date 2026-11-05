using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Integrations.Email;
using MiPeluqueria.Api.Integrations.Notifications;
using MiPeluqueria.Api.Integrations.Payments;
using MiPeluqueria.Api.Mappings;
using MiPeluqueria.Api.Middlewares;
using MiPeluqueria.Api.Repositories.Implementations;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Background;
using MiPeluqueria.Api.Services.Implementations;
using MiPeluqueria.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// 1. BASE DE DATOS (SQL SERVER CON RESILIENCIA PARA PRODUCCIÓN)
// ============================================================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);
        }));

// ============================================================================
// 2. AUTOMAPPER, FLUENTVALIDATION Y CACHÉ MULTI-TENANT
// ============================================================================
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddMemoryCache();

// ============================================================================
// 3. INYECCIÓN DE DEPENDENCIAS: UNIT OF WORK Y LOS 19 REPOSITORIOS
// ============================================================================
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IFichaTecnicaRepository, FichaTecnicaRepository>();
builder.Services.AddScoped<IPeluqueroRepository, PeluqueroRepository>();
builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
builder.Services.AddScoped<IRecetaServicioRepository, RecetaServicioRepository>();
builder.Services.AddScoped<IBloqueoHorarioRepository, BloqueoHorarioRepository>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IOrdenCompraRepository, OrdenCompraRepository>();
builder.Services.AddScoped<ISesionCajaRepository, SesionCajaRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IComisionConfigRepository, ComisionConfigRepository>();
builder.Services.AddScoped<ILiquidacionRepository, LiquidacionRepository>();
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<IConfiguracionSalonRepository, ConfiguracionSalonRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<ISaaSRepository, SaaSRepository>();

// ============================================================================
// 4. INYECCIÓN DE DEPENDENCIAS: CONTEXTO TENANT Y SERVICIOS DE NEGOCIO
// ============================================================================
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IFichaTecnicaService, FichaTecnicaService>();
builder.Services.AddScoped<IPeluqueroService, PeluqueroService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IRecetaServicioService, RecetaServicioService>();
builder.Services.AddScoped<IBloqueoHorarioService, BloqueoHorarioService>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IOrdenCompraService, OrdenCompraService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IComisionConfigService, ComisionConfigService>();
builder.Services.AddScoped<ILiquidacionService, LiquidacionService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IConfiguracionSalonService, ConfiguracionSalonService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISaaSService, SaaSService>();

// ============================================================================
// 5. INTEGRACIONES EXTERNAS Y BACKGROUND WORKER MULTI-TENANT
// ============================================================================
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppNotificationService>();
builder.Services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();

builder.Services.AddHostedService<RecordatorioTurnosBackgroundService>();

// ============================================================================
// 6. RATE LIMITING ("StrictAuthOtpPolicy" - 5 REQ/MIN POR IP)
// ============================================================================
int strictPermitLimit = Math.Max(1, builder.Configuration.GetValue<int?>("RateLimiting:StrictAuthOtpPolicy:PermitLimit") ?? 5);
int strictWindowMinutes = Math.Max(1, builder.Configuration.GetValue<int?>("RateLimiting:StrictAuthOtpPolicy:WindowMinutes") ?? 1);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(
            "Se superó el límite de peticiones permitidas desde su dirección IP. Aguarde un minuto e intente nuevamente.");

        await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
    };

    options.AddPolicy("StrictAuthOtpPolicy", httpContext =>
    {
        string clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = strictPermitLimit,
                Window = TimeSpan.FromMinutes(strictWindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

// ============================================================================
// 7. SEGURIDAD JWT Y POLÍTICA CORS DINÁMICA
// ============================================================================
var jwtKey = builder.Configuration["Jwt:Key"] ?? "MiPeluqueria_Super_Secret_Key_Tesis_Cervantes_Edu_2026_Segura!";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://localhost:5173", "https://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ============================================================================
// 8. CONTROLADORES Y CONTRATO UNIFICADO DE VALIDACIÓN (ApiResponse + FieldErrors)
// ============================================================================
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var fieldErrors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            foreach (var (rawKey, entry) in context.ModelState)
            {
                if (entry.Errors.Count == 0)
                    continue;

                var messages = entry.Errors
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? "El valor ingresado no posee un formato válido."
                        : e.ErrorMessage)
                    .ToArray();

                errors.AddRange(messages);

                string fieldKey = NormalizeFieldKey(rawKey);
                if (!string.IsNullOrWhiteSpace(fieldKey))
                {
                    fieldErrors[fieldKey] = messages;
                }
            }

            var payload = ApiResponse<object>.Fail(
                "Error de validación de datos.",
                errors.Count > 0 ? errors : ["Error de validación de datos."],
                fieldErrors.Count > 0 ? fieldErrors : null);

            return new BadRequestObjectResult(payload);
        };
    });

builder.Services.AddEndpointsApiExplorer();

// ============================================================================
// 9. SWAGGER CON SOPORTE BEARER JWT
// ============================================================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MiPeluqueria.Api - SaaS Management System",
        Version = "v1",
        Description = "API Empresarial Multi-Tenant para Gestión Integral de Peluquerías y Barberías"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autorización JWT usando el esquema Bearer. Ejemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ============================================================================
// 10. PIPELINE HTTP MIDDLEWARES (ORDEN INMUTABLE SDD CAPA 2)
// ============================================================================
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

static string NormalizeFieldKey(string rawKey)
{
    if (string.IsNullOrWhiteSpace(rawKey) || rawKey == "$")
        return "general";

    string trimmed = rawKey.StartsWith("$.", StringComparison.Ordinal) ? rawKey[2..] : rawKey;
    if (string.Equals(trimmed, "dto", StringComparison.OrdinalIgnoreCase))
        return "general";

    if (trimmed.StartsWith("dto.", StringComparison.OrdinalIgnoreCase))
        trimmed = trimmed[4..];

    var segments = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries);
    for (int i = 0; i < segments.Length; i++)
    {
        int bracketIndex = segments[i].IndexOf('[');
        if (bracketIndex > 0)
        {
            string namePart = JsonNamingPolicy.CamelCase.ConvertName(segments[i][..bracketIndex]);
            segments[i] = namePart + segments[i][bracketIndex..];
        }
        else
        {
            segments[i] = JsonNamingPolicy.CamelCase.ConvertName(segments[i]);
        }
    }

    return string.Join('.', segments);
}