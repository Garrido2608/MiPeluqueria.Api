using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using Xunit;

namespace MiPeluqueria.Api.Tests
{
    public class BddTests06To10 : IClassFixture<CustomWebApplicationFactory>
    {
        private const string MercadoPagoWebhookSecret = "mp_webhook_secret_dev_2026";
        private readonly CustomWebApplicationFactory _factory;

        public BddTests06To10(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        // =========================================================================
        // TEST 06: Webhooks Criptográficos (HMAC-SHA256) y Transacción Compensatoria "Cruce de la Muerte"
        // =========================================================================
        [Fact]
        public async Task Test06_WebhookHmacSha256_And_DeathCrossCompensation()
        {
            using var adminClient = _factory.CreateAuthenticatedClient();
            using var webhookClient = _factory.CreateClient();
            webhookClient.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            webhookClient.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            // Preparar horario de Peluquero #1 para un martes futuro
            var targetTuesday = GetNextDayOfWeek(DateTime.UtcNow.Date.AddDays(28), DayOfWeek.Tuesday);
            await ConfigurarHorarioPeluqueroAsync(adminClient, 1, targetTuesday.DayOfWeek);

            // Crear Turno A (10:00) y simular expiración/cancelación automática (EstadoId = 5)
            int turnoAId = await CrearTurnoAsync(adminClient, 1, 1, targetTuesday.AddHours(10), 1, "[Reserva Online] Test06 Escenario A");
            var cancelARes = await adminClient.PatchAsJsonAsync($"/api/turnos/{turnoAId}/estado", new CambiarEstadoTurnoDto
            {
                NuevoEstadoId = (int)EstadoTurnoEnum.Cancelado,
                MotivoCancelacionId = 1,
                Observaciones = "Expiración automática de seña online (simulación worker)"
            });
            cancelARes.StatusCode.Should().Be(HttpStatusCode.OK);

            // 1. Firma x-signature inválida -> Rechazo 400/401 (Fail-Closed con FixedTimeEquals)
            var invalidPayload = new WebhookPagoNotificacionDto
            {
                Id = "MP-INVALID-SIG",
                Topic = "payment",
                ExternalReference = $"TURNO_{turnoAId}",
                Status = "approved",
                TransactionAmount = 5000.00m
            };

            var invalidReq = CreateSignedWebhookRequest("MercadoPago", invalidPayload, overrideSignature: "deadbeef001122334455");
            var invalidRes = await webhookClient.SendAsync(invalidReq);
            invalidRes.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);

            // 2. Escenario A (Slot aún libre): Webhook con firma HMAC-SHA256 válida reactiva a Confirmado (2)
            var scenarioAPayload = new WebhookPagoNotificacionDto
            {
                Id = "MP-PAY-DEATHCROSS-A",
                Topic = "payment",
                ExternalReference = $"TURNO_{turnoAId}",
                Status = "approved",
                TransactionAmount = 5000.00m
            };

            var validReqA = CreateSignedWebhookRequest("MercadoPago", scenarioAPayload);
            var validResA = await webhookClient.SendAsync(validReqA);
            validResA.StatusCode.Should().Be(HttpStatusCode.OK);

            var turnoARes = await adminClient.GetAsync($"/api/turnos/{turnoAId}");
            turnoARes.StatusCode.Should().Be(HttpStatusCode.OK);
            var turnoABody = await turnoARes.Content.ReadFromJsonAsync<ApiResponse<TurnoResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);

            turnoABody!.Data!.EstadoId.Should().Be((int)EstadoTurnoEnum.Confirmado);
            turnoABody.Data.SenaPagada.Should().BeTrue();
            turnoABody.Data.ReferenciaPagoExterno.Should().Be("MP-PAY-DEATHCROSS-A");

            var histARes = await adminClient.GetAsync($"/api/turnos/{turnoAId}/historial");
            histARes.StatusCode.Should().Be(HttpStatusCode.OK);
            var histABody = await histARes.Content.ReadFromJsonAsync<ApiResponse<List<HistorialEstadoTurnoResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            histABody!.Data.Should().Contain(h =>
                h.EstadoAnteriorId == (int)EstadoTurnoEnum.Cancelado &&
                h.EstadoNuevoId == (int)EstadoTurnoEnum.Confirmado);

            // 3. Escenario B (Slot ocupado por otro turno en el ínterin): Mantiene Cancelado (5), SenaPagada = true y alerta en Observaciones
            int turnoBId = await CrearTurnoAsync(adminClient, 1, 1, targetTuesday.AddHours(14), 1, "[Reserva Online] Test06 Escenario B");
            var cancelBRes = await adminClient.PatchAsJsonAsync($"/api/turnos/{turnoBId}/estado", new CambiarEstadoTurnoDto
            {
                NuevoEstadoId = (int)EstadoTurnoEnum.Cancelado,
                MotivoCancelacionId = 1,
                Observaciones = "Expiración automática de seña online"
            });
            cancelBRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // Otro cliente ocupa el mismo slot 14:00 mientras llegaba el webhook
            int turnoOcupanteId = await CrearTurnoAsync(adminClient, 1, 1, targetTuesday.AddHours(14), 1, "Cliente ocupa slot 14:00");
            turnoOcupanteId.Should().BeGreaterThan(0);

            var scenarioBPayload = new WebhookPagoNotificacionDto
            {
                Id = "MP-PAY-DEATHCROSS-B",
                Topic = "payment",
                ExternalReference = $"TURNO_{turnoBId}",
                Status = "approved",
                TransactionAmount = 5000.00m
            };

            var validReqB = CreateSignedWebhookRequest("MercadoPago", scenarioBPayload);
            var validResB = await webhookClient.SendAsync(validReqB);
            validResB.StatusCode.Should().Be(HttpStatusCode.OK);

            var turnoBRes = await adminClient.GetAsync($"/api/turnos/{turnoBId}");
            var turnoBBody = await turnoBRes.Content.ReadFromJsonAsync<ApiResponse<TurnoResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);

            turnoBBody!.Data!.EstadoId.Should().Be((int)EstadoTurnoEnum.Cancelado);
            turnoBBody.Data.SenaPagada.Should().BeTrue();
            turnoBBody.Data.MontoSena.Should().Be(5000.00m);
            turnoBBody.Data.Observaciones.Should().NotBeNullOrWhiteSpace();
            turnoBBody.Data.Observaciones!.ToUpperInvariant().Should().Contain("ALERTA");
        }

        // =========================================================================
        // TEST 07: POS Idempotente, Deducción de Seña y Arqueo Físico vs. Digital
        // =========================================================================
        [Fact]
        public async Task Test07_PosIdempotencyKey_SenaDeduction_PhysicalVsDigitalCash()
        {
            using var adminClient = _factory.CreateAuthenticatedClient();
            using var webhookClient = _factory.CreateClient();
            webhookClient.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            webhookClient.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            // Asegurar caja limpia y abrir con MontoInicial = 10000
            await CerrarCajaSiEstaAbiertaAsync(adminClient);

            var abrirRes = await adminClient.PostAsJsonAsync("/api/caja/abrir", new AperturaCajaDto
            {
                MontoInicial = 10000.00m
            });
            abrirRes.StatusCode.Should().Be(HttpStatusCode.Created);
            var cajaAbierta = (await abrirRes.Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            // Crear servicio de $20.000 (30 min) y turno con seña abonada de $5.000
            var servRes = await adminClient.PostAsJsonAsync("/api/servicios", new CrearServicioDto
            {
                CategoriaId = 1,
                Nombre = $"Servicio POS Test07 {Guid.NewGuid():N}"[..26],
                Descripcion = "Servicio $20.000 para Test 07",
                Precio = 20000.00m,
                DuracionMinutos = 30
            });
            servRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int servicioId = (await servRes.Content.ReadFromJsonAsync<ApiResponse<ServicioDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            var targetWednesday = GetNextDayOfWeek(DateTime.UtcNow.Date.AddDays(35), DayOfWeek.Wednesday);
            await ConfigurarHorarioPeluqueroAsync(adminClient, 1, targetWednesday.DayOfWeek);
            int turnoId = await CrearTurnoAsync(adminClient, 1, 1, targetWednesday.AddHours(11), servicioId, "[Reserva Online] Test07 POS");

            // Acreditar seña de $5.000 vía Webhook HMAC-SHA256 válido
            var senaWebhookReq = CreateSignedWebhookRequest("MercadoPago", new WebhookPagoNotificacionDto
            {
                Id = "MP-SENA-TEST07",
                Topic = "payment",
                ExternalReference = $"TURNO_{turnoId}",
                Status = "approved",
                TransactionAmount = 5000.00m
            });
            var senaRes = await webhookClient.SendAsync(senaWebhookReq);
            senaRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // Facturar en POS con X-Idempotency-Key y MedioPagoId = 5 (Mercado Pago QR, EsEfectivoFisico = false) por $15.000
            string idempotencyKey = $"pos-uuid-test07-{Guid.NewGuid():N}"[..36];
            var ventaDto = new CrearVentaDto
            {
                SesionCajaId = cajaAbierta.Id,
                ClienteId = 1,
                TurnoId = turnoId,
                Detalles = new List<DetalleVentaRequestDto>
                {
                    new()
                    {
                        TipoItem = TipoItemVentaEnum.Servicio,
                        ItemId = servicioId,
                        Cantidad = 1,
                        PeluqueroId = null // Debe heredar turno.PeluqueroId = 1
                    }
                },
                Pagos = new List<PagoRequestDto>
                {
                    new()
                    {
                        MedioPagoId = 5, // Mercado Pago QR (EsEfectivoFisico = false)
                        Importe = 15000.00m,
                        ReferenciaOperacion = "MP-QR-TEST07"
                    }
                }
            };

            var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/ventas")
            {
                Content = JsonContent.Create(ventaDto)
            };
            req1.Headers.Add("X-Idempotency-Key", idempotencyKey);

            var ventaRes1 = await adminClient.SendAsync(req1);
            ventaRes1.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
            var venta1 = (await ventaRes1.Content.ReadFromJsonAsync<ApiResponse<VentaResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            venta1.Subtotal.Should().Be(20000.00m);
            venta1.DescuentoSenaAplicado.Should().Be(5000.00m);
            venta1.Total.Should().Be(15000.00m);
            venta1.Detalles.Should().HaveCount(1);
            venta1.Detalles[0].PeluqueroId.Should().Be(1, "los renglones sin profesional explícito deben heredar turno.PeluqueroId");

            // Verificar que el turno pasó a Completado (4)
            var turnoPostVenta = (await (await adminClient.GetAsync($"/api/turnos/{turnoId}"))
                .Content.ReadFromJsonAsync<ApiResponse<TurnoResponseDto>>(CustomWebApplicationFactory.JsonOptions))!.Data!;
            turnoPostVenta.EstadoId.Should().Be((int)EstadoTurnoEnum.Completado);

            // Reenvío idempotente con la MISMA X-Idempotency-Key
            var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/ventas")
            {
                Content = JsonContent.Create(ventaDto)
            };
            req2.Headers.Add("X-Idempotency-Key", idempotencyKey);

            var ventaRes2 = await adminClient.SendAsync(req2);
            ventaRes2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
            var venta2 = (await ventaRes2.Content.ReadFromJsonAsync<ApiResponse<VentaResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            venta2.Id.Should().Be(venta1.Id, "el reenvío con la misma Idempotency-Key debe devolver la venta existente sin duplicar");

            var ventasSesion = (await (await adminClient.GetAsync($"/api/ventas/sesion/{cajaAbierta.Id}"))
                .Content.ReadFromJsonAsync<ApiResponse<PagedResult<VentaResponseDto>>>(CustomWebApplicationFactory.JsonOptions))!.Data!;
            ventasSesion.TotalRecords.Should().Be(1);

            // Verificar Arqueo Físico vs Digital en GET /api/caja/estado-actual
            var cajaActual = (await (await adminClient.GetAsync("/api/caja/estado-actual"))
                .Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto>>(CustomWebApplicationFactory.JsonOptions))!.Data!;

            cajaActual.TotalVentasDigital.Should().Be(15000.00m);
            cajaActual.TotalVentasEfectivo.Should().Be(0.00m);
            cajaActual.MontoFinalEsperado.Should().Be(10000.00m);

            await CerrarCajaSiEstaAbiertaAsync(adminClient);
        }

        // =========================================================================
        // TEST 08: Consumo de Gabinete No Bloqueante con Warnings y Kardex
        // =========================================================================
        [Fact]
        public async Task Test08_GabineteConsumption_NonBlockingWarnings_And_Kardex()
        {
            using var adminClient = _factory.CreateAuthenticatedClient();

            // 1. Crear producto insumo con StockInicial = 1
            var prodRes = await adminClient.PostAsJsonAsync("/api/productos", new CrearProductoDto
            {
                CategoriaId = 1,
                Nombre = $"Insumo Tinte Test08 {Guid.NewGuid():N}"[..28],
                CodigoBarras = $"779{Random.Shared.Next(100000000, 999999999)}",
                PrecioCosto = 1200.00m,
                PrecioVenta = 3000.00m,
                StockInicial = 1,
                StockMinimoAlerta = 2,
                EsParaVenta = false
            });
            prodRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int productoId = (await prodRes.Content.ReadFromJsonAsync<ApiResponse<ProductoDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            // 2. Crear servicio y receta técnica que consume 2 unidades (pero solo hay 1 en stock)
            var servRes = await adminClient.PostAsJsonAsync("/api/servicios", new CrearServicioDto
            {
                CategoriaId = 1,
                Nombre = $"Color Test08 {Guid.NewGuid():N}"[..24],
                Descripcion = "Servicio con receta de 2 unidades",
                Precio = 15000.00m,
                DuracionMinutos = 45
            });
            servRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int servicioId = (await servRes.Content.ReadFromJsonAsync<ApiResponse<ServicioDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            var putRecetaRes = await adminClient.PutAsJsonAsync("/api/recetas-servicio", new ConfigurarRecetaServicioDto
            {
                ServicioId = servicioId,
                Insumos = new List<ItemRecetaInputDto>
                {
                    new()
                    {
                        ProductoId = productoId,
                        CantidadUnidadesDescontar = 2,
                        CantidadMedidaEstandar = 60m,
                        UnidadMedida = "g",
                        DescuentoAutomaticoAlCompletarTurno = true
                    }
                }
            });
            putRecetaRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // 3. Crear turno y consumir gabinete (no bloqueante, descuenta hasta 0 y retorna Warnings)
            var targetDay = GetNextDayOfWeek(DateTime.UtcNow.Date.AddDays(42), DayOfWeek.Thursday);
            await ConfigurarHorarioPeluqueroAsync(adminClient, 1, targetDay.DayOfWeek);
            int turnoId = await CrearTurnoAsync(adminClient, 1, 1, targetDay.AddHours(10), servicioId, "Turno Test08 Gabinete");

            var consumirRes1 = await adminClient.PostAsync($"/api/recetas-servicio/turno/{turnoId}/consumir-gabinete", null);
            consumirRes1.StatusCode.Should().Be(HttpStatusCode.OK);
            var consumirBody1 = await consumirRes1.Content.ReadFromJsonAsync<ApiResponse<int>>(
                CustomWebApplicationFactory.JsonOptions);

            consumirBody1!.Success.Should().BeTrue();
            consumirBody1.Data.Should().Be(1);
            consumirBody1.Warnings.Should().NotBeNullOrEmpty("el stock insuficiente (1 < 2) debe emitirse como alerta no bloqueante en Warnings");

            // Verificar StockActual = 0 en el producto
            var prodPost = (await (await adminClient.GetAsync($"/api/productos/{productoId}"))
                .Content.ReadFromJsonAsync<ApiResponse<ProductoDto>>(CustomWebApplicationFactory.JsonOptions))!.Data!;
            prodPost.StockActual.Should().Be(0);

            // 4. Segunda invocación para el mismo turno -> idempotente (0 nuevos descuentos)
            var consumirRes2 = await adminClient.PostAsync($"/api/recetas-servicio/turno/{turnoId}/consumir-gabinete", null);
            consumirRes2.StatusCode.Should().Be(HttpStatusCode.OK);
            var consumirBody2 = await consumirRes2.Content.ReadFromJsonAsync<ApiResponse<int>>(
                CustomWebApplicationFactory.JsonOptions);
            consumirBody2!.Success.Should().BeTrue();
            consumirBody2.Data.Should().Be(0, "el consumo de gabinete por turno debe ser idempotente");

            // 5. Verificar GET /api/productos/{id}/kardex devuelve PagedResult con saldoResultante == 0
            var kardexRes = await adminClient.GetAsync($"/api/productos/{productoId}/kardex");
            kardexRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var kardexBody = await kardexRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<KardexItemResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);

            kardexBody!.Success.Should().BeTrue();
            kardexBody.Data.Should().NotBeNull();
            kardexBody.Data!.Data.Should().NotBeEmpty();
            kardexBody.Data.Data.First().SaldoResultante.Should().Be(0);
        }

        // =========================================================================
        // TEST 09: Jerarquía de Comisiones, Liquidaciones y Protección Contable de Anulación
        // =========================================================================
        [Fact]
        public async Task Test09_CommissionHierarchy_And_AccountingVoidProtection()
        {
            using var adminClient = _factory.CreateAuthenticatedClient();

            // 1. Crear Peluquero aislado y 2 servicios de $10.000 cada uno
            var pelRes = await adminClient.PostAsJsonAsync("/api/peluqueros", new CrearPeluqueroDto
            {
                Nombre = "Estilista",
                Apellido = $"ComisionTest09_{Guid.NewGuid():N}"[..18],
                Email = $"estilista09_{Guid.NewGuid():N}@tesis.com",
                Telefono = "+5493514440099",
                ColorAgendaHex = "#10B981",
                EspecialidadesIds = new List<int> { 1 }
            });
            pelRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int peluqueroId = (await pelRes.Content.ReadFromJsonAsync<ApiResponse<PeluqueroDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            var servGenRes = await adminClient.PostAsJsonAsync("/api/servicios", new CrearServicioDto
            {
                CategoriaId = 1,
                Nombre = $"ServGen Test09 {Guid.NewGuid():N}"[..24],
                Precio = 10000.00m,
                DuracionMinutos = 30
            });
            servGenRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int servGenId = (await servGenRes.Content.ReadFromJsonAsync<ApiResponse<ServicioDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            var servEspRes = await adminClient.PostAsJsonAsync("/api/servicios", new CrearServicioDto
            {
                CategoriaId = 1,
                Nombre = $"ServEsp Test09 {Guid.NewGuid():N}"[..24],
                Precio = 10000.00m,
                DuracionMinutos = 30
            });
            servEspRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int servEspId = (await servEspRes.Content.ReadFromJsonAsync<ApiResponse<ServicioDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            // 2. Configurar comisión general 40% (ServicioId = null) y específica 60% (ServicioId = servEspId)
            var comGenRes = await adminClient.PostAsJsonAsync("/api/comisiones", new GuardarComisionConfigDto
            {
                PeluqueroId = peluqueroId,
                ServicioId = null,
                PorcentajeServicio = 40.00m,
                PorcentajeVentaProducto = 10.00m
            });
            comGenRes.StatusCode.Should().Be(HttpStatusCode.Created);

            var comEspRes = await adminClient.PostAsJsonAsync("/api/comisiones", new GuardarComisionConfigDto
            {
                PeluqueroId = peluqueroId,
                ServicioId = servEspId,
                PorcentajeServicio = 60.00m,
                PorcentajeVentaProducto = 10.00m
            });
            comEspRes.StatusCode.Should().Be(HttpStatusCode.Created);

            // 3. Abrir caja y registrar Venta con ambos servicios ($10.000 general + $10.000 específico)
            await CerrarCajaSiEstaAbiertaAsync(adminClient);
            var abrirRes = await adminClient.PostAsJsonAsync("/api/caja/abrir", new AperturaCajaDto
            {
                MontoInicial = 5000.00m
            });
            abrirRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int sesionCajaId = (await abrirRes.Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            var ventaRes = await adminClient.PostAsJsonAsync("/api/ventas", new CrearVentaDto
            {
                SesionCajaId = sesionCajaId,
                ClienteId = 1,
                IdempotencyKey = $"test09-venta-{Guid.NewGuid():N}"[..36],
                Detalles = new List<DetalleVentaRequestDto>
                {
                    new() { TipoItem = TipoItemVentaEnum.Servicio, ItemId = servGenId, Cantidad = 1, PeluqueroId = peluqueroId },
                    new() { TipoItem = TipoItemVentaEnum.Servicio, ItemId = servEspId, Cantidad = 1, PeluqueroId = peluqueroId }
                },
                Pagos = new List<PagoRequestDto>
                {
                    new() { MedioPagoId = 1, Importe = 20000.00m }
                }
            });
            ventaRes.StatusCode.Should().Be(HttpStatusCode.Created);
            int ventaId = (await ventaRes.Content.ReadFromJsonAsync<ApiResponse<VentaResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!.Id;

            // 4. Generar liquidación y validar jerarquía: 40% de 10000 (4000) + 60% de 10000 (6000) = 10000
            var liqRes = await adminClient.PostAsJsonAsync("/api/liquidaciones/generar", new GenerarLiquidacionDto
            {
                PeluqueroId = peluqueroId,
                FechaDesde = DateTime.UtcNow.Date.AddDays(-1),
                FechaHasta = DateTime.UtcNow.Date.AddDays(1),
                Bonificaciones = 0m,
                Descuentos = 0m,
                Observaciones = "Liquidación Test 09 Jerarquía"
            });
            liqRes.StatusCode.Should().Be(HttpStatusCode.Created);
            var liqBody = (await liqRes.Content.ReadFromJsonAsync<ApiResponse<LiquidacionResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;

            liqBody.TotalServicios.Should().Be(10000.00m, "debe aplicar 60% al servicio específico (6000) y 40% al servicio general (4000)");
            liqBody.TotalPagar.Should().Be(10000.00m);
            liqBody.Detalles.Should().Contain(d => d.ItemId == servEspId && d.MontoComisionCalculado == 6000.00m);
            liqBody.Detalles.Should().Contain(d => d.ItemId == servGenId && d.MontoComisionCalculado == 4000.00m);

            // 5. Protección contable #1: Rechazar con 400 anulación de venta incluida en liquidación activa
            var voidWhileLiquidatedRes = await adminClient.DeleteAsync($"/api/ventas/{ventaId}?motivo=IntentoAnulacionConLiquidacion");
            voidWhileLiquidatedRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // 6. Anular liquidación en estado Pendiente (1) -> libera DetallesLiquidacion con IsDeleted = true
            var anularLiqRes = await adminClient.PostAsync($"/api/liquidaciones/{liqBody.Id}/anular?motivo=ErrorCalculo", null);
            anularLiqRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var anularLiqBody = (await anularLiqRes.Content.ReadFromJsonAsync<ApiResponse<LiquidacionResponseDto>>(
                CustomWebApplicationFactory.JsonOptions))!.Data!;
            anularLiqBody.Estado.Should().Be(EstadoLiquidacionEnum.Anulada.ToString());

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var detallesSql = await db.DetallesLiquidacion
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(dl => dl.LiquidacionId == liqBody.Id)
                    .ToListAsync();

                detallesSql.Should().HaveCount(2);
                detallesSql.Should().OnlyContain(dl => dl.IsDeleted);
            }

            // 7. Protección contable #2: Cerrar caja y rechazar con 400 anulación de venta de caja cerrada
            await CerrarCajaSiEstaAbiertaAsync(adminClient);
            var voidAfterCloseRes = await adminClient.DeleteAsync($"/api/ventas/{ventaId}?motivo=IntentoAnulacionCajaCerrada");
            voidAfterCloseRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // =========================================================================
        // TEST 10: Alertas No Bloqueantes en Bloqueos Horarios y Tolerancia a Fallos SMTP
        // =========================================================================
        [Fact]
        public async Task Test10_BloqueosOverlapWarnings_And_SmtpFailSafe()
        {
            using var adminClient = _factory.CreateAuthenticatedClient();

            // 1. Crear turno confirmado a las 16:00 de un viernes futuro
            var targetFriday = GetNextDayOfWeek(DateTime.UtcNow.Date.AddDays(49), DayOfWeek.Friday);
            await ConfigurarHorarioPeluqueroAsync(adminClient, 1, targetFriday.DayOfWeek);
            int turnoId = await CrearTurnoAsync(adminClient, 1, 1, targetFriday.AddHours(16), 1, "Turno 16:00 Test10");

            var confirmRes = await adminClient.PatchAsJsonAsync($"/api/turnos/{turnoId}/estado", new CambiarEstadoTurnoDto
            {
                NuevoEstadoId = (int)EstadoTurnoEnum.Confirmado
            });
            confirmRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // 2. Registrar bloqueo horario de 15:00 a 18:00 -> guarda con éxito pero informa el turno afectado en Warnings
            var bloqueoRes = await adminClient.PostAsJsonAsync("/api/bloqueos", new BloqueoHorarioDto
            {
                PeluqueroId = 1,
                FechaInicio = targetFriday.AddHours(15),
                FechaFin = targetFriday.AddHours(18),
                Motivo = "Capacitación Técnica Test 10"
            });
            bloqueoRes.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);
            var bloqueoBody = await bloqueoRes.Content.ReadFromJsonAsync<ApiResponse<BloqueoHorarioResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);

            bloqueoBody!.Success.Should().BeTrue();
            bloqueoBody.Data.Should().NotBeNull();
            bloqueoBody.Warnings.Should().NotBeNullOrEmpty("debe alertar en Warnings sobre los turnos solapados sin bloquear el guardado");
            string joinedWarnings = string.Join(" | ", bloqueoBody.Warnings!);
            joinedWarnings.Should().Contain(turnoId.ToString());
            joinedWarnings.Should().Contain("16:00");

            // 3. SMTP Fail-Safe (Smtp:Host vacío en entorno de test): no demora > 5000 ms, no aborta y devuelve Warnings
            using var isolatedFactory = _factory.WithWebHostBuilder(_ => { });
            using var anonClient = isolatedFactory.CreateClient();
            anonClient.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            anonClient.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            var sw = Stopwatch.StartNew();
            var otpRes = await anonClient.PostAsJsonAsync("/api/auth/olvide-password", new OlvidePasswordDto
            {
                Email = "admin@tesis.com"
            });
            sw.Stop();

            otpRes.StatusCode.Should().Be(HttpStatusCode.OK);
            sw.ElapsedMilliseconds.Should().BeLessThan(5000, "el cortocircuito defensivo SMTP no debe demorar más de 5000 ms");

            var otpBody = await otpRes.Content.ReadFromJsonAsync<ApiResponse<bool>>(
                CustomWebApplicationFactory.JsonOptions);
            otpBody!.Success.Should().BeTrue();
            otpBody.Warnings.Should().NotBeNullOrEmpty("la ausencia de servidor SMTP debe notificarse de forma no bloqueante en ApiResponse.Warnings");
        }

        // =========================================================================
        // HELPERS PRIVADOS
        // =========================================================================
        private static HttpRequestMessage CreateSignedWebhookRequest(
            string proveedor,
            WebhookPagoNotificacionDto payload,
            string? overrideSignature = null)
        {
            string rawJson = JsonSerializer.Serialize(payload, CustomWebApplicationFactory.JsonOptions);
            string signature = overrideSignature ?? ComputeHmacSha256Header(rawJson, MercadoPagoWebhookSecret);

            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/webhooks/pagos/{proveedor}")
            {
                Content = new StringContent(rawJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-signature", signature);
            return request;
        }

        private static string ComputeHmacSha256Header(string rawBody, string secret)
        {
            byte[] hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(rawBody));
            return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static DateTime GetNextDayOfWeek(DateTime fromDate, DayOfWeek targetDay)
        {
            int daysAhead = ((int)targetDay - (int)fromDate.DayOfWeek + 7) % 7;
            return fromDate.Date.AddDays(daysAhead == 0 ? 7 : daysAhead);
        }

        private static async Task ConfigurarHorarioPeluqueroAsync(HttpClient client, int peluqueroId, DayOfWeek day)
        {
            var res = await client.PutAsJsonAsync($"/api/peluqueros/{peluqueroId}/horarios", new GuardarHorariosPeluqueroDto
            {
                Horarios = new List<HorarioLaboralDto>
                {
                    new()
                    {
                        PeluqueroId = peluqueroId,
                        DiaSemana = (int)day,
                        HoraInicio = new TimeSpan(9, 0, 0),
                        HoraFin = new TimeSpan(20, 0, 0),
                        Activo = true
                    }
                }
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
                    ObservacionesCierre = "Cierre automático de limpieza de test"
                });
                cierreRes.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
    }
}
