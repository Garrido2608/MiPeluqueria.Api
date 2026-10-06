using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Services.Interfaces;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            var res = await _authService.LoginAsync(dto);
            if (!res.Success)
            {
                // Código HTTP 401
                return Unauthorized(res);
            }

            // Código HTTP 200 con el Token
            return Ok(res);
        }
    }
}