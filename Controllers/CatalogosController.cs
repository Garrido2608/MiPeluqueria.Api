using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/catalogos")]
    [Produces("application/json")]
    public class CatalogosController : ControllerBase
    {
        private readonly ICatalogoService _catalogoService;

        public CatalogosController(ICatalogoService catalogoService)
        {
            _catalogoService = catalogoService;
        }

        // =========================================================================
        // 1. ENDPOINTS CONSOLIDADOS Y GLOBALES (SOLO LECTURA)
        // =========================================================================

        [HttpGet("todos")]
        [HttpGet("maestros")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<CatalogosMaestrosResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCatalogosMaestros()
        {
            var res = await _catalogoService.ObtenerTodosLosCatalogosAsync();
            return Ok(res);
        }

        [HttpGet("estados-turno")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogoItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEstadosTurno()
        {
            var res = await _catalogoService.ObtenerEstadosTurnoAsync();
            return Ok(res);
        }

        [HttpGet("roles")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogoItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRoles()
        {
            var res = await _catalogoService.ObtenerRolesAsync();
            return Ok(res);
        }

        // =========================================================================
        // 2. TIPOS DE CABELLO (CRUD)
        // =========================================================================

        [HttpGet("tipos-cabello")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogoItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTiposCabello()
        {
            var res = await _catalogoService.ObtenerTiposCabelloAsync();
            return Ok(res);
        }

        [HttpPost("tipos-cabello")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearTipoCabello([FromBody] CrearCatalogoItemDto dto)
        {
            var res = await _catalogoService.CrearTipoCabelloAsync(dto);
            return res.Success ? StatusCode(StatusCodes.Status201Created, res) : BadRequest(res);
        }

        [HttpPut("tipos-cabello/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarTipoCabello(int id, [FromBody] ActualizarCatalogoItemDto dto)
        {
            var res = await _catalogoService.ActualizarTipoCabelloAsync(id, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("tipos-cabello/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarTipoCabello(int id)
        {
            var res = await _catalogoService.EliminarTipoCabelloAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        // =========================================================================
        // 3. TIPOS DE ROSTRO (CRUD)
        // =========================================================================

        [HttpGet("tipos-rostro")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogoItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTiposRostro()
        {
            var res = await _catalogoService.ObtenerTiposRostroAsync();
            return Ok(res);
        }

        [HttpPost("tipos-rostro")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearTipoRostro([FromBody] CrearCatalogoItemDto dto)
        {
            var res = await _catalogoService.CrearTipoRostroAsync(dto);
            return res.Success ? StatusCode(StatusCodes.Status201Created, res) : BadRequest(res);
        }

        [HttpPut("tipos-rostro/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarTipoRostro(int id, [FromBody] ActualizarCatalogoItemDto dto)
        {
            var res = await _catalogoService.ActualizarTipoRostroAsync(id, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("tipos-rostro/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarTipoRostro(int id)
        {
            var res = await _catalogoService.EliminarTipoRostroAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        // =========================================================================
        // 4. CATEGORÍAS DE SERVICIO (CRUD)
        // =========================================================================

        [HttpGet("categorias-servicio")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogoItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategoriasServicio()
        {
            var res = await _catalogoService.ObtenerCategoriasServicioAsync();
            return Ok(res);
        }

        [HttpPost("categorias-servicio")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearCategoriaServicio([FromBody] CrearCatalogoItemDto dto)
        {
            var res = await _catalogoService.CrearCategoriaServicioAsync(dto);
            return res.Success ? StatusCode(StatusCodes.Status201Created, res) : BadRequest(res);
        }

        [HttpPut("categorias-servicio/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarCategoriaServicio(int id, [FromBody] ActualizarCatalogoItemDto dto)
        {
            var res = await _catalogoService.ActualizarCategoriaServicioAsync(id, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("categorias-servicio/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarCategoriaServicio(int id)
        {
            var res = await _catalogoService.EliminarCategoriaServicioAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        // =========================================================================
        // 5. ESPECIALIDADES (CRUD)
        // =========================================================================

        [HttpGet("especialidades")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogoItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEspecialidades()
        {
            var res = await _catalogoService.ObtenerEspecialidadesAsync();
            return Ok(res);
        }

        [HttpPost("especialidades")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearEspecialidad([FromBody] CrearCatalogoItemDto dto)
        {
            var res = await _catalogoService.CrearEspecialidadAsync(dto);
            return res.Success ? StatusCode(StatusCodes.Status201Created, res) : BadRequest(res);
        }

        [HttpPut("especialidades/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarEspecialidad(int id, [FromBody] ActualizarCatalogoItemDto dto)
        {
            var res = await _catalogoService.ActualizarEspecialidadAsync(id, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("especialidades/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarEspecialidad(int id)
        {
            var res = await _catalogoService.EliminarEspecialidadAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        // =========================================================================
        // 6. MOTIVOS DE CANCELACIÓN (CRUD)
        // =========================================================================

        [HttpGet("motivos-cancelacion")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogoItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMotivosCancelacion()
        {
            var res = await _catalogoService.ObtenerMotivosCancelacionAsync();
            return Ok(res);
        }

        [HttpPost("motivos-cancelacion")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearMotivoCancelacion([FromBody] CrearCatalogoItemDto dto)
        {
            var res = await _catalogoService.CrearMotivoCancelacionAsync(dto);
            return res.Success ? StatusCode(StatusCodes.Status201Created, res) : BadRequest(res);
        }

        [HttpPut("motivos-cancelacion/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarMotivoCancelacion(int id, [FromBody] ActualizarCatalogoItemDto dto)
        {
            var res = await _catalogoService.ActualizarMotivoCancelacionAsync(id, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("motivos-cancelacion/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarMotivoCancelacion(int id)
        {
            var res = await _catalogoService.EliminarMotivoCancelacionAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        // =========================================================================
        // 7. CATEGORÍAS DE PRODUCTO (CRUD)
        // =========================================================================

        [HttpGet("categorias-producto")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogoItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategoriasProducto()
        {
            var res = await _catalogoService.ObtenerCategoriasProductoAsync();
            return Ok(res);
        }

        [HttpPost("categorias-producto")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearCategoriaProducto([FromBody] CrearCatalogoItemDto dto)
        {
            var res = await _catalogoService.CrearCategoriaProductoAsync(dto);
            return res.Success ? StatusCode(StatusCodes.Status201Created, res) : BadRequest(res);
        }

        [HttpPut("categorias-producto/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarCategoriaProducto(int id, [FromBody] ActualizarCatalogoItemDto dto)
        {
            var res = await _catalogoService.ActualizarCategoriaProductoAsync(id, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("categorias-producto/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarCategoriaProducto(int id)
        {
            var res = await _catalogoService.EliminarCategoriaProductoAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        // =========================================================================
        // 8. MEDIOS DE PAGO (CRUD)
        // =========================================================================

        [HttpGet("medios-pago")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<CatalogoItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMediosPago()
        {
            var res = await _catalogoService.ObtenerMediosPagoAsync();
            return Ok(res);
        }

        [HttpPost("medios-pago")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CrearMedioPago([FromBody] CrearCatalogoItemDto dto)
        {
            var res = await _catalogoService.CrearMedioPagoAsync(dto);
            return res.Success ? StatusCode(StatusCodes.Status201Created, res) : BadRequest(res);
        }

        [HttpPut("medios-pago/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CatalogoItemDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActualizarMedioPago(int id, [FromBody] ActualizarCatalogoItemDto dto)
        {
            var res = await _catalogoService.ActualizarMedioPagoAsync(id, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("medios-pago/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> EliminarMedioPago(int id)
        {
            var res = await _catalogoService.EliminarMedioPagoAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }
    }
}
