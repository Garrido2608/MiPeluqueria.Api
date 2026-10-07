using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class TurnoService : ITurnoService
    {
        private readonly ITurnoRepository _turnoRepo;
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public TurnoService(
            ITurnoRepository turnoRepo,
            ApplicationDbContext context,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _turnoRepo = turnoRepo;
            _context = context;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<TurnoResponseDto>> ReservarTurnoAsync(CrearTurnoDto dto)
        {
            // 1. Validar existencia del cliente
            bool clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == dto.ClienteId && c.Activo);
            if (!clienteExiste)
                return ApiResponse<TurnoResponseDto>.Falla("El cliente indicado no existe o se encuentra inactivo.");

            // 2. Validar existencia del profesional y cargar horarios + bloqueos
            var peluquero = await _context.Peluqueros
                .Include(p => p.HorariosLaborales)
                .Include(p => p.BloqueosHorario)
                .FirstOrDefaultAsync(p => p.Id == dto.PeluqueroId && p.Activo);

            if (peluquero == null)
                return ApiResponse<TurnoResponseDto>.Falla("El profesional seleccionado no existe o está inactivo.");

            // 3. Recuperar servicios solicitados y calcular duración y monto total
            var serviciosIdsDistintos = dto.ServiciosIds.Distinct().ToList();
            var servicios = await _context.Servicios
                .Where(s => serviciosIdsDistintos.Contains(s.Id) && s.Activo)
                .ToListAsync();

            if (servicios.Count != serviciosIdsDistintos.Count)
                return ApiResponse<TurnoResponseDto>.Falla("Uno o más servicios seleccionados no existen o están inactivos.");

            int duracionTotalMinutos = servicios.Sum(s => s.DuracionMinutos);
            decimal montoTotal = servicios.Sum(s => s.Precio);
            DateTime fechaFinCalculada = dto.FechaHoraInicio.AddMinutes(duracionTotalMinutos);

            // 4. Validar jornada laboral del profesional (si tiene horarios configurados)
            if (peluquero.HorariosLaborales.Any())
            {
                var diaSemana = dto.FechaHoraInicio.DayOfWeek;
                var horaInicio = dto.FechaHoraInicio.TimeOfDay;
                var horaFin = fechaFinCalculada.TimeOfDay;

                bool horarioValido = peluquero.HorariosLaborales.Any(h =>
                    h.DiaSemana == diaSemana &&
                    horaInicio >= h.HoraInicio &&
                    horaFin <= h.HoraFin);

                if (!horarioValido)
                    return ApiResponse<TurnoResponseDto>.Falla("El horario solicitado se encuentra fuera de la jornada laboral del profesional.");
            }

            // 5. Validar que no colisione con un BloqueoHorario (vacaciones, médico, franco)
            bool tieneBloqueo = peluquero.BloqueosHorario.Any(b =>
                dto.FechaHoraInicio < b.FechaFin && fechaFinCalculada > b.FechaInicio);

            if (tieneBloqueo)
                return ApiResponse<TurnoResponseDto>.Falla("El profesional registra un bloqueo por ausencia programada en esa franja horaria.");

            // 6. Validar que no se solape con otro turno activo del mismo profesional
            bool solapado = await _turnoRepo.ExisteSolapamientoAsync(dto.PeluqueroId, dto.FechaHoraInicio, fechaFinCalculada);

            if (solapado)
                return ApiResponse<TurnoResponseDto>.Falla("El profesional ya posee un turno asignado que se superpone con ese horario.");

            // 7. Construir grafo completo de la entidad Turno + TurnoServicios + HistorialEstadoTurno
            var nuevoTurno = new Turno
            {
                ClienteId = dto.ClienteId,
                PeluqueroId = dto.PeluqueroId,
                FechaHoraInicio = dto.FechaHoraInicio,
                FechaHoraFin = fechaFinCalculada,
                EstadoId = (int)EstadoTurnoEnum.Pendiente,
                MontoTotalEstimado = montoTotal,
                Observaciones = dto.Observaciones
            };

            foreach (var s in servicios)
            {
                nuevoTurno.TurnoServicios.Add(new TurnoServicio
                {
                    ServicioId = s.Id,
                    PrecioHistorico = s.Precio,
                    DuracionMinutosHistorica = s.DuracionMinutos
                });
            }

            // Usamos la colección de navegación para que EF Core asigne el TurnoId automáticamente al hacer el INSERT
            nuevoTurno.HistorialEstados.Add(new HistorialEstadoTurno
            {
                EstadoAnteriorId = null,
                EstadoNuevoId = (int)EstadoTurnoEnum.Pendiente,
                FechaCambio = DateTime.UtcNow,
                Observaciones = "Creación inicial de la reserva de turno."
            });

            await _turnoRepo.AddAsync(nuevoTurno);
            await _unitOfWork.SaveChangesAsync();

            var turnoCompleto = await _turnoRepo.GetByIdConDetallesAsync(nuevoTurno.Id);
            var responseDto = _mapper.Map<TurnoResponseDto>(turnoCompleto);

            return ApiResponse<TurnoResponseDto>.Exito(responseDto, "Turno reservado exitosamente.");
        }

        public async Task<ApiResponse<bool>> CambiarEstadoAsync(int turnoId, CambiarEstadoTurnoDto dto, int usuarioId)
        {
            var turno = await _turnoRepo.GetByIdConDetallesAsync(turnoId);
            if (turno == null)
                return ApiResponse<bool>.Falla("El turno solicitado no existe.");

            if (dto.NuevoEstadoId == (int)EstadoTurnoEnum.Cancelado && dto.MotivoCancelacionId == null)
                return ApiResponse<bool>.Falla("Es obligatorio especificar el motivo de cancelación.");

            int estadoAnterior = turno.EstadoId;
            turno.EstadoId = dto.NuevoEstadoId;
            turno.MotivoCancelacionId = dto.MotivoCancelacionId;

            await _turnoRepo.UpdateAsync(turno);
            await _turnoRepo.AddHistorialEstadoAsync(new HistorialEstadoTurno
            {
                TurnoId = turno.Id,
                EstadoAnteriorId = estadoAnterior,
                EstadoNuevoId = dto.NuevoEstadoId,
                FechaCambio = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Observaciones = dto.Observaciones ?? "Actualización manual de estado."
            });

            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Estado del turno actualizado con trazabilidad de auditoría.");
        }

        public async Task<ApiResponse<List<TurnoResponseDto>>> ObtenerAgendaPeluqueroFechaAsync(int peluqueroId, DateTime fecha)
        {
            var turnos = await _turnoRepo.GetTurnosPeluqueroEnFechaAsync(peluqueroId, fecha);
            var dtos = _mapper.Map<List<TurnoResponseDto>>(turnos);
            return ApiResponse<List<TurnoResponseDto>>.Exito(dtos, "Agenda diaria obtenida correctamente.");
        }
    }
}