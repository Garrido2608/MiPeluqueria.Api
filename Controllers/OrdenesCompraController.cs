using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/ordenes-compra")]
    [ApiController]
    [Authorize]
    public class OrdenesCompraController : ControllerBase
    {
        private readonly IOrdenCompraService _ordenCompraService;

        public OrdenesCompraController(IOrdenCompraService ordenCompraService)
        {
            _ordenCompraService = ordenCompraService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? proveedorId = null,
            [FromQuery] EstadoOrdenCompraEnum? estado = null,
            CancellationToken ct = default)
        {
            var res = await _ordenCompraService.ObtenerTodasAsync(proveedorId, estado, ct);
            return Ok(res);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _ordenCompraService.ObtenerPorIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearOrdenCompraDto dto, CancellationToken ct = default)
        {
            var res = await _ordenCompraService.CrearOrdenAsync(dto, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("{id}/recepcionar")]
        public async Task<IActionResult> Recepcionar(int id, [FromBody] RecepcionarOrdenCompraDto dto, CancellationToken ct = default)
        {
            var res = await _ordenCompraService.RecepcionarOrdenAsync(id, dto, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(int id, [FromQuery] string motivo = "Cancelación administrativa", CancellationToken ct = default)
        {
            var res = await _ordenCompraService.CancelarOrdenAsync(id, motivo, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 1;
        }
    }
}