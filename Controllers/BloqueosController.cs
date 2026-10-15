using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> GetPorPeluquero(int peluqueroId)
        {
            var res = await _bloqueoService.ObtenerPorPeluqueroAsync(peluqueroId);
            return Ok(res);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Crear([FromBody] BloqueoHorarioDto dto)
        {
            var res = await _bloqueoService.CrearBloqueoAsync(dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Eliminar(int id)
        {
            var res = await _bloqueoService.EliminarBloqueoAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }
    }
}