// filepath: Controllers/CatalogosController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/catalogos")]
    [ApiController]
    public class CatalogosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CatalogosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 0. CONSOLIDADO DE LAS 9 TABLAS MAESTRAS (/api/catalogos/todos y /api/catalogos/maestros)
        [HttpGet("todos")]
        [HttpGet("maestros")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerTodos()
        {
            var dto = new CatalogosMaestrosResponseDto
            {
                TiposCabello = await _context.TiposCabello
                    .AsNoTracking()
                    .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                    .ToListAsync(),

                TiposRostro = await _context.TiposRostro
                    .AsNoTracking()
                    .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                    .ToListAsync(),

                CategoriasServicio = await _context.CategoriasServicio
                    .AsNoTracking()
                    .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                    .ToListAsync(),

                Especialidades = await _context.Especialidades
                    .AsNoTracking()
                    .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                    .ToListAsync(),

                EstadosTurno = await _context.EstadosTurno
                    .AsNoTracking()
                    .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                    .ToListAsync(),

                MotivosCancelacion = await _context.MotivosCancelacion
                    .AsNoTracking()
                    .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Descripcion, Descripcion = x.Descripcion })
                    .ToListAsync(),

                CategoriasProducto = await _context.CategoriasProducto
                    .AsNoTracking()
                    .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                    .ToListAsync(),

                MediosPago = await _context.MediosPago
                    .AsNoTracking()
                    .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                    .ToListAsync(),

                Roles = await _context.Roles
                    .AsNoTracking()
                    .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                    .ToListAsync()
            };

            return Ok(ApiResponse<CatalogosMaestrosResponseDto>.Exito(dto, "Catálogos maestros obtenidos correctamente."));
        }

        // 1. TIPOS DE CABELLO
        [HttpGet("tipos-cabello")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerTiposCabello()
        {
            var items = await _context.TiposCabello
                .AsNoTracking()
                .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                .ToListAsync();
            return Ok(ApiResponse<List<CatalogoItemDto>>.Exito(items));
        }

        // 2. TIPOS DE ROSTRO
        [HttpGet("tipos-rostro")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerTiposRostro()
        {
            var items = await _context.TiposRostro
                .AsNoTracking()
                .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                .ToListAsync();
            return Ok(ApiResponse<List<CatalogoItemDto>>.Exito(items));
        }

        // 3. CATEGORÍAS DE SERVICIO (GET + POST)
        [HttpGet("categorias-servicio")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerCategoriasServicio()
        {
            var items = await _context.CategoriasServicio
                .AsNoTracking()
                .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                .ToListAsync();
            return Ok(ApiResponse<List<CatalogoItemDto>>.Exito(items));
        }

        [HttpPost("categorias-servicio")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        public async Task<IActionResult> CrearCategoriaServicio([FromBody] CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest(ApiResponse<CatalogoItemDto>.Falla("El nombre de la categoría es obligatorio."));

            var nueva = new CategoriaServicio { Nombre = dto.Nombre.Trim() };
            _context.CategoriasServicio.Add(nueva);
            await _context.SaveChangesAsync();

            var result = new CatalogoItemDto { Id = nueva.Id, Nombre = nueva.Nombre, Descripcion = nueva.Nombre };
            return Ok(ApiResponse<CatalogoItemDto>.Exito(result, "Categoría de servicio creada."));
        }

        // 4. ESPECIALIDADES DE PELUQUERO (GET + POST + DELETE)
        [HttpGet("especialidades")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerEspecialidades()
        {
            var items = await _context.Especialidades
                .AsNoTracking()
                .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                .ToListAsync();
            return Ok(ApiResponse<List<CatalogoItemDto>>.Exito(items));
        }

        [HttpPost("especialidades")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        public async Task<IActionResult> CrearEspecialidad([FromBody] CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest(ApiResponse<CatalogoItemDto>.Falla("El nombre de la especialidad es obligatorio."));

            var nueva = new Especialidad { Nombre = dto.Nombre.Trim() };
            _context.Especialidades.Add(nueva);
            await _context.SaveChangesAsync();

            var result = new CatalogoItemDto { Id = nueva.Id, Nombre = nueva.Nombre, Descripcion = nueva.Nombre };
            return Ok(ApiResponse<CatalogoItemDto>.Exito(result, "Especialidad creada."));
        }

        [HttpDelete("especialidades/{id:int}")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        public async Task<IActionResult> EliminarEspecialidad(int id)
        {
            var esp = await _context.Especialidades.FindAsync(id);
            if (esp == null)
                return NotFound(ApiResponse<bool>.Falla("Especialidad no encontrada."));

            _context.Especialidades.Remove(esp);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<bool>.Exito(true, "Especialidad eliminada correctamente."));
        }

        // 5. ESTADOS DE TURNO
        [HttpGet("estados-turno")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerEstadosTurno()
        {
            var items = await _context.EstadosTurno
                .AsNoTracking()
                .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                .ToListAsync();
            return Ok(ApiResponse<List<CatalogoItemDto>>.Exito(items));
        }

        // 6. MOTIVOS DE CANCELACIÓN (GET + POST)
        [HttpGet("motivos-cancelacion")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerMotivosCancelacion()
        {
            var items = await _context.MotivosCancelacion
                .AsNoTracking()
                .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Descripcion, Descripcion = x.Descripcion })
                .ToListAsync();
            return Ok(ApiResponse<List<CatalogoItemDto>>.Exito(items));
        }

        [HttpPost("motivos-cancelacion")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        public async Task<IActionResult> CrearMotivoCancelacion([FromBody] CrearCatalogoItemDto dto)
        {
            var texto = !string.IsNullOrWhiteSpace(dto.Descripcion) ? dto.Descripcion.Trim() : dto.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(texto))
                return BadRequest(ApiResponse<CatalogoItemDto>.Falla("La descripción del motivo es obligatoria."));

            var nuevo = new MotivoCancelacion { Descripcion = texto };
            _context.MotivosCancelacion.Add(nuevo);
            await _context.SaveChangesAsync();

            var result = new CatalogoItemDto { Id = nuevo.Id, Nombre = nuevo.Descripcion, Descripcion = nuevo.Descripcion };
            return Ok(ApiResponse<CatalogoItemDto>.Exito(result, "Motivo de cancelación creado."));
        }

        // 7. CATEGORÍAS DE PRODUCTO (GET + POST)
        [HttpGet("categorias-producto")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerCategoriasProducto()
        {
            var items = await _context.CategoriasProducto
                .AsNoTracking()
                .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                .ToListAsync();
            return Ok(ApiResponse<List<CatalogoItemDto>>.Exito(items));
        }

        [HttpPost("categorias-producto")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        public async Task<IActionResult> CrearCategoriaProducto([FromBody] CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest(ApiResponse<CatalogoItemDto>.Falla("El nombre de la categoría de producto es obligatorio."));

            var nueva = new CategoriaProducto { Nombre = dto.Nombre.Trim() };
            _context.CategoriasProducto.Add(nueva);
            await _context.SaveChangesAsync();

            var result = new CatalogoItemDto { Id = nueva.Id, Nombre = nueva.Nombre, Descripcion = nueva.Nombre };
            return Ok(ApiResponse<CatalogoItemDto>.Exito(result, "Categoría de producto creada."));
        }

        // 8. MEDIOS DE PAGO (GET + POST)
        [HttpGet("medios-pago")]
        [AllowAnonymous]
        public async Task<IActionResult> ObtenerMediosPago()
        {
            var items = await _context.MediosPago
                .AsNoTracking()
                .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                .ToListAsync();
            return Ok(ApiResponse<List<CatalogoItemDto>>.Exito(items));
        }

        [HttpPost("medios-pago")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        public async Task<IActionResult> CrearMedioPago([FromBody] CrearCatalogoItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return BadRequest(ApiResponse<CatalogoItemDto>.Falla("El nombre del medio de pago es obligatorio."));

            var nuevo = new MedioPago { Nombre = dto.Nombre.Trim() };
            _context.MediosPago.Add(nuevo);
            await _context.SaveChangesAsync();

            var result = new CatalogoItemDto { Id = nuevo.Id, Nombre = nuevo.Nombre, Descripcion = nuevo.Nombre };
            return Ok(ApiResponse<CatalogoItemDto>.Exito(result, "Medio de pago creado."));
        }

        // 9. ROLES DEL SISTEMA
        [HttpGet("roles")]
        [Authorize(Roles = "Administrador,SuperAdminSaaS")]
        public async Task<IActionResult> ObtenerRoles()
        {
            var items = await _context.Roles
                .AsNoTracking()
                .Select(x => new CatalogoItemDto { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Nombre })
                .ToListAsync();
            return Ok(ApiResponse<List<CatalogoItemDto>>.Exito(items));
        }
    }
}