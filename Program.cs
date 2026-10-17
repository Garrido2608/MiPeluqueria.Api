using System;
using System.Text;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Integrations.Email;
using MiPeluqueria.Api.Integrations.Notifications;
using MiPeluqueria.Api.Mappings;
using MiPeluqueria.Api.Middlewares;
using MiPeluqueria.Api.Repositories.Implementations;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Implementations;
using MiPeluqueria.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Controladores REST, Caché en Memoria y Contexto HTTP
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

// 2. Base de Datos SQL Server con Resiliencia de Conexión Automática
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)
    )
);

// 3. Contenedor IoC (Inyección de Dependencias Scoped)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Módulo Clientes y Fichas Técnicas
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IFichaTecnicaRepository, FichaTecnicaRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IFichaTecnicaService, FichaTecnicaService>();

// Módulo Staff, Servicios Comerciales y Catálogos Maestros
builder.Services.AddScoped<IPeluqueroRepository, PeluqueroRepository>();
builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<IPeluqueroService, PeluqueroService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();

// Módulo Turnos y Bloqueos de Agenda
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<IBloqueoHorarioRepository, BloqueoHorarioRepository>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IBloqueoHorarioService, BloqueoHorarioService>();

// Módulo Stock, Proveedores, Caja Diaria y Ventas
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<ISesionCajaRepository, SesionCajaRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
// F. Módulo Caja Diaria y Ventas (100% Separado por Responsabilidad)
builder.Services.AddScoped<ISesionCajaRepository, SesionCajaRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<IVentaService, VentaService>();

// Módulo Comisiones, Liquidaciones y Dashboard Analítico
builder.Services.AddScoped<IComisionConfigRepository, ComisionConfigRepository>();
builder.Services.AddScoped<ILiquidacionRepository, LiquidacionRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IComisionConfigService, ComisionConfigService>();
builder.Services.AddScoped<ILiquidacionService, LiquidacionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Módulo Seguridad, Usuarios, Configuración SaaS e Integraciones Externas
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IConfiguracionSalonRepository, ConfiguracionSalonRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IConfiguracionSalonService, ConfiguracionSalonService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppNotificationService>();

// 4. AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// 5. FluentValidation Automático en Frontera HTTP
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 6. Rate Limiting Nativo (.NET 8)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("StrictAuthOtpPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "global_ip",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2,
                AutoReplenishment = true
            }));
});

// 7. Política CORS Dinámica
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// 8. Autenticación JWT Bearer
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("CRÍTICO: No se configuró 'Jwt:Key' en appsettings.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 9. Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MiPeluqueria.Api — SaaS Backend Empresarial",
        Version = "v5.0",
        Description = "API RESTful para Gestión Integral de Salones y Barberías (.NET 8 + EF Core 8 + Clean Architecture)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Ingrese el token JWT con el formato: Bearer {su_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// 10. Pipeline de Middlewares
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
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();