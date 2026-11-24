using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,SuperAdminSaaS")]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosController(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UsuarioResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            var res = await _usuarioService.ObtenerTodosAsync(ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _usuarioService.ObtenerPorIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearStaff([FromBody] CrearUsuarioStaffDto dto, CancellationToken ct = default)
        {
            var res = await _usuarioService.CrearUsuarioStaffAsync(dto, ct);
            if (!res.Success)
                return BadRequest(res);

            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPut("{id:int}/estado")]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] ActualizarEstadoUsuarioDto dto, CancellationToken ct = default)
        {
            var res = await _usuarioService.ActualizarEstadoAsync(id, dto, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPatch("{id:int}/rol")]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CambiarRol(int id, [FromBody] CambiarRolUsuarioDto dto, CancellationToken ct = default)
        {
            var res = await _usuarioService.CambiarRolAsync(id, dto.RolId, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("{id:int}/desbloquear")]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Desbloquear(int id, CancellationToken ct = default)
        {
            var res = await _usuarioService.DesbloquearUsuarioAsync(id, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpGet("historial-sesiones")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<HistorialSesionResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistorialSesiones(
            [FromQuery] PaginationFilterDto filter,
            [FromQuery] int? usuarioId = null,
            CancellationToken ct = default)
        {
            var res = await _usuarioService.ObtenerHistorialSesionesAsync(filter, usuarioId, ct);
            return Ok(res);
        }
    }
}
