// filepath: src/app/(dashboard)/recepcion/calendario/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  BloqueoHorarioDto,
  ClienteDto,
  EstadoTurnoEnum,
  HistorialEstadoTurnoDto,
  PeluqueroDto,
  ServicioDto,
  TurnoDto,
} from '@/types/api.types';
import {
  bloqueosService,
  clientesService,
  peluquerosService,
  serviciosService,
  turnosService,
} from '@/services/api.services';
import { useCajaPos, useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import { formatCurrency, formatDateTime } from '@/lib/formatters';
import {
  AsyncStateGuard,
  Badge,
  Button,
  InputField,
  Modal,
} from '@/components/ui';

export default function CalendarioMasterRecepcionPage() {
  const router = useRouter();
  const { enviarTurnoAlPos } = useCajaPos();
  const { configSalon, catalogos } = useCatalogos();
  const moneda = configSalon?.moneda || 'ARS';

  const [fecha, setFecha] = useState<string>(
    () => new Date().toISOString().split('T')[0]
  );
  const [filtroEstado, setFiltroEstado] = useState<number>(0);
  const [peluqueros, setPeluqueros] = useState<PeluqueroDto[]>([]);
  const [turnos, setTurnos] = useState<TurnoDto[]>([]);
  const [servicios, setServicios] = useState<ServicioDto[]>([]);
  const [clientes, setClientes] = useState<ClienteDto[]>([]);
  const [cargandoAgenda, setCargandoAgenda] = useState<boolean>(true);
  const [errorAgenda, setErrorAgenda] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  // Modal Nuevo Turno + Walk-In Express + Disponibilidad Real
  const [modalNuevoOpen, setModalNuevoOpen] = useState<boolean>(false);
  const [modoWalkIn, setModoWalkIn] = useState<boolean>(false);
  const [walkInNombre, setWalkInNombre] = useState<string>('');
  const [walkInApellido, setWalkInApellido] = useState<string>('');
  const [walkInTelefono, setWalkInTelefono] = useState<string>('');

  const [clienteId, setClienteId] = useState<number>(0);
  const [peluqueroId, setPeluqueroId] = useState<number>(0);
  const [serviciosIds, setServiciosIds] = useState<number[]>([]);
  const [slotsDisponibles, setSlotsDisponibles] = useState<string[]>([]);
  const [horaInicio, setHoraInicio] = useState<string>('10:00');
  const [observaciones, setObservaciones] = useState<string>('');

  // Modal Reprogramar Turno (PUT /api/turnos/{id}/reprogramar)
  const [turnoAReprogramar, setTurnoAReprogramar] = useState<TurnoDto | null>(
    null
  );
  const [reprogPeluqueroId, setReprogPeluqueroId] = useState<number>(0);
  const [reprogFecha, setReprogFecha] = useState<string>('');
  const [reprogHora, setReprogHora] = useState<string>('10:00');
  const [reprogObservaciones, setReprogObservaciones] = useState<string>('');

  // Modal Cambiar Estado del Turno en Mostrador
  const [turnoCambiarEstado, setTurnoCambiarEstado] = useState<TurnoDto | null>(
    null
  );
  const [nuevoEstadoId, setNuevoEstadoId] = useState<EstadoTurnoEnum>(
    EstadoTurnoEnum.Confirmado
  );
  const [motivoCancelacionId, setMotivoCancelacionId] = useState<number>(1);
  const [obsEstado, setObsEstado] = useState<string>('');

  // Modal Gestión de Bloqueos Horarios Rápidos
  const [modalBloqueoOpen, setModalBloqueoOpen] = useState<boolean>(false);
  const [bloqueoPeluqueroId, setBloqueoPeluqueroId] = useState<number>(0);
  const [bloqueosLista, setBloqueosLista] = useState<BloqueoHorarioDto[]>([]);
  const [bloqueoInicio, setBloqueoInicio] = useState<string>('');
  const [bloqueoFin, setBloqueoFin] = useState<string>('');
  const [bloqueoMotivo, setBloqueoMotivo] = useState<string>('');

  // Modal Historial de Estados
  const [historialTurno, setHistorialTurno] = useState<
    HistorialEstadoTurnoDto[] | null
  >(null);

  const cargarDatosAgenda = useCallback(async () => {
    setCargandoAgenda(true);
    setErrorAgenda(null);
    try {
      const [pelRes, turRes, servRes, cliRes] = await Promise.all([
        peluquerosService.getAll(),
        turnosService.getAll(fecha),
        serviciosService.getAll(),
        clientesService.getAll(1, 100),
      ]);
      if (pelRes.success) {
        const activos = extractPagedItems<PeluqueroDto>(pelRes.data).filter(
          (p) => p.activo
        );
        setPeluqueros(activos);
        if (activos.length > 0) {
          setPeluqueroId((prev) => (prev > 0 ? prev : activos[0].id));
          setBloqueoPeluqueroId((prev) => (prev > 0 ? prev : activos[0].id));
        }
      }
      if (turRes.success) {
        setTurnos(extractPagedItems<TurnoDto>(turRes.data));
      }
      if (servRes.success) {
        setServicios(
          extractPagedItems<ServicioDto>(servRes.data).filter((s) => s.activo)
        );
      }
      if (cliRes.success) {
        const cliItems = extractPagedItems<ClienteDto>(cliRes.data);
        setClientes(cliItems);
        if (cliItems.length > 0) {
          setClienteId((prev) => (prev > 0 ? prev : cliItems[0].id));
        }
      }
    } catch (err) {
      setErrorAgenda({
        message: extractErrorMessage(err, 'Error al cargar la agenda del día.'),
        correlationId: extractErrorCorrelationId(err),
      });
      notifyApiError(err, {
        fallbackMessage: 'Error al cargar la agenda del día.',
      });
    } finally {
      setCargandoAgenda(false);
    }
  }, [fecha]);

  useEffect(() => {
    cargarDatosAgenda();
  }, [cargarDatosAgenda]);

  // Consulta en tiempo real de slots disponibles cuando se eligen servicios y estilista en el modal
  useEffect(() => {
    if (modalNuevoOpen && peluqueroId && fecha && serviciosIds.length > 0) {
      turnosService
        .getDisponibilidad(peluqueroId, fecha, serviciosIds)
        .then((res) => {
          if (res.success && res.data && res.data.length > 0) {
            setSlotsDisponibles(res.data);
            setHoraInicio(res.data[0]);
          } else {
            setSlotsDisponibles([]);
          }
        })
        .catch(() => setSlotsDisponibles([]));
    }
  }, [modalNuevoOpen, peluqueroId, fecha, serviciosIds]);

  const handleCrearClienteExpress = async () => {
    if (
      !walkInNombre.trim() ||
      !walkInApellido.trim() ||
      !walkInTelefono.trim()
    )
      return;
    try {
      const res = await clientesService.create({
        nombre: walkInNombre.trim(),
        apellido: walkInApellido.trim(),
        telefono: walkInTelefono.trim(),
        fechaNacimiento: '2000-01-01',
      });
      if (res.success && res.data) {
        notifyApiSuccess(
          res,
          `Cliente Walk-In "${res.data.nombre} ${res.data.apellido}" creado.`
        );
        setClientes((prev) => [res.data!, ...prev]);
        setClienteId(res.data.id);
        setModoWalkIn(false);
        setWalkInNombre('');
        setWalkInApellido('');
        setWalkInTelefono('');
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo registrar el cliente Walk-In.',
      });
    }
  };

  const handleCrearTurno = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!clienteId || !peluqueroId || serviciosIds.length === 0) return;
    try {
      const res = await turnosService.create({
        clienteId,
        peluqueroId,
        fechaHoraInicio: `${fecha}T${horaInicio}:00`,
        serviciosIds,
        observaciones,
      });
      if (res.success) {
        notifyApiSuccess(res, `Turno #${res.data?.id} agendado correctamente.`);
        setModalNuevoOpen(false);
        setServiciosIds([]);
        setObservaciones('');
        setFeedback(`Turno #${res.data?.id} agendado correctamente.`);
        await cargarDatosAgenda();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo agendar el turno.',
      });
    }
  };

  const abrirModalReprogramar = (turno: TurnoDto) => {
    setTurnoAReprogramar(turno);
    setReprogPeluqueroId(turno.peluqueroId);
    const fechaIso = turno.fechaHoraInicio.split('T')[0] || fecha;
    const horaTexto = new Date(turno.fechaHoraInicio).toLocaleTimeString(
      'es-AR',
      {
        hour: '2-digit',
        minute: '2-digit',
        hour12: false,
      }
    );
    setReprogFecha(fechaIso);
    setReprogHora(horaTexto);
    setReprogObservaciones(turno.observaciones || '');
  };

  const handleConfirmarReprogramacion = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!turnoAReprogramar) return;
    try {
      const res = await turnosService.reprogramar(turnoAReprogramar.id, {
        peluqueroId: reprogPeluqueroId,
        nuevaFechaHoraInicio: `${reprogFecha}T${reprogHora}:00`,
        observaciones:
          reprogObservaciones.trim() || 'Reprogramado desde Calendario Master',
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          `Turno #${turnoAReprogramar.id} reprogramado exitosamente.`
        );
        setTurnoAReprogramar(null);
        await cargarDatosAgenda();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage:
          'Conflicto de horario: no se pudo reprogramar el turno.',
      });
    }
  };

  const handleConfirmarCambioEstado = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!turnoCambiarEstado) return;
    try {
      const res = await turnosService.cambiarEstado(turnoCambiarEstado.id, {
        nuevoEstadoId,
        motivoCancelacionId:
          nuevoEstadoId === EstadoTurnoEnum.Cancelado
            ? motivoCancelacionId
            : null,
        observaciones: obsEstado || 'Actualizado en mostrador de recepción',
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          nuevoEstadoId === EstadoTurnoEnum.Completado
            ? `Turno #${turnoCambiarEstado.id} completado (insumos de receta descontados del Kardex).`
            : `Estado del turno #${turnoCambiarEstado.id} actualizado.`
        );
        setTurnoCambiarEstado(null);
        setObsEstado('');
        await cargarDatosAgenda();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo actualizar el estado del turno.',
      });
    }
  };

  const cargarBloqueosDeEstilista = async (idEstilista: number) => {
    setBloqueoPeluqueroId(idEstilista);
    try {
      const res = await bloqueosService.getPorPeluquero(idEstilista);
      if (res.success && res.data) setBloqueosLista(res.data);
    } catch {
      setBloqueosLista([]);
    }
  };

  const handleCrearBloqueo = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await bloqueosService.crear({
        peluqueroId: bloqueoPeluqueroId,
        fechaInicio: bloqueoInicio,
        fechaFin: bloqueoFin,
        motivo: bloqueoMotivo,
      });
      if (res.success) {
        // Si el bloqueo se solapa con turnos activos, notifyApiSuccess dispara toast.warning por cada item en res.warnings
        notifyApiSuccess(res, 'Bloqueo horario registrado en la agenda.');
        setBloqueoInicio('');
        setBloqueoFin('');
        setBloqueoMotivo('');
        await cargarBloqueosDeEstilista(bloqueoPeluqueroId);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo registrar el bloqueo de agenda.',
      });
    }
  };

  const handleEliminarBloqueo = async (id?: number) => {
    if (!id) return;
    try {
      const res = await bloqueosService.eliminar(id);
      if (res.success) {
        notifyApiSuccess(res, 'Bloqueo horario eliminado.');
        await cargarBloqueosDeEstilista(bloqueoPeluqueroId);
      }
    } catch (err) {
      notifyApiError(err);
    }
  };

  const handlePasarACobrarEnCaja = (turno: TurnoDto) => {
    enviarTurnoAlPos(turno);
    router.push(`/recepcion/pos?turnoId=${turno.id}`);
  };

  const verHistorial = async (turnoId: number) => {
    try {
      const res = await turnosService.getHistorialEstados(turnoId);
      if (res.success) {
        setHistorialTurno(extractPagedItems<HistorialEstadoTurnoDto>(res.data));
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo obtener el historial del turno.',
      });
    }
  };

  return (
    <div className="p-6 space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4 bg-white border border-slate-200 rounded-lg p-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900">
            Calendario Master Multi-Columna (Agenda Global)
          </h1>
          <p className="text-xs text-slate-500">
            Gestión en vivo por estilista, recepción de clientes Walk-In,
            reprogramación, bloqueos y cobro directo en POS.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2.5">
          <input
            type="date"
            value={fecha}
            onChange={(e) => setFecha(e.target.value)}
            className="rounded-md border border-slate-300 px-3 py-1.5 text-xs"
          />
          <select
            value={filtroEstado}
            onChange={(e) => setFiltroEstado(Number(e.target.value))}
            className="rounded-md border border-slate-300 px-3 py-1.5 text-xs"
          >
            <option value={0}>Todos los Estados</option>
            <option value={EstadoTurnoEnum.Pendiente}>1 - Pendiente</option>
            <option value={EstadoTurnoEnum.Confirmado}>2 - Confirmado</option>
            <option value={EstadoTurnoEnum.EnProceso}>3 - En Proceso</option>
            <option value={EstadoTurnoEnum.Completado}>4 - Completado</option>
            <option value={EstadoTurnoEnum.Cancelado}>5 - Cancelado</option>
            <option value={EstadoTurnoEnum.NoAsistio}>6 - No Asistió</option>
          </select>
          <Button
            variant="secondary"
            size="sm"
            onClick={() => {
              if (peluqueros[0]) cargarBloqueosDeEstilista(peluqueros[0].id);
              setModalBloqueoOpen(true);
            }}
          >
            Bloqueos / Ausencias
          </Button>
          <Button
            variant="primary"
            size="sm"
            onClick={() => setModalNuevoOpen(true)}
          >
            + Nuevo Turno / Walk-In
          </Button>
        </div>
      </header>

      {feedback && (
        <div className="p-3 rounded bg-slate-900 text-white text-xs flex justify-between items-center">
          <span>{feedback}</span>
          <button type="button" onClick={() => setFeedback(null)}>
            ✕
          </button>
        </div>
      )}

      {/* Grilla Multi-Columna por Estilista Activo */}
      <AsyncStateGuard
        isLoading={cargandoAgenda}
        isError={Boolean(errorAgenda)}
        errorMessage={errorAgenda?.message}
        correlationId={errorAgenda?.correlationId}
        isEmpty={(peluqueros ?? []).length === 0}
        loadingText="Cargando calendario de estilistas..."
        emptyTitle="Sin profesionales activos"
        emptyDescription="No hay estilistas activos configurados en esta sucursal para la fecha seleccionada."
        onRetry={cargarDatosAgenda}
      >
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
          {(peluqueros ?? []).map((pel) => {
            const turnosEstilista = (turnos ?? []).filter(
              (t) =>
                t.peluqueroId === pel.id &&
                (filtroEstado === 0 || t.estadoId === filtroEstado)
            );
            return (
              <div
                key={pel.id}
                className="bg-white border border-slate-200 rounded-lg overflow-hidden flex flex-col shadow-sm"
              >
                <div
                  className="px-4 py-3 text-white font-bold text-sm flex items-center justify-between"
                  style={{ backgroundColor: pel.colorAgendaHex || '#0F172A' }}
                >
                  <span>
                    {pel.nombre} {pel.apellido}
                  </span>
                  <span className="text-xs bg-black/25 px-2 py-0.5 rounded font-mono">
                    {turnosEstilista.length}
                  </span>
                </div>

                <div className="p-3 space-y-3 flex-1">
                  {turnosEstilista.length === 0 ? (
                    <p className="text-xs text-slate-400 text-center py-8">
                      Sin turnos en este filtro
                    </p>
                  ) : (
                    turnosEstilista.map((turno) => (
                      <div
                        key={turno.id}
                        style={{
                          borderLeftColor:
                            turno.colorAgendaHex ||
                            pel.colorAgendaHex ||
                            '#0F172A',
                        }}
                        className="border border-l-4 border-slate-200 rounded-md p-3 space-y-2 hover:border-slate-400 transition-colors"
                      >
                        <div className="flex justify-between items-center text-xs">
                          <span className="font-mono font-bold text-slate-900">
                            {new Date(turno.fechaHoraInicio).toLocaleTimeString(
                              'es-AR',
                              {
                                hour: '2-digit',
                                minute: '2-digit',
                              }
                            )}{' '}
                            hs
                          </span>
                          <button
                            type="button"
                            onClick={() => {
                              setTurnoCambiarEstado(turno);
                              setNuevoEstadoId(turno.estadoId);
                            }}
                            className="font-semibold px-2 py-0.5 rounded bg-amber-50 text-amber-900 border border-amber-200 hover:bg-amber-100"
                          >
                            {turno.estadoNombre}
                          </button>
                        </div>

                        <div className="flex flex-wrap items-center justify-between gap-1">
                          <p className="text-sm font-bold text-slate-900">
                            {turno.clienteNombreCompleto}
                          </p>
                          {turno.senaPagada && (
                            <Badge variant="success">Seña OK</Badge>
                          )}
                          {turno.tieneAlertaAlergia && (
                            <Badge variant="danger">⚠️ Alergia</Badge>
                          )}
                        </div>

                        <p className="text-xs text-slate-500">
                          {(turno.servicios ?? [])
                            .map((s) => s.servicioNombre)
                            .join(' + ')}
                        </p>

                        <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-slate-100 text-xs">
                          <span className="font-mono font-bold">
                            {formatCurrency(turno.montoTotalEstimado, moneda)}
                          </span>
                          <div className="flex flex-wrap items-center gap-2">
                            <button
                              type="button"
                              onClick={() => abrirModalReprogramar(turno)}
                              className="underline text-slate-600 hover:text-slate-900 font-medium"
                            >
                              Reprogramar
                            </button>
                            <button
                              type="button"
                              onClick={() => verHistorial(turno.id)}
                              className="underline text-slate-500 hover:text-slate-900"
                            >
                              Auditar
                            </button>
                            {(turno.estadoId === EstadoTurnoEnum.Completado ||
                              turno.estadoId === EstadoTurnoEnum.Confirmado ||
                              turno.estadoId === EstadoTurnoEnum.EnProceso) && (
                              <Button
                                variant="editorial"
                                size="sm"
                                onClick={() => handlePasarACobrarEnCaja(turno)}
                              >
                                Facturar en POS →
                              </Button>
                            )}
                          </div>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </AsyncStateGuard>

      {/* Modal Nuevo Turno con Alta Express de Cliente y Slots en Vivo */}
      <Modal
        isOpen={modalNuevoOpen}
        onClose={() => setModalNuevoOpen(false)}
        title="Otorgar Nuevo Turno en Recepción"
        maxWidthClass="max-w-2xl"
      >
        <form onSubmit={handleCrearTurno} className="space-y-4">
          <div className="flex items-center justify-between bg-slate-50 p-3 rounded border border-slate-200 text-xs">
            <span>¿El cliente es nuevo en el salón (Walk-In)?</span>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setModoWalkIn(!modoWalkIn)}
            >
              {modoWalkIn
                ? 'Seleccionar Cliente Existente'
                : '+ Alta Express de Cliente'}
            </Button>
          </div>

          {modoWalkIn ? (
            <div className="p-4 rounded border border-amber-300 bg-amber-50/40 space-y-3">
              <h4 className="text-xs font-bold uppercase text-amber-900">
                Registro Express de Cliente Nuevo
              </h4>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-2">
                <InputField
                  label="Nombre *"
                  value={walkInNombre}
                  onChange={(e) => setWalkInNombre(e.target.value)}
                />
                <InputField
                  label="Apellido *"
                  value={walkInApellido}
                  onChange={(e) => setWalkInApellido(e.target.value)}
                />
                <InputField
                  label="Teléfono *"
                  value={walkInTelefono}
                  onChange={(e) => setWalkInTelefono(e.target.value)}
                />
              </div>
              <Button
                type="button"
                variant="editorial"
                size="sm"
                onClick={handleCrearClienteExpress}
              >
                Guardar y Seleccionar Cliente
              </Button>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div className="flex flex-col gap-1">
                <label className="text-xs font-semibold uppercase text-slate-700">
                  Cliente *
                </label>
                <select
                  value={clienteId}
                  onChange={(e) => setClienteId(Number(e.target.value))}
                  className="rounded-md border border-slate-300 px-3 py-2 text-sm"
                >
                  {clientes.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.nombre} {c.apellido} ({c.telefono})
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex flex-col gap-1">
                <label className="text-xs font-semibold uppercase text-slate-700">
                  Profesional *
                </label>
                <select
                  value={peluqueroId}
                  onChange={(e) => setPeluqueroId(Number(e.target.value))}
                  className="rounded-md border border-slate-300 px-3 py-2 text-sm"
                >
                  {peluqueros.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nombre} {p.apellido}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}

          <div className="space-y-1.5">
            <label className="text-xs font-semibold uppercase text-slate-700">
              Servicios Solicitados (Calcula Duración y Slots Libres) *
            </label>
            <div className="grid grid-cols-2 gap-2 max-h-40 overflow-y-auto p-2.5 border border-slate-200 rounded">
              {servicios.map((s) => (
                <label key={s.id} className="flex items-center gap-2 text-xs">
                  <input
                    type="checkbox"
                    checked={serviciosIds.includes(s.id)}
                    onChange={() =>
                      setServiciosIds((prev) =>
                        prev.includes(s.id)
                          ? prev.filter((id) => id !== s.id)
                          : [...prev, s.id]
                      )
                    }
                  />
                  <span>
                    {s.nombre} ({s.duracionMinutos}m —{' '}
                    {formatCurrency(s.precio, moneda)})
                  </span>
                </label>
              ))}
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="flex flex-col gap-1">
              <label className="text-xs font-semibold uppercase text-slate-700">
                Slots Libres Verificados ({slotsDisponibles.length})
              </label>
              {slotsDisponibles.length > 0 ? (
                <select
                  value={horaInicio}
                  onChange={(e) => setHoraInicio(e.target.value)}
                  className="rounded-md border border-slate-300 px-3 py-2 text-sm font-mono"
                >
                  {slotsDisponibles.map((slot) => (
                    <option key={slot} value={slot}>
                      {slot} hs
                    </option>
                  ))}
                </select>
              ) : (
                <input
                  type="time"
                  value={horaInicio}
                  onChange={(e) => setHoraInicio(e.target.value)}
                  className="rounded-md border border-slate-300 px-3 py-2 text-sm font-mono"
                />
              )}
            </div>
            <InputField
              label="Observaciones / Alergias"
              value={observaciones}
              onChange={(e) => setObservaciones(e.target.value)}
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setModalNuevoOpen(false)}
            >
              Cancelar
            </Button>
            <Button type="submit" variant="primary">
              Confirmar Turno en Agenda
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Reprogramar Turno (PUT /api/turnos/{id}/reprogramar) */}
      <Modal
        isOpen={!!turnoAReprogramar}
        onClose={() => setTurnoAReprogramar(null)}
        title={`Reprogramar Turno #${turnoAReprogramar?.id} (${turnoAReprogramar?.clienteNombreCompleto})`}
        subtitle="Valida automáticamente colisiones horarias y bloqueos del profesional."
      >
        <form onSubmit={handleConfirmarReprogramacion} className="space-y-4">
          <div className="flex flex-col gap-1">
            <label className="text-xs font-semibold uppercase text-slate-700">
              Profesional Asignado *
            </label>
            <select
              value={reprogPeluqueroId}
              onChange={(e) => setReprogPeluqueroId(Number(e.target.value))}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            >
              {peluqueros.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.nombre} {p.apellido}
                </option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <InputField
              label="Nueva Fecha *"
              type="date"
              required
              value={reprogFecha}
              onChange={(e) => setReprogFecha(e.target.value)}
            />
            <InputField
              label="Nuevo Horario *"
              type="time"
              required
              value={reprogHora}
              onChange={(e) => setReprogHora(e.target.value)}
            />
          </div>

          <InputField
            label="Motivo / Observaciones de Reprogramación"
            value={reprogObservaciones}
            onChange={(e) => setReprogObservaciones(e.target.value)}
          />

          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setTurnoAReprogramar(null)}
            >
              Cancelar
            </Button>
            <Button type="submit" variant="editorial">
              Confirmar Reprogramación
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Cambiar Estado del Turno en Recepción */}
      <Modal
        isOpen={!!turnoCambiarEstado}
        onClose={() => setTurnoCambiarEstado(null)}
        title={`Cambiar Estado — Turno #${turnoCambiarEstado?.id} (${turnoCambiarEstado?.clienteNombreCompleto})`}
      >
        <form onSubmit={handleConfirmarCambioEstado} className="space-y-4">
          <div className="flex flex-col gap-1">
            <label className="text-xs font-semibold uppercase text-slate-700">
              Nuevo Estado del Turno *
            </label>
            <select
              value={nuevoEstadoId}
              onChange={(e) =>
                setNuevoEstadoId(Number(e.target.value) as EstadoTurnoEnum)
              }
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            >
              <option value={EstadoTurnoEnum.Pendiente}>1 - Pendiente</option>
              <option value={EstadoTurnoEnum.Confirmado}>
                2 - Confirmado (Cliente llegó / Seña OK)
              </option>
              <option value={EstadoTurnoEnum.EnProceso}>
                3 - En Proceso (En sillón)
              </option>
              <option value={EstadoTurnoEnum.Completado}>
                4 - Completado (Dispara descuento de Receta en Kardex)
              </option>
              <option value={EstadoTurnoEnum.Cancelado}>5 - Cancelado</option>
              <option value={EstadoTurnoEnum.NoAsistio}>
                6 - No Asistió (Ausentismo)
              </option>
            </select>
          </div>

          {nuevoEstadoId === EstadoTurnoEnum.Cancelado && (
            <div className="flex flex-col gap-1">
              <label className="text-xs font-semibold uppercase text-slate-700">
                Motivo de Cancelación Obligatorio *
              </label>
              <select
                value={motivoCancelacionId}
                onChange={(e) =>
                  setMotivoCancelacionId(Number(e.target.value))
                }
                className="rounded-md border border-slate-300 px-3 py-2 text-sm"
              >
                {(
                  catalogos?.motivosCancelacion || [
                    { id: 1, nombre: 'Cancelación con aviso del cliente' },
                    { id: 2, nombre: 'Reprogramación de horario' },
                  ]
                ).map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.nombre || m.descripcion}
                  </option>
                ))}
              </select>
            </div>
          )}

          <InputField
            label="Observaciones de Auditoría"
            value={obsEstado}
            onChange={(e) => setObsEstado(e.target.value)}
          />

          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setTurnoCambiarEstado(null)}
            >
              Cerrar
            </Button>
            <Button type="submit" variant="editorial">
              Guardar Transición de Estado
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal de Bloqueos Horarios Rápidos */}
      <Modal
        isOpen={modalBloqueoOpen}
        onClose={() => setModalBloqueoOpen(false)}
        title="Gestión de Bloqueos Horarios y Ausencias"
        maxWidthClass="max-w-2xl"
      >
        <div className="space-y-4 text-xs">
          <form
            onSubmit={handleCrearBloqueo}
            className="space-y-3 border-b border-slate-200 pb-4"
          >
            <div className="flex flex-col gap-1">
              <label className="font-semibold uppercase text-slate-700">
                Profesional *
              </label>
              <select
                value={bloqueoPeluqueroId}
                onChange={(e) =>
                  cargarBloqueosDeEstilista(Number(e.target.value))
                }
                className="rounded border border-slate-300 px-3 py-2 text-sm"
              >
                {peluqueros.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nombre} {p.apellido}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <InputField
                label="Inicio del Bloqueo *"
                type="datetime-local"
                required
                value={bloqueoInicio}
                onChange={(e) => setBloqueoInicio(e.target.value)}
              />
              <InputField
                label="Fin del Bloqueo *"
                type="datetime-local"
                required
                value={bloqueoFin}
                onChange={(e) => setBloqueoFin(e.target.value)}
              />
            </div>
            <InputField
              label="Motivo *"
              required
              value={bloqueoMotivo}
              onChange={(e) => setBloqueoMotivo(e.target.value)}
              placeholder="Ej. Trámite personal / Ausencia imprevista"
            />
            <Button type="submit" variant="primary">
              Bloquear Agenda
            </Button>
          </form>

          <div className="space-y-2">
            <h4 className="font-bold uppercase text-slate-700">
              Bloqueos Activos del Profesional ({bloqueosLista.length})
            </h4>
            {bloqueosLista.map((b) => (
              <div
                key={b.id}
                className="flex items-center justify-between p-2.5 rounded border border-slate-200"
              >
                <span>
                  <strong>
                    {formatDateTime(b.fechaInicio)} →{' '}
                    {formatDateTime(b.fechaFin)}
                  </strong>{' '}
                  • {b.motivo}
                </span>
                <Button
                  variant="danger"
                  size="sm"
                  onClick={() => handleEliminarBloqueo(b.id)}
                >
                  Eliminar
                </Button>
              </div>
            ))}
          </div>
        </div>
      </Modal>

      {/* Modal Historial de Estados del Turno */}
      <Modal
        isOpen={!!historialTurno}
        onClose={() => setHistorialTurno(null)}
        title="Trazabilidad Forense de Estados del Turno"
      >
        <div className="space-y-2 text-xs">
          {(historialTurno || []).map((h) => (
            <div key={h.id} className="border-b border-slate-100 pb-2">
              <p className="font-mono font-bold text-slate-800">
                {formatDateTime(h.fechaCambio)} — Estado #{h.estadoNuevoId} (
                {h.estadoNuevoNombre})
              </p>
              {h.observaciones && (
                <p className="text-slate-600">{h.observaciones}</p>
              )}
            </div>
          ))}
        </div>
      </Modal>
    </div>
  );
}
