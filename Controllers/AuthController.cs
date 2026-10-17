using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Services.Interfaces;
using Microsoft.AspNetCore.RateLimiting;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("StrictAuthOtpPolicy")] // <-- Blinda login, registro y OTPs a 5 req/min por IP
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var userAgent = Request.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(userAgent)) userAgent = "WebClient";

            var res = await _authService.LoginAsync(dto, ip, userAgent);
            if (!res.Success) return Unauthorized(res);
            return Ok(res);
        }

        [HttpPost("registro")]
        public async Task<IActionResult> Registro([FromBody] RegistroUsuarioDto dto)
        {
            var res = await _authService.RegistrarUsuarioAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("verificar-email")]
        public async Task<IActionResult> VerificarEmail([FromBody] VerificarEmailDto dto)
        {
            var res = await _authService.VerificarEmailAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("cambiar-password")]
        [Authorize]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int usuarioId = int.TryParse(claim, out int id) ? id : 1;

            var res = await _authService.CambiarPasswordAsync(usuarioId, dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("olvide-password")]
        public async Task<IActionResult> OlvidePassword([FromBody] OlvidePasswordDto dto)
        {
            var res = await _authService.SolicitarRecuperacionPasswordAsync(dto);
            return Ok(res);
        }

        [HttpPost("restablecer-password")]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDto dto)
        {
            var res = await _authService.RestablecerPasswordConOtpAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}