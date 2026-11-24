using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Dashboard;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,SuperAdminSaaS")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("kpis")]
        [ProducesResponseType(typeof(ApiResponse<DashboardKpiResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKpis(
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            CancellationToken ct = default)
        {
            var res = await _dashboardService.ObtenerKpisAsync(fechaDesde, fechaHasta, ct);
            return Ok(res);
        }

        [HttpGet("metricas-mes")]
        [ProducesResponseType(typeof(ApiResponse<DashboardKpiResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMetricasMesActual(CancellationToken ct = default)
        {
            var res = await _dashboardService.ObtenerMetricasMesActualAsync(ct);
            return Ok(res);
        }
    }
}
