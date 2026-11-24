using System.Collections.Generic;
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
    [Route("api/bloqueos")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,Recepcionista,Cajero,Peluquero,SuperAdminSaaS")]
    public class BloqueosController : ControllerBase
    {
        private readonly IBloqueoHorarioService _bloqueoService;

        public BloqueosController(IBloqueoHorarioService bloqueoService)
        {
            _bloqueoService = bloqueoService;
        }

        [HttpGet("peluquero/{peluqueroId:int}")]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BloqueoHorarioResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPorPeluquero(int peluqueroId, CancellationToken ct = default)
        {
            var res = await _bloqueoService.ObtenerPorPeluqueroAsync(peluqueroId, ct);
            return Ok(res);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<BloqueoHorarioResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<BloqueoHorarioResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Crear([FromBody] BloqueoHorarioDto dto, CancellationToken ct = default)
        {
            var res = await _bloqueoService.CrearBloqueoAsync(dto, ct);
            if (!res.Success)
                return BadRequest(res);

            return StatusCode(StatusCodes.Status201Created, res);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Eliminar(int id, CancellationToken ct = default)
        {
            var res = await _bloqueoService.EliminarBloqueoAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }
    }
}
