using System;
using System.Collections.Generic;
using System.Linq;
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
    public class PeluqueroService : IPeluqueroService
    {
        private const int MaxPeluquerosAntiAbuso = 50;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PeluqueroService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<PeluqueroDto>>> GetAllAsync(
            bool soloActivos = false,
            CancellationToken ct = default)
        {
            var peluqueros = await _unitOfWork.Peluqueros.GetAllWithDetailsAsync(soloActivos, ct);
            var dtos = _mapper.Map<IEnumerable<PeluqueroDto>>(peluqueros);
            return ApiResponse<IEnumerable<PeluqueroDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<PeluqueroDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var peluquero = await _unitOfWork.Peluqueros.GetByIdWithDetailsAsync(id, false, ct);
            if (peluquero == null)
                return ApiResponse<PeluqueroDto>.Falla("El peluquero solicitado no existe.");

            return ApiResponse<PeluqueroDto>.Exito(_mapper.Map<PeluqueroDto>(peluquero));
        }

        public async Task<ApiResponse<PeluqueroDto>> CreateAsync(CrearPeluqueroDto dto, CancellationToken ct = default)
        {
            int activos = await _unitOfWork.Peluqueros.CountActivosAsync(ct);
            if (activos >= MaxPeluquerosAntiAbuso)
            {
                return ApiResponse<PeluqueroDto>.Falla(
                    $"Se ha alcanzado el límite técnico anti-abuso máximo de {MaxPeluquerosAntiAbuso} profesionales activos en esta sucursal.");
            }

            if (await _unitOfWork.Peluqueros.ExistsEmailAsync(dto.Email.Trim(), null, ct))
            {
                return ApiResponse<PeluqueroDto>.Falla("Ya existe un profesional registrado con ese correo electrónico en esta sucursal.");
            }

            var peluquero = new Peluquero
            {
                Nombre = dto.Nombre.Trim(),
                Apellido = dto.Apellido.Trim(),
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                ColorAgendaHex = string.IsNullOrWhiteSpace(dto.ColorAgendaHex) ? "#3B82F6" : dto.ColorAgendaHex.Trim(),
                Activo = true
            };

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await _unitOfWork.Peluqueros.AddAsync(peluquero, token);
                await _unitOfWork.SaveChangesAsync(token);

                if (dto.EspecialidadesIds != null && dto.EspecialidadesIds.Count > 0)
                {
                    await _unitOfWork.Peluqueros.ReemplazarEspecialidadesAsync(peluquero.Id, dto.EspecialidadesIds, token);
                    await _unitOfWork.SaveChangesAsync(token);
                }
            }, ct: ct);

            return await GetByIdAsync(peluquero.Id, ct);
        }

        public async Task<ApiResponse<PeluqueroDto>> UpdateAsync(
            int id,
            ActualizarPeluqueroDto dto,
            CancellationToken ct = default)
        {
            var peluquero = await _unitOfWork.Peluqueros.GetByIdWithDetailsAsync(id, true, ct);
            if (peluquero == null)
                return ApiResponse<PeluqueroDto>.Falla("El peluquero solicitado no existe.");

            if (!peluquero.Activo && dto.Activo)
            {
                int activos = await _unitOfWork.Peluqueros.CountActivosAsync(ct);
                if (activos >= MaxPeluquerosAntiAbuso)
                {
                    return ApiResponse<PeluqueroDto>.Falla(
                        $"No se puede reactivar al profesional porque se alcanzó el límite máximo de {MaxPeluquerosAntiAbuso} profesionales activos.");
                }
            }

            if (await _unitOfWork.Peluqueros.ExistsEmailAsync(dto.Email.Trim(), id, ct))
            {
                return ApiResponse<PeluqueroDto>.Falla("Ya existe otro profesional registrado con ese correo electrónico en esta sucursal.");
            }

            if (peluquero.Activo && !dto.Activo)
            {
                if (await _unitOfWork.Peluqueros.TieneTurnosFuturosActivosAsync(id, DateTime.UtcNow, ct))
                    return ApiResponse<PeluqueroDto>.Falla("No se puede desactivar el profesional porque posee turnos futuros pendientes o confirmados.");

                if (await _unitOfWork.Peluqueros.TieneLiquidacionesPendientesAsync(id, ct))
                    return ApiResponse<PeluqueroDto>.Falla("No se puede desactivar el profesional porque posee liquidaciones de comisiones pendientes de pago.");
            }

            peluquero.Nombre = dto.Nombre.Trim();
            peluquero.Apellido = dto.Apellido.Trim();
            peluquero.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
            peluquero.Email = dto.Email.Trim().ToLowerInvariant();
            peluquero.ColorAgendaHex = string.IsNullOrWhiteSpace(dto.ColorAgendaHex) ? "#3B82F6" : dto.ColorAgendaHex.Trim();
            peluquero.Activo = dto.Activo;

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                _unitOfWork.Peluqueros.Update(peluquero);
                await _unitOfWork.Peluqueros.ReemplazarEspecialidadesAsync(peluquero.Id, dto.EspecialidadesIds ?? new List<int>(), token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct: ct);

            return await GetByIdAsync(id, ct);
        }

        public async Task<ApiResponse<PeluqueroDto>> UpdateAsync(int id, CrearPeluqueroDto dto)
        {
            var actual = await _unitOfWork.Peluqueros.GetByIdWithDetailsAsync(id, false);
            return await UpdateAsync(id, new ActualizarPeluqueroDto
            {
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Telefono = dto.Telefono,
                Email = dto.Email,
                ColorAgendaHex = dto.ColorAgendaHex,
                Activo = actual?.Activo ?? true,
                EspecialidadesIds = dto.EspecialidadesIds ?? new List<int>()
            });
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var peluquero = await _unitOfWork.Peluqueros.GetByIdWithDetailsAsync(id, true, ct);
            if (peluquero == null)
                return ApiResponse<bool>.Falla("El peluquero solicitado no existe.");

            if (await _unitOfWork.Peluqueros.TieneTurnosFuturosActivosAsync(id, DateTime.UtcNow, ct))
            {
                return ApiResponse<bool>.Falla(
                    "No se permite dar de baja al profesional porque posee turnos futuros en estado Pendiente, Confirmado o En Proceso.");
            }

            if (await _unitOfWork.Peluqueros.TieneLiquidacionesPendientesAsync(id, ct))
            {
                return ApiResponse<bool>.Falla(
                    "No se permite dar de baja al profesional porque posee liquidaciones de comisiones pendientes de pago.");
            }

            await _unitOfWork.Peluqueros.DeleteAsync(id, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<bool>.Exito(true, "Peluquero dado de baja correctamente.");
        }

        public async Task<ApiResponse<IEnumerable<HorarioLaboralDto>>> ObtenerHorariosAsync(
            int peluqueroId,
            CancellationToken ct = default)
        {
            var horarios = await _unitOfWork.Peluqueros.GetHorariosByPeluqueroIdAsync(peluqueroId, ct);

            var dtos = horarios.Select(h => new HorarioLaboralDto
            {
                Id = h.Id,
                PeluqueroId = h.PeluqueroId,
                DiaSemana = (int)h.DiaSemana,
                HoraInicio = h.HoraInicio,
                HoraFin = h.HoraFin,
                HoraInicioDescanso = h.HoraInicioDescanso,
                HoraFinDescanso = h.HoraFinDescanso,
                Activo = h.Activo
            })
            .OrderBy(h => h.DiaSemana)
            .ToList();

            return ApiResponse<IEnumerable<HorarioLaboralDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<bool>> ConfigurarHorariosAsync(
            int peluqueroId,
            GuardarHorariosPeluqueroDto dto,
            CancellationToken ct = default)
        {
            var peluquero = await _unitOfWork.Peluqueros.GetByIdWithDetailsAsync(peluqueroId, false, ct);
            if (peluquero == null)
                return ApiResponse<bool>.Falla("El peluquero especificado no existe.");

            var nuevosHorarios = new List<HorarioLaboral>();

            foreach (var item in dto.Horarios)
            {
                if (item.DiaSemana < 0 || item.DiaSemana > 6)
                    return ApiResponse<bool>.Falla($"Día de semana inválido ({item.DiaSemana}). Debe estar entre 0 (Domingo) y 6 (Sábado).");

                if (item.Activo && item.HoraFin <= item.HoraInicio)
                    return ApiResponse<bool>.Falla($"La hora de fin debe ser posterior a la hora de inicio para el día {(DayOfWeek)item.DiaSemana}.");

                if (item.HoraInicioDescanso.HasValue && item.HoraFinDescanso.HasValue &&
                    item.HoraFinDescanso.Value <= item.HoraInicioDescanso.Value)
                {
                    return ApiResponse<bool>.Falla($"El fin de descanso debe ser posterior al inicio de descanso para el día {(DayOfWeek)item.DiaSemana}.");
                }

                nuevosHorarios.Add(new HorarioLaboral
                {
                    PeluqueroId = peluqueroId,
                    DiaSemana = (DayOfWeek)item.DiaSemana,
                    HoraInicio = item.HoraInicio,
                    HoraFin = item.HoraFin,
                    HoraInicioDescanso = item.HoraInicioDescanso,
                    HoraFinDescanso = item.HoraFinDescanso,
                    Activo = item.Activo
                });
            }

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                await _unitOfWork.Peluqueros.ReemplazarHorariosLaboralesAsync(peluqueroId, nuevosHorarios, token);
                await _unitOfWork.SaveChangesAsync(token);
            }, ct: ct);

            return ApiResponse<bool>.Exito(true, "Horarios laborales actualizados correctamente.");
        }
    }
}
