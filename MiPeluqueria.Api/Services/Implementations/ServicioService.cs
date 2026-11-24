using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ServicioService : IServicioService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ServicioService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<ServicioDto>>> GetAllAsync(
            bool soloActivos = false,
            CancellationToken ct = default)
        {
            var servicios = await _unitOfWork.Servicios.GetAllWithCategoriaAsync(soloActivos, ct);
            var dtos = _mapper.Map<IEnumerable<ServicioDto>>(servicios);
            return ApiResponse<IEnumerable<ServicioDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<ServicioDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var servicio = await _unitOfWork.Servicios.GetByIdWithCategoriaAsync(id, false, ct);
            if (servicio == null)
                return ApiResponse<ServicioDto>.Falla("El servicio solicitado no existe.");

            return ApiResponse<ServicioDto>.Exito(_mapper.Map<ServicioDto>(servicio));
        }

        public async Task<ApiResponse<ServicioDto>> CreateAsync(CrearServicioDto dto, CancellationToken ct = default)
        {
            var categoria = await _unitOfWork.Catalogos.GetCategoriaServicioByIdAsync(dto.CategoriaId, ct);
            if (categoria == null)
                return ApiResponse<ServicioDto>.Falla("La categoría de servicio especificada no existe.");

            var servicio = new Servicio
            {
                CategoriaId = dto.CategoriaId,
                Nombre = dto.Nombre.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
                Precio = dto.Precio,
                DuracionMinutos = dto.DuracionMinutos,
                Activo = true
            };

            await _unitOfWork.Servicios.AddAsync(servicio, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var creado = await _unitOfWork.Servicios.GetByIdWithCategoriaAsync(servicio.Id, false, ct) ?? servicio;
            return ApiResponse<ServicioDto>.Exito(_mapper.Map<ServicioDto>(creado), "Servicio creado exitosamente.");
        }

        public async Task<ApiResponse<ServicioDto>> UpdateAsync(
            int id,
            ActualizarServicioDto dto,
            int? usuarioModificacionId = null,
            CancellationToken ct = default)
        {
            var servicio = await _unitOfWork.Servicios.GetByIdWithCategoriaAsync(id, true, ct);
            if (servicio == null)
                return ApiResponse<ServicioDto>.Falla("El servicio solicitado no existe.");

            var categoria = await _unitOfWork.Catalogos.GetCategoriaServicioByIdAsync(dto.CategoriaId, ct);
            if (categoria == null)
                return ApiResponse<ServicioDto>.Falla("La categoría de servicio especificada no existe.");

            decimal precioAnterior = servicio.Precio;
            bool cambioPrecio = precioAnterior != dto.Precio;

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                servicio.CategoriaId = dto.CategoriaId;
                servicio.Nombre = dto.Nombre.Trim();
                servicio.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();
                servicio.Precio = dto.Precio;
                servicio.DuracionMinutos = dto.DuracionMinutos;
                servicio.Activo = dto.Activo;

                _unitOfWork.Servicios.Update(servicio);

                if (cambioPrecio)
                {
                    await _unitOfWork.Servicios.AddHistorialPrecioAsync(new HistorialPrecioServicio
                    {
                        ServicioId = servicio.Id,
                        PrecioAnterior = precioAnterior,
                        PrecioNuevo = dto.Precio,
                        FechaCambio = DateTime.UtcNow,
                        UsuarioModificacionId = usuarioModificacionId,
                        Motivo = "Actualización de precio de catálogo"
                    }, token);
                }

                await _unitOfWork.SaveChangesAsync(token);
            }, ct: ct);

            var actualizado = await _unitOfWork.Servicios.GetByIdWithCategoriaAsync(id, false, ct) ?? servicio;
            return ApiResponse<ServicioDto>.Exito(_mapper.Map<ServicioDto>(actualizado), "Servicio actualizado correctamente.");
        }

        public async Task<ApiResponse<ServicioDto>> UpdateAsync(int id, CrearServicioDto dto)
        {
            var actual = await _unitOfWork.Servicios.GetByIdWithCategoriaAsync(id, false);
            return await UpdateAsync(id, new ActualizarServicioDto
            {
                CategoriaId = dto.CategoriaId,
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Precio = dto.Precio,
                DuracionMinutos = dto.DuracionMinutos,
                Activo = actual?.Activo ?? true
            });
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var servicio = await _unitOfWork.Servicios.GetByIdWithCategoriaAsync(id, true, ct);
            if (servicio == null)
                return ApiResponse<bool>.Falla("El servicio solicitado no existe.");

            bool tieneTurnosFuturos = await _unitOfWork.Servicios.TieneTurnosFuturosActivosAsync(id, DateTime.UtcNow, ct);

            await _unitOfWork.Servicios.DeleteAsync(id, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            List<string>? warnings = null;
            if (tieneTurnosFuturos)
            {
                warnings = new List<string>
                {
                    "El servicio fue desactivado del catálogo, pero los turnos futuros ya agendados conservarán su registro histórico intacto."
                };
            }

            return ApiResponse<bool>.Ok(true, "Servicio dado de baja correctamente.", warnings);
        }
    }
}
