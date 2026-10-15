using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Services.Implementations;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CajaController : ControllerBase
    {
        private readonly ICajaVentaService _cajaService;

        public CajaController(ICajaVentaService cajaService)
        {
            _cajaService = cajaService;
        }

        [HttpPost("abrir")]
        public async Task<IActionResult> Abrir([FromBody] AperturaCajaDto dto)
        {
            var res = await _cajaService.AbrirCajaAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("cerrar")]
        public async Task<IActionResult> Cerrar([FromBody] CierreCajaDto dto)
        {
            var res = await _cajaService.CerrarCajaAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}