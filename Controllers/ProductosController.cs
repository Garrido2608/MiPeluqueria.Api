using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly IProductoService _productoService;

        public ProductosController(IProductoService productoService)
        {
            _productoService = productoService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _productoService.GetAllAsync());

        [HttpGet("stock-critico")]
        public async Task<IActionResult> GetStockCritico() =>
            Ok(await _productoService.GetStockCriticoAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _productoService.GetByIdAsync(id);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearProductoDto dto)
        {
            int usuarioId = ObtenerUsuarioId();
            var res = await _productoService.CreateAsync(dto, usuarioId);
            if (!res.Success) return BadRequest(res);
            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPost("movimiento")]
        public async Task<IActionResult> RegistrarMovimiento([FromBody] MovimientoStockDto dto)
        {
            int usuarioId = ObtenerUsuarioId();
            var res = await _productoService.RegistrarMovimientoAsync(dto, usuarioId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        private int ObtenerUsuarioId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            return int.TryParse(claim, out int id) ? id : 1;
        }
    }
}