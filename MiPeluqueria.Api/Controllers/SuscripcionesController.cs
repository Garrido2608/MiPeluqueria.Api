using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.SaaS;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/suscripciones")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,SuperAdminSaaS")]
    public class SuscripcionesController : ControllerBase
    {
        private readonly ISaaSService _saasService;

        public SuscripcionesController(ISaaSService saasService)
        {
            _saasService = saasService;
        }

        [HttpGet("planes")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PlanSuscripcionResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerPlanes(CancellationToken ct = default)
        {
            var res = await _saasService.ObtenerPlanesActivosAsync(ct);
            return Ok(res);
        }

        [HttpGet("mi-suscripcion")]
        [ProducesResponseType(typeof(ApiResponse<SuscripcionEstadoResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SuscripcionEstadoResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerMiSuscripcion(CancellationToken ct = default)
        {
            var res = await _saasService.ObtenerMiSuscripcionAsync(ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost("cambiar-plan")]
        [ProducesResponseType(typeof(ApiResponse<SuscripcionEstadoResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SuscripcionEstadoResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CambiarPlan([FromBody] CambiarPlanSuscripcionDto dto, CancellationToken ct = default)
        {
            var res = await _saasService.CambiarPlanAsync(dto, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}
