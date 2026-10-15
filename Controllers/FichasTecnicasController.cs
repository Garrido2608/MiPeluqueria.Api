using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.Services.Interfaces;

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
            var res = await _fichaService.ObtenerHistorialClienteAsync(clienteId);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearFichaTecnicaDto dto)
        {
            var res = await _fichaService.CrearFichaAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] CrearFichaTecnicaDto dto)
        {
            var res = await _fichaService.ActualizarFichaAsync(id, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var res = await _fichaService.EliminarFichaAsync(id);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }
    }
}