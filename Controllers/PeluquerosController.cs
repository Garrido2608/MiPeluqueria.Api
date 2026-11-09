using System.Collections.Generic;
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
    [Route("api/peluqueros")]
    [Produces("application/json")]
    [Authorize]
    public class PeluquerosController : ControllerBase
    {
        private readonly IPeluqueroService _peluqueroService;

        public PeluquerosController(IPeluqueroService peluqueroService)
        {
            _peluqueroService = peluqueroService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PeluqueroDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] bool soloActivos = false, CancellationToken ct = default)
        {
            var res = await _peluqueroService.GetAllAsync(soloActivos, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<PeluqueroDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PeluqueroDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _peluqueroService.GetByIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<PeluqueroDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<PeluqueroDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CrearPeluqueroDto dto, CancellationToken ct = default)
        {
            var res = await _peluqueroService.CreateAsync(dto, ct);
            if (!res.Success)
                return BadRequest(res);

            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<PeluqueroDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PeluqueroDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] ActualizarPeluqueroDto dto, CancellationToken ct = default)
        {
            var res = await _peluqueroService.UpdateAsync(id, dto, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var res = await _peluqueroService.DeleteAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpGet("{id:int}/horarios")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<HorarioLaboralDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHorarios(int id, CancellationToken ct = default)
        {
            var res = await _peluqueroService.ObtenerHorariosAsync(id, ct);
            return Ok(res);
        }

        [HttpPut("{id:int}/horarios")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ConfigurarHorarios(int id, [FromBody] GuardarHorariosPeluqueroDto dto, CancellationToken ct = default)
        {
            var res = await _peluqueroService.ConfigurarHorariosAsync(id, dto, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}
