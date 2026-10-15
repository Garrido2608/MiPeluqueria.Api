using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CatalogosController : ControllerBase
    {
        private readonly ICatalogoService _catalogoService;

        public CatalogosController(ICatalogoService catalogoService)
        {
            _catalogoService = catalogoService;
        }

        [HttpGet("todos")]
        public async Task<IActionResult> GetAllCatalogos()
        {
            var result = await _catalogoService.ObtenerTodosLosCatalogosAsync();
            return Ok(result);
        }

        [HttpGet("tipos-cabello")]
        public async Task<IActionResult> GetTiposCabello()
        {
            return Ok(await _catalogoService.ObtenerTiposCabelloAsync());
        }

        [HttpGet("tipos-rostro")]
        public async Task<IActionResult> GetTiposRostro()
        {
            return Ok(await _catalogoService.ObtenerTiposRostroAsync());
        }

        [HttpGet("categorias-servicio")]
        public async Task<IActionResult> GetCategoriasServicio()
        {
            return Ok(await _catalogoService.ObtenerCategoriasServicioAsync());
        }

        [HttpPost("categorias-servicio")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateCategoriaServicio([FromBody] CrearCatalogoItemDto dto)
        {
            var result = await _catalogoService.CrearCategoriaServicioAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("especialidades")]
        public async Task<IActionResult> GetEspecialidades()
        {
            return Ok(await _catalogoService.ObtenerEspecialidadesAsync());
        }

        [HttpPost("especialidades")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateEspecialidad([FromBody] CrearCatalogoItemDto dto)
        {
            var result = await _catalogoService.CrearEspecialidadAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("especialidades/{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteEspecialidad(int id)
        {
            var result = await _catalogoService.EliminarEspecialidadAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpGet("estados-turno")]
        public async Task<IActionResult> GetEstadosTurno()
        {
            return Ok(await _catalogoService.ObtenerEstadosTurnoAsync());
        }

        [HttpGet("motivos-cancelacion")]
        public async Task<IActionResult> GetMotivosCancelacion()
        {
            return Ok(await _catalogoService.ObtenerMotivosCancelacionAsync());
        }

        [HttpPost("motivos-cancelacion")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateMotivoCancelacion([FromBody] CrearCatalogoItemDto dto)
        {
            var result = await _catalogoService.CrearMotivoCancelacionAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("categorias-producto")]
        public async Task<IActionResult> GetCategoriasProducto()
        {
            return Ok(await _catalogoService.ObtenerCategoriasProductoAsync());
        }

        [HttpPost("categorias-producto")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateCategoriaProducto([FromBody] CrearCatalogoItemDto dto)
        {
            var result = await _catalogoService.CrearCategoriaProductoAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("medios-pago")]
        public async Task<IActionResult> GetMediosPago()
        {
            return Ok(await _catalogoService.ObtenerMediosPagoAsync());
        }

        [HttpPost("medios-pago")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> CreateMedioPago([FromBody] CrearCatalogoItemDto dto)
        {
            var result = await _catalogoService.CrearMedioPagoAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("roles")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> GetRoles()
        {
            return Ok(await _catalogoService.ObtenerRolesAsync());
        }
    }
}