using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Services.Implementations;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VentasController : ControllerBase
    {
        private readonly ICajaVentaService _cajaService;

        public VentasController(ICajaVentaService cajaService)
        {
            _cajaService = cajaService;
        }

        [HttpPost]
        public async Task<IActionResult> Registrar([FromBody] CrearVentaDto dto)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            int usuarioId = int.TryParse(claim, out int id) ? id : 1;

            var res = await _cajaService.RegistrarVentaAsync(dto, usuarioId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}