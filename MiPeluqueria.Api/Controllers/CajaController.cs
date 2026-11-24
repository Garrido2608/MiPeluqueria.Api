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
    [Route("api/caja")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,Recepcionista,Cajero,SuperAdminSaaS")]
    public class CajaController : ControllerBase
    {
        private readonly ICajaService _cajaService;

        public CajaController(ICajaService cajaService)
        {
            _cajaService = cajaService;
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int userId) ? userId : 1;
        }

        [HttpGet("actual")]
        [HttpGet("estado-actual")]
        [ProducesResponseType(typeof(ApiResponse<SesionCajaResponseDto?>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCajaActual(CancellationToken ct = default)
        {
            var res = await _cajaService.ObtenerEstadoCajaActualAsync(ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<SesionCajaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SesionCajaResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _cajaService.ObtenerPorIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpGet("historial")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<SesionCajaResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistorial([FromQuery] PaginationFilterDto filter, CancellationToken ct = default)
        {
            var res = await _cajaService.ObtenerHistorialCajasAsync(filter, ct);
            return Ok(res);
        }

        [HttpPost("abrir")]
        [ProducesResponseType(typeof(ApiResponse<SesionCajaResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<SesionCajaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Abrir([FromBody] AperturaCajaDto dto, CancellationToken ct = default)
        {
            var res = await _cajaService.AbrirCajaAsync(dto, GetCurrentUserId(), ct);
            if (!res.Success)
                return BadRequest(res);

            return StatusCode(StatusCodes.Status201Created, res);
        }

        [HttpPost("egreso")]
        [ProducesResponseType(typeof(ApiResponse<MovimientoCajaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<MovimientoCajaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegistrarEgreso([FromBody] RegistrarMovimientoCajaDto dto, CancellationToken ct = default)
        {
            var res = await _cajaService.RegistrarEgresoCajaAsync(
                dto.SesionCajaId,
                dto.Monto,
                dto.Concepto,
                string.IsNullOrWhiteSpace(dto.Tipo) ? "EgresoMenor" : dto.Tipo,
                GetCurrentUserId(),
                ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("ingreso")]
        [ProducesResponseType(typeof(ApiResponse<MovimientoCajaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<MovimientoCajaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegistrarIngreso([FromBody] RegistrarMovimientoCajaDto dto, CancellationToken ct = default)
        {
            var res = await _cajaService.RegistrarIngresoCajaAsync(
                dto.SesionCajaId,
                dto.Monto,
                dto.Concepto,
                string.IsNullOrWhiteSpace(dto.Tipo) ? "IngresoManual" : dto.Tipo,
                GetCurrentUserId(),
                ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("cerrar")]
        [ProducesResponseType(typeof(ApiResponse<SesionCajaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<SesionCajaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cerrar([FromBody] CierreCajaDto dto, CancellationToken ct = default)
        {
            var res = await _cajaService.CerrarCajaAsync(dto, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}
