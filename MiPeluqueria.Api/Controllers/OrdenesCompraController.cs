using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/ordenes-compra")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,Recepcionista,Cajero,SuperAdminSaaS")]
    public class OrdenesCompraController : ControllerBase
    {
        private readonly IOrdenCompraService _ordenService;

        public OrdenesCompraController(IOrdenCompraService ordenService)
        {
            _ordenService = ordenService;
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int userId) ? userId : 1;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<OrdenCompraResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] PaginationFilterDto filter,
            [FromQuery] int? proveedorId = null,
            [FromQuery] EstadoOrdenCompraEnum? estado = null,
            CancellationToken ct = default)
        {
            var res = await _ordenService.ObtenerTodasAsync(filter, proveedorId, estado, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<OrdenCompraResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OrdenCompraResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _ordenService.ObtenerPorIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<OrdenCompraResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<OrdenCompraResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CrearOrdenCompraDto dto, CancellationToken ct = default)
        {
            var res = await _ordenService.CrearOrdenAsync(dto, GetCurrentUserId(), ct);
            if (!res.Success)
                return BadRequest(res);

            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPost("{id:int}/recibir")]
        [ProducesResponseType(typeof(ApiResponse<OrdenCompraResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OrdenCompraResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RecibirMercaderia(
            int id,
            [FromBody] RecepcionarOrdenCompraDto? dto = null,
            CancellationToken ct = default)
        {
            var res = await _ordenService.RecepcionarOrdenAsync(id, dto ?? new RecepcionarOrdenCompraDto(), GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPatch("{id:int}/cancelar")]
        [HttpPost("{id:int}/cancelar")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancelar(
            int id,
            [FromQuery] string motivo = "Cancelación administrativa",
            CancellationToken ct = default)
        {
            var res = await _ordenService.CancelarOrdenAsync(id, motivo, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}
