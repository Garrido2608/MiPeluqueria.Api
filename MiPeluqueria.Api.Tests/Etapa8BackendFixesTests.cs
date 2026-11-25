using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MiPeluqueria.Api.DTOs.Common;
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
