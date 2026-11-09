using System.Collections.Generic;
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
    [Route("api/fichas-tecnicas")]
    [Route("api/fichastecnicas")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,Recepcionista,Cajero,Peluquero,SuperAdminSaaS")]
    public class FichasTecnicasController : ControllerBase
    {
        private readonly IFichaTecnicaService _fichaService;

        public FichasTecnicasController(IFichaTecnicaService fichaService)
        {
            _fichaService = fichaService;
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<FichaTecnicaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<FichaTecnicaResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _fichaService.ObtenerPorIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpGet("cliente/{clienteId:int}")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<FichaTecnicaResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPorCliente(int clienteId)
        {
            var res = await _fichaService.ObtenerHistorialClienteAsync(clienteId);
            return Ok(res);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<FichaTecnicaResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<FichaTecnicaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Crear([FromBody] CrearFichaTecnicaDto dto)
        {
            var res = await _fichaService.CrearFichaAsync(dto);
            if (!res.Success)
                return BadRequest(res);

            return StatusCode(StatusCodes.Status201Created, res);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<FichaTecnicaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<FichaTecnicaResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Actualizar(int id, [FromBody] ActualizarFichaTecnicaDto dto, CancellationToken ct = default)
        {
            var res = await _fichaService.ActualizarFichaAsync(id, dto, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Eliminar(int id)
        {
            var res = await _fichaService.EliminarFichaAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }
    }
}
