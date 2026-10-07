using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.Services.Implementations;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FichasTecnicasController : ControllerBase
    {
        private readonly IFichaTecnicaService _fichaService;

        public FichasTecnicasController(IFichaTecnicaService fichaService)
        {
            _fichaService = fichaService;
        }

        [HttpGet("cliente/{clienteId}")]
        public async Task<IActionResult> GetPorCliente(int clienteId)
        {
            var res = await _fichaService.ObtenerPorClienteAsync(clienteId);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearFichaTecnicaDto dto)
        {
            var res = await _fichaService.CrearFichaAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}