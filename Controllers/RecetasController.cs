using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/recetas-servicio")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,Recepcionista,Cajero,Peluquero,SuperAdminSaaS")]
    public class RecetasServicioController : ControllerBase
    {
        private readonly IRecetaServicioService _recetaService;

        public RecetasServicioController(IRecetaServicioService recetaService)
        {
            _recetaService = recetaService;
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int userId) ? userId : 1;
        }

        [HttpGet("{servicioId:int}")]
        [HttpGet("servicio/{servicioId:int}")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RecetaItemResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerPorServicio(int servicioId, CancellationToken ct = default)
        {
            var res = await _recetaService.ObtenerRecetaPorServicioAsync(servicioId, ct);
            return Ok(res);
        }

        [HttpPut]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfigurarBody(
            [FromBody] ConfigurarRecetaServicioDto dto,
            CancellationToken ct = default)
        {
            var res = await _recetaService.GuardarRecetaServicioAsync(dto, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPut("{servicioId:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Configurar(
            int servicioId,
            [FromBody] ConfigurarRecetaServicioDto dto,
            CancellationToken ct = default)
        {
            dto.ServicioId = servicioId;
            var res = await _recetaService.GuardarRecetaServicioAsync(dto, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("descontar-turno/{turnoId:int}")]
        [HttpPost("turno/{turnoId:int}/consumir-gabinete")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DescontarTurno(int turnoId, CancellationToken ct = default)
        {
            var res = await _recetaService.AplicarConsumoGabinetePorTurnoAsync(turnoId, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}
