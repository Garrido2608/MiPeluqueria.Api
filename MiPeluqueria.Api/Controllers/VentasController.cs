using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/ventas")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,Recepcionista,Cajero,SuperAdminSaaS")]
    public class VentasController : ControllerBase
    {
        private readonly IVentaService _ventaService;

        public VentasController(IVentaService ventaService)
        {
            _ventaService = ventaService;
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int userId) ? userId : 1;
        }

        [HttpGet("sesion/{sesionCajaId:int}")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<VentaResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPorSesion(
            int sesionCajaId,
            [FromQuery] PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            var res = await _ventaService.ObtenerVentasPorSesionAsync(sesionCajaId, filter, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<VentaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<VentaResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _ventaService.ObtenerVentaPorIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<VentaResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<VentaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Registrar(
            [FromBody] CrearVentaDto dto,
            [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.IdempotencyKey) && !string.IsNullOrWhiteSpace(idempotencyKey))
            {
                dto.IdempotencyKey = idempotencyKey.Trim();
            }

            var res = await _ventaService.RegistrarVentaAsync(dto, GetCurrentUserId(), ct);
            if (!res.Success)
                return BadRequest(res);

            return StatusCode(StatusCodes.Status201Created, res);
        }

        [HttpPost("{id:int}/anular")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<VentaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<VentaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Anular(
            int id,
            [FromQuery] string motivo = "Anulación administrativa",
            CancellationToken ct = default)
        {
            var res = await _ventaService.AnularVentaAsync(id, motivo, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}
