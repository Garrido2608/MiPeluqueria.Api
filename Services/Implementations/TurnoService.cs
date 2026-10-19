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
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IRecetaServicioService _recetaServicioService;

        public TurnoService(
            IUnitOfWork unitOfWork,
            ApplicationDbContext context,
            IMapper mapper,
            IRecetaServicioService recetaServicioService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _mapper = mapper;
            _recetaServicioService = recetaServicioService;
        }

        public async Task<ApiResponse<IEnumerable<TurnoResponseDto>>> GetAllAsync(DateTime? fecha = null, int? peluqueroId = null)
        {
            var query = _context.Turnos
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.TurnoServicios)
                    .ThenInclude(ts => ts.Servicio)
                .AsNoTracking()
                .AsQueryable();

            if (fecha.HasValue)
            {
                var inicioDia = fecha.Value.Date;
                var finDia = inicioDia.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.FechaHoraInicio >= inicioDia && t.FechaHoraInicio <= finDia);
            }

            if (peluqueroId.HasValue)
                query = query.Where(t => t.PeluqueroId == peluqueroId.Value);

            var turnos = await query.OrderBy(t => t.FechaHoraInicio).ToListAsync();
            var dtos = _mapper.Map<IEnumerable<TurnoResponseDto>>(turnos);

            return ApiResponse<IEnumerable<TurnoResponseDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<TurnoResponseDto>> GetByIdAsync(int id)
        {
            var turno = await _context.Turnos
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.TurnoServicios)
                    .ThenInclude(ts => ts.Servicio)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null)
                return ApiResponse<TurnoResponseDto>.Falla("El turno solicitado no existe.");

            return ApiResponse<TurnoResponseDto>.Exito(_mapper.Map<TurnoResponseDto>(turno));
        }

        public async Task<ApiResponse<TurnoResponseDto>> CreateAsync(CrearTurnoDto dto, int usuarioId = 1)
        {
            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
                return ApiResponse<TurnoResponseDto>.Falla("El cliente especificado no existe.");

            var peluquero = await _context.Peluqueros.FindAsync(dto.PeluqueroId);
            if (peluquero == null)
                return ApiResponse<TurnoResponseDto>.Falla("El peluquero especificado no existe.");

            var serviciosIds = dto.ServiciosIds ?? new List<int>();
            int duracionTotal = 30;
            decimal montoTotalEstimado = 0m;
            var servicios = new List<Servicio>();

            if (serviciosIds.Any())
            {
                servicios = await _context.Servicios
                    .Where(s => serviciosIds.Contains(s.Id) && s.Activo)
                    .ToListAsync();

                if (!servicios.Any())
                    return ApiResponse<TurnoResponseDto>.Falla("Los servicios seleccionados no existen o están inactivos.");

                duracionTotal = servicios.Sum(s => s.DuracionMinutos > 0 ? s.DuracionMinutos : 30);
                montoTotalEstimado = servicios.Sum(s => s.Precio);
            }

            var fechaInicio = dto.FechaHoraInicio;
            var fechaFin = fechaInicio.AddMinutes(duracionTotal);

            bool existeSolapamiento = await _context.Turnos.AnyAsync(t =>
                t.PeluqueroId == dto.PeluqueroId &&
                t.EstadoId != (int)EstadoTurnoEnum.Cancelado &&
                fechaInicio < t.FechaHoraFin &&
                fechaFin > t.FechaHoraInicio);

            if (existeSolapamiento)
                return ApiResponse<TurnoResponseDto>.Falla("El peluquero ya posee otro turno asignado en ese rango horario.");

            var turno = new Turno
            {
                ClienteId = dto.ClienteId,
                PeluqueroId = dto.PeluqueroId,
                FechaHoraInicio = fechaInicio,
                FechaHoraFin = fechaFin,
                EstadoId = (int)EstadoTurnoEnum.Pendiente,
                MontoTotalEstimado = montoTotalEstimado,
                Observaciones = dto.Observaciones,
                CreatedByUserId = usuarioId
            };

            await _context.Turnos.AddAsync(turno);
            await _unitOfWork.SaveChangesAsync();

            foreach (var serv in servicios)
            {
                var ts = new TurnoServicio
                {
                    TurnoId = turno.Id,
                    ServicioId = serv.Id
                };
                SetOptionalProp(ts, "PrecioAcordado", serv.Precio);
                SetOptionalProp(ts, "Precio", serv.Precio);
                await _context.TurnoServicios.AddAsync(ts);
            }

            await _context.HistorialEstadoTurnos.AddAsync(new HistorialEstadoTurno
            {
                TurnoId = turno.Id,
                EstadoAnteriorId = (int)EstadoTurnoEnum.Pendiente,
                EstadoNuevoId = (int)EstadoTurnoEnum.Pendiente,
                FechaCambio = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Observaciones = "Alta inicial de reserva de turno."
            });

            await _unitOfWork.SaveChangesAsync();
            return await GetByIdAsync(turno.Id);
        }

        public async Task<ApiResponse<bool>> CambiarEstadoAsync(int id, CambiarEstadoTurnoDto dto, int usuarioId = 1)
        {
            var turno = await _context.Turnos.FirstOrDefaultAsync(t => t.Id == id);
            if (turno == null)
                return ApiResponse<bool>.Falla("El turno solicitado no existe.");

            int estadoAnterior = turno.EstadoId;
            int nuevoEstado = dto.NuevoEstadoId > 0 ? dto.NuevoEstadoId : (int)EstadoTurnoEnum.Confirmado;

            turno.EstadoId = nuevoEstado;

            if (dto.MotivoCancelacionId.HasValue)
                turno.MotivoCancelacionId = dto.MotivoCancelacionId.Value;

            await _context.HistorialEstadoTurnos.AddAsync(new HistorialEstadoTurno
            {
                TurnoId = turno.Id,
                EstadoAnteriorId = estadoAnterior,
                EstadoNuevoId = nuevoEstado,
                FechaCambio = DateTime.UtcNow,
                UsuarioId = usuarioId,
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? "Cambio de estado del turno." : dto.Observaciones
            });

            _context.Turnos.Update(turno);
            await _unitOfWork.SaveChangesAsync();

            // GANCHO AUTOMÁTICO DE GABINETE: Si el turno pasa a Completado por primera vez, descuenta insumos de la receta
            if (estadoAnterior != (int)EstadoTurnoEnum.Completado && nuevoEstado == (int)EstadoTurnoEnum.Completado)
            {
                await _recetaServicioService.AplicarConsumoGabinetePorTurnoAsync(turno.Id, usuarioId);
            }

            return ApiResponse<bool>.Exito(true, "Estado del turno actualizado correctamente.");
        }

        public async Task<ApiResponse<IEnumerable<string>>> ObtenerHorariosDisponiblesAsync(int peluqueroId, DateTime fecha, List<int>? serviciosIds = null)
        {
            var peluquero = await _context.Peluqueros.FindAsync(peluqueroId);
            if (peluquero == null)
                return ApiResponse<IEnumerable<string>>.Falla("El peluquero especificado no existe.");

            int duracionTotalMinutos = 30;
            if (serviciosIds != null && serviciosIds.Any())
            {
                var servicios = await _context.Servicios
                    .Where(s => serviciosIds.Contains(s.Id))
                    .AsNoTracking()
                    .ToListAsync();

                if (servicios.Any())
                    duracionTotalMinutos = servicios.Sum(s => s.DuracionMinutos > 0 ? s.DuracionMinutos : 30);
            }

            int diaSemana = (int)fecha.DayOfWeek;
            var horariosPeluquero = await _context.HorariosLaborales
                .Where(h => h.PeluqueroId == peluqueroId)
                .AsNoTracking()
                .ToListAsync();

            var horarioDia = horariosPeluquero.FirstOrDefault(h => (int)h.DiaSemana == diaSemana);

            TimeSpan horaApertura = new TimeSpan(9, 0, 0);
            TimeSpan horaCierre = new TimeSpan(20, 0, 0);
            TimeSpan? descansoInicio = null;
            TimeSpan? descansoFin = null;

            if (horarioDia != null)
            {
                bool activo = (bool?)(GetOptionalProp(horarioDia, new[] { "Activo", "EsLaborable" })) ?? true;
                if (!activo)
                    return ApiResponse<IEnumerable<string>>.Exito(Enumerable.Empty<string>(), "El profesional no atiende en el día seleccionado.");

                horaApertura = horarioDia.HoraInicio;
                horaCierre = horarioDia.HoraFin;
                descansoInicio = GetTimeSpanProp(horarioDia, new[] { "HoraInicioDescanso", "DescansoInicio" });
                descansoFin = GetTimeSpanProp(horarioDia, new[] { "HoraFinDescanso", "DescansoFin" });
            }

            var inicioDia = fecha.Date;
            var finDia = inicioDia.AddDays(1).AddTicks(-1);

            var turnosOcupados = await _context.Turnos
                .Where(t => t.PeluqueroId == peluqueroId
                         && t.FechaHoraInicio >= inicioDia
                         && t.FechaHoraInicio <= finDia
                         && t.EstadoId != (int)EstadoTurnoEnum.Cancelado)
                .Select(t => new { t.FechaHoraInicio, t.FechaHoraFin })
                .AsNoTracking()
                .ToListAsync();

            var bloqueosPeluquero = await _context.BloqueosHorario
                .Where(b => b.PeluqueroId == peluqueroId)
                .AsNoTracking()
                .ToListAsync();

            var slotsDisponibles = new List<string>();
            var cursor = fecha.Date.Add(horaApertura);
            var limiteJornada = fecha.Date.Add(horaCierre);

            while (cursor.AddMinutes(duracionTotalMinutos) <= limiteJornada)
            {
                var slotInicio = cursor;
                var slotFin = cursor.AddMinutes(duracionTotalMinutos);

                bool enDescanso = false;
                if (descansoInicio.HasValue && descansoFin.HasValue)
                {
                    var dInicio = fecha.Date.Add(descansoInicio.Value);
                    var dFin = fecha.Date.Add(descansoFin.Value);
                    if (slotInicio < dFin && slotFin > dInicio)
                        enDescanso = true;
                }

                bool solapaTurno = turnosOcupados.Any(t => slotInicio < t.FechaHoraFin && slotFin > t.FechaHoraInicio);
                bool solapaBloqueo = bloqueosPeluquero.Any(b => slotInicio < b.FechaFin && slotFin > b.FechaInicio);

                if (!enDescanso && !solapaTurno && !solapaBloqueo)
                {
                    slotsDisponibles.Add(slotInicio.ToString("HH:mm"));
                }

                cursor = cursor.AddMinutes(30);
            }

            return ApiResponse<IEnumerable<string>>.Exito(slotsDisponibles);
        }

        public async Task<ApiResponse<IEnumerable<TurnoResponseDto>>> ObtenerTurnosPorClienteAsync(int clienteId)
        {
            var turnos = await _context.Turnos
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.TurnoServicios)
                    .ThenInclude(ts => ts.Servicio)
                .Where(t => t.ClienteId == clienteId)
                .OrderByDescending(t => t.FechaHoraInicio)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<TurnoResponseDto>>(turnos);
            return ApiResponse<IEnumerable<TurnoResponseDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<IEnumerable<object>>> ObtenerHistorialEstadoTurnoAsync(int turnoId)
        {
            var historial = await _context.HistorialEstadoTurnos
                .Where(h => h.TurnoId == turnoId)
                .OrderBy(h => h.FechaCambio)
                .AsNoTracking()
                .Select(h => (object)new
                {
                    h.Id,
                    h.TurnoId,
                    h.EstadoAnteriorId,
                    EstadoAnterior = h.EstadoAnteriorId.HasValue ? ((EstadoTurnoEnum)h.EstadoAnteriorId.Value).ToString() : "N/A",
                    h.EstadoNuevoId,
                    EstadoNuevo = ((EstadoTurnoEnum)h.EstadoNuevoId).ToString(),
                    h.FechaCambio,
                    h.UsuarioId,
                    h.Observaciones
                })
                .ToListAsync();

            return ApiResponse<IEnumerable<object>>.Exito(historial);
        }

        private static object? GetOptionalProp(object target, string[] names)
        {
            var type = target.GetType();
            foreach (var name in names)
            {
                var prop = type.GetProperty(name);
                if (prop != null && prop.CanRead)
                    return prop.GetValue(target);
            }
            return null;
        }

        private static TimeSpan? GetTimeSpanProp(object target, string[] names)
        {
            var val = GetOptionalProp(target, names);
            if (val is TimeSpan ts) return ts;
            if (val is TimeOnly to) return to.ToTimeSpan();
            return null;
        }

        private static void SetOptionalProp(object target, string name, object? value)
        {
            if (value == null) return;
            var prop = target.GetType().GetProperty(name);
            if (prop != null && prop.CanWrite)
            {
                try { prop.SetValue(target, value); } catch { }
            }
        }
    }
}