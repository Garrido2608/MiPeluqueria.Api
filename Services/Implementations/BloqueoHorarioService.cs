using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class BloqueoHorarioService : IBloqueoHorarioService
    {
        private readonly IUnitOfWork _uow;

        public BloqueoHorarioService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ApiResponse<IReadOnlyList<BloqueoHorarioResponseDto>>> ObtenerPorPeluqueroAsync(
            int peluqueroId,
            CancellationToken ct = default)
        {
            var bloqueos = await _uow.BloqueosHorarios.GetByPeluqueroIdAsync(peluqueroId, null, ct);
            var dtos = bloqueos.Select(MapToDto).ToList();
            return ApiResponse<IReadOnlyList<BloqueoHorarioResponseDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<BloqueoHorarioResponseDto>> CrearBloqueoAsync(
            BloqueoHorarioDto dto,
            CancellationToken ct = default)
        {
            if (dto.FechaFin <= dto.FechaInicio)
                return ApiResponse<BloqueoHorarioResponseDto>.Falla("La fecha y hora de fin debe ser posterior a la fecha y hora de inicio.");

            var peluquero = await _uow.Peluqueros.GetByIdWithDetailsAsync(dto.PeluqueroId, false, ct);
            if (peluquero == null)
                return ApiResponse<BloqueoHorarioResponseDto>.Falla("El peluquero especificado no existe.");

            var bloqueo = new BloqueoHorario
            {
                PeluqueroId = dto.PeluqueroId,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                Motivo = dto.Motivo.Trim()
            };

            await _uow.BloqueosHorarios.AddAsync(bloqueo, ct);
            await _uow.SaveChangesAsync(ct);

            bloqueo.Peluquero = peluquero;

            // Regla Permisiva con Alerta (05_BUSINESS_RULES.md Sección 2.3):
            var turnosSolapados = await _uow.Turnos.GetTurnosActivosEnRangoAsync(
                dto.PeluqueroId,
                dto.FechaInicio,
                dto.FechaFin,
                ct);

            var turnosAfectados = turnosSolapados
                .Where(t => t.EstadoId == (int)EstadoTurnoEnum.Pendiente
                         || t.EstadoId == (int)EstadoTurnoEnum.Confirmado
                         || t.EstadoId == (int)EstadoTurnoEnum.EnProceso)
                .ToList();

            List<string>? warnings = null;
            string mensaje = "Bloqueo horario registrado correctamente.";

            if (turnosAfectados.Count > 0)
            {
                warnings = turnosAfectados
                    .Select(t =>
                        $"Turno #{t.Id} solapado ({t.FechaHoraInicio:dd/MM/yyyy HH:mm} a {t.FechaHoraFin:HH:mm}) - Cliente: {(t.Cliente != null ? $"{t.Cliente.Nombre} {t.Cliente.Apellido}".Trim() : $"ID {t.ClienteId}")}.")
                    .ToList();
                mensaje = $"Bloqueo horario registrado. Atención: existen {turnosAfectados.Count} turno(s) activo(s) solapado(s) que requieren reprogramación.";
            }

            return ApiResponse<BloqueoHorarioResponseDto>.Ok(MapToDto(bloqueo), mensaje, warnings);
        }

        public async Task<ApiResponse<bool>> EliminarBloqueoAsync(int id, CancellationToken ct = default)
        {
            var bloqueo = await _uow.BloqueosHorarios.GetByIdAsync(id, ct);
            if (bloqueo == null)
                return ApiResponse<bool>.Falla("El bloqueo horario especificado no existe.");

            _uow.BloqueosHorarios.Delete(bloqueo);
            await _uow.SaveChangesAsync(ct);

            return ApiResponse<bool>.Exito(true, "Bloqueo horario eliminado correctamente.");
        }

        private static BloqueoHorarioResponseDto MapToDto(BloqueoHorario b) => new()
        {
            Id = b.Id,
            PeluqueroId = b.PeluqueroId,
            PeluqueroNombre = b.Peluquero != null ? $"{b.Peluquero.Nombre} {b.Peluquero.Apellido}".Trim() : string.Empty,
            FechaInicio = b.FechaInicio,
            FechaFin = b.FechaFin,
            Motivo = b.Motivo
        };
    }
}
