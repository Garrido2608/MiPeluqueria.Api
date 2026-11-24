using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Middlewares;
using Xunit;

namespace MiPeluqueria.Api.Tests
{
    public class ObservabilityTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public ObservabilityTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HealthLive_Returns200Healthy_AndPropagatesCorrelationIdHeader()
        {
            using var client = _factory.CreateClient();
            const string customCorrelationId = "test-health-live-corr-75";

            var req = new HttpRequestMessage(HttpMethod.Get, "/health/live");
            req.Headers.Add(CorrelationIdMiddleware.HeaderName, customCorrelationId);

            var res = await client.SendAsync(req);
            string body = await res.Content.ReadAsStringAsync();

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            body.Trim().Should().Be("Healthy");
            res.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values).Should().BeTrue();
            values!.First().Should().Be(customCorrelationId);
        }

        [Fact]
        public async Task HealthReady_Returns200Healthy_ValidatingSqlServerConnection()
        {
            using var client = _factory.CreateClient();

            var res = await client.GetAsync("/health/ready");
            string body = await res.Content.ReadAsStringAsync();

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            body.Trim().Should().Be("Healthy");
            res.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values).Should().BeTrue();
            values!.First().Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task TelemetryClientErrors_Returns202Accepted_WithApiResponse()
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "1");
            client.DefaultRequestHeaders.Add("X-Sucursal-Id", "1");
            const string correlationId = "test-telemetry-corr-75";

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/telemetry/client-errors")
            {
                Content = JsonContent.Create(new ClientErrorReportDto
                {
                    Message = "Simulated UI crash in ObservabilityTests (email=user@example.com)",
                    Route = "/recepcion/pos",
                    Stack = "at PosPage (page.tsx:100:12)",
                    ComponentStack = "in PosPage > in DashboardLayout",
                    CorrelationId = correlationId,
                    TimestampUtc = DateTime.UtcNow.ToString("O"),
                    Severity = "error"
                })
            };
            req.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

            var res = await client.SendAsync(req);

            res.StatusCode.Should().Be(HttpStatusCode.Accepted);
            res.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values).Should().BeTrue();
            values!.First().Should().Be(correlationId);

            var body = await res.Content.ReadFromJsonAsync<ApiResponse<object>>(
                CustomWebApplicationFactory.JsonOptions);
            body.Should().NotBeNull();
            body!.Success.Should().BeTrue();
        }
    }
}
