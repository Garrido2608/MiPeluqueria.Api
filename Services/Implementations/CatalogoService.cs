using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class CatalogoService : ICatalogoService
    {
        private readonly ApplicationDbContext _context;

        public CatalogoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<CatalogosMaestrosResponseDto>> ObtenerTodosLosCatalogosAsync()
        {
            var response = new CatalogosMaestrosResponseDto
            {
                TiposCabello = (await ObtenerTiposCabelloAsync()).Data ?? Enumerable.Empty<CatalogoItemDto>(),
                TiposRostro = (await ObtenerTiposRostroAsync()).Data ?? Enumerable.Empty<CatalogoItemDto>(),
                CategoriasServicio = (await ObtenerCategoriasServicioAsync()).Data ?? Enumerable.Empty<CatalogoItemDto>(),
                Especialidades = (await ObtenerEspecialidadesAsync()).Data ?? Enumerable.Empty<CatalogoItemDto>(),
                EstadosTurno = (await ObtenerEstadosTurnoAsync()).Data ?? Enumerable.Empty<CatalogoItemDto>(),
                MotivosCancelacion = (await ObtenerMotivosCancelacionAsync()).Data ?? Enumerable.Empty<CatalogoItemDto>(),
                CategoriasProducto = (await ObtenerCategoriasProductoAsync()).Data ?? Enumerable.Empty<CatalogoItemDto>(),
                MediosPago = (await ObtenerMediosPagoAsync()).Data ?? Enumerable.Empty<CatalogoItemDto>(),
                Roles = (await ObtenerRolesAsync()).Data ?? Enumerable.Empty<CatalogoItemDto>()
            };

            return ApiResponse<CatalogosMaestrosResponseDto>.Ok(response);
        }

        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerTiposCabelloAsync()
        {
            var lista = await _context.TiposCabello.AsNoTracking().ToListAsync();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Ok(lista.Select(MapToCatalogoDto));
        }

        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerTiposRostroAsync()
        {
            var lista = await _context.TiposRostro.AsNoTracking().ToListAsync();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Ok(lista.Select(MapToCatalogoDto));
        }

        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerCategoriasServicioAsync()
        {
            var lista = await _context.CategoriasServicio.AsNoTracking().ToListAsync();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Ok(lista.Select(MapToCatalogoDto));
        }

        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerEspecialidadesAsync()
        {
            var lista = await _context.Especialidades.AsNoTracking().ToListAsync();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Ok(lista.Select(MapToCatalogoDto));
        }

        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerEstadosTurnoAsync()
        {
            var lista = await _context.EstadosTurno.AsNoTracking().ToListAsync();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Ok(lista.Select(MapToCatalogoDto));
        }

        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerMotivosCancelacionAsync()
        {
            var lista = await _context.MotivosCancelacion.AsNoTracking().ToListAsync();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Ok(lista.Select(MapToCatalogoDto));
        }

        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerCategoriasProductoAsync()
        {
            var lista = await _context.CategoriasProducto.AsNoTracking().ToListAsync();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Ok(lista.Select(MapToCatalogoDto));
        }

        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerMediosPagoAsync()
        {
            var lista = await _context.MediosPago.AsNoTracking().ToListAsync();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Ok(lista.Select(MapToCatalogoDto));
        }

        public async Task<ApiResponse<IEnumerable<CatalogoItemDto>>> ObtenerRolesAsync()
        {
            var lista = await _context.Roles.AsNoTracking().ToListAsync();
            return ApiResponse<IEnumerable<CatalogoItemDto>>.Ok(lista.Select(MapToCatalogoDto));
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearCategoriaServicioAsync(CrearCatalogoItemDto dto)
        {
            var entidad = new CategoriaServicio();
            SetCatalogProperties(entidad, dto.Nombre, dto.Descripcion);

            _context.CategoriasServicio.Add(entidad);
            await _context.SaveChangesAsync();

            return ApiResponse<CatalogoItemDto>.Ok(MapToCatalogoDto(entidad), "Categoría de servicio creada exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearEspecialidadAsync(CrearCatalogoItemDto dto)
        {
            var entidad = new Especialidad();
            SetCatalogProperties(entidad, dto.Nombre, dto.Descripcion);

            _context.Especialidades.Add(entidad);
            await _context.SaveChangesAsync();

            return ApiResponse<CatalogoItemDto>.Ok(MapToCatalogoDto(entidad), "Especialidad creada exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearCategoriaProductoAsync(CrearCatalogoItemDto dto)
        {
            var entidad = new CategoriaProducto();
            SetCatalogProperties(entidad, dto.Nombre, dto.Descripcion);

            _context.CategoriasProducto.Add(entidad);
            await _context.SaveChangesAsync();

            return ApiResponse<CatalogoItemDto>.Ok(MapToCatalogoDto(entidad), "Categoría de producto creada exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearMedioPagoAsync(CrearCatalogoItemDto dto)
        {
            var entidad = new MedioPago();
            SetCatalogProperties(entidad, dto.Nombre, dto.Descripcion);

            _context.MediosPago.Add(entidad);
            await _context.SaveChangesAsync();

            return ApiResponse<CatalogoItemDto>.Ok(MapToCatalogoDto(entidad), "Medio de pago creado exitosamente.");
        }

        public async Task<ApiResponse<CatalogoItemDto>> CrearMotivoCancelacionAsync(CrearCatalogoItemDto dto)
        {
            var entidad = new MotivoCancelacion();
            SetCatalogProperties(entidad, dto.Nombre, dto.Descripcion);

            _context.MotivosCancelacion.Add(entidad);
            await _context.SaveChangesAsync();

            return ApiResponse<CatalogoItemDto>.Ok(MapToCatalogoDto(entidad), "Motivo de cancelación creado exitosamente.");
        }

        public async Task<ApiResponse<bool>> EliminarEspecialidadAsync(int id)
        {
            var especialidad = await _context.Especialidades.FindAsync(id);
            if (especialidad == null)
                return ApiResponse<bool>.Fail("La especialidad especificada no existe.");

            _context.Especialidades.Remove(especialidad);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Especialidad dada de baja correctamente.");
        }

        private static CatalogoItemDto MapToCatalogoDto<T>(T entity) where T : class
        {
            var type = entity.GetType();
            var id = (int)(type.GetProperty("Id")?.GetValue(entity) ?? 0);
            var nombre = type.GetProperty("Nombre")?.GetValue(entity)?.ToString()
                         ?? type.GetProperty("Descripcion")?.GetValue(entity)?.ToString()
                         ?? string.Empty;
            var descripcion = type.GetProperty("Descripcion")?.GetValue(entity)?.ToString();
            var color = type.GetProperty("ColorHex")?.GetValue(entity)?.ToString();

            return new CatalogoItemDto
            {
                Id = id,
                Nombre = nombre,
                Descripcion = descripcion,
                ExtraInfo = color
            };
        }

        private static void SetCatalogProperties<T>(T entity, string nombre, string? descripcion) where T : class
        {
            var type = entity.GetType();
            var propNombre = type.GetProperty("Nombre");
            var propDescripcion = type.GetProperty("Descripcion");

            if (propNombre != null && propNombre.CanWrite)
                propNombre.SetValue(entity, nombre);

            if (propDescripcion != null && propDescripcion.CanWrite)
                propDescripcion.SetValue(entity, descripcion ?? nombre);
        }
    }
}