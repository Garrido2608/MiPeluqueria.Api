using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TurnosController : ControllerBase
    {
        private readonly ITurnoService _turnoService;

        public TurnosController(ITurnoService turnoService)
        {
            _turnoService = turnoService;
        }

        [HttpPost("reservar")]
        public async Task<IActionResult> Reservar([FromBody] CrearTurnoDto dto)
        {
            var res = await _turnoService.ReservarTurnoAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPut("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoTurnoDto dto)
        {
            int usuarioId = 1; // Extraíble del Claim del Token JWT en producción
            var res = await _turnoService.CambiarEstadoAsync(id, dto, usuarioId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpGet("agenda/{peluqueroId}")]
        public async Task<IActionResult> GetAgenda(int peluqueroId, [FromQuery] DateTime fecha)
        {
            var res = await _turnoService.ObtenerAgendaPeluqueroFechaAsync(peluqueroId, fecha);
            return Ok(res);
        }
    }
}