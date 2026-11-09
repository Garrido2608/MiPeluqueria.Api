using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/liquidaciones")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,SuperAdminSaaS")]
    public class LiquidacionesController : ControllerBase
    {
        private readonly ILiquidacionService _liquidacionService;

        public LiquidacionesController(ILiquidacionService liquidacionService)
        {
            _liquidacionService = liquidacionService;
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int userId) ? userId : 1;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<LiquidacionResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] PaginationFilterDto filter,
            [FromQuery] int? peluqueroId = null,
            [FromQuery] EstadoLiquidacionEnum? estado = null,
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            CancellationToken ct = default)
        {
            if (desde.HasValue && !filter.FechaDesde.HasValue)
                filter.FechaDesde = desde;

            if (hasta.HasValue && !filter.FechaHasta.HasValue)
                filter.FechaHasta = hasta;

            var res = await _liquidacionService.ObtenerTodasAsync(filter, peluqueroId, estado, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<LiquidacionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LiquidacionResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _liquidacionService.ObtenerPorIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost("generar")]
        [ProducesResponseType(typeof(ApiResponse<LiquidacionResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<LiquidacionResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Generar([FromBody] GenerarLiquidacionDto dto, CancellationToken ct = default)
        {
            var res = await _liquidacionService.LiquidarPeriodoAsync(dto, GetCurrentUserId(), ct);
            if (!res.Success)
                return BadRequest(res);

            return StatusCode(StatusCodes.Status201Created, res);
        }

        [HttpPost("{id:int}/pagar")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Pagar(
            int id,
            [FromQuery] int? sesionCajaId = null,
            CancellationToken ct = default)
        {
            var res = await _liquidacionService.RegistrarPagoLiquidacionAsync(id, sesionCajaId, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("{id:int}/anular")]
        [ProducesResponseType(typeof(ApiResponse<LiquidacionResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LiquidacionResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Anular(
            int id,
            [FromQuery] string motivo = "Anulación de liquidación",
            CancellationToken ct = default)
        {
            var res = await _liquidacionService.AnularLiquidacionAsync(id, motivo, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}
