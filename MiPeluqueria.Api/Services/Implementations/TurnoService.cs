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
            var peluquero = await _context.Peluqueros
                .Include(p => p.HorariosLaborales)
                .Include(p => p.BloqueosHorario)
                .FirstOrDefaultAsync(p => p.Id == dto.PeluqueroId && p.Activo);

            if (peluquero == null)
                return ApiResponse<TurnoResponseDto>.Falla("El profesional seleccionado no existe o está inactivo.");

            var servicios = await _context.Servicios
                .Where(s => dto.ServiciosIds.Contains(s.Id) && s.Activo)
                .ToListAsync();

            if (servicios.Count != dto.ServiciosIds.Count)
                return ApiResponse<TurnoResponseDto>.Falla("Uno o más servicios seleccionados no son válidos.");

            int duracionTotalMinutos = servicios.Sum(s => s.DuracionMinutos);
            decimal montoTotal = servicios.Sum(s => s.Precio);
            DateTime fechaFinCalculada = dto.FechaHoraInicio.AddMinutes(duracionTotalMinutos);

            var diaSemana = dto.FechaHoraInicio.DayOfWeek;
            var horaInicio = dto.FechaHoraInicio.TimeOfDay;
            var horaFin = fechaFinCalculada.TimeOfDay;

            bool horarioValido = peluquero.HorariosLaborales.Any(h =>
                h.DiaSemana == diaSemana &&
                horaInicio >= h.HoraInicio &&
                horaFin <= h.HoraFin);

            if (!horarioValido)
                return ApiResponse<TurnoResponseDto>.Falla("El horario seleccionado está fuera de la jornada laboral del profesional.");

            bool tieneBloqueo = peluquero.BloqueosHorario.Any(b =>
                dto.FechaHoraInicio < b.FechaFin && fechaFinCalculada > b.FechaInicio);

            if (tieneBloqueo)
                return ApiResponse<TurnoResponseDto>.Falla("El profesional no se encuentra disponible en esa franja horaria (bloqueo activo).");

            bool solapado = await _turnoRepo.ExisteSolapamientoAsync(dto.PeluqueroId, dto.FechaHoraInicio, fechaFinCalculada);

            if (solapado)
                return ApiResponse<TurnoResponseDto>.Falla("El profesional ya tiene un turno reservado en ese intervalo.");

            var nuevoTurno = new Turno
            {
                ClienteId = dto.ClienteId,
                PeluqueroId = dto.PeluqueroId,
                FechaHoraInicio = dto.FechaHoraInicio,
                FechaHoraFin = fechaFinCalculada,
                EstadoId = 1, // 1 = Pendiente
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

            // Preparar Turno en memoria
            await _turnoRepo.AddAsync(nuevoTurno);

            // Preparar Historial en memoria (Usando la propiedad de navegación 'Turno', no el 'Id')
            await _turnoRepo.AddHistorialEstadoAsync(new HistorialEstadoTurno
            {
                Turno = nuevoTurno,
                EstadoAnteriorId = null,
                EstadoNuevoId = 1, // 1 = Pendiente
                FechaCambio = DateTime.UtcNow,
                Observaciones = "Creación de la reserva."
            });

            // GATILLO TRANSACCIONAL - Todo se guarda junto aquí
            await _unitOfWork.SaveChangesAsync();

            var turnoCompleto = await _turnoRepo.GetByIdConDetallesAsync(nuevoTurno.Id);
            var responseDto = _mapper.Map<TurnoResponseDto>(turnoCompleto);
            responseDto.Servicios = servicios.Select(s => s.Nombre).ToList();

            return ApiResponse<TurnoResponseDto>.Exito(responseDto, "Turno reservado exitosamente.");
        }

        public async Task<ApiResponse<bool>> CambiarEstadoAsync(int turnoId, CambiarEstadoTurnoDto dto, int usuarioId)
        {
            var turno = await _turnoRepo.GetByIdConDetallesAsync(turnoId);
            if (turno == null)
                return ApiResponse<bool>.Falla("El turno no existe.");

            if (dto.NuevoEstadoId == 5 && dto.MotivoCancelacionId == null) // 5 = Cancelado
                return ApiResponse<bool>.Falla("Es obligatorio especificar el motivo de cancelación.");

            int estadoAnterior = turno.EstadoId;
            turno.EstadoId = dto.NuevoEstadoId;
            turno.MotivoCancelacionId = dto.MotivoCancelacionId;

            // Preparamos cambios en memoria
            _turnoRepo.Update(turno);
            await _turnoRepo.AddHistorialEstadoAsync(new HistorialEstadoTurno
            {
                TurnoId = turno.Id, // Aquí sí podemos usar el ID porque el turno ya existía
                EstadoAnteriorId = estadoAnterior,
                EstadoNuevoId = dto.NuevoEstadoId,
                FechaCambio = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Observaciones = dto.Observaciones
            });

            // Guardado atómico
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Estado de turno actualizado con trazabilidad.");
        }

        public async Task<ApiResponse<List<TurnoResponseDto>>> ObtenerAgendaPeluqueroFechaAsync(int peluqueroId, DateTime fecha)
        {
            var turnos = await _turnoRepo.GetTurnosPeluqueroEnFechaAsync(peluqueroId, fecha);

            var dtos = turnos.Select(t =>
            {
                var dto = _mapper.Map<TurnoResponseDto>(t);
                dto.Servicios = t.TurnoServicios.Select(ts => ts.Servicio?.Nombre ?? "").ToList();
                return dto;
            }).ToList();

            return ApiResponse<List<TurnoResponseDto>>.Exito(dtos, "Agenda diaria obtenida.");
        }
    }
}