using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/configuracion")]
    [Produces("application/json")]
    public class ConfiguracionSalonController : ControllerBase
    {
        private readonly IConfiguracionSalonService _configuracionService;

        public ConfiguracionSalonController(IConfiguracionSalonService configuracionService)
        {
            _configuracionService = configuracionService;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<ConfiguracionSalonDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConfiguracion(CancellationToken ct = default)
        {
            var config = await _configuracionService.ObtenerConfiguracionAsync(ct);
            return Ok(ApiResponse<ConfiguracionSalonDto>.Ok(config, "Configuración del salón obtenida correctamente."));
        }

        [HttpPut]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<ConfiguracionSalonDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ConfiguracionSalonDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarConfiguracion(
            [FromBody] ActualizarConfiguracionSalonDto dto,
            CancellationToken ct = default)
        {
            var actualizada = await _configuracionService.ActualizarConfiguracionAsync(dto, ct);
            return Ok(ApiResponse<ConfiguracionSalonDto>.Ok(actualizada, "Configuración del salón actualizada exitosamente."));
        }
    }
}
