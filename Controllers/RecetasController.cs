using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/recetas-servicio")]
    [ApiController]
    [Authorize]
    public class RecetasServicioController : ControllerBase
    {
        private readonly IRecetaServicioService _recetaService;

        public RecetasServicioController(IRecetaServicioService recetaService)
        {
            _recetaService = recetaService;
        }

        [HttpGet("servicio/{servicioId}")]
        public async Task<IActionResult> GetPorServicio(int servicioId, CancellationToken ct = default)
        {
            var res = await _recetaService.ObtenerRecetaPorServicioAsync(servicioId, ct);
            return Ok(res);
        }

        [HttpPut]
        public async Task<IActionResult> GuardarReceta([FromBody] ConfigurarRecetaServicioDto dto, CancellationToken ct = default)
        {
            var res = await _recetaService.GuardarRecetaServicioAsync(dto, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("turno/{turnoId}/consumir-gabinete")]
        public async Task<IActionResult> ConsumirPorTurno(int turnoId, CancellationToken ct = default)
        {
            var res = await _recetaService.AplicarConsumoGabinetePorTurnoAsync(turnoId, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int id) ? id : 1;
        }
    }
}