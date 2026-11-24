using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Dashboard;
using MiPeluqueria.Api.DTOs.SaaS;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.DTOs.Ventas;
using Xunit;

namespace MiPeluqueria.Api.Tests
{
    public class ContractTests12To22 : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ContractTests12To22(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Controller12_ProductosController_GetPagedAndStockCritico_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();

            var pagedRes = await client.GetAsync("/api/productos");
            pagedRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var pagedBody = await pagedRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductoDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            pagedBody.Should().NotBeNull();
            pagedBody!.Success.Should().BeTrue();
            pagedBody.Data.Should().NotBeNull();
            pagedBody.Data!.Data.Should().NotBeNull();

            var critRes = await client.GetAsync("/api/productos/stock-critico");
            critRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var critBody = await critRes.Content.ReadFromJsonAsync<ApiResponse<List<ProductoDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            critBody.Should().NotBeNull();
            critBody!.Success.Should().BeTrue();
            critBody.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller13_ProveedoresController_GetPaged_Returns200AndPagedResult()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/proveedores");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProveedorDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller14_OrdenesCompraController_GetPaged_Returns200AndPagedResult()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/ordenes-compra");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<OrdenCompraResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller15_CajaController_GetEstadoActualAndHistorial_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();

            var actualRes = await client.GetAsync("/api/caja/estado-actual");
            actualRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var actualBody = await actualRes.Content.ReadFromJsonAsync<ApiResponse<SesionCajaResponseDto?>>(
                CustomWebApplicationFactory.JsonOptions);
            actualBody.Should().NotBeNull();
            actualBody!.Success.Should().BeTrue();

            var histRes = await client.GetAsync("/api/caja/historial");
            histRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var histBody = await histRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<SesionCajaResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            histBody.Should().NotBeNull();
            histBody!.Success.Should().BeTrue();
            histBody.Data.Should().NotBeNull();
            histBody.Data!.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller16_VentasController_GetPorSesion_Returns200AndPagedResult()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/ventas/sesion/1");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<VentaResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller17_ComisionesController_GetAll_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/comisiones");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<ComisionConfigDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller18_LiquidacionesController_GetPaged_Returns200AndPagedResult()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/liquidaciones");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<LiquidacionResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller19_ConfiguracionSalonController_GetConfiguracion_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/configuracion");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<ConfiguracionSalonDto>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.NombreSalon.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Controller20_DashboardController_GetKpis_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/dashboard/kpis");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<DashboardKpiResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller21_SucursalesController_GetAll_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/sucursales");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<SucursalResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Controller22_SuscripcionesController_GetPlanes_Returns200AndApiResponse()
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            client.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            var res = await client.GetAsync("/api/suscripciones/planes");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<PlanSuscripcionResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNullOrEmpty();
        }
    }
}
