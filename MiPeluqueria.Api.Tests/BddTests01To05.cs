using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Clientes;
using Xunit;

namespace MiPeluqueria.Api.Tests
{
    public class BddTests01To05 : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public BddTests01To05(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // =========================================================================
        // TEST 01: Aislamiento Multi-Tenant y Validación Cruzada en Caché (OWASP A01 - [BUG-03])
        // =========================================================================
        [Fact]
        public async Task Test01_MultiTenantIsolation_And_CrossSucursalCache_403()
        {
            // 1. Petición con headers manipulados X-Tenant-Id: 1 y X-Sucursal-Id: 999 (inexistente)
            using var anonClient = _factory.CreateClient();
            anonClient.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            anonClient.DefaultRequestHeaders.Add("X-Sucursal-Id", "999");

            var forbiddenRes = await anonClient.GetAsync("/api/catalogos/todos");
            forbiddenRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var forbiddenBody = await forbiddenRes.Content.ReadFromJsonAsync<ApiResponse<object>>(
                CustomWebApplicationFactory.JsonOptions);
            forbiddenBody.Should().NotBeNull();
            forbiddenBody!.Success.Should().BeFalse();
            forbiddenBody.Message.Should().Be("La sucursal indicada no pertenece al salón actual o se encuentra inactiva.");

            // 2. Usuario autenticado con JWT válido (TenantId = 1, SucursalId = 1) que inyecta X-Tenant-Id: 2 malicioso
            using var jwtClient = _factory.CreateClient();
            string validToken = CustomWebApplicationFactory.GenerateJwtToken(
                userId: 1,
                username: "admin",
                email: "admin@tesis.com",
                role: "Administrador",
                tenantId: 1,
                sucursalId: 1);

            jwtClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
            jwtClient.DefaultRequestHeaders.Add("X-Tenant-Id", "2");
            jwtClient.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            var okRes = await jwtClient.GetAsync("/api/configuracion");
            okRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var configBody = await okRes.Content.ReadFromJsonAsync<ApiResponse<ConfiguracionSalonDto>>(
                CustomWebApplicationFactory.JsonOptions);
            configBody.Should().NotBeNull();
            configBody!.Success.Should().BeTrue();
            configBody.Data.Should().NotBeNull();
        }

        // =========================================================================
        // TEST 02: Escudo Rate Limiting (429) y Prevención de Escalación de Privilegios (RolId = 4)
        // =========================================================================
        [Fact]
        public async Task Test02_RateLimiting_429_And_RoleEscalationBlocked_RolId4()
        {
            using var isolatedFactory = _factory.WithWebHostBuilder(_ => { });
            using var client = isolatedFactory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            client.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            // Parte A: Intento de escalación de privilegios en POST /api/auth/registro enviando rolId = 1 (Administrador)
            string attackerEmail = $"attacker_{Guid.NewGuid():N}@test.com";
            string attackerUsername = $"atk_{Guid.NewGuid():N}"[..16];

            var registerPayload = new
            {
                username = attackerUsername,
                email = attackerEmail,
                password = "PasswordSegura123!",
                rolId = 1, // Intento malicioso de inyectar Administrador
                aceptaTerminos = true
            };

            var regRes = await client.PostAsJsonAsync("/api/auth/registro", registerPayload);
            regRes.StatusCode.Should().Be(HttpStatusCode.Created);

            var regBody = await regRes.Content.ReadFromJsonAsync<ApiResponse<UsuarioResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);
            regBody.Should().NotBeNull();
            regBody!.Success.Should().BeTrue();
            regBody.Data.Should().NotBeNull();
            regBody.Data!.RolId.Should().Be(4, "el registro público debe forzar estrictamente RolId = 4 (Cliente)");

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var persistedUser = await db.Usuarios
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == attackerEmail);

                persistedUser.Should().NotBeNull();
                persistedUser!.RolId.Should().Be(4);
            }

            // Parte B: 6 peticiones consecutivas a POST /api/auth/login (1 ya consumida por /registro en la misma partición IP,
            // por lo que usamos otra instancia limpia exclusivamente para medir las 6 llamadas exactas a /api/auth/login)
            using var rateLimitFactory = _factory.WithWebHostBuilder(_ => { });
            using var rlClient = rateLimitFactory.CreateClient();
            rlClient.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            rlClient.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            var loginDto = new LoginRequestDto
            {
                UsernameOrEmail = "admin@tesis.com",
                Password = "Admin123!"
            };

            for (int i = 1; i <= 5; i++)
            {
                var attempt = await rlClient.PostAsJsonAsync("/api/auth/login", loginDto);
                attempt.StatusCode.Should().Be(
                    HttpStatusCode.OK,
                    $"la petición #{i} dentro del límite de 5 req/min debe procesarse con 200 OK");
            }

            var sixthAttempt = await rlClient.PostAsJsonAsync("/api/auth/login", loginDto);
            sixthAttempt.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

            var rlBody = await sixthAttempt.Content.ReadFromJsonAsync<ApiResponse<object>>(
                CustomWebApplicationFactory.JsonOptions);
            rlBody.Should().NotBeNull();
            rlBody!.Success.Should().BeFalse();
        }

        // =========================================================================
        // TEST 03: Persistencia Tipada Cero-Reflection en Turnos, Horarios y Fichas Técnicas ([BUG-01])
        // =========================================================================
        [Fact]
        public async Task Test03_ZeroReflection_Turnos_HorariosDescanso_FichasTecnicas()
        {
            using var client = _factory.CreateAuthenticatedClient();

            // 1. Configurar horario cortado para Peluquero #1 (09:00 a 20:00, descanso 13:00 a 14:00)
            var targetDate = GetNextMonday(DateTime.UtcNow.Date.AddDays(14));
            int targetDayOfWeek = (int)targetDate.DayOfWeek;

            var horariosPayload = new GuardarHorariosPeluqueroDto
            {
                Horarios = new List<HorarioLaboralDto>
                {
                    new()
                    {
                        PeluqueroId = 1,
                        DiaSemana = targetDayOfWeek,
                        HoraInicio = new TimeSpan(9, 0, 0),
                        HoraFin = new TimeSpan(20, 0, 0),
                        HoraInicioDescanso = new TimeSpan(13, 0, 0),
                        HoraFinDescanso = new TimeSpan(14, 0, 0),
                        Activo = true
                    }
                }
            };

            var putHorariosRes = await client.PutAsJsonAsync("/api/peluqueros/1/horarios", horariosPayload);
            putHorariosRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // 2. Crear un servicio de $12.000 y 30 min para verificar PrecioHistorico y DuracionMinutosHistorica
            var crearServicioPayload = new CrearServicioDto
            {
                CategoriaId = 1,
                Nombre = $"Corte Test03 {Guid.NewGuid():N}"[..24],
                Descripcion = "Servicio de prueba BDD Test 03",
                Precio = 12000.00m,
                DuracionMinutos = 30
            };

            var createServRes = await client.PostAsJsonAsync("/api/servicios", crearServicioPayload);
            createServRes.StatusCode.Should().Be(HttpStatusCode.Created);
            var servBody = await createServRes.Content.ReadFromJsonAsync<ApiResponse<ServicioDto>>(
                CustomWebApplicationFactory.JsonOptions);
            int servicioId = servBody!.Data!.Id;

            // 3. Verificar disponibilidad: 13:00 y 13:30 NO deben aparecer disponibles
            string fechaQuery = targetDate.ToString("yyyy-MM-dd");
            var dispRes = await client.GetAsync(
                $"/api/turnos/disponibilidad?peluqueroId=1&fecha={fechaQuery}&serviciosIds={servicioId}");
            dispRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var dispBody = await dispRes.Content.ReadFromJsonAsync<ApiResponse<DisponibilidadHorarioResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);
            dispBody.Should().NotBeNull();
            dispBody!.Success.Should().BeTrue();
            dispBody.Data.Should().NotBeNull();
            dispBody.Data!.HorariosDisponibles.Should().Contain("09:00");
            dispBody.Data.HorariosDisponibles.Should().Contain("12:30");
            dispBody.Data.HorariosDisponibles.Should().NotContain("13:00");
            dispBody.Data.HorariosDisponibles.Should().NotContain("13:30");
            dispBody.Data.HorariosDisponibles.Should().Contain("14:00");

            // 4. Crear Turno y verificar PrecioHistorico = 12000.00 y DuracionMinutosHistorica = 30
            var crearTurnoPayload = new CrearTurnoDto
            {
                ClienteId = 1,
                PeluqueroId = 1,
                FechaHoraInicio = targetDate.Date.AddHours(10),
                ServiciosIds = new List<int> { servicioId },
                Observaciones = "Turno Test 03 Cero-Reflection"
            };

            var createTurnoRes = await client.PostAsJsonAsync("/api/turnos", crearTurnoPayload);
            createTurnoRes.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdTurnoBody = await createTurnoRes.Content.ReadFromJsonAsync<ApiResponse<TurnoResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);
            int turnoId = createdTurnoBody!.Data!.Id;

            var getTurnoRes = await client.GetAsync($"/api/turnos/{turnoId}");
            getTurnoRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var getTurnoBody = await getTurnoRes.Content.ReadFromJsonAsync<ApiResponse<TurnoResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);

            getTurnoBody!.Data.Should().NotBeNull();
            getTurnoBody.Data!.ServiciosDetalle.Should().NotBeEmpty();
            getTurnoBody.Data.ServiciosDetalle[0].PrecioHistorico.Should().Be(12000.00m);
            getTurnoBody.Data.ServiciosDetalle[0].DuracionMinutosHistorica.Should().Be(30);

            // 5. Crear FichaTécnica y verificar que todos los campos persistan sin pérdida por Reflection
            var crearFichaPayload = new CrearFichaTecnicaDto
            {
                ClienteId = 1,
                PeluqueroId = 1,
                TurnoId = turnoId,
                FormulaAplicada = "Base 7.1 (45g) + 8.11 (15g) + Oxidante 20 Vol",
                Observaciones = "Aplicación en raíz y emulsión en largos 10 min",
                NotasSensibilidadAlergias = "Sensibilidad leve a decolorantes con amoníaco",
                ProductosUtilizados = "Ampolla Keratina + Shampoo Ácido",
                FotoResultadoUrl = "https://res.cloudinary.com/demo/image/upload/v1/ficha_test03.jpg"
            };

            var createFichaRes = await client.PostAsJsonAsync("/api/fichastecnicas", crearFichaPayload);
            createFichaRes.StatusCode.Should().Be(HttpStatusCode.Created);

            var getFichasRes = await client.GetAsync("/api/fichastecnicas/cliente/1");
            getFichasRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var getFichasBody = await getFichasRes.Content.ReadFromJsonAsync<ApiResponse<List<FichaTecnicaResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);

            getFichasBody!.Data.Should().NotBeNullOrEmpty();
            var fichaGuardada = getFichasBody.Data!.First(f => f.TurnoId == turnoId);
            fichaGuardada.FormulaAplicada.Should().Be(crearFichaPayload.FormulaAplicada);
            fichaGuardada.Observaciones.Should().Be(crearFichaPayload.Observaciones);
            fichaGuardada.NotasSensibilidadAlergias.Should().Be(crearFichaPayload.NotasSensibilidadAlergias);
            fichaGuardada.ProductosUtilizados.Should().Be(crearFichaPayload.ProductosUtilizados);
            fichaGuardada.FotoResultadoUrl.Should().Be(crearFichaPayload.FotoResultadoUrl);
        }

        // =========================================================================
        // TEST 04: Persistencia Real de Configuración del Salón en SQL Server ([BUG-05])
        // =========================================================================
        [Fact]
        public async Task Test04_ConfiguracionSalon_PersistsInSqlServer()
        {
            using var client = _factory.CreateAuthenticatedClient();

            var updateDto = new ActualizarConfiguracionSalonDto
            {
                NombreSalon = "Salon Test SDD",
                NombreComercial = "Salon Test SDD",
                RazonSocial = "Salon Test SDD S.A.",
                Cuit = "30-71122334-9",
                Direccion = "Av. Colón 500, Córdoba",
                Telefono = "+5493515550000",
                EmailContacto = "contacto@tesis.com",
                MonedaIso = "ARS",
                TimeZoneId = "America/Argentina/Cordoba",
                HorarioAtencionTexto = "Lunes a Sábados de 09:00 a 20:00 hs",
                IntervaloTurnosMinutos = 45,
                MinutosToleranciaEspera = 15,
                HorasAnticipacionCancelacion = 24,
                PorcentajeSenaReserva = 25.00m,
                NotificacionesWhatsAppActivas = true,
                NotificacionesEmailActivas = true
            };

            var putRes = await client.PutAsJsonAsync("/api/configuracion", updateDto);
            putRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // 1. Verificar vía GET /api/configuracion (invalida caché SaaS_Config_T1_S1)
            var getRes = await client.GetAsync("/api/configuracion");
            getRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var getBody = await getRes.Content.ReadFromJsonAsync<ApiResponse<ConfiguracionSalonDto>>(
                CustomWebApplicationFactory.JsonOptions);

            getBody.Should().NotBeNull();
            getBody!.Success.Should().BeTrue();
            getBody.Data!.NombreComercial.Should().Be("Salon Test SDD");
            getBody.Data.IntervaloTurnosMinutos.Should().Be(45);
            getBody.Data.PorcentajeSenaReserva.Should().Be(25.00m);

            // 2. Verificar directamente en SQL Server con un nuevo DbContext (simulando reinicio de proceso)
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var configSql = await db.ConfiguracionSalon
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.TenantId == 1 && c.SucursalId == 1 && !c.IsDeleted);

                configSql.Should().NotBeNull();
                configSql!.NombreComercial.Should().Be("Salon Test SDD");
                configSql.IntervaloTurnosMinutos.Should().Be(45);
                configSql.PorcentajeSenaReserva.Should().Be(25.00m);
            }

            // Restaurar IntervaloTurnosMinutos = 30 para mantener estado base limpio
            updateDto.IntervaloTurnosMinutos = 30;
            await client.PutAsJsonAsync("/api/configuracion", updateDto);
        }

        // =========================================================================
        // TEST 05: Prevención de Condiciones de Carrera (Serializable) y Concurrencia Optimista (RowVersion -> 409)
        // =========================================================================
        [Fact]
        public async Task Test05_SerializableTurnos_And_RowVersion_409Conflict()
        {
            using var client1 = _factory.CreateAuthenticatedClient();
            using var client2 = _factory.CreateAuthenticatedClient();

            // Parte A: Dos reservas simultáneas para el mismo Peluquero y mismo slot libre (Serializable)
            var slotDate = GetNextMonday(DateTime.UtcNow.Date.AddDays(21));
            int dayOfWeek = (int)slotDate.DayOfWeek;

            await client1.PutAsJsonAsync("/api/peluqueros/1/horarios", new GuardarHorariosPeluqueroDto
            {
                Horarios = new List<HorarioLaboralDto>
                {
                    new()
                    {
                        PeluqueroId = 1,
                        DiaSemana = dayOfWeek,
                        HoraInicio = new TimeSpan(9, 0, 0),
                        HoraFin = new TimeSpan(20, 0, 0),
                        Activo = true
                    }
                }
            });

            var turnoConcurrenteDto = new CrearTurnoDto
            {
                ClienteId = 1,
                PeluqueroId = 1,
                FechaHoraInicio = slotDate.Date.AddHours(15), // 15:00 a 15:30
                ServiciosIds = new List<int> { 1 },
                Observaciones = "Test 05 Concurrencia Serializable"
            };

            var task1 = client1.PostAsJsonAsync("/api/turnos", turnoConcurrenteDto);
            var task2 = client2.PostAsJsonAsync("/api/turnos", turnoConcurrenteDto);

            var responses = await Task.WhenAll(task1, task2);
            int createdCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
            int rejectedCount = responses.Count(r => r.StatusCode != HttpStatusCode.Created);

            createdCount.Should().Be(1, "solo una de las dos peticiones concurrentes puede reservar el slot bajo IsolationLevel.Serializable");
            rejectedCount.Should().Be(1, "la segunda petición concurrente debe ser rechazada por solapamiento o serialización");

            // Parte B: Concurrencia Optimista (RowVersion) -> DbUpdateConcurrencyException -> HTTP 409 Conflict
            var createCliRes = await client1.PostAsJsonAsync("/api/clientes", new ClienteRequestDto
            {
                Nombre = "Cliente",
                Apellido = "Concurrencia",
                Telefono = $"+549351{Random.Shared.Next(1000000, 9999999)}",
                Email = $"conc_{Guid.NewGuid():N}@test.com",
                FechaNacimiento = new DateTime(1990, 5, 10)
            });
            createCliRes.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdCli = await createCliRes.Content.ReadFromJsonAsync<ApiResponse<ClienteResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);
            int clienteId = createdCli!.Data!.Id;

            using var concurrencyFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var dbDescriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (dbDescriptor != null)
                        services.Remove(dbDescriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(CustomWebApplicationFactory.TestConnectionString)
                               .AddInterceptors(new SimulateConcurrentRowVersionBumpInterceptor(clienteId)));
                });
            });

            using var concurrencyClient = concurrencyFactory.CreateClient();
            string adminToken = CustomWebApplicationFactory.GenerateJwtToken();
            concurrencyClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            concurrencyClient.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            concurrencyClient.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            var conflictRes = await concurrencyClient.PutAsJsonAsync($"/api/clientes/{clienteId}", new ClienteRequestDto
            {
                Nombre = "Cliente Editado",
                Apellido = "Concurrencia",
                Telefono = createdCli.Data.Telefono,
                Email = createdCli.Data.Email,
                FechaNacimiento = new DateTime(1990, 5, 10)
            });

            conflictRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
            var conflictBody = await conflictRes.Content.ReadFromJsonAsync<ApiResponse<object>>(
                CustomWebApplicationFactory.JsonOptions);

            conflictBody.Should().NotBeNull();
            conflictBody!.Success.Should().BeFalse();
            conflictBody.Message.Should().Be(
                "El registro fue modificado simultáneamente por otro usuario. Recargue la pantalla e intente nuevamente.");
        }

        private static DateTime GetNextMonday(DateTime fromDate)
        {
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)fromDate.DayOfWeek + 7) % 7;
            return fromDate.Date.AddDays(daysUntilMonday == 0 ? 7 : daysUntilMonday);
        }

        private sealed class SimulateConcurrentRowVersionBumpInterceptor : SaveChangesInterceptor
        {
            private readonly int _targetClienteId;
            private int _triggered;

            public SimulateConcurrentRowVersionBumpInterceptor(int targetClienteId)
            {
                _targetClienteId = targetClienteId;
            }

            public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                var ctx = eventData.Context;
                if (ctx != null &&
                    Interlocked.CompareExchange(ref _triggered, 1, 0) == 0 &&
                    ctx.ChangeTracker.Entries<Cliente>().Any(e => e.Entity.Id == _targetClienteId && e.State == EntityState.Modified))
                {
                    await ctx.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE [Clientes] SET [Apellido] = N'ModificadoPorOtroOperador' WHERE [Id] = {_targetClienteId}",
                        cancellationToken);
                }

                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }
    }
}
