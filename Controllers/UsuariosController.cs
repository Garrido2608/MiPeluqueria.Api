using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UsuariosController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var res = await _authService.ObtenerUsuariosAsync();
            return Ok(res);
        }

        [HttpPut("{id}/estado")]
        public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoUsuarioDto dto)
        {
            var res = await _authService.ActualizarEstadoUsuarioAsync(id, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("{id}/desbloquear")]
        public async Task<IActionResult> Desbloquear(int id)
        {
            var res = await _authService.ActualizarEstadoUsuarioAsync(id, new ActualizarEstadoUsuarioDto
            {
                DesbloquearCuenta = true,
                Activo = true
            });
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpGet("historial-sesiones")]
        public async Task<IActionResult> GetHistorialSesiones([FromQuery] int? usuarioId = null, [FromQuery] int top = 100)
        {
            var res = await _authService.ObtenerHistorialSesionesAsync(usuarioId, top);
            return Ok(res);
        }
    }
}