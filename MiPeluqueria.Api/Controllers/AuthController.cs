using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    [EnableRateLimiting("StrictAuthOtpPolicy")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            var res = await _authService.LoginAsync(dto, ip, userAgent);
            if (!res.Success)
                return Unauthorized(res);

            return Ok(res);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto, CancellationToken ct = default)
        {
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var res = await _authService.RefreshTokenAsync(dto, ip, ct);
            if (!res.Success)
                return Unauthorized(res);

            return Ok(res);
        }

        [HttpPost("registro")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<UsuarioResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Registrar([FromBody] RegistroUsuarioDto dto)
        {
            var res = await _authService.RegistrarUsuarioAsync(dto);
            if (!res.Success)
                return BadRequest(res);

            return StatusCode(StatusCodes.Status201Created, res);
        }

        [HttpPost("verificar-email")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerificarEmail([FromBody] VerificarEmailDto dto)
        {
            var res = await _authService.VerificarEmailAsync(dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("olvide-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> OlvidePassword([FromBody] OlvidePasswordDto dto)
        {
            var res = await _authService.SolicitarRecuperacionPasswordAsync(dto);
            return Ok(res);
        }

        [HttpPost("restablecer-password")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RestablecerPassword([FromBody] RestablecerPasswordDto dto)
        {
            var res = await _authService.RestablecerPasswordConOtpAsync(dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPost("cambiar-password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int usuarioId))
                return Unauthorized(ApiResponse<bool>.Fail("Token de sesión inválido."));

            var res = await _authService.CambiarPasswordAsync(usuarioId, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}
