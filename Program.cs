using System;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Mappings;
using MiPeluqueria.Api.Middlewares;
using MiPeluqueria.Api.Repositories.Implementations;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Implementations;
using MiPeluqueria.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Controladores REST
builder.Services.AddControllers();

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

// 3. Contenedor IoC (Inyección de Dependencias Scoped) — MATRIZ DEFINITIVA COMPLETA
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Módulo Clientes y Fichas Técnicas
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IFichaTecnicaService, FichaTecnicaService>();

// Módulo Staff y Catálogo Comercial
builder.Services.AddScoped<IPeluqueroRepository, PeluqueroRepository>();
builder.Services.AddScoped<IPeluqueroService, PeluqueroService>();
builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
builder.Services.AddScoped<IServicioService, ServicioService>();

// Módulo Turnos y Bloqueos de Agenda
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IBloqueoHorarioService, BloqueoHorarioService>();

// Módulo Stock, Caja Diaria y Ventas
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ISesionCajaRepository, SesionCajaRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<ICajaVentaService, CajaVentaService>();

// Módulo Liquidaciones y Dashboard KPIs
builder.Services.AddScoped<ILiquidacionService, LiquidacionService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Módulo Seguridad y Autenticación
builder.Services.AddScoped<IAuthService, AuthService>();

// 4. AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// 5. FluentValidation Automático en Frontera HTTP
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// 6. Política CORS para Frontend React / Vite
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// 7. Autenticación JWT Bearer
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 8. Swagger / OpenAPI con Soporte para Token Bearer (Botón Authorize)
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

// 9. Pipeline de Middlewares en Orden Estricto
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
app.MapControllers();

app.Run();