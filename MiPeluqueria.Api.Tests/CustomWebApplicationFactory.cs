using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Services.Background;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace MiPeluqueria.Api.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public const string TestDatabaseName = "MiPeluqueriaDB_Tests";
        private const string DefaultLocalConnectionString =
            "Server=localhost;Database=MiPeluqueriaDB_Tests;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=false;Pooling=true;Min Pool Size=1;Max Pool Size=100;Connect Timeout=30";

        public static string TestConnectionString =>
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"))
                ? DefaultLocalConnectionString
                : Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;

        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly object DbInitLock = new();
        private static bool _databaseInitialized;

        private static string ResolveApiContentRoot()
        {
            string candidate = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "MiPeluqueria.Api"));
            return Directory.Exists(candidate) ? candidate : Directory.GetCurrentDirectory();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseContentRoot(ResolveApiContentRoot());

            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = TestConnectionString,
                    ["Smtp:Host"] = "",
                    ["Payments:MercadoPago:WebhookSecret"] = "mp_webhook_secret_dev_2026"
                });
            });

            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbDescriptor != null)
                {
                    services.Remove(dbDescriptor);
                }

                var workerDescriptors = services
                    .Where(d => d.ServiceType == typeof(IHostedService) &&
                                d.ImplementationType == typeof(RecordatorioTurnosBackgroundService))
                    .ToList();
                foreach (var wd in workerDescriptors)
                {
                    services.Remove(wd);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(
                        TestConnectionString,
                        sqlOptions =>
                        {
                            sqlOptions.CommandTimeout(120);
                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(10),
                                errorNumbersToAdd: null);
                        }));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);
            EnsureTestDatabaseMigrated(host.Services);
            return host;
        }

        private static void EnsureTestDatabaseMigrated(IServiceProvider serviceProvider)
        {
            lock (DbInitLock)
            {
                if (_databaseInitialized)
                    return;

                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.SetCommandTimeout(120);

                var dbName = db.Database.GetDbConnection().Database;
                if (!string.Equals(dbName, TestDatabaseName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Violación de seguridad de entorno: se intentó inicializar '{dbName}' en lugar de '{TestDatabaseName}'.");
                }

                var pending = db.Database.GetPendingMigrations().ToList();
                if (pending.Count > 0)
                {
                    db.Database.ExecuteSqlRaw(@"
                        DELETE FROM [RefreshTokens] WHERE [UsuarioId] > 1;
                        DELETE FROM [HistorialSesiones] WHERE [UsuarioId] > 1;
                        DELETE FROM [Usuarios] WHERE [Id] > 1 AND [Email] <> N'superadmin@mipeluqueria.com';
                    ");
                    db.Database.Migrate();
                }

                // Limpieza determinista de datos transaccionales de corridas previas preservando el Seeding maestro
                db.Database.ExecuteSqlRaw(@"
                    DELETE FROM [DetallesLiquidacion];
                    DELETE FROM [Liquidaciones];
                    DELETE FROM [ComisionesConfig] WHERE [Id] > 1;
                    IF NOT EXISTS (SELECT 1 FROM [ComisionesConfig] WHERE [Id] = 1)
                    BEGIN
                        SET IDENTITY_INSERT [ComisionesConfig] ON;
                        INSERT INTO [ComisionesConfig] ([Id], [TenantId], [SucursalId], [PeluqueroId], [ServicioId], [PorcentajeServicio], [PorcentajeVentaProducto], [CreatedAt], [IsDeleted])
                        VALUES (1, 1, 1, 1, NULL, 50.00, 10.00, '2026-01-01T00:00:00.0000000', 0);
                        SET IDENTITY_INSERT [ComisionesConfig] OFF;
                    END
                    ELSE
                    BEGIN
                        UPDATE [ComisionesConfig]
                        SET [TenantId] = 1,
                            [SucursalId] = 1,
                            [PeluqueroId] = 1,
                            [ServicioId] = NULL,
                            [PorcentajeServicio] = 50.00,
                            [PorcentajeVentaProducto] = 10.00,
                            [IsDeleted] = 0,
                            [DeletedAt] = NULL
                        WHERE [Id] = 1;
                    END
                    DELETE FROM [Pagos];
                    DELETE FROM [DetallesVenta];
                    DELETE FROM [Ventas];
                    DELETE FROM [MovimientosCaja];
                    DELETE FROM [SesionesCaja];
                    DELETE FROM [FichasTecnicas];
                    DELETE FROM [HistorialEstadosTurno];
                    DELETE FROM [TurnoServicios];
                    DELETE FROM [Turnos];
                    DELETE FROM [BloqueosHorario];
                    DELETE FROM [MovimientosStock];
                    DELETE FROM [DetallesOrdenCompra];
                    DELETE FROM [OrdenesCompra];
                    DELETE FROM [ServiciosInsumosRecetas];
                    DELETE FROM [HistorialPreciosServicio] WHERE [ServicioId] > 10;
                    DELETE FROM [Servicios] WHERE [Id] > 10;
                    DELETE FROM [HistorialPreciosProducto] WHERE [ProductoId] > 10;
                    DELETE FROM [Productos] WHERE [Id] > 10;
                    DELETE FROM [Clientes] WHERE [Id] > 10;
                    DELETE FROM [PeluqueroEspecialidades] WHERE [PeluqueroId] > 10;
                    DELETE FROM [HorariosLaborales] WHERE [PeluqueroId] > 10;
                    DELETE FROM [Peluqueros] WHERE [Id] > 10;
                    DELETE FROM [RefreshTokens];
                    DELETE FROM [HistorialSesiones];
                    DELETE FROM [Usuarios] WHERE [Id] NOT IN (1, 2) AND [Email] <> N'superadmin@mipeluqueria.com';
                    UPDATE [ConfiguracionSalon]
                    SET [IntervaloTurnosMinutos] = 30,
                        [PorcentajeSenaReserva] = 25.00
                    WHERE [TenantId] = 1 AND [SucursalId] = 1;
                ");

                _databaseInitialized = true;
            }
        }

        public HttpClient CreateAuthenticatedClient(
            int userId = 1,
            string username = "admin",
            string email = "admin@tesis.com",
            string role = "Administrador",
            int tenantId = 1,
            int sucursalId = 1)
        {
            var client = CreateClient();
            string token = GenerateJwtToken(userId, username, email, role, tenantId, sucursalId);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
            client.DefaultRequestHeaders.Add("X-Sucursal-Id", sucursalId.ToString());
            return client;
        }

        public static string GenerateJwtToken(
            int userId = 1,
            string username = "admin",
            string email = "admin@tesis.com",
            string role = "Administrador",
            int tenantId = 1,
            int sucursalId = 1)
        {
            const string jwtKey = "MiPeluqueria_Super_Secret_Key_Tesis_Cervantes_Edu_2026_Segura!";
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Name, username),
                new(ClaimTypes.Email, email),
                new(ClaimTypes.Role, role),
                new("TenantId", tenantId.ToString()),
                new("SucursalId", sucursalId.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = "MiPeluqueriaApi",
                Audience = "MiPeluqueriaFrontend",
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(keyBytes),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(handler.CreateToken(tokenDescriptor));
        }
    }
}
