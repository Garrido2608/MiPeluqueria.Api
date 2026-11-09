using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/comisiones")]
    [Produces("application/json")]
    [Authorize]
    public class ComisionesController : ControllerBase
    {
        private readonly IComisionConfigService _comisionService;

        public ComisionesController(IComisionConfigService comisionService)
        {
            _comisionService = comisionService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ComisionConfigDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] int? peluqueroId = null, CancellationToken ct = default)
        {
            var res = peluqueroId.HasValue
                ? await _comisionService.ObtenerPorPeluqueroAsync(peluqueroId.Value, ct)
                : await _comisionService.ObtenerTodasAsync(ct);
            return Ok(res);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<ComisionConfigDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ComisionConfigDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Crear([FromBody] GuardarComisionConfigDto dto, CancellationToken ct = default)
        {
            var res = await _comisionService.GuardarConfiguracionAsync(dto, ct);
            if (!res.Success)
                return BadRequest(res);

            return StatusCode(StatusCodes.Status201Created, res);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<ComisionConfigDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ComisionConfigDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarComisionConfigDto dto, CancellationToken ct = default)
        {
            var res = await _comisionService.ActualizarConfiguracionAsync(id, dto, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Eliminar(int id, CancellationToken ct = default)
        {
            var res = await _comisionService.EliminarConfiguracionAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }
    }
}
