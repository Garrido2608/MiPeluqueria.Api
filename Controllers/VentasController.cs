using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VentasController : ControllerBase
    {
        private readonly IVentaService _ventaService;

        public VentasController(IVentaService ventaService)
        {
            _ventaService = ventaService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _ventaService.ObtenerVentaPorIdAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpGet("sesion/{sesionCajaId}")]
        public async Task<IActionResult> GetPorSesion(int sesionCajaId)
        {
            var res = await _ventaService.ObtenerVentasPorSesionAsync(sesionCajaId);
            return Ok(res);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Registrar([FromBody] CrearVentaDto dto)
        {
            int usuarioId = GetCurrentUserId();
            var res = await _ventaService.RegistrarVentaAsync(dto, usuarioId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("{id}/anular")]
        [Authorize]
        public async Task<IActionResult> Anular(int id, [FromBody] AnularVentaDto? dto = null)
        {
            int usuarioId = GetCurrentUserId();
            string motivo = dto?.Motivo ?? "Anulación de venta en mostrador.";
            var res = await _ventaService.AnularVentaAsync(id, motivo, usuarioId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 1;
        }
    }
}