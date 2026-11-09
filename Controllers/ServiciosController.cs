using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/servicios")]
    [Produces("application/json")]
    [Authorize]
    public class ServiciosController : ControllerBase
    {
        private readonly IServicioService _servicioService;

        public ServiciosController(IServicioService servicioService)
        {
            _servicioService = servicioService;
        }

        private int GetCurrentUserId()
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out int userId) ? userId : 1;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ServicioDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool soloActivos = false, CancellationToken ct = default)
        {
            var res = await _servicioService.GetAllAsync(soloActivos, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ServicioDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ServicioDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _servicioService.GetByIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<ServicioDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ServicioDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CrearServicioDto dto, CancellationToken ct = default)
        {
            var res = await _servicioService.CreateAsync(dto, ct);
            if (!res.Success)
                return BadRequest(res);

            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<ServicioDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ServicioDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] ActualizarServicioDto dto, CancellationToken ct = default)
        {
            var res = await _servicioService.UpdateAsync(id, dto, GetCurrentUserId(), ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var res = await _servicioService.DeleteAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }
    }
}
