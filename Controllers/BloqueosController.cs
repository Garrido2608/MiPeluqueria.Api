using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BloqueosController : ControllerBase
    {
        private readonly IBloqueoHorarioService _bloqueoService;

        public BloqueosController(IBloqueoHorarioService bloqueoService)
        {
            _bloqueoService = bloqueoService;
        }

        [HttpGet("peluquero/{peluqueroId}")]
        public async Task<IActionResult> GetByPeluquero(int peluqueroId)
        {
            var res = await _bloqueoService.ObtenerPorPeluqueroAsync(peluqueroId);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] BloqueoHorarioDto dto)
        {
            var res = await _bloqueoService.CrearBloqueoAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var res = await _bloqueoService.EliminarBloqueoAsync(id);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }
    }
}