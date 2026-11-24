using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.DTOs.Turnos;
using Xunit;

namespace MiPeluqueria.Api.Tests
{
    public class ContractTests01To11 : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ContractTests01To11(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Controller01_AuthController_Login_Returns200AndApiResponse()
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            client.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            var res = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto
            {
                UsernameOrEmail = "admin@tesis.com",
                Password = "Admin123!"
            });

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.Token.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Controller02_UsuariosController_GetAll_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/usuarios");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<UsuarioResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Controller03_CatalogosController_GetTodos_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/catalogos/todos");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<CatalogosMaestrosResponseDto>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.MediosPago.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Controller04_ClientesController_GetPaged_Returns200AndPagedResult()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/clientes?pageNumber=1&pageSize=5");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ClienteResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller05_FichasTecnicasController_GetPorCliente_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/fichastecnicas/cliente/1");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<FichaTecnicaResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller06_PeluquerosController_GetAll_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/peluqueros");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<PeluqueroDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Controller07_ServiciosController_GetAll_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/servicios");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<ServicioDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Controller08_RecetasServicioController_GetPorServicio_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/recetas-servicio/servicio/1");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<RecetaItemResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller09_BloqueosController_GetPorPeluquero_Returns200AndApiResponse()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/bloqueos/peluquero/1");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<List<BloqueoHorarioResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller10_TurnosController_GetPaged_Returns200AndPagedResult()
        {
            using var client = _factory.CreateAuthenticatedClient();
            var res = await client.GetAsync("/api/turnos");

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await res.Content.ReadFromJsonAsync<ApiResponse<PagedResult<TurnoResponseDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
            body.Data.Should().NotBeNull();
            body.Data!.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task Controller11_PortalReservasController_GetPublicCatalogAndStaff_Returns200AndApiResponse()
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            client.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");

            var servRes = await client.GetAsync("/api/public/catalogo-servicios");
            servRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var servBody = await servRes.Content.ReadFromJsonAsync<ApiResponse<List<ServicioDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            servBody.Should().NotBeNull();
            servBody!.Success.Should().BeTrue();
            servBody.Data.Should().NotBeNullOrEmpty();

            var pelRes = await client.GetAsync("/api/public/peluqueros");
            pelRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var pelBody = await pelRes.Content.ReadFromJsonAsync<ApiResponse<List<PeluqueroDto>>>(
                CustomWebApplicationFactory.JsonOptions);
            pelBody.Should().NotBeNull();
            pelBody!.Success.Should().BeTrue();
            pelBody.Data.Should().NotBeNullOrEmpty();
        }
    }
}
