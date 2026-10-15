using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoService _productoService;

        public ProductosController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool soloBajoStock = false)
        {
            var result = await _productoService.GetAllAsync(soloBajoStock);
            return Ok(result);
        }

        [HttpGet("stock-critico")]
        public async Task<IActionResult> GetStockCritico()
        {
            var result = await _productoService.GetStockCriticoAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _productoService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CrearProductoDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _productoService.CreateAsync(dto, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] CrearProductoDto dto)
        {
            var result = await _productoService.UpdateAsync(id, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _productoService.DeleteAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("movimiento-stock")]
        [Authorize]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] MovimientoStockDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _productoService.RegistrarMovimientoStockAsync(dto, userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}/kardex")]
        [Authorize]
        public async Task<IActionResult> GetKardex(int id)
        {
            var result = await _productoService.ObtenerKardexProductoAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 1;
        }
    }
}