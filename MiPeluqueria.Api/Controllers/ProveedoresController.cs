using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/proveedores")]
    [Produces("application/json")]
    [Authorize(Roles = "Administrador,Recepcionista,Cajero,SuperAdminSaaS")]
    public class ProveedoresController : ControllerBase
    {
        private readonly IProveedorService _proveedorService;

        public ProveedoresController(IProveedorService proveedorService)
        {
            _proveedorService = proveedorService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ProveedorDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] PaginationFilterDto filter, CancellationToken ct = default)
        {
            var res = await _proveedorService.GetPagedAsync(filter, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ProveedorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProveedorDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
        {
            var res = await _proveedorService.GetByIdAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ProveedorDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<ProveedorDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CrearProveedorDto dto, CancellationToken ct = default)
        {
            var res = await _proveedorService.CreateAsync(dto, ct);
            if (!res.Success)
                return BadRequest(res);

            return CreatedAtAction(nameof(GetById), new { id = res.Data!.Id }, res);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<ProveedorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<ProveedorDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromBody] ActualizarProveedorDto dto, CancellationToken ct = default)
        {
            var res = await _proveedorService.UpdateAsync(id, dto, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            var res = await _proveedorService.DeleteAsync(id, ct);
            return res.Success ? Ok(res) : NotFound(res);
        }
    }
}
