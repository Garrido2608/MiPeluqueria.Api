using System;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiPeluqueria.Api.Data.Context;
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
// 2. AUTOMAPPER Y FLUENTVALIDATION
// ============================================================================
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Caché en memoria (para Configuración del Salón y Catálogos)
builder.Services.AddMemoryCache();

// ============================================================================
// 3. INYECCIÓN DE DEPENDENCIAS: REPOSITORIOS Y UNIT OF WORK (SIN DUPLICADOS)
// ============================================================================
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IPeluqueroRepository, PeluqueroRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProveedorRepository, ProveedorRepository>();
builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<ISesionCajaRepository, SesionCajaRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IFichaTecnicaRepository, FichaTecnicaRepository>();
builder.Services.AddScoped<IBloqueoHorarioRepository, BloqueoHorarioRepository>();
builder.Services.AddScoped<IComisionConfigRepository, ComisionConfigRepository>();
builder.Services.AddScoped<ILiquidacionRepository, LiquidacionRepository>();
builder.Services.AddScoped<ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IConfiguracionSalonRepository, ConfiguracionSalonRepository>();
builder.Services.AddScoped<IOrdenCompraRepository, OrdenCompraRepository>();

// ============================================================================
// 4. INYECCIÓN DE DEPENDENCIAS: SERVICIOS DE NEGOCIO Y SAAS CORE
// ============================================================================
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IFichaTecnicaService, FichaTecnicaService>();
builder.Services.AddScoped<IPeluqueroService, PeluqueroService>();
builder.Services.AddScoped<IServicioService, ServicioService>();
builder.Services.AddScoped<IBloqueoHorarioService, BloqueoHorarioService>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<IComisionConfigService, ComisionConfigService>();
builder.Services.AddScoped<ILiquidacionService, LiquidacionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICatalogoService, CatalogoService>();
builder.Services.AddScoped<IConfiguracionSalonService, ConfiguracionSalonService>();
builder.Services.AddScoped<IOrdenCompraService, OrdenCompraService>();
builder.Services.AddScoped<IRecetaServicioService, RecetaServicioService>();

// ============================================================================
// 5. INTEGRACIONES EXTERNAS Y BACKGROUND WORKERS (24H + 2H WHATSAPP)
// ============================================================================
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppNotificationService>();
builder.Services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();

// Registro del Worker Automático de Recordatorios (24hs y 2hs antes)
builder.Services.AddHostedService<RecordatorioTurnosBackgroundService>();

// ============================================================================
// 6. SEGURIDAD JWT Y POLÍTICA CORS DINÁMICA
// ============================================================================
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ClaveSecretaSuperSeguraMiPeluqueriaSaaS2026!";
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
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ============================================================================
// 7. SWAGGER CON SOPORTE BEARER JWT
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
// 8. PIPELINE HTTP MIDDLEWARES
// ============================================================================
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CorsFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Resolución automática de TenantId y SucursalId en cada request
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapControllers();

app.Run();