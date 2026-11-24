using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/turnos")]
    [Produces("application/json")]
    [Authorize]
    public class TurnosController : ControllerBase
    {
        private readonly ITurnoService _turnoService;

        public TurnosController(ITurnoService turnoService)
        {
            _turnoService = turnoService;
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int userId) ? userId : 1;
        }

        private string? GetCurrentUserRole()
        {
            return User?.FindFirst(ClaimTypes.Role)?.Value;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TurnoResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] PaginationFilterDto filter,
            [FromQuery] DateTime? fecha = null,
            [FromQuery] int? peluqueroId = null,
            [FromQuery] int? estadoId = null,
            [FromQuery] int? clienteId = null,
            CancellationToken ct = default)
        {
            var res = await _turnoService.GetAllAsync(filter, fecha, peluqueroId, estadoId, clienteId, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<TurnoResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<TurnoResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _turnoService.GetByIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpGet("cliente/{clienteId:int}")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TurnoResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPorCliente(int clienteId, CancellationToken ct = default)
        {
            var res = await _turnoService.ObtenerTurnosPorClienteAsync(clienteId, ct);
            return Ok(res);
        }

        [HttpGet("disponibilidad")]
        [ProducesResponseType(typeof(ApiResponse<DisponibilidadHorarioResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<DisponibilidadHorarioResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDisponibilidad(
            [FromQuery] int peluqueroId,
            [FromQuery] DateTime fecha,
            [FromQuery] List<int>? serviciosIds = null,
            CancellationToken ct = default)
        {
            var res = await _turnoService.ObtenerHorariosDisponiblesAsync(peluqueroId, fecha, serviciosIds, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TurnoResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<TurnoResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CrearTurnoDto dto, CancellationToken ct = default)
        {
            var res = await _turnoService.CreateAsync(dto, GetCurrentUserId(), ct);
            if (!res.Success)
                return BadRequest(res);

            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPut("{id:int}/reprogramar")]
        [ProducesResponseType(typeof(ApiResponse<TurnoResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<TurnoResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Reprogramar(
            int id,
            [FromBody] ReprogramarTurnoDto dto,
            CancellationToken ct = default)
        {
            var res = await _turnoService.ReprogramarAsync(id, dto, GetCurrentUserId(), GetCurrentUserRole(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPatch("{id:int}/estado")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CambiarEstado(
            int id,
            [FromBody] CambiarEstadoTurnoDto dto,
            CancellationToken ct = default)
        {
            var res = await _turnoService.CambiarEstadoAsync(id, dto, GetCurrentUserId(), GetCurrentUserRole(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpGet("{id:int}/historial")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<HistorialEstadoTurnoResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistorial(int id, CancellationToken ct = default)
        {
            var res = await _turnoService.ObtenerHistorialEstadoTurnoAsync(id, ct);
            return Ok(res);
        }
    }
}
