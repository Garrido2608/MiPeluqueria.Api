using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.SaaS;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using Xunit;

namespace MiPeluqueria.Api.Tests
{
    public class Etapa8BackendFixesTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public Etapa8BackendFixesTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // =========================================================================
        // TAREA 8.2 - TEST 1: Consistencia Al Centavo en Caja Abierta
        // (GET /api/caja/estado-actual vs GET /api/caja/{id} vs GET /api/caja/historial)
        // =========================================================================
        [Fact]
        public async Task Tarea82_CajaAbierta_EstadoActual_PorId_E_Historial_CoincidenAlCentavo()
        {
            using var adminClient = _factory.CreateAuthenticatedClient();

            await CerrarCajaSiEstaAbiertaAsync(adminClient);

            // 1. Abrir sesión de caja con fondo inicial = $10.000,00
            var abrirRes = await adminClient.PostAsJsonAsync("/api/caja/abrir", new AperturaCajaDto
            {
                MontoInicial = 10000.00m
            });
            abrirRes.StatusCode.Should().Be(HttpStatusCode.Created);
            var sesionAbierta = (await abrirRes.Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;
            sesionAbierta.EstadoAbierta.Should().BeTrue();

            // Crear 2 servicios dedicados ($37.500 efectivo y $28.000 digital) para la prueba
            var servEfectivoRes = await adminClient.PostAsJsonAsync("/api/servicios", new CrearServicioDto
            {
                CategoriaId = 1,
                Nombre = $"Serv Efvo 37500 {Guid.NewGuid():N}"[..26],
                Descripcion = "Servicio cobrado en efectivo $37.500",
                Precio = 37500.00m,
                DuracionMinutos = 30
            });
            servEfectivoRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int servEfectivoId = (await servEfectivoRes.Content.ReadFromJsonAsync<ApiResponse<ServicioDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            var servDigitalRes = await adminClient.PostAsJsonAsync("/api/servicios", new CrearServicioDto
            {
                CategoriaId = 1,
                Nombre = $"Serv Dig 28000 {Guid.NewGuid():N}"[..26],
                Descripcion = "Servicio cobrado en medio digital $28.000",
                Precio = 28000.00m,
                DuracionMinutos = 30
            });
            servDigitalRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int servDigitalId = (await servDigitalRes.Content.ReadFromJsonAsync<ApiResponse<ServicioDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            // 2. Cobrar venta en efectivo físico ($37.500,00 - MedioPagoId = 1 Efectivo)
            var ventaEfectivoRes = await adminClient.PostAsJsonAsync("/api/ventas", new CrearVentaDto
            {
                SesionCajaId = sesionAbierta.Id,
                ClienteId = 1,
                IdempotencyKey = $"t82-efvo-{Guid.NewGuid():N}"[..36],
                Detalles = new List<DetalleVentaRequestDto>
                {
                    new()
                    {
                        TipoItem = TipoItemVentaEnum.Servicio,
                        ItemId = servEfectivoId,
                        Cantidad = 1,
                        PeluqueroId = 1
                    }
                },
                Pagos = new List<PagoRequestDto>
                {
                    new()
                    {
                        MedioPagoId = 1, // Efectivo (EsEfectivoFisico = true)
                        Importe = 37500.00m
                    }
                }
            });
            ventaEfectivoRes.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

            // 3. Cobrar venta digital ($28.000,00 - MedioPagoId = 5 Mercado Pago QR, EsEfectivoFisico = false)
            var ventaDigitalRes = await adminClient.PostAsJsonAsync("/api/ventas", new CrearVentaDto
            {
                SesionCajaId = sesionAbierta.Id,
                ClienteId = 1,
                IdempotencyKey = $"t82-dig-{Guid.NewGuid():N}"[..36],
                Detalles = new List<DetalleVentaRequestDto>
                {
                    new()
                    {
                        TipoItem = TipoItemVentaEnum.Servicio,
                        ItemId = servDigitalId,
                        Cantidad = 1,
                        PeluqueroId = 1
                    }
                },
                Pagos = new List<PagoRequestDto>
                {
                    new()
                    {
                        MedioPagoId = 5, // Mercado Pago QR (EsEfectivoFisico = false)
                        Importe = 28000.00m,
                        ReferenciaOperacion = "MP-TAREA82-28000"
                    }
                }
            });
            ventaDigitalRes.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

            // 4. Registrar egreso manual de $10.000,00
            var egresoRes = await adminClient.PostAsJsonAsync("/api/caja/egreso", new RegistrarMovimientoCajaDto
            {
                SesionCajaId = sesionAbierta.Id,
                Monto = 10000.00m,
                Concepto = "Pago proveedor / Egreso manual Tarea 8.2",
                Tipo = "EgresoMenor"
            });
            egresoRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // 5. Consultar los 3 endpoints de Caja y verificar consistencia exacta al centavo
            var estadoActualRes = await adminClient.GetAsync("/api/caja/estado-actual");
            estadoActualRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var dtoEstadoActual = (await estadoActualRes.Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            var porIdRes = await adminClient.GetAsync($"/api/caja/{sesionAbierta.Id}");
            porIdRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var dtoPorId = (await porIdRes.Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            var historialRes = await adminClient.GetAsync("/api/caja/historial?pageNumber=1&pageSize=15");
            historialRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var historialBody = (await historialRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<SesionCajaResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            var dtoEnHistorial = historialBody.Data.FirstOrDefault(s => s.Id == sesionAbierta.Id);
            dtoEnHistorial.Should().NotBeNull("la sesión de caja abierta debe figurar en el historial paginado");

            // Fórmula Regla 4.1 (05_BUSINESS_RULES.md):
            // MontoFinalEsperado = 10000 (Inicial) + 37500 (Efectivo) + 0 (Ingresos) - 10000 (Egresos) = 37500.00
            // TotalVentasDigital = 28000.00
            foreach (var dto in new[] { dtoEstadoActual, dtoPorId, dtoEnHistorial! })
            {
                dto.Id.Should().Be(sesionAbierta.Id);
                dto.EstadoAbierta.Should().BeTrue();
                dto.MontoInicial.Should().Be(10000.00m);
                dto.TotalVentasEfectivo.Should().Be(37500.00m);
                dto.TotalVentasDigital.Should().Be(28000.00m);
                dto.TotalIngresosManuales.Should().Be(0.00m);
                dto.TotalEgresosManuales.Should().Be(10000.00m);
                dto.MontoFinalEsperado.Should().Be(37500.00m);
            }

            await CerrarCajaSiEstaAbiertaAsync(adminClient);
        }

        // =========================================================================
        // TAREA 8.2 - TEST 2: Rango Semanal de Turnos (fechaDesde / fechaHasta)
        // =========================================================================
        [Fact]
        public async Task Tarea82_Turnos_GetPaged_FiltraPorRangoSemanal_Y_PreservaFechaPuntual()
        {
            using var adminClient = _factory.CreateAuthenticatedClient();

            // Elegir una semana futura aislada (Lunes a Sábado) para evitar colisiones con otros tests
            var baseMonday = GetNextDayOfWeek(DateTime.UtcNow.Date.AddDays(95), DayOfWeek.Monday);
            var prevFriday = baseMonday.AddDays(-3);     // Fuera de rango (semana anterior)
            var wednesday = baseMonday.AddDays(2);       // Dentro de rango (miércoles)
            var saturday = baseMonday.AddDays(5);        // Dentro de rango (sábado, borde superior fechaHasta)
            var nextTuesday = baseMonday.AddDays(8);     // Fuera de rango (semana siguiente)

            await ConfigurarHorariosSemanaCompletaAsync(adminClient, peluqueroId: 1);

            int turnoPrevId = await CrearTurnoAsync(adminClient, 1, 1, prevFriday.AddHours(10), 1, "Turno Semana Previa T82");
            int turnoLunesId = await CrearTurnoAsync(adminClient, 1, 1, baseMonday.AddHours(9), 1, "Turno Lunes Semana T82");
            int turnoMiercolesId = await CrearTurnoAsync(adminClient, 1, 1, wednesday.AddHours(11), 1, "Turno Miercoles Semana T82");
            int turnoSabadoId = await CrearTurnoAsync(adminClient, 1, 1, saturday.AddHours(16), 1, "Turno Sabado Borde T82");
            int turnoNextId = await CrearTurnoAsync(adminClient, 1, 1, nextTuesday.AddHours(10), 1, "Turno Semana Siguiente T82");

            // 1. Consulta por rango semanal (sin fecha puntual, enviando fechaDesde y fechaHasta en formato YYYY-MM-DD)
            string desdeStr = baseMonday.ToString("yyyy-MM-dd");
            string hastaStr = saturday.ToString("yyyy-MM-dd");

            var rangoRes = await adminClient.GetAsync(
                $"/api/turnos?fechaDesde={desdeStr}&fechaHasta={hastaStr}&peluqueroId=1&pageNumber=1&pageSize=100");
            rangoRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var rangoBody = (await rangoRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TurnoResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            var idsEnRango = rangoBody.Data.Select(t => t.Id).ToList();

            idsEnRango.Should().Contain(new[] { turnoLunesId, turnoMiercolesId, turnoSabadoId },
                "el filtro semanal debe incluir todos los turnos entre fechaDesde (00:00) y fechaHasta (23:59:59)");
            idsEnRango.Should().NotContain(turnoPrevId, "no debe incluir turnos anteriores a fechaDesde");
            idsEnRango.Should().NotContain(turnoNextId, "no debe incluir turnos posteriores a fechaHasta");

            // 2. Consulta con fecha puntual (debe conservar 100% el comportamiento previo de un solo día)
            string miercolesStr = wednesday.ToString("yyyy-MM-dd");
            var fechaPuntualRes = await adminClient.GetAsync(
                $"/api/turnos?fecha={miercolesStr}&peluqueroId=1&pageNumber=1&pageSize=100");
            fechaPuntualRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var fechaPuntualBody = (await fechaPuntualRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TurnoResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            var idsFechaPuntual = fechaPuntualBody.Data.Select(t => t.Id).ToList();
            idsFechaPuntual.Should().Contain(turnoMiercolesId);
            idsFechaPuntual.Should().NotContain(new[] { turnoLunesId, turnoSabadoId, turnoPrevId, turnoNextId });
        }

        // =========================================================================
        // TAREA 8.3 - TEST 1: Liquidaciones sin duplicados (400 en reintento) + Pagar (con y sin Caja)
        // =========================================================================
        [Fact]
        public async Task Tarea83_Liquidaciones_SinDuplicados_400EnReintento_Y_PagoConYSinCaja()
        {
            using var adminClient = _factory.CreateAuthenticatedClient();

            await CerrarCajaSiEstaAbiertaAsync(adminClient);

            // 1. Crear profesional aislado y servicio dedicado ($20.000 -> comisión 50% default = $10.000)
            var pelRes = await adminClient.PostAsJsonAsync("/api/peluqueros", new CrearPeluqueroDto
            {
                Nombre = "Valeria",
                Apellido = $"LiqT83_{Guid.NewGuid():N}"[..16],
                Email = $"valeria.t83.{Guid.NewGuid():N}@tesis.com",
                Telefono = "+5493515558301",
                ColorAgendaHex = "#6366F1",
                EspecialidadesIds = new List<int> { 1 }
            });
            pelRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int peluqueroId = (await pelRes.Content.ReadFromJsonAsync<ApiResponse<PeluqueroDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            var servRes = await adminClient.PostAsJsonAsync("/api/servicios", new CrearServicioDto
            {
                CategoriaId = 1,
                Nombre = $"Serv Liq T83 {Guid.NewGuid():N}"[..24],
                Precio = 20000.00m,
                DuracionMinutos = 30
            });
            servRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int servicioId = (await servRes.Content.ReadFromJsonAsync<ApiResponse<ServicioDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            // 2. Abrir caja ($10.000) y cobrar primera venta ($20.000 efectivo)
            var abrirRes = await adminClient.PostAsJsonAsync("/api/caja/abrir", new AperturaCajaDto
            {
                MontoInicial = 10000.00m
            });
            abrirRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int sesionCajaId = (await abrirRes.Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            var venta1Res = await adminClient.PostAsJsonAsync("/api/ventas", new CrearVentaDto
            {
                SesionCajaId = sesionCajaId,
                ClienteId = 1,
                IdempotencyKey = $"t83-v1-{Guid.NewGuid():N}"[..36],
                Detalles = new List<DetalleVentaRequestDto>
                {
                    new()
                    {
                        TipoItem = TipoItemVentaEnum.Servicio,
                        ItemId = servicioId,
                        Cantidad = 1,
                        PeluqueroId = peluqueroId
                    }
                },
                Pagos = new List<PagoRequestDto>
                {
                    new() { MedioPagoId = 1, Importe = 20000.00m }
                }
            });
            venta1Res.StatusCode.Should().Be(HttpStatusCode.Created);

            var generarDto = new GenerarLiquidacionDto
            {
                PeluqueroId = peluqueroId,
                FechaDesde = DateTime.UtcNow.Date.AddDays(-1),
                FechaHasta = DateTime.UtcNow.Date,
                Bonificaciones = 0m,
                Descuentos = 0m,
                Observaciones = "Liquidación #1 Tarea 8.3"
            };

            // 3. Emitir primera liquidación -> 201 Created ($10.000)
            var liq1Res = await adminClient.PostAsJsonAsync("/api/liquidaciones/generar", generarDto);
            liq1Res.StatusCode.Should().Be(HttpStatusCode.Created);
            var liq1 = (await liq1Res.Content.ReadFromJsonAsync<ApiResponse<LiquidacionResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            liq1.TotalServicios.Should().Be(10000.00m);
            liq1.TotalPagar.Should().Be(10000.00m);
            liq1.Estado.Should().Be(EstadoLiquidacionEnum.Pendiente.ToString());
            liq1.CantidadItemsLiquidados.Should().Be(1);

            // 4. Reintentar misma liquidación sin nuevas ventas -> 400 Bad Request (cero duplicados)
            var retryPendienteRes = await adminClient.PostAsJsonAsync("/api/liquidaciones/generar", generarDto);
            retryPendienteRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var retryPendienteBody = await retryPendienteRes.Content.ReadFromJsonAsync<ApiResponse<LiquidacionResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);
            retryPendienteBody!.Success.Should().BeFalse();
            retryPendienteBody.Message.Should().Be("No existen ventas pendientes de liquidar para este profesional en el período seleccionado.");

            // 5. Pagar liquidación #1 SIN imputación en caja -> 200 OK y marca Estado = Pagado + FechaPago != null
            var pagarSinCajaRes = await adminClient.PostAsync($"/api/liquidaciones/{liq1.Id}/pagar", null);
            pagarSinCajaRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var pagarSinCajaBody = await pagarSinCajaRes.Content.ReadFromJsonAsync<ApiResponse<bool>>(
                CustomWebApplicationFactory.JsonOptions);
            pagarSinCajaBody!.Success.Should().BeTrue();

            var liq1PostPago = (await (await adminClient.GetAsync($"/api/liquidaciones/{liq1.Id}"))
                .Content.ReadFromJsonAsync<ApiResponse<LiquidacionResponseDto>>(CustomWebApplicationFactory.JsonOptions))!.Data!;
            liq1PostPago.Estado.Should().Be(EstadoLiquidacionEnum.Pagado.ToString());
            liq1PostPago.FechaPago.Should().NotBeNull();

            // Reintentar liquidación cuando la previa ya está Pagada -> sigue retornando 400 Bad Request (cero duplicados)
            var retryPagadaRes = await adminClient.PostAsJsonAsync("/api/liquidaciones/generar", generarDto);
            retryPagadaRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var listCheck = (await (await adminClient.GetAsync($"/api/liquidaciones?peluqueroId={peluqueroId}"))
                .Content.ReadFromJsonAsync<ApiResponse<PagedResult<LiquidacionResponseDto>>>(CustomWebApplicationFactory.JsonOptions))!.Data!;
            listCheck.TotalRecords.Should().Be(1, "no deben generarse liquidaciones duplicadas");

            // 6. Nueva venta en el mismo período -> liquida SOLO el renglón nuevo y permite pagar CON imputación en caja abierta
            var venta2Res = await adminClient.PostAsJsonAsync("/api/ventas", new CrearVentaDto
            {
                SesionCajaId = sesionCajaId,
                ClienteId = 1,
                IdempotencyKey = $"t83-v2-{Guid.NewGuid():N}"[..36],
                Detalles = new List<DetalleVentaRequestDto>
                {
                    new()
                    {
                        TipoItem = TipoItemVentaEnum.Servicio,
                        ItemId = servicioId,
                        Cantidad = 1,
                        PeluqueroId = peluqueroId
                    }
                },
                Pagos = new List<PagoRequestDto>
                {
                    new() { MedioPagoId = 1, Importe = 20000.00m }
                }
            });
            venta2Res.StatusCode.Should().Be(HttpStatusCode.Created);

            var liq2Res = await adminClient.PostAsJsonAsync("/api/liquidaciones/generar", generarDto);
            liq2Res.StatusCode.Should().Be(HttpStatusCode.Created);
            var liq2 = (await liq2Res.Content.ReadFromJsonAsync<ApiResponse<LiquidacionResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            liq2.CantidadItemsLiquidados.Should().Be(1, "solo debe incluir el nuevo DetalleVenta sin reliquidar el anterior");
            liq2.TotalPagar.Should().Be(10000.00m);

            var pagarConCajaRes = await adminClient.PostAsync($"/api/liquidaciones/{liq2.Id}/pagar?sesionCajaId={sesionCajaId}", null);
            pagarConCajaRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var estadoCajaPostPago = (await (await adminClient.GetAsync("/api/caja/estado-actual"))
                .Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto>>(CustomWebApplicationFactory.JsonOptions))!.Data!;

            // MontoInicial (10.000) + VentasEfectivo (40.000) - EgresoLiquidacion #2 (10.000) = 40.000
            estadoCajaPostPago.TotalEgresosManuales.Should().Be(10000.00m);
            estadoCajaPostPago.MontoFinalEsperado.Should().Be(40000.00m);

            await CerrarCajaSiEstaAbiertaAsync(adminClient);
        }

        // =========================================================================
        // TAREA 8.3 - TEST 2: GET /api/comisiones retorna PeluqueroNombre == "Carlos Gómez"
        // =========================================================================
        [Fact]
        public async Task Tarea83_Comisiones_GetAll_Y_PorPeluquero_RetornanPeluqueroNombre_CarlosGomez()
        {
            using var adminClient = _factory.CreateAuthenticatedClient();

            var allRes = await adminClient.GetAsync("/api/comisiones");
            allRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var allBody = await allRes.Content.ReadFromJsonAsync<ApiResponse<List<ComisionConfigDto>>>(
                CustomWebApplicationFactory.JsonOptions);

            allBody.Should().NotBeNull();
            allBody!.Success.Should().BeTrue();
            allBody.Data.Should().NotBeNullOrEmpty();

            var carlosConfig = allBody.Data!.FirstOrDefault(c => c.PeluqueroId == 1 && c.ServicioId == null);
            carlosConfig.Should().NotBeNull("debe existir el esquema general semilla del Peluquero #1");
            carlosConfig!.PeluqueroNombre.Should().Be("Carlos Gómez");
            carlosConfig.NombrePeluquero.Should().Be("Carlos Gómez");
            carlosConfig.ServicioNombre.Should().NotBeNullOrWhiteSpace();
            carlosConfig.PorcentajeServicio.Should().Be(50.00m);
            carlosConfig.PorcentajeVentaProducto.Should().Be(10.00m);

            var byPelRes = await adminClient.GetAsync("/api/comisiones?peluqueroId=1");
            byPelRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var byPelBody = await byPelRes.Content.ReadFromJsonAsync<ApiResponse<List<ComisionConfigDto>>>(
                CustomWebApplicationFactory.JsonOptions);

            byPelBody!.Success.Should().BeTrue();
            byPelBody.Data.Should().Contain(c =>
                c.PeluqueroId == 1 &&
                c.PeluqueroNombre == "Carlos Gómez" &&
                c.PorcentajeServicio == 50.00m &&
                c.PorcentajeVentaProducto == 10.00m);
        }

        // =========================================================================
        // TAREA 8.3 - TEST 3: Separación Real SuperAdminSaaS (RolId = 6) en SuscripcionesController
        // =========================================================================
        [Fact]
        public async Task Tarea83_Suscripciones_CambiarPlan_403ParaAdministrador_Y_200ParaSuperAdminSaaS()
        {
            // 1. Administrador de salón (admin@tesis.com): puede leer su suscripción (200), pero recibe 403 al intentar cambiar plan
            using var adminClient = _factory.CreateAuthenticatedClient(
                userId: 1,
                username: "admin",
                email: "admin@tesis.com",
                role: "Administrador",
                tenantId: 1,
                sucursalId: 1);

            var miSubRes = await adminClient.GetAsync("/api/suscripciones/mi-suscripcion");
            miSubRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var forbiddenRes = await adminClient.PostAsJsonAsync("/api/suscripciones/cambiar-plan", new CambiarPlanSuscripcionDto
            {
                NuevoPlanSuscripcionId = 3
            });
            forbiddenRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // 2. Autenticación real contra BD del usuario semilla superadmin@mipeluqueria.com / SuperAdmin123! (RolId = 6)
            using var isolatedAuthFactory = _factory.WithWebHostBuilder(_ => { });
            using var superAdminClient = isolatedAuthFactory.CreateClient();
            superAdminClient.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            superAdminClient.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            var loginRes = await superAdminClient.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                UsernameOrEmail = "superadmin@mipeluqueria.com",
                Password = "SuperAdmin123!"
            });
            loginRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var loginBody = (await loginRes.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            loginBody.Rol.Should().Be("SuperAdminSaaS");
            loginBody.Token.Should().NotBeNullOrWhiteSpace();

            // 3. SuperAdminSaaS ejecuta POST /api/suscripciones/cambiar-plan -> 200 OK
            superAdminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

            var okEnterpriseRes = await superAdminClient.PostAsJsonAsync("/api/suscripciones/cambiar-plan", new CambiarPlanSuscripcionDto
            {
                NuevoPlanSuscripcionId = 3
            });
            okEnterpriseRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var enterpriseBody = (await okEnterpriseRes.Content.ReadFromJsonAsync<ApiResponse<SuscripcionEstadoResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;
            enterpriseBody.PlanActual.Should().Be("Enterprise");

            // Restaurar Plan Professional (Id = 2) para mantener el estado semilla limpio
            var restoreRes = await superAdminClient.PostAsJsonAsync("/api/suscripciones/cambiar-plan", new CambiarPlanSuscripcionDto
            {
                NuevoPlanSuscripcionId = 2
            });
            restoreRes.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // =========================================================================
        // HELPERS PRIVADOS
        // =========================================================================
        private static DateTime GetNextDayOfWeek(DateTime fromDate, DayOfWeek targetDay)
        {
            int daysAhead = ((int)targetDay - (int)fromDate.DayOfWeek + 7) % 7;
            return fromDate.Date.AddDays(daysAhead == 0 ? 7 : daysAhead);
        }

        private static async Task ConfigurarHorariosSemanaCompletaAsync(HttpClient client, int peluqueroId)
        {
            var horarios = new List<HorarioLaboralDto>();
            foreach (DayOfWeek day in new[]
                     {
                         DayOfWeek.Monday,
                         DayOfWeek.Tuesday,
                         DayOfWeek.Wednesday,
                         DayOfWeek.Thursday,
                         DayOfWeek.Friday,
                         DayOfWeek.Saturday
                     })
            {
                horarios.Add(new HorarioLaboralDto
                {
                    PeluqueroId = peluqueroId,
                    DiaSemana = (int)day,
                    HoraInicio = new TimeSpan(9, 0, 0),
                    HoraFin = new TimeSpan(20, 0, 0),
                    Activo = true
                });
            }

            var res = await client.PutAsJsonAsync($"/api/peluqueros/{peluqueroId}/horarios", new GuardarHorariosPeluqueroDto
            {
                Horarios = horarios
            });
            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        private static async Task<int> CrearTurnoAsync(
            HttpClient client,
            int clienteId,
            int peluqueroId,
            DateTime inicio,
            int servicioId,
            string observaciones)
        {
            var res = await client.PostAsJsonAsync("/api/turnos", new CrearTurnoDto
            {
                ClienteId = clienteId,
                PeluqueroId = peluqueroId,
                FechaHoraInicio = inicio,
                ServiciosIds = new List<int> { servicioId },
                Observaciones = observaciones
            });
            res.StatusCode.Should().Be(HttpStatusCode.Created);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<TurnoResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);
            return body!.Data!.Id;
        }

        private static async Task CerrarCajaSiEstaAbiertaAsync(HttpClient client)
        {
            var estadoRes = await client.GetAsync("/api/caja/estado-actual");
            if (estadoRes.StatusCode != HttpStatusCode.OK)
                return;

            var estadoBody = await estadoRes.Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto?>>(
                CustomWebApplicationFactory.JsonOptions);

            if (estadoBody?.Data != null && estadoBody.Data.EstadoAbierta)
            {
                var cierreRes = await client.PostAsJsonAsync("/api/caja/cerrar", new CierreCajaDto
                {
                    MontoFinalReal = estadoBody.Data.MontoFinalEsperado,
                    ObservacionesCierre = "Cierre automático de limpieza Tarea 8.2"
                });
                cierreRes.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
    }
}
