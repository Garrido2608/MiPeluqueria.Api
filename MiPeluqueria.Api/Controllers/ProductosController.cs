using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/productos")]
    [Produces("application/json")]
    [Authorize]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoService _productoService;

        public ProductosController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int userId) ? userId : 1;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductoDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] PaginationFilterDto filter,
            [FromQuery] bool soloBajoStock = false,
            [FromQuery] int? categoriaId = null,
            [FromQuery] int? proveedorId = null,
            CancellationToken ct = default)
        {
            var res = await _productoService.GetAllAsync(filter, soloBajoStock, categoriaId, proveedorId, ct);
            return Ok(res);
        }

        [HttpGet("stock-critico")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProductoDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStockCritico(CancellationToken ct = default)
        {
            var res = await _productoService.GetStockCriticoAsync(ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ProductoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProductoDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _productoService.GetByIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Recepcionista,Cajero,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<ProductoDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ProductoDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CrearProductoDto dto, CancellationToken ct = default)
        {
            var res = await _productoService.CreateAsync(dto, GetCurrentUserId(), ct);
            if (!res.Success)
                return BadRequest(res);

            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador,Recepcionista,Cajero,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<ProductoDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProductoDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] ActualizarProductoDto dto, CancellationToken ct = default)
        {
            var res = await _productoService.UpdateAsync(id, dto, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador,Recepcionista,Cajero,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var res = await _productoService.DeleteAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost("movimiento-stock")]
        [Authorize(Roles = "Administrador,Recepcionista,Cajero,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] MovimientoStockDto dto, CancellationToken ct = default)
        {
            var res = await _productoService.RegistrarMovimientoStockAsync(dto, GetCurrentUserId(), ct);
            return res.Success ? StatusCode(StatusCodes.Status201Created, res) : BadRequest(res);
        }

        [HttpGet("{id:int}/kardex")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<KardexItemResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetKardex(
            int id,
            [FromQuery] PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            var res = await _productoService.ObtenerKardexProductoAsync(id, filter, ct);
            return Ok(res);
        }
    }
}
