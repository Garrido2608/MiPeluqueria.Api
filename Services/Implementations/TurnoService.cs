using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Exceptions;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class TurnoService : ITurnoService
    {
        private const string AlertaCruceMuerteWebhook =
            " | [ALERTA CRÍTICA: Seña acreditada tras expiración con horario ocupado. Reprogramar o acreditar saldo a favor]";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IRecetaServicioService _recetaServicioService;
        private readonly IConfiguracionSalonService _configuracionSalonService;

        public TurnoService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IRecetaServicioService recetaServicioService,
            IConfiguracionSalonService configuracionSalonService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _recetaServicioService = recetaServicioService;
            _configuracionSalonService = configuracionSalonService;
        }

        public async Task<ApiResponse<PagedResult<TurnoResponseDto>>> GetAllAsync(
            PaginationFilterDto filter,
            DateTime? fecha = null,
            int? peluqueroId = null,
            int? estadoId = null,
            int? clienteId = null,
            CancellationToken ct = default)
        {
            var effectiveFilter = filter ?? new PaginationFilterDto();
            var (items, totalRecords) = await _unitOfWork.Turnos.GetPagedAsync(
                effectiveFilter,
                fecha,
                peluqueroId,
                estadoId,
                clienteId,
                ct);

            var dtos = _mapper.Map<List<TurnoResponseDto>>(items);

            var paged = new PagedResult<TurnoResponseDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = effectiveFilter.PageNumber,
                PageSize = effectiveFilter.PageSize
            };

            return ApiResponse<PagedResult<TurnoResponseDto>>.Exito(paged);
        }

        public Task<ApiResponse<PagedResult<TurnoResponseDto>>> GetAllAsync(
            DateTime? fecha = null,
            int? peluqueroId = null,
            CancellationToken ct = default)
        {
            var filter = new PaginationFilterDto
            {
                PageNumber = 1,
                PageSize = fecha.HasValue ? 100 : 15,
                SortDescending = !fecha.HasValue
            };

            return GetAllAsync(filter, fecha, peluqueroId, null, null, ct);
        }

        public async Task<ApiResponse<TurnoResponseDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var turno = await _unitOfWork.Turnos.GetByIdConDetallesAsync(id, tracking: false, ct);
            if (turno == null)
                return ApiResponse<TurnoResponseDto>.Falla("El turno solicitado no existe.");

            return ApiResponse<TurnoResponseDto>.Exito(_mapper.Map<TurnoResponseDto>(turno));
        }

        public async Task<ApiResponse<TurnoResponseDto>> CreateAsync(
            CrearTurnoDto dto,
            int usuarioId = 1,
            CancellationToken ct = default)
        {
            var cliente = await _unitOfWork.Clientes.GetByIdWithDetailsAsync(dto.ClienteId, tracking: false, ct);
            if (cliente == null || !cliente.Activo)
                return ApiResponse<TurnoResponseDto>.Falla("El cliente especificado no existe o se encuentra inactivo.");

            var peluquero = await _unitOfWork.Peluqueros.GetByIdWithDetailsAsync(dto.PeluqueroId, tracking: false, ct);
            if (peluquero == null || !peluquero.Activo)
                return ApiResponse<TurnoResponseDto>.Falla("El profesional especificado no existe o se encuentra inactivo.");

            var serviciosIds = (dto.ServiciosIds ?? new List<int>()).Distinct().ToList();
            if (serviciosIds.Count == 0)
                return ApiResponse<TurnoResponseDto>.Falla("Debe seleccionar al menos un servicio válido para agendar el turno.");

            var config = await _configuracionSalonService.ObtenerConfiguracionAsync(ct);
            int intervaloDefault = config.IntervaloTurnosMinutos > 0 ? config.IntervaloTurnosMinutos : 30;

            // Carga en lote en 1 sola consulta SQL (05_BUSINESS_RULES.md Sección 2.1 / BUG-06)
            var serviciosDict = await _unitOfWork.Servicios.GetByIdsDictionaryAsync(serviciosIds, tracking: false, ct);
            var serviciosSeleccionados = new List<Servicio>(serviciosIds.Count);

            foreach (var servId in serviciosIds)
            {
                if (!serviciosDict.TryGetValue(servId, out var serv) || !serv.Activo)
                    return ApiResponse<TurnoResponseDto>.Falla($"El servicio con ID {servId} no existe o se encuentra inactivo.");

                serviciosSeleccionados.Add(serv);
            }

            int duracionTotal = serviciosSeleccionados.Sum(s => s.DuracionMinutos > 0 ? s.DuracionMinutos : intervaloDefault);
            decimal montoTotalEstimado = serviciosSeleccionados.Sum(s => s.Precio);

            bool requiereSena = config.PorcentajeSenaReserva > 0m && montoTotalEstimado > 0m;
            decimal montoSena = requiereSena
                ? Math.Round(montoTotalEstimado * (config.PorcentajeSenaReserva / 100m), 2)
                : 0m;

            var fechaInicio = dto.FechaHoraInicio;
            var fechaFin = fechaInicio.AddMinutes(duracionTotal);

            var validacionJornada = await ValidarJornadaLaboralAsync(dto.PeluqueroId, fechaInicio, fechaFin, ct);
            if (validacionJornada != null)
                return ApiResponse<TurnoResponseDto>.Falla(validacionJornada);

            int turnoCreadoId = 0;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    bool solapaBloqueo = await _unitOfWork.BloqueosHorarios.ExisteSolapamientoAsync(
                        dto.PeluqueroId,
                        fechaInicio,
                        fechaFin,
                        null,
                        token);

                    if (solapaBloqueo)
                        throw new BusinessRuleException("El profesional posee un bloqueo horario registrado en ese rango.");

                    bool solapaTurno = await _unitOfWork.Turnos.ExisteSolapamientoAsync(
                        dto.PeluqueroId,
                        fechaInicio,
                        fechaFin,
                        null,
                        token);

                    if (solapaTurno)
                        throw new BusinessRuleException("El profesional ya posee otro turno asignado en ese rango horario.");

                    var turno = new Turno
                    {
                        ClienteId = dto.ClienteId,
                        PeluqueroId = dto.PeluqueroId,
                        FechaHoraInicio = fechaInicio,
                        FechaHoraFin = fechaFin,
                        EstadoId = (int)EstadoTurnoEnum.Pendiente,
                        MontoTotalEstimado = montoTotalEstimado,
                        RequiereSena = requiereSena,
                        MontoSena = montoSena,
                        SenaPagada = false,
                        Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
                        CreatedByUserId = usuarioId
                    };

                    await _unitOfWork.Turnos.AddAsync(turno, token);
                    await _unitOfWork.SaveChangesAsync(token);

                    var turnoServicios = serviciosSeleccionados.Select(serv => new TurnoServicio
                    {
                        TurnoId = turno.Id,
                        ServicioId = serv.Id,
                        PrecioHistorico = serv.Precio,
                        DuracionMinutosHistorica = serv.DuracionMinutos > 0 ? serv.DuracionMinutos : intervaloDefault,
                        CreatedByUserId = usuarioId
                    }).ToList();

                    await _unitOfWork.Turnos.AddTurnoServiciosRangeAsync(turnoServicios, token);

                    await _unitOfWork.Turnos.AddHistorialEstadoAsync(new HistorialEstadoTurno
                    {
                        TurnoId = turno.Id,
                        EstadoAnteriorId = null,
                        EstadoNuevoId = (int)EstadoTurnoEnum.Pendiente,
                        FechaCambio = DateTime.UtcNow,
                        UsuarioId = usuarioId,
                        Observaciones = "Alta inicial de reserva de turno.",
                        CreatedByUserId = usuarioId
                    }, token);

                    await _unitOfWork.SaveChangesAsync(token);
                    turnoCreadoId = turno.Id;
                }, IsolationLevel.Serializable, ct);
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<TurnoResponseDto>.Falla(ex.Message);
            }

            return await GetByIdAsync(turnoCreadoId, ct);
        }

        public async Task<ApiResponse<TurnoResponseDto>> ReprogramarAsync(
            int id,
            ReprogramarTurnoDto dto,
            int usuarioId = 1,
            string? rolEjecutor = null,
            CancellationToken ct = default)
        {
            var turno = await _unitOfWork.Turnos.GetByIdConDetallesAsync(id, tracking: true, ct);
            if (turno == null)
                return ApiResponse<TurnoResponseDto>.Falla("El turno solicitado no existe.");

            if (turno.EstadoId is (int)EstadoTurnoEnum.Completado or (int)EstadoTurnoEnum.Cancelado or (int)EstadoTurnoEnum.NoAsistio)
            {
                return ApiResponse<TurnoResponseDto>.Falla(
                    "No se puede reprogramar un turno finalizado, cancelado o registrado como inasistencia.");
            }

            var peluquero = await _unitOfWork.Peluqueros.GetByIdWithDetailsAsync(dto.PeluqueroId, tracking: false, ct);
            if (peluquero == null || !peluquero.Activo)
                return ApiResponse<TurnoResponseDto>.Falla("El profesional seleccionado no existe o se encuentra inactivo.");

            var config = await _configuracionSalonService.ObtenerConfiguracionAsync(ct);

            // Regla 2.5: Ventana de protección en autogestión de Clientes
            if (string.Equals(rolEjecutor, "Cliente", StringComparison.OrdinalIgnoreCase))
            {
                var ahoraLocalSalon = ObtenerHoraLocalSalon(config.TimeZoneId);
                if ((turno.FechaHoraInicio - ahoraLocalSalon).TotalHours < config.HorasAnticipacionCancelacion)
                {
                    return ApiResponse<TurnoResponseDto>.Falla(
                        $"Solo es posible reprogramar online con al menos {config.HorasAnticipacionCancelacion} horas de anticipación.");
                }
            }

            int duracionHistorica = turno.TurnoServicios.Sum(ts => ts.DuracionMinutosHistorica);
            int duracionMinutos = duracionHistorica > 0
                ? duracionHistorica
                : (int)Math.Max(
                    config.IntervaloTurnosMinutos > 0 ? config.IntervaloTurnosMinutos : 30,
                    (turno.FechaHoraFin - turno.FechaHoraInicio).TotalMinutes);

            var nuevaFechaInicio = dto.NuevaFechaHoraInicio;
            var nuevaFechaFin = nuevaFechaInicio.AddMinutes(duracionMinutos);

            var validacionJornada = await ValidarJornadaLaboralAsync(dto.PeluqueroId, nuevaFechaInicio, nuevaFechaFin, ct);
            if (validacionJornada != null)
                return ApiResponse<TurnoResponseDto>.Falla(validacionJornada);

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var turnoTrackeado = await _unitOfWork.Turnos.GetByIdConDetallesAsync(id, tracking: true, token)
                        ?? throw new BusinessRuleException("El turno solicitado no existe.");

                    bool solapaBloqueo = await _unitOfWork.BloqueosHorarios.ExisteSolapamientoAsync(
                        dto.PeluqueroId,
                        nuevaFechaInicio,
                        nuevaFechaFin,
                        null,
                        token);

                    if (solapaBloqueo)
                        throw new BusinessRuleException("El profesional posee un bloqueo horario registrado en el nuevo rango.");

                    bool solapaTurno = await _unitOfWork.Turnos.ExisteSolapamientoAsync(
                        dto.PeluqueroId,
                        nuevaFechaInicio,
                        nuevaFechaFin,
                        turnoIdExcluir: id,
                        token);

                    if (solapaTurno)
                        throw new BusinessRuleException("El profesional ya posee otro turno asignado en el nuevo rango horario.");

                    turnoTrackeado.PeluqueroId = dto.PeluqueroId;
                    turnoTrackeado.FechaHoraInicio = nuevaFechaInicio;
                    turnoTrackeado.FechaHoraFin = nuevaFechaFin;
                    turnoTrackeado.UpdatedByUserId = usuarioId;

                    if (!string.IsNullOrWhiteSpace(dto.Observaciones))
                        turnoTrackeado.Observaciones = Truncar(dto.Observaciones.Trim(), 500);

                    _unitOfWork.Turnos.Update(turnoTrackeado);

                    await _unitOfWork.Turnos.AddHistorialEstadoAsync(new HistorialEstadoTurno
                    {
                        TurnoId = turnoTrackeado.Id,
                        EstadoAnteriorId = turnoTrackeado.EstadoId,
                        EstadoNuevoId = turnoTrackeado.EstadoId,
                        FechaCambio = DateTime.UtcNow,
                        UsuarioId = usuarioId,
                        Observaciones = Truncar(
                            string.IsNullOrWhiteSpace(dto.Observaciones)
                                ? $"Turno reprogramado para {nuevaFechaInicio:dd/MM/yyyy HH:mm}."
                                : dto.Observaciones.Trim(),
                            300),
                        CreatedByUserId = usuarioId
                    }, token);

                    await _unitOfWork.SaveChangesAsync(token);
                }, IsolationLevel.Serializable, ct);
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<TurnoResponseDto>.Falla(ex.Message);
            }

            return await GetByIdAsync(id, ct);
        }

        public async Task<ApiResponse<bool>> CambiarEstadoAsync(
            int id,
            CambiarEstadoTurnoDto dto,
            int usuarioId = 1,
            string? rolEjecutor = null,
            CancellationToken ct = default)
        {
            var turno = await _unitOfWork.Turnos.GetByIdConDetallesAsync(id, tracking: true, ct);
            if (turno == null)
                return ApiResponse<bool>.Falla("El turno solicitado no existe.");

            int estadoAnterior = turno.EstadoId;
            int nuevoEstado = dto.NuevoEstadoId;

            if (!Enum.IsDefined(typeof(EstadoTurnoEnum), nuevoEstado))
                return ApiResponse<bool>.Falla("El nuevo estado de turno especificado no es válido.");

            if (estadoAnterior == nuevoEstado)
                return ApiResponse<bool>.Exito(true, "El turno ya se encuentra en el estado solicitado.");

            if (!EsTransicionManualValida(estadoAnterior, nuevoEstado))
            {
                return ApiResponse<bool>.Falla(
                    $"Transición de estado inválida: no se permite pasar manualmente de '{(EstadoTurnoEnum)estadoAnterior}' a '{(EstadoTurnoEnum)nuevoEstado}'.");
            }

            if (string.Equals(rolEjecutor, "Cliente", StringComparison.OrdinalIgnoreCase))
            {
                if (nuevoEstado != (int)EstadoTurnoEnum.Cancelado)
                    return ApiResponse<bool>.Falla("Los clientes únicamente pueden solicitar la cancelación de su propio turno.");

                var config = await _configuracionSalonService.ObtenerConfiguracionAsync(ct);
                var ahoraLocalSalon = ObtenerHoraLocalSalon(config.TimeZoneId);

                if ((turno.FechaHoraInicio - ahoraLocalSalon).TotalHours < config.HorasAnticipacionCancelacion)
                {
                    return ApiResponse<bool>.Falla(
                        $"Solo es posible cancelar online con al menos {config.HorasAnticipacionCancelacion} horas de anticipación.");
                }
            }

            if (nuevoEstado == (int)EstadoTurnoEnum.Cancelado)
            {
                if (!dto.MotivoCancelacionId.HasValue || dto.MotivoCancelacionId.Value <= 0)
                    return ApiResponse<bool>.Falla("Debe especificar el motivo de cancelación del turno.");

                var motivo = await _unitOfWork.Catalogos.GetMotivoCancelacionByIdAsync(dto.MotivoCancelacionId.Value, ct);
                if (motivo == null)
                    return ApiResponse<bool>.Falla("El motivo de cancelación seleccionado no existe.");
            }

            List<string>? warnings = null;

            await _unitOfWork.ExecuteInTransactionAsync(async token =>
            {
                turno.EstadoId = nuevoEstado;
                turno.UpdatedByUserId = usuarioId;

                if (nuevoEstado == (int)EstadoTurnoEnum.Cancelado && dto.MotivoCancelacionId.HasValue)
                    turno.MotivoCancelacionId = dto.MotivoCancelacionId.Value;

                _unitOfWork.Turnos.Update(turno);

                await _unitOfWork.Turnos.AddHistorialEstadoAsync(new HistorialEstadoTurno
                {
                    TurnoId = turno.Id,
                    EstadoAnteriorId = estadoAnterior,
                    EstadoNuevoId = nuevoEstado,
                    FechaCambio = DateTime.UtcNow,
                    UsuarioId = usuarioId,
                    Observaciones = Truncar(
                        string.IsNullOrWhiteSpace(dto.Observaciones)
                            ? $"Cambio de estado a {(EstadoTurnoEnum)nuevoEstado}."
                            : dto.Observaciones.Trim(),
                        300),
                    CreatedByUserId = usuarioId
                }, token);

                await _unitOfWork.SaveChangesAsync(token);

                // Gancho automático de consumo de gabinete idempotente y no bloqueante (05_BUSINESS_RULES.md Sección 3.2)
                if (nuevoEstado == (int)EstadoTurnoEnum.Completado)
                {
                    var consumoRes = await _recetaServicioService.AplicarConsumoGabinetePorTurnoAsync(turno.Id, usuarioId, token);
                    if (consumoRes.Warnings != null && consumoRes.Warnings.Count > 0)
                        warnings = consumoRes.Warnings;
                }
            }, ct: ct);

            return ApiResponse<bool>.Ok(true, "Estado del turno actualizado correctamente.", warnings);
        }

        public async Task<ApiResponse<bool>> ProcesarPagoSenaWebhookAsync(
            int turnoId,
            WebhookPagoNotificacionDto payload,
            string proveedor = "MercadoPago",
            CancellationToken ct = default)
        {
            string referenciaPago = !string.IsNullOrWhiteSpace(payload.Id)
                ? payload.Id.Trim()
                : (!string.IsNullOrWhiteSpace(payload.ExternalReference)
                    ? payload.ExternalReference.Trim()
                    : $"WEBHOOK_{proveedor}_{turnoId}");

            ApiResponse<bool>? resultado = null;

            try
            {
                await _unitOfWork.ExecuteInTransactionAsync(async token =>
                {
                    var turno = await _unitOfWork.Turnos.GetByIdConDetallesAsync(turnoId, tracking: true, token)
                        ?? throw new BusinessRuleException($"El turno #{turnoId} referenciado en el webhook no existe.");

                    // 1. Regla de Idempotencia Estricta (05_BUSINESS_RULES.md Sección 2.4.2)
                    if (turno.SenaPagada &&
                        string.Equals(turno.ReferenciaPagoExterno, referenciaPago, StringComparison.OrdinalIgnoreCase))
                    {
                        resultado = ApiResponse<bool>.Ok(
                            true,
                            "Notificación de pago ya procesada previamente de forma idempotente.");
                        return;
                    }

                    // 2. Transacción Compensatoria en el "Cruce de la Muerte" (Webhook vs. Worker de Expiración)
                    if (turno.EstadoId == (int)EstadoTurnoEnum.Cancelado)
                    {
                        bool solapaTurno = await _unitOfWork.Turnos.ExisteSolapamientoAsync(
                            turno.PeluqueroId,
                            turno.FechaHoraInicio,
                            turno.FechaHoraFin,
                            turnoIdExcluir: turno.Id,
                            token);

                        bool solapaBloqueo = await _unitOfWork.BloqueosHorarios.ExisteSolapamientoAsync(
                            turno.PeluqueroId,
                            turno.FechaHoraInicio,
                            turno.FechaHoraFin,
                            null,
                            token);

                        if (!solapaTurno && !solapaBloqueo)
                        {
                            turno.EstadoId = (int)EstadoTurnoEnum.Confirmado;
                            turno.MotivoCancelacionId = null;
                            turno.SenaPagada = true;
                            turno.FechaPagoSena = DateTime.UtcNow;
                            if (payload.TransactionAmount.HasValue && payload.TransactionAmount.Value > 0m)
                                turno.MontoSena = payload.TransactionAmount.Value;
                            turno.ReferenciaPagoExterno = Truncar(referenciaPago, 150);

                            _unitOfWork.Turnos.Update(turno);

                            await _unitOfWork.Turnos.AddHistorialEstadoAsync(new HistorialEstadoTurno
                            {
                                TurnoId = turno.Id,
                                EstadoAnteriorId = (int)EstadoTurnoEnum.Cancelado,
                                EstadoNuevoId = (int)EstadoTurnoEnum.Confirmado,
                                FechaCambio = DateTime.UtcNow,
                                UsuarioId = null,
                                Observaciones = Truncar(
                                    $"Reactivación compensatoria automática vía Webhook ({proveedor}). Seña acreditada (Ref: {referenciaPago}).",
                                    300)
                            }, token);

                            await _unitOfWork.SaveChangesAsync(token);

                            resultado = ApiResponse<bool>.Ok(
                                true,
                                "Seña acreditada y turno reactivado exitosamente mediante transacción compensatoria.");
                            return;
                        }

                        // El horario fue ocupado en el ínterin: conservar Cancelado pero registrar seña y alerta crítica
                        turno.SenaPagada = true;
                        turno.FechaPagoSena = DateTime.UtcNow;
                        if (payload.TransactionAmount.HasValue && payload.TransactionAmount.Value > 0m)
                            turno.MontoSena = payload.TransactionAmount.Value;
                        turno.ReferenciaPagoExterno = Truncar(referenciaPago, 150);
                        turno.Observaciones = Truncar((turno.Observaciones ?? string.Empty) + AlertaCruceMuerteWebhook, 500);

                        _unitOfWork.Turnos.Update(turno);

                        await _unitOfWork.Turnos.AddHistorialEstadoAsync(new HistorialEstadoTurno
                        {
                            TurnoId = turno.Id,
                            EstadoAnteriorId = (int)EstadoTurnoEnum.Cancelado,
                            EstadoNuevoId = (int)EstadoTurnoEnum.Cancelado,
                            FechaCambio = DateTime.UtcNow,
                            UsuarioId = null,
                            Observaciones = Truncar(
                                $"ALERTA CRÍTICA Webhook ({proveedor}): Seña acreditada tras expiración con horario ocupado (Ref: {referenciaPago}).",
                                300)
                        }, token);

                        await _unitOfWork.SaveChangesAsync(token);

                        resultado = ApiResponse<bool>.Ok(
                            true,
                            "Seña acreditada sobre turno expirado con horario ocupado. Se registró alerta crítica de reprogramación/saldo a favor.",
                            new List<string>
                            {
                                $"Turno #{turno.Id}: Seña acreditada tras expiración con horario ocupado. Reprogramar o acreditar saldo a favor."
                            });
                        return;
                    }

                    // 3. Acreditación normal de seña sobre turno activo
                    int estadoPrevio = turno.EstadoId;
                    int estadoDestino = turno.EstadoId == (int)EstadoTurnoEnum.Pendiente
                        ? (int)EstadoTurnoEnum.Confirmado
                        : turno.EstadoId;

                    turno.EstadoId = estadoDestino;
                    turno.SenaPagada = true;
                    turno.FechaPagoSena = DateTime.UtcNow;
                    if (payload.TransactionAmount.HasValue && payload.TransactionAmount.Value > 0m)
                        turno.MontoSena = payload.TransactionAmount.Value;
                    turno.ReferenciaPagoExterno = Truncar(referenciaPago, 150);

                    _unitOfWork.Turnos.Update(turno);

                    await _unitOfWork.Turnos.AddHistorialEstadoAsync(new HistorialEstadoTurno
                    {
                        TurnoId = turno.Id,
                        EstadoAnteriorId = estadoPrevio,
                        EstadoNuevoId = estadoDestino,
                        FechaCambio = DateTime.UtcNow,
                        UsuarioId = null,
                        Observaciones = Truncar(
                            $"Seña acreditada automáticamente vía Webhook ({proveedor} - Ref: {referenciaPago}).",
                            300)
                    }, token);

                    await _unitOfWork.SaveChangesAsync(token);

                    resultado = ApiResponse<bool>.Ok(true, "Seña acreditada y turno confirmado correctamente.");
                }, IsolationLevel.Serializable, ct);
            }
            catch (BusinessRuleException ex)
            {
                return ApiResponse<bool>.Falla(ex.Message);
            }

            return resultado ?? ApiResponse<bool>.Ok(true);
        }

        public async Task<ApiResponse<DisponibilidadHorarioResponseDto>> ObtenerHorariosDisponiblesAsync(
            int peluqueroId,
            DateTime fecha,
            List<int>? serviciosIds = null,
            CancellationToken ct = default)
        {
            var peluquero = await _unitOfWork.Peluqueros.GetByIdWithDetailsAsync(peluqueroId, tracking: false, ct);
            if (peluquero == null || !peluquero.Activo)
            {
                return ApiResponse<DisponibilidadHorarioResponseDto>.Falla(
                    "El profesional especificado no existe o se encuentra inactivo.");
            }

            var config = await _configuracionSalonService.ObtenerConfiguracionAsync(ct);
            int intervaloMinutos = config.IntervaloTurnosMinutos > 0 ? config.IntervaloTurnosMinutos : 30;
            DateTime ahoraLocalSalon = ObtenerHoraLocalSalon(config.TimeZoneId);

            int duracionTotalMinutos = intervaloMinutos;
            if (serviciosIds != null && serviciosIds.Count > 0)
            {
                var idsUnicos = serviciosIds.Distinct().ToList();
                var serviciosDict = await _unitOfWork.Servicios.GetByIdsDictionaryAsync(idsUnicos, tracking: false, ct);
                var serviciosActivos = idsUnicos
                    .Where(id => serviciosDict.TryGetValue(id, out var s) && s.Activo)
                    .Select(id => serviciosDict[id])
                    .ToList();

                if (serviciosActivos.Count > 0)
                {
                    duracionTotalMinutos = serviciosActivos.Sum(
                        s => s.DuracionMinutos > 0 ? s.DuracionMinutos : intervaloMinutos);
                }
            }

            var response = new DisponibilidadHorarioResponseDto
            {
                PeluqueroId = peluquero.Id,
                PeluqueroNombre = $"{peluquero.Nombre} {peluquero.Apellido}".Trim(),
                Fecha = fecha.Date,
                IntervaloMinutos = intervaloMinutos,
                DuracionTotalMinutos = duracionTotalMinutos,
                HorariosDisponibles = new List<string>(),
                Slots = new List<SlotHorarioDisponibleDto>()
            };

            if (fecha.Date < ahoraLocalSalon.Date)
            {
                return ApiResponse<DisponibilidadHorarioResponseDto>.Exito(
                    response,
                    "No existen horarios disponibles para fechas pasadas.");
            }

            var horarioDia = await _unitOfWork.Peluqueros.GetHorarioDiaAsync(peluqueroId, fecha.DayOfWeek, ct);
            if (horarioDia != null && !horarioDia.Activo)
            {
                return ApiResponse<DisponibilidadHorarioResponseDto>.Exito(
                    response,
                    "El profesional no atiende en el día seleccionado.");
            }

            TimeSpan horaApertura = horarioDia?.HoraInicio ?? new TimeSpan(9, 0, 0);
            TimeSpan horaCierre = horarioDia?.HoraFin ?? new TimeSpan(20, 0, 0);
            TimeSpan? descansoInicio = horarioDia?.HoraInicioDescanso;
            TimeSpan? descansoFin = horarioDia?.HoraFinDescanso;

            var inicioDia = fecha.Date;
            var finDia = inicioDia.AddDays(1);

            var turnosOcupados = await _unitOfWork.Turnos.GetTurnosActivosEnRangoAsync(
                peluqueroId,
                inicioDia,
                finDia,
                ct);

            var bloqueosPeluquero = await _unitOfWork.BloqueosHorarios.GetByPeluqueroIdAsync(peluqueroId, null, ct);
            var bloqueosDia = bloqueosPeluquero
                .Where(b => b.FechaInicio < finDia && b.FechaFin > inicioDia)
                .ToList();

            var cursor = inicioDia.Add(horaApertura);
            var limiteJornada = inicioDia.Add(horaCierre);

            while (cursor.AddMinutes(duracionTotalMinutos) <= limiteJornada)
            {
                var slotInicio = cursor;
                var slotFin = cursor.AddMinutes(duracionTotalMinutos);

                // a) Horario pasado respecto a la hora local del salón
                bool esPasado = inicioDia == ahoraLocalSalon.Date && slotInicio <= ahoraLocalSalon;

                // b) Descanso de mediodía
                bool enDescanso = false;
                if (descansoInicio.HasValue && descansoFin.HasValue)
                {
                    var dInicio = inicioDia.Add(descansoInicio.Value);
                    var dFin = inicioDia.Add(descansoFin.Value);
                    if (slotInicio < dFin && slotFin > dInicio)
                        enDescanso = true;
                }

                // c) Bloqueo horario
                bool solapaBloqueo = bloqueosDia.Any(b => slotInicio < b.FechaFin && slotFin > b.FechaInicio);

                // d) Turno ocupado activo (Estado != Cancelado && Estado != NoAsistio)
                bool solapaTurno = turnosOcupados.Any(t => slotInicio < t.FechaHoraFin && slotFin > t.FechaHoraInicio);

                if (!esPasado && !enDescanso && !solapaBloqueo && !solapaTurno)
                {
                    string inicioTexto = slotInicio.ToString("HH:mm");
                    string finTexto = slotFin.ToString("HH:mm");

                    response.HorariosDisponibles.Add(inicioTexto);
                    response.Slots.Add(new SlotHorarioDisponibleDto
                    {
                        FechaHoraInicio = slotInicio,
                        FechaHoraFin = slotFin,
                        HoraInicioTexto = inicioTexto,
                        HoraFinTexto = finTexto,
                        Disponible = true
                    });
                }

                cursor = cursor.AddMinutes(intervaloMinutos);
            }

            return ApiResponse<DisponibilidadHorarioResponseDto>.Exito(response);
        }

        public async Task<ApiResponse<IReadOnlyList<TurnoResponseDto>>> ObtenerTurnosPorClienteAsync(
            int clienteId,
            CancellationToken ct = default)
        {
            var turnos = await _unitOfWork.Turnos.GetByClienteIdAsync(clienteId, ct);
            var dtos = _mapper.Map<List<TurnoResponseDto>>(turnos);
            return ApiResponse<IReadOnlyList<TurnoResponseDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<IReadOnlyList<HistorialEstadoTurnoResponseDto>>> ObtenerHistorialEstadoTurnoAsync(
            int turnoId,
            CancellationToken ct = default)
        {
            var historial = await _unitOfWork.Turnos.GetHistorialEstadosByTurnoIdAsync(turnoId, ct);

            var dtos = historial.Select(h => new HistorialEstadoTurnoResponseDto
            {
                Id = h.Id,
                TurnoId = h.TurnoId,
                EstadoAnteriorId = h.EstadoAnteriorId,
                EstadoAnteriorNombre = h.EstadoAnterior?.Nombre
                    ?? (h.EstadoAnteriorId.HasValue && Enum.IsDefined(typeof(EstadoTurnoEnum), h.EstadoAnteriorId.Value)
                        ? ((EstadoTurnoEnum)h.EstadoAnteriorId.Value).ToString()
                        : null),
                EstadoNuevoId = h.EstadoNuevoId,
                EstadoNuevoNombre = h.EstadoNuevo?.Nombre
                    ?? (Enum.IsDefined(typeof(EstadoTurnoEnum), h.EstadoNuevoId)
                        ? ((EstadoTurnoEnum)h.EstadoNuevoId).ToString()
                        : string.Empty),
                FechaCambio = h.FechaCambio,
                UsuarioId = h.UsuarioId,
                Observaciones = h.Observaciones
            }).ToList();

            return ApiResponse<IReadOnlyList<HistorialEstadoTurnoResponseDto>>.Exito(dtos);
        }

        private async Task<string?> ValidarJornadaLaboralAsync(
            int peluqueroId,
            DateTime fechaInicio,
            DateTime fechaFin,
            CancellationToken ct)
        {
            var horarioDia = await _unitOfWork.Peluqueros.GetHorarioDiaAsync(peluqueroId, fechaInicio.DayOfWeek, ct);
            if (horarioDia == null)
                return null;

            if (!horarioDia.Activo)
                return "El profesional no atiende en el día seleccionado (día franco).";

            var inicioJornada = fechaInicio.Date.Add(horarioDia.HoraInicio);
            var finJornada = fechaInicio.Date.Add(horarioDia.HoraFin);

            if (fechaInicio < inicioJornada || fechaFin > finJornada)
                return "El horario solicitado se encuentra fuera de la jornada laboral configurada del profesional.";

            if (horarioDia.HoraInicioDescanso.HasValue && horarioDia.HoraFinDescanso.HasValue)
            {
                var inicioDescanso = fechaInicio.Date.Add(horarioDia.HoraInicioDescanso.Value);
                var finDescanso = fechaInicio.Date.Add(horarioDia.HoraFinDescanso.Value);

                if (fechaInicio < finDescanso && fechaFin > inicioDescanso)
                    return "El horario solicitado se solapa con el horario de descanso del profesional.";
            }

            return null;
        }

        private static bool EsTransicionManualValida(int estadoActual, int nuevoEstado) =>
            (EstadoTurnoEnum)estadoActual switch
            {
                EstadoTurnoEnum.Pendiente => nuevoEstado is
                    (int)EstadoTurnoEnum.Confirmado or
                    (int)EstadoTurnoEnum.EnProceso or
                    (int)EstadoTurnoEnum.Cancelado or
                    (int)EstadoTurnoEnum.NoAsistio,

                EstadoTurnoEnum.Confirmado => nuevoEstado is
                    (int)EstadoTurnoEnum.EnProceso or
                    (int)EstadoTurnoEnum.Completado or
                    (int)EstadoTurnoEnum.Cancelado or
                    (int)EstadoTurnoEnum.NoAsistio,

                EstadoTurnoEnum.EnProceso => nuevoEstado is
                    (int)EstadoTurnoEnum.Completado or
                    (int)EstadoTurnoEnum.Cancelado,

                _ => false
            };

        private static DateTime ObtenerHoraLocalSalon(string? timeZoneId)
        {
            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(
                    string.IsNullOrWhiteSpace(timeZoneId) ? "America/Argentina/Cordoba" : timeZoneId);
            }
            catch
            {
                tz = TimeZoneInfo.CreateCustomTimeZone("AR", TimeSpan.FromHours(-3), "Argentina", "Argentina");
            }

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
        }

        private static string Truncar(string valor, int maxLen) =>
            valor.Length <= maxLen ? valor : valor.Substring(0, maxLen);
    }
}
