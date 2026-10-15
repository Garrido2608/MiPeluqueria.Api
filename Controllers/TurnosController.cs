using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
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

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] DateTime? fecha = null, [FromQuery] int? peluqueroId = null)
        {
            var res = await _turnoService.GetAllAsync(fecha, peluqueroId);
            return Ok(res);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _turnoService.GetByIdAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpGet("disponibilidad")]
        public async Task<IActionResult> GetDisponibilidad(
            [FromQuery] int peluqueroId,
            [FromQuery] DateTime fecha,
            [FromQuery] List<int>? serviciosIds = null)
        {
            var res = await _turnoService.ObtenerHorariosDisponiblesAsync(peluqueroId, fecha, serviciosIds);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> GetPorCliente(int clienteId)
        {
            var res = await _turnoService.ObtenerTurnosPorClienteAsync(clienteId);
            return Ok(res);
        }

        [HttpGet("{id}/historial")]
        public async Task<IActionResult> GetHistorialEstados(int id)
        {
            var res = await _turnoService.ObtenerHistorialEstadoTurnoAsync(id);
            return Ok(res);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CrearTurnoDto dto)
        {
            var res = await _turnoService.CreateAsync(dto, GetCurrentUserId());
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPatch("{id}/estado")]
        [Authorize]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoTurnoDto dto)
        {
            var res = await _turnoService.CambiarEstadoAsync(id, dto, GetCurrentUserId());
            return res.Success ? Ok(res) : BadRequest(res);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 1;
        }
    }
}