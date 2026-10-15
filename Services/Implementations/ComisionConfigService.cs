using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ComisionConfigService : IComisionConfigService
    {
        private readonly ApplicationDbContext _context;

        public ComisionConfigService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<IEnumerable<ComisionConfigDto>>> ObtenerTodasAsync()
        {
            var configs = await _context.ComisionesConfigs
                .Include(c => c.Peluquero)
                .Include(c => c.Servicio)
                .OrderBy(c => c.PeluqueroId)
                .ThenBy(c => c.ServicioId)
                .AsNoTracking()
                .ToListAsync();

            return ApiResponse<IEnumerable<ComisionConfigDto>>.Ok(configs.Select(MapToDto));
        }

        public async Task<ApiResponse<IEnumerable<ComisionConfigDto>>> ObtenerPorPeluqueroAsync(int peluqueroId)
        {
            var existePeluquero = await _context.Peluqueros.AnyAsync(p => p.Id == peluqueroId);
            if (!existePeluquero)
                return ApiResponse<IEnumerable<ComisionConfigDto>>.Fail("El peluquero especificado no existe.");

            var configs = await _context.ComisionesConfigs
                .Include(c => c.Peluquero)
                .Include(c => c.Servicio)
                .Where(c => c.PeluqueroId == peluqueroId)
                .OrderBy(c => c.ServicioId)
                .AsNoTracking()
                .ToListAsync();

            return ApiResponse<IEnumerable<ComisionConfigDto>>.Ok(configs.Select(MapToDto));
        }

        public async Task<ApiResponse<ComisionConfigDto>> GuardarConfiguracionAsync(GuardarComisionConfigDto dto)
        {
            var peluquero = await _context.Peluqueros.FindAsync(dto.PeluqueroId);
            if (peluquero == null)
                return ApiResponse<ComisionConfigDto>.Fail("El peluquero especificado no existe.");

            if (dto.ServicioId.HasValue)
            {
                var servicio = await _context.Servicios.FindAsync(dto.ServicioId.Value);
                if (servicio == null)
                    return ApiResponse<ComisionConfigDto>.Fail("El servicio especificado no existe.");
            }

            // Si ya existe una regla para ese peluquero y ese mismo ServicioId (o null para regla general), la actualiza (Upsert)
            var existente = await _context.ComisionesConfigs
                .FirstOrDefaultAsync(c => c.PeluqueroId == dto.PeluqueroId && c.ServicioId == dto.ServicioId);

            if (existente != null)
            {
                existente.PorcentajeServicio = dto.PorcentajeServicio;
                existente.PorcentajeVentaProducto = dto.PorcentajeVentaProducto;
                await _context.SaveChangesAsync();

                await _context.Entry(existente).Reference(c => c.Peluquero).LoadAsync();
                if (existente.ServicioId.HasValue)
                    await _context.Entry(existente).Reference(c => c.Servicio).LoadAsync();

                return ApiResponse<ComisionConfigDto>.Ok(MapToDto(existente), "Esquema de comisión actualizado correctamente.");
            }

            var nuevaConfig = new ComisionesConfig
            {
                PeluqueroId = dto.PeluqueroId,
                ServicioId = dto.ServicioId,
                PorcentajeServicio = dto.PorcentajeServicio,
                PorcentajeVentaProducto = dto.PorcentajeVentaProducto
            };

            _context.ComisionesConfigs.Add(nuevaConfig);
            await _context.SaveChangesAsync();

            await _context.Entry(nuevaConfig).Reference(c => c.Peluquero).LoadAsync();
            if (nuevaConfig.ServicioId.HasValue)
                await _context.Entry(nuevaConfig).Reference(c => c.Servicio).LoadAsync();

            return ApiResponse<ComisionConfigDto>.Ok(MapToDto(nuevaConfig), "Esquema de comisión registrado exitosamente.");
        }

        public async Task<ApiResponse<ComisionConfigDto>> ActualizarConfiguracionAsync(int id, GuardarComisionConfigDto dto)
        {
            var config = await _context.ComisionesConfigs
                .Include(c => c.Peluquero)
                .Include(c => c.Servicio)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (config == null)
                return ApiResponse<ComisionConfigDto>.Fail("La configuración de comisión solicitada no existe.");

            config.PeluqueroId = dto.PeluqueroId;
            config.ServicioId = dto.ServicioId;
            config.PorcentajeServicio = dto.PorcentajeServicio;
            config.PorcentajeVentaProducto = dto.PorcentajeVentaProducto;

            await _context.SaveChangesAsync();

            return ApiResponse<ComisionConfigDto>.Ok(MapToDto(config), "Configuración de comisión modificada exitosamente.");
        }

        public async Task<ApiResponse<bool>> EliminarConfiguracionAsync(int id)
        {
            var config = await _context.ComisionesConfigs.FindAsync(id);
            if (config == null)
                return ApiResponse<bool>.Fail("La configuración de comisión solicitada no existe.");

            _context.ComisionesConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Configuración de comisión eliminada correctamente.");
        }

        private static ComisionConfigDto MapToDto(ComisionesConfig c)
        {
            return new ComisionConfigDto
            {
                Id = c.Id,
                PeluqueroId = c.PeluqueroId,
                NombrePeluquero = c.Peluquero != null ? $"{c.Peluquero.Nombre} {c.Peluquero.Apellido}" : string.Empty,
                ServicioId = c.ServicioId,
                NombreServicio = c.Servicio != null ? c.Servicio.Nombre : "Esquema General (Todos los servicios)",
                PorcentajeServicio = c.PorcentajeServicio,
                PorcentajeVentaProducto = c.PorcentajeVentaProducto
            };
        }
    }
}