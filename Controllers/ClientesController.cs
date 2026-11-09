using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/clientes")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,Recepcionista,Cajero,Peluquero,SuperAdminSaaS")]
    public class ClientesController : ControllerBase
    {
        private readonly IClienteService _clienteService;

        public ClientesController(IClienteService clienteService)
        {
            _clienteService = clienteService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ClienteResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationFilterDto filter, CancellationToken ct = default)
        {
            var result = await _clienteService.ObtenerTodosAsync(filter, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _clienteService.ObtenerPorIdAsync(id);
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] ClienteRequestDto dto)
        {
            var result = await _clienteService.CrearClienteAsync(dto);
            if (!result.Success)
                return BadRequest(result);

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ClienteResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] ClienteRequestDto dto)
        {
            var result = await _clienteService.ActualizarClienteAsync(id, dto);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _clienteService.EliminarClienteAsync(id);
            if (!result.Success)
                return NotFound(result);

            return Ok(result);
        }
    }
}
