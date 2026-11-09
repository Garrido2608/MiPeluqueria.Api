using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.SaaS;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/sucursales")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,SuperAdminSaaS")]
    public class SucursalesController : ControllerBase
    {
        private readonly ISaaSService _saasService;

        public SucursalesController(ISaaSService saasService)
        {
            _saasService = saasService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SucursalResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerMisSucursales(CancellationToken ct = default)
        {
            var res = await _saasService.ObtenerSucursalesAsync(ct);
            return Ok(res);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<SucursalResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<SucursalResponseDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearSucursal([FromBody] CrearSucursalDto dto, CancellationToken ct = default)
        {
            var res = await _saasService.CrearSucursalAsync(dto, ct);
            if (!res.Success)
                return BadRequest(res);

            return StatusCode(StatusCodes.Status201Created, res);
        }
    }
}
