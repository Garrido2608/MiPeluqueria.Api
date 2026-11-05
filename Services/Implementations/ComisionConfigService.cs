using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ComisionConfigService : IComisionConfigService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ComisionConfigService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<IReadOnlyList<ComisionConfigDto>>> ObtenerTodasAsync(CancellationToken ct = default)
        {
            var configs = await _unitOfWork.ComisionesConfig.GetAllWithDetailsAsync(ct);
            var lista = configs.Select(c => MapToDto(c, c.Peluquero, c.Servicio)).ToList();
            return ApiResponse<IReadOnlyList<ComisionConfigDto>>.Ok(lista);
        }

        public async Task<ApiResponse<IReadOnlyList<ComisionConfigDto>>> ObtenerPorPeluqueroAsync(int peluqueroId, CancellationToken ct = default)
        {
            bool existePeluquero = await _unitOfWork.Peluqueros.ExistsAsync(p => p.Id == peluqueroId, ct);
            if (!existePeluquero)
                return ApiResponse<IReadOnlyList<ComisionConfigDto>>.Fail("El peluquero especificado no existe.");

            var configs = await _unitOfWork.ComisionesConfig.GetByPeluqueroIdWithDetailsAsync(peluqueroId, ct);
            var lista = configs.Select(c => MapToDto(c, c.Peluquero, c.Servicio)).ToList();
            return ApiResponse<IReadOnlyList<ComisionConfigDto>>.Ok(lista);
        }

        public async Task<ApiResponse<ComisionConfigDto>> GuardarConfiguracionAsync(GuardarComisionConfigDto dto, CancellationToken ct = default)
        {
            var errorPorcentajes = ValidarPorcentajes(dto);
            if (errorPorcentajes != null)
                return ApiResponse<ComisionConfigDto>.Fail(errorPorcentajes);

            var peluquero = await _unitOfWork.Peluqueros.GetByIdAsync(dto.PeluqueroId, ct);
            if (peluquero == null)
                return ApiResponse<ComisionConfigDto>.Fail("El peluquero especificado no existe.");

            Servicio? servicio = null;
            if (dto.ServicioId.HasValue && dto.ServicioId.Value > 0)
            {
                servicio = await _unitOfWork.Servicios.GetByIdAsync(dto.ServicioId.Value, ct);
                if (servicio == null)
                    return ApiResponse<ComisionConfigDto>.Fail("El servicio especificado no existe.");
            }

            int? servicioIdNormalizado = servicio?.Id;

            var existente = await _unitOfWork.ComisionesConfig.GetExactaByPeluqueroYServicioAsync(
                dto.PeluqueroId,
                servicioIdNormalizado,
                tracking: true,
                ct);

            if (existente != null)
            {
                existente.PorcentajeServicio = dto.PorcentajeServicio;
                existente.PorcentajeVentaProducto = dto.PorcentajeVentaProducto;
                _unitOfWork.ComisionesConfig.Update(existente);
                await _unitOfWork.SaveChangesAsync(ct);

                return ApiResponse<ComisionConfigDto>.Ok(
                    MapToDto(existente, peluquero, servicio),
                    "Esquema de comisión actualizado correctamente.");
            }

            var nuevaConfig = new ComisionesConfig
            {
                PeluqueroId = dto.PeluqueroId,
                ServicioId = servicioIdNormalizado,
                PorcentajeServicio = dto.PorcentajeServicio,
                PorcentajeVentaProducto = dto.PorcentajeVentaProducto
            };

            await _unitOfWork.ComisionesConfig.AddAsync(nuevaConfig, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<ComisionConfigDto>.Ok(
                MapToDto(nuevaConfig, peluquero, servicio),
                "Esquema de comisión registrado exitosamente.");
        }

        public async Task<ApiResponse<ComisionConfigDto>> ActualizarConfiguracionAsync(
            int id,
            GuardarComisionConfigDto dto,
            CancellationToken ct = default)
        {
            var errorPorcentajes = ValidarPorcentajes(dto);
            if (errorPorcentajes != null)
                return ApiResponse<ComisionConfigDto>.Fail(errorPorcentajes);

            var config = await _unitOfWork.ComisionesConfig.GetByIdWithDetailsAsync(id, tracking: true, ct);
            if (config == null)
                return ApiResponse<ComisionConfigDto>.Fail("La configuración de comisión solicitada no existe.");

            var peluquero = await _unitOfWork.Peluqueros.GetByIdAsync(dto.PeluqueroId, ct);
            if (peluquero == null)
                return ApiResponse<ComisionConfigDto>.Fail("El peluquero especificado no existe.");

            Servicio? servicio = null;
            if (dto.ServicioId.HasValue && dto.ServicioId.Value > 0)
            {
                servicio = await _unitOfWork.Servicios.GetByIdAsync(dto.ServicioId.Value, ct);
                if (servicio == null)
                    return ApiResponse<ComisionConfigDto>.Fail("El servicio especificado no existe.");
            }

            int? servicioIdNormalizado = servicio?.Id;

            var duplicada = await _unitOfWork.ComisionesConfig.GetExactaByPeluqueroYServicioAsync(
                dto.PeluqueroId,
                servicioIdNormalizado,
                tracking: false,
                ct);

            if (duplicada != null && duplicada.Id != id)
                return ApiResponse<ComisionConfigDto>.Fail("Ya existe otra configuración de comisión activa para ese profesional y servicio.");

            config.PeluqueroId = dto.PeluqueroId;
            config.ServicioId = servicioIdNormalizado;
            config.PorcentajeServicio = dto.PorcentajeServicio;
            config.PorcentajeVentaProducto = dto.PorcentajeVentaProducto;

            _unitOfWork.ComisionesConfig.Update(config);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<ComisionConfigDto>.Ok(
                MapToDto(config, peluquero, servicio),
                "Configuración de comisión modificada exitosamente.");
        }

        public async Task<ApiResponse<bool>> EliminarConfiguracionAsync(int id, CancellationToken ct = default)
        {
            var config = await _unitOfWork.ComisionesConfig.GetByIdWithDetailsAsync(id, tracking: true, ct);
            if (config == null)
                return ApiResponse<bool>.Fail("La configuración de comisión solicitada no existe.");

            _unitOfWork.ComisionesConfig.Delete(config);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<bool>.Ok(true, "Configuración de comisión eliminada correctamente.");
        }

        private static string? ValidarPorcentajes(GuardarComisionConfigDto dto)
        {
            if (dto.PorcentajeServicio < 0m || dto.PorcentajeServicio > 100m)
                return "El porcentaje de comisión por servicio debe encontrarse entre 0% y 100%.";

            if (dto.PorcentajeVentaProducto < 0m || dto.PorcentajeVentaProducto > 100m)
                return "El porcentaje de comisión por venta de producto debe encontrarse entre 0% y 100%.";

            return null;
        }

        private static ComisionConfigDto MapToDto(ComisionesConfig c, Peluquero? peluquero, Servicio? servicio)
        {
            return new ComisionConfigDto
            {
                Id = c.Id,
                PeluqueroId = c.PeluqueroId,
                NombrePeluquero = peluquero != null
                    ? $"{peluquero.Nombre} {peluquero.Apellido}".Trim()
                    : string.Empty,
                ServicioId = c.ServicioId,
                NombreServicio = servicio != null
                    ? servicio.Nombre
                    : "Esquema General (Todos los servicios)",
                PorcentajeServicio = c.PorcentajeServicio,
                PorcentajeVentaProducto = c.PorcentajeVentaProducto
            };
        }
    }
}
