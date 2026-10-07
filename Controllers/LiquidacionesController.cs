using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.Services.Implementations;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LiquidacionesController : ControllerBase
    {
        private readonly ILiquidacionService _liquidacionService;

        public LiquidacionesController(ILiquidacionService liquidacionService)
        {
            _liquidacionService = liquidacionService;
        }

        [HttpGet("peluquero/{peluqueroId}")]
        public async Task<IActionResult> GetPorPeluquero(int peluqueroId)
        {
            var res = await _liquidacionService.ObtenerPorPeluqueroAsync(peluqueroId);
            return Ok(res);
        }

        [HttpPost("generar")]
        public async Task<IActionResult> Generar([FromBody] GenerarLiquidacionDto dto)
        {
            var res = await _liquidacionService.LiquidarPeriodoAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPatch("{id}/pagar")]
        public async Task<IActionResult> Pagar(int id)
        {
            var res = await _liquidacionService.RegistrarPagoLiquidacionAsync(id);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}