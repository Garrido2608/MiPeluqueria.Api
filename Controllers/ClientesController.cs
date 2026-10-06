using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.Services.Implementations;
using MiPeluqueria.Api.Services.Interfaces;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClientesController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _clienteService.ObtenerTodosAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _clienteService.ObtenerPorIdAsync(id);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClienteRequestDto dto)
        {
            var res = await _clienteService.CrearClienteAsync(dto);
            if (!res.Success) return BadRequest(res);

            // Devuelve un 201 Created y la ruta para consultar al cliente nuevo
            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClienteRequestDto dto)
        {
            var res = await _clienteService.ActualizarClienteAsync(id, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _clienteService.EliminarClienteAsync(id);
            if (!res.Success) return NotFound(res);
            return Ok(res);
        }
    }
}