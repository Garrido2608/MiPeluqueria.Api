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
        private readonly ICajaVentaService _cajaVentaService;

        public VentasController(ICajaVentaService cajaVentaService)
        {
            _cajaVentaService = cajaVentaService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _cajaVentaService.ObtenerVentaPorIdAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpGet("sesion/{sesionCajaId}")]
        public async Task<IActionResult> GetPorSesion(int sesionCajaId)
        {
            var res = await _cajaVentaService.ObtenerVentasPorSesionAsync(sesionCajaId);
            return Ok(res);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Registrar([FromBody] CrearVentaDto dto)
        {
            int usuarioId = GetCurrentUserId();
            var res = await _cajaVentaService.RegistrarVentaAsync(dto, usuarioId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("{id}/anular")]
        [Authorize]
        public async Task<IActionResult> Anular(int id, [FromBody] AnularVentaDto? dto = null)
        {
            int usuarioId = GetCurrentUserId();
            string motivo = dto?.Motivo ?? "Anulación de venta en mostrador.";
            var res = await _cajaVentaService.AnularVentaAsync(id, motivo, usuarioId);
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