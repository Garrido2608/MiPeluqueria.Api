// filepath: src/app/(dashboard)/recepcion/calendario/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  AlertTriangle,
  Ban,
  CalendarClock,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  CreditCard,
  History,
  Play,
  Plus,
  Trash2,
  UserPlus,
  XCircle,
} from 'lucide-react';
import {
  BloqueoHorarioDto,
  ClienteDto,
  EstadoTurnoEnum,
  HistorialEstadoTurnoDto,
  HorarioLaboralDto,
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
import {
  formatCurrency,
  formatDateOnly,
  formatDateTime,
  formatTimeOnly,
} from '@/lib/formatters';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import {
  AsyncStateGuard,
  Badge,
  BadgeProps,
  Button,
  InputField,
  Modal,
  SearchCombobox,
  SearchComboboxOption,
} from '@/components/ui';

type CalendarViewMode = 'dia' | 'semana';

const DIAS_SEMANA_CORTOS = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
const START_HOUR = 9;
const END_HOUR = 20;
const PIXELS_PER_HOUR = 64;
const SLOT_HEIGHT = PIXELS_PER_HOUR / 2; // 30 min = 32px

function toLocalDateIso(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function parseLocalDateIso(isoDate: string): Date {
  const parts = isoDate.split('-').map(Number);
  if (parts.length === 3 && parts.every((n) => Number.isFinite(n))) {
    return new Date(parts[0], parts[1] - 1, parts[2], 12, 0, 0);
  }
  return new Date();
}

function addDaysIso(isoDate: string, days: number): string {
  const d = parseLocalDateIso(isoDate);
  d.setDate(d.getDate() + days);
  return toLocalDateIso(d);
}

function extractDateKey(isoDateTime: string): string {
  if (/^\d{4}-\d{2}-\d{2}/.test(isoDateTime)) {
    return isoDateTime.slice(0, 10);
  }
  const d = new Date(isoDateTime);
  return Number.isNaN(d.getTime()) ? '' : toLocalDateIso(d);
}

function parseMinutesFromTimeOrIso(value?: string | null): number | null {
  if (!value) return null;
  const trimmed = value.trim();
  const timeMatch = trimmed.match(/^(\d{1,2}):(\d{2})(?::\d{2})?$/);
  if (timeMatch) {
    const h = Number(timeMatch[1]);
    const m = Number(timeMatch[2]);
    if (Number.isFinite(h) && Number.isFinite(m)) {
      return h * 60 + m;
    }
  }
  const d = new Date(trimmed);
  if (Number.isNaN(d.getTime())) return null;
  return d.getHours() * 60 + d.getMinutes();
}

function formatSlotTime(minutesFromMidnight: number): string {
  const h = Math.floor(minutesFromMidnight / 60);
  const m = minutesFromMidnight % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
}

function getInitials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return 'PR';
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

function getWeekDaysMonToSat(anchorIso: string): {
  iso: string;
  weekday: string;
  dayNumber: string;
  monthShort: string;
}[] {
  const d = parseLocalDateIso(anchorIso);
  const dow = d.getDay();
  const diffToMon = dow === 0 ? -6 : 1 - dow;
  d.setDate(d.getDate() + diffToMon);
  return Array.from({ length: 6 }, (_, index) => {
    const dayDate = new Date(
      d.getFullYear(),
      d.getMonth(),
      d.getDate() + index,
      12,
      0,
      0
    );
    const iso = toLocalDateIso(dayDate);
    const monthRaw = new Intl.DateTimeFormat('es-AR', { month: 'short' })
      .format(dayDate)
      .replace(/\.$/, '');
    return {
      iso,
      weekday: DIAS_SEMANA_CORTOS[dayDate.getDay()] || '',
      dayNumber: String(dayDate.getDate()).padStart(2, '0'),
      monthShort:
        monthRaw.charAt(0).toUpperCase() + monthRaw.slice(1).toLowerCase(),
    };
  });
}

function getEstadoBadgeVariant(
  estadoId: EstadoTurnoEnum
): NonNullable<BadgeProps['variant']> {
  switch (estadoId) {
    case EstadoTurnoEnum.Completado:
      return 'success';
    case EstadoTurnoEnum.Confirmado:
      return 'info';
    case EstadoTurnoEnum.EnProceso:
      return 'courtesy';
    case EstadoTurnoEnum.Pendiente:
      return 'warning';
    case EstadoTurnoEnum.Cancelado:
    case EstadoTurnoEnum.NoAsistio:
      return 'danger';
    default:
      return 'neutral';
  }
}

interface PositionedTurno {
  turno: TurnoDto;
  startMin: number;
  endMin: number;
  lane: number;
  totalLanes: number;
}

function computeColumnPositions(turnos: TurnoDto[]): PositionedTurno[] {
  const sorted = [...turnos]
    .map((turno) => {
      const startMin = parseMinutesFromTimeOrIso(turno.fechaHoraInicio) ?? 540;
      const parsedEnd = parseMinutesFromTimeOrIso(turno.fechaHoraFin);
      const durationMin =
        turno.duracionTotalMinutos && turno.duracionTotalMinutos > 0
          ? turno.duracionTotalMinutos
          : parsedEnd !== null && parsedEnd > startMin
          ? parsedEnd - startMin
          : 45;
      return {
        turno,
        startMin,
        endMin: startMin + Math.max(durationMin, 30),
      };
    })
    .sort((a, b) => a.startMin - b.startMin || a.endMin - b.endMin);

  const laneEnds: number[] = [];
  const placed: Array<{
    turno: TurnoDto;
    startMin: number;
    endMin: number;
    lane: number;
  }> = [];

  for (const item of sorted) {
    let lane = laneEnds.findIndex((end) => end <= item.startMin);
    if (lane === -1) {
      lane = laneEnds.length;
      laneEnds.push(item.endMin);
    } else {
      laneEnds[lane] = item.endMin;
    }
    placed.push({ ...item, lane });
  }

  return placed.map((item) => {
    const overlapping = placed.filter(
      (o) => o.startMin < item.endMin && o.endMin > item.startMin
    );
    const maxLane = overlapping.reduce((acc, o) => Math.max(acc, o.lane), 0);
    return { ...item, totalLanes: Math.max(1, maxLane + 1) };
  });
}

export default function CalendarioMasterRecepcionPage() {
  const router = useRouter();
  const { enviarTurnoAlPos } = useCajaPos();
  const { configSalon, catalogos } = useCatalogos();
  const moneda = configSalon?.moneda || 'ARS';

  const [todayIso, setTodayIso] = useState<string>(() =>
    toLocalDateIso(new Date())
  );
  const [nowMinutes, setNowMinutes] = useState<number | null>(null);
  const [viewMode, setViewMode] = useState<CalendarViewMode>('dia');
  const [fecha, setFecha] = useState<string>(() => toLocalDateIso(new Date()));
  const [filtroEstado, setFiltroEstado] = useState<number>(0);
  const [filtroPeluqueroSemana, setFiltroPeluqueroSemana] = useState<number>(0);

  const [peluqueros, setPeluqueros] = useState<PeluqueroDto[]>([]);
  const [horariosPorPeluquero, setHorariosPorPeluquero] = useState<
    Record<number, HorarioLaboralDto[]>
  >({});
  const [bloqueosPorPeluquero, setBloqueosPorPeluquero] = useState<
    Record<number, BloqueoHorarioDto[]>
  >({});
  const [turnos, setTurnos] = useState<TurnoDto[]>([]);
  const [servicios, setServicios] = useState<ServicioDto[]>([]);
  const [clientes, setClientes] = useState<ClienteDto[]>([]);

  const [cargandoAgenda, setCargandoAgenda] = useState<boolean>(true);
  const [errorAgenda, setErrorAgenda] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);

  // Modal de acciones rápidas al hacer clic en un turno
  const [turnoActivo, setTurnoActivo] = useState<TurnoDto | null>(null);
  const [procesandoAccion, setProcesandoAccion] = useState<boolean>(false);

  // Modal Nuevo Turno + Registro Express + Disponibilidad
  const [modalNuevoOpen, setModalNuevoOpen] = useState<boolean>(false);
  const [modoWalkIn, setModoWalkIn] = useState<boolean>(false);
  const [walkInNombre, setWalkInNombre] = useState<string>('');
  const [walkInApellido, setWalkInApellido] = useState<string>('');
  const [walkInTelefono, setWalkInTelefono] = useState<string>('');
  const [creandoWalkIn, setCreandoWalkIn] = useState<boolean>(false);

  const [nuevoTurnoFecha, setNuevoTurnoFecha] = useState<string>(() =>
    toLocalDateIso(new Date())
  );
  const [clienteId, setClienteId] = useState<number | null>(null);
  const [peluqueroId, setPeluqueroId] = useState<number>(0);
  const [serviciosIds, setServiciosIds] = useState<number[]>([]);
  const [slotsDisponibles, setSlotsDisponibles] = useState<string[]>([]);
  const [horaInicio, setHoraInicio] = useState<string>('10:00');
  const [horaPreseleccionada, setHoraPreseleccionada] = useState<string | null>(
    null
  );
  const [observaciones, setObservaciones] = useState<string>('');
  const [guardandoTurno, setGuardandoTurno] = useState<boolean>(false);

  // Modal Reprogramar Turno
  const [turnoAReprogramar, setTurnoAReprogramar] = useState<TurnoDto | null>(
    null
  );
  const [reprogPeluqueroId, setReprogPeluqueroId] = useState<number>(0);
  const [reprogFecha, setReprogFecha] = useState<string>('');
  const [reprogHora, setReprogHora] = useState<string>('10:00');
  const [reprogObservaciones, setReprogObservaciones] = useState<string>('');
  const [guardandoReprog, setGuardandoReprog] = useState<boolean>(false);

  // Modal Cambiar Estado (para cancelación con motivo o cambio avanzado)
  const [turnoCambiarEstado, setTurnoCambiarEstado] = useState<TurnoDto | null>(
    null
  );
  const [nuevoEstadoId, setNuevoEstadoId] = useState<EstadoTurnoEnum>(
    EstadoTurnoEnum.Confirmado
  );
  const [motivoCancelacionId, setMotivoCancelacionId] = useState<number>(1);
  const [obsEstado, setObsEstado] = useState<string>('');
  const [guardandoEstado, setGuardandoEstado] = useState<boolean>(false);

  // Modal Gestión de Bloqueos Horarios
  const [modalBloqueoOpen, setModalBloqueoOpen] = useState<boolean>(false);
  const [bloqueoPeluqueroId, setBloqueoPeluqueroId] = useState<number>(0);
  const [bloqueosLista, setBloqueosLista] = useState<BloqueoHorarioDto[]>([]);
  const [bloqueoInicio, setBloqueoInicio] = useState<string>('');
  const [bloqueoFin, setBloqueoFin] = useState<string>('');
  const [bloqueoMotivo, setBloqueoMotivo] = useState<string>('');
  const [guardandoBloqueo, setGuardandoBloqueo] = useState<boolean>(false);

  // Modal Historial de Estados
  const [historialTurno, setHistorialTurno] = useState<
    HistorialEstadoTurnoDto[] | null
  >(null);

  useEffect(() => {
    const syncClock = () => {
      const now = new Date();
      setTodayIso(toLocalDateIso(now));
      setNowMinutes(now.getHours() * 60 + now.getMinutes());
    };
    syncClock();
    const timer = window.setInterval(syncClock, 60_000);
    return () => window.clearInterval(timer);
  }, []);

  const weekDays = useMemo(() => getWeekDaysMonToSat(fecha), [fecha]);

  const cargarDatosAgenda = useCallback(async () => {
    setCargandoAgenda(true);
    setErrorAgenda(null);
    try {
      const [pelRes, turRes, servRes, cliRes] = await Promise.all([
        peluquerosService.getAll(),
        viewMode === 'semana'
          ? turnosService.getByRange(
              weekDays[0].iso,
              weekDays[weekDays.length - 1].iso
            )
          : turnosService.getAll(fecha),
        serviciosService.getAll(),
        clientesService.getAll(1, 100),
      ]);

      let activos: PeluqueroDto[] = [];
      if (pelRes.success) {
        activos = extractPagedItems<PeluqueroDto>(pelRes.data).filter(
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
        setClientes(extractPagedItems<ClienteDto>(cliRes.data));
      }

      // Cargar descansos y bloqueos de cada profesional para sombreado en la grilla
      if (activos.length > 0) {
        const metaResults = await Promise.allSettled(
          activos.map(async (p) => {
            const [horRes, bloqRes] = await Promise.all([
              peluquerosService.getHorarios(p.id),
              bloqueosService.getPorPeluquero(p.id),
            ]);
            return {
              id: p.id,
              horarios: horRes.success
                ? extractPagedItems<HorarioLaboralDto>(horRes.data)
                : [],
              bloqueos: bloqRes.success
                ? extractPagedItems<BloqueoHorarioDto>(bloqRes.data)
                : [],
            };
          })
        );

        const nextHorarios: Record<number, HorarioLaboralDto[]> = {};
        const nextBloqueos: Record<number, BloqueoHorarioDto[]> = {};
        for (const r of metaResults) {
          if (r.status === 'fulfilled') {
            nextHorarios[r.value.id] = r.value.horarios;
            nextBloqueos[r.value.id] = r.value.bloqueos;
          }
        }
        setHorariosPorPeluquero(nextHorarios);
        setBloqueosPorPeluquero(nextBloqueos);
      }
    } catch (err) {
      setErrorAgenda({
        message: extractErrorMessage(err, 'No se pudo cargar el calendario.'),
        correlationId: extractErrorCorrelationId(err),
      });
      notifyApiError(err, {
        fallbackMessage: 'No se pudo cargar el calendario.',
      });
    } finally {
      setCargandoAgenda(false);
    }
  }, [fecha, viewMode, weekDays]);

  useEffect(() => {
    void cargarDatosAgenda();
  }, [cargarDatosAgenda]);

  // Consulta en tiempo real de slots disponibles
  useEffect(() => {
    if (
      modalNuevoOpen &&
      peluqueroId > 0 &&
      nuevoTurnoFecha &&
      serviciosIds.length > 0
    ) {
      turnosService
        .getDisponibilidad(peluqueroId, nuevoTurnoFecha, serviciosIds)
        .then((res) => {
          if (res.success && res.data && res.data.length > 0) {
            setSlotsDisponibles(res.data);
            if (
              horaPreseleccionada &&
              res.data.includes(horaPreseleccionada)
            ) {
              setHoraInicio(horaPreseleccionada);
            } else if (!res.data.includes(horaInicio)) {
              setHoraInicio(res.data[0]);
            }
          } else {
            setSlotsDisponibles([]);
          }
        })
        .catch(() => setSlotsDisponibles([]));
    } else if (modalNuevoOpen && serviciosIds.length === 0) {
      setSlotsDisponibles([]);
    }
  }, [
    modalNuevoOpen,
    peluqueroId,
    nuevoTurnoFecha,
    serviciosIds,
    horaPreseleccionada,
    horaInicio,
  ]);

  const abrirModalNuevoTurno = (preset?: {
    peluqueroId?: number;
    fecha?: string;
    hora?: string;
  }) => {
    setModoWalkIn(false);
    setNuevoTurnoFecha(preset?.fecha || fecha);
    if (preset?.peluqueroId) {
      setPeluqueroId(preset.peluqueroId);
    } else if (peluqueros.length > 0 && peluqueroId === 0) {
      setPeluqueroId(peluqueros[0].id);
    }
    const targetHour = preset?.hora || '10:00';
    setHoraInicio(targetHour);
    setHoraPreseleccionada(preset?.hora || null);
    setServiciosIds([]);
    setObservaciones('');
    setSlotsDisponibles([]);
    setModalNuevoOpen(true);
  };

  const handleShiftDate = (delta: number) => {
    const step = viewMode === 'semana' ? delta * 7 : delta;
    setFecha((prev) => addDaysIso(prev, step));
  };

  const clienteOptions: SearchComboboxOption<number>[] = useMemo(() => {
    return clientes.map((c) => ({
      value: c.id,
      label: `${c.nombre} ${c.apellido}`.trim(),
      subtitle: c.telefono || c.email || 'Sin teléfono',
      keywords: `${c.telefono || ''} ${c.email || ''}`,
    }));
  }, [clientes]);

  const handleCrearClienteExpress = async () => {
    if (
      !walkInNombre.trim() ||
      !walkInApellido.trim() ||
      !walkInTelefono.trim()
    ) {
      return;
    }
    setCreandoWalkIn(true);
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
          `Cliente "${res.data.nombre} ${res.data.apellido}" registrado y seleccionado.`
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
        fallbackMessage: 'No se pudo registrar el cliente.',
      });
    } finally {
      setCreandoWalkIn(false);
    }
  };

  const handleCrearTurno = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!clienteId || !peluqueroId || serviciosIds.length === 0) return;
    setGuardandoTurno(true);
    try {
      const res = await turnosService.create({
        clienteId,
        peluqueroId,
        fechaHoraInicio: `${nuevoTurnoFecha}T${horaInicio}:00`,
        serviciosIds,
        observaciones,
      });
      if (res.success) {
        notifyApiSuccess(res, 'Turno agendado correctamente.');
        setModalNuevoOpen(false);
        setServiciosIds([]);
        setObservaciones('');
        setHoraPreseleccionada(null);
        await cargarDatosAgenda();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo agendar el turno.',
      });
    } finally {
      setGuardandoTurno(false);
    }
  };

  const handleCambioEstadoDirecto = async (
    turno: TurnoDto,
    estadoDestino: EstadoTurnoEnum
  ) => {
    if (estadoDestino === EstadoTurnoEnum.Cancelado) {
      setTurnoActivo(null);
      setTurnoCambiarEstado(turno);
      setNuevoEstadoId(EstadoTurnoEnum.Cancelado);
      setObsEstado('');
      return;
    }
    setProcesandoAccion(true);
    try {
      const res = await turnosService.cambiarEstado(turno.id, {
        nuevoEstadoId: estadoDestino,
        observaciones: 'Actualizado desde Calendario',
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          estadoDestino === EstadoTurnoEnum.Completado
            ? 'Turno completado y consumo de insumos actualizado.'
            : 'Estado del turno actualizado.'
        );
        setTurnoActivo(null);
        await cargarDatosAgenda();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo cambiar el estado del turno.',
      });
    } finally {
      setProcesandoAccion(false);
    }
  };

  const abrirModalReprogramar = (turno: TurnoDto) => {
    setTurnoActivo(null);
    setTurnoAReprogramar(turno);
    setReprogPeluqueroId(turno.peluqueroId);
    const fechaIso = extractDateKey(turno.fechaHoraInicio) || fecha;
    const horaTexto = formatTimeOnly(turno.fechaHoraInicio);
    setReprogFecha(fechaIso);
    setReprogHora(horaTexto !== '-' ? horaTexto : '10:00');
    setReprogObservaciones(turno.observaciones || '');
  };

  const handleConfirmarReprogramacion = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!turnoAReprogramar) return;
    setGuardandoReprog(true);
    try {
      const res = await turnosService.reprogramar(turnoAReprogramar.id, {
        peluqueroId: reprogPeluqueroId,
        nuevaFechaHoraInicio: `${reprogFecha}T${reprogHora}:00`,
        observaciones:
          reprogObservaciones.trim() || 'Reprogramado desde Calendario',
      });
      if (res.success) {
        notifyApiSuccess(res, 'Turno reprogramado correctamente.');
        setTurnoAReprogramar(null);
        await cargarDatosAgenda();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage:
          'No se pudo reprogramar el turno por conflicto de horario.',
      });
    } finally {
      setGuardandoReprog(false);
    }
  };

  const handleConfirmarCambioEstado = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!turnoCambiarEstado) return;
    setGuardandoEstado(true);
    try {
      const res = await turnosService.cambiarEstado(turnoCambiarEstado.id, {
        nuevoEstadoId,
        motivoCancelacionId:
          nuevoEstadoId === EstadoTurnoEnum.Cancelado
            ? motivoCancelacionId
            : null,
        observaciones: obsEstado || 'Actualizado desde recepción',
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          nuevoEstadoId === EstadoTurnoEnum.Completado
            ? 'Turno completado y consumo de insumos actualizado.'
            : 'Estado del turno actualizado.'
        );
        setTurnoCambiarEstado(null);
        setObsEstado('');
        await cargarDatosAgenda();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo actualizar el estado del turno.',
      });
    } finally {
      setGuardandoEstado(false);
    }
  };

  const cargarBloqueosDeEstilista = async (idEstilista: number) => {
    setBloqueoPeluqueroId(idEstilista);
    try {
      const res = await bloqueosService.getPorPeluquero(idEstilista);
      if (res.success && res.data) {
        setBloqueosLista(res.data);
      } else {
        setBloqueosLista([]);
      }
    } catch {
      setBloqueosLista([]);
    }
  };

  const handleCrearBloqueo = async (e: React.FormEvent) => {
    e.preventDefault();
    setGuardandoBloqueo(true);
    try {
      const res = await bloqueosService.crear({
        peluqueroId: bloqueoPeluqueroId,
        fechaInicio: bloqueoInicio,
        fechaFin: bloqueoFin,
        motivo: bloqueoMotivo,
      });
      if (res.success) {
        notifyApiSuccess(res, 'Bloqueo horario registrado en la agenda.');
        setBloqueoInicio('');
        setBloqueoFin('');
        setBloqueoMotivo('');
        await cargarBloqueosDeEstilista(bloqueoPeluqueroId);
        await cargarDatosAgenda();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo registrar el bloqueo de agenda.',
      });
    } finally {
      setGuardandoBloqueo(false);
    }
  };

  const handleEliminarBloqueo = async (id?: number) => {
    if (!id) return;
    try {
      const res = await bloqueosService.eliminar(id);
      if (res.success) {
        notifyApiSuccess(res, 'Bloqueo horario eliminado.');
        await cargarBloqueosDeEstilista(bloqueoPeluqueroId);
        await cargarDatosAgenda();
      }
    } catch (err) {
      notifyApiError(err);
    }
  };

  const handlePasarACobrarEnCaja = (turno: TurnoDto) => {
    setTurnoActivo(null);
    enviarTurnoAlPos(turno);
    router.push(`/recepcion/pos?turnoId=${turno.id}`);
  };

  const verHistorial = async (turnoId: number) => {
    setTurnoActivo(null);
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

  const turnosFiltrados = useMemo(() => {
    return turnos.filter(
      (t) =>
        (filtroEstado === 0 || t.estadoId === filtroEstado) &&
        (viewMode === 'dia' ||
          filtroPeluqueroSemana === 0 ||
          t.peluqueroId === filtroPeluqueroSemana)
    );
  }, [turnos, filtroEstado, viewMode, filtroPeluqueroSemana]);

  const gridHours = useMemo(
    () =>
      Array.from(
        { length: END_HOUR - START_HOUR + 1 },
        (_, i) => START_HOUR + i
      ),
    []
  );
  const totalGridHeight = (END_HOUR - START_HOUR) * PIXELS_PER_HOUR;
  const totalSlots = (END_HOUR - START_HOUR) * 2;
  const currentDayOfWeek = useMemo(
    () => parseLocalDateIso(fecha).getDay(),
    [fecha]
  );

  const nowLineTopPx =
    nowMinutes !== null &&
    fecha === todayIso &&
    nowMinutes >= START_HOUR * 60 &&
    nowMinutes <= END_HOUR * 60
      ? ((nowMinutes - START_HOUR * 60) / 60) * PIXELS_PER_HOUR
      : null;

  return (
    <div className="max-w-[1440px] mx-auto w-full space-y-4">
      {/* Cabecera y Toolbar Compacta */}
      <header className="bg-surface border border-border-subtle rounded-xl p-4 space-y-3">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div className="min-w-0">
            <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary">
              Calendario
            </h1>
            <p className="text-xs sm:text-sm text-ink-secondary mt-0.5">
              Hacé clic en cualquier horario libre para agendar o en un turno
              para gestionarlo.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2 shrink-0">
            <Button
              variant="secondary"
              size="sm"
              leftIcon={<Ban className="h-4 w-4" aria-hidden="true" />}
              onClick={() => {
                if (peluqueros[0]) {
                  void cargarBloqueosDeEstilista(peluqueros[0].id);
                }
                setModalBloqueoOpen(true);
              }}
            >
              Bloqueos
            </Button>
            <Button
              variant="primary"
              size="sm"
              leftIcon={<Plus className="h-4 w-4" aria-hidden="true" />}
              onClick={() => abrirModalNuevoTurno()}
            >
              Nuevo Turno
            </Button>
          </div>
        </div>

        {/* Barra de controles de vista, fecha y filtros */}
        <div className="flex flex-wrap items-center justify-between gap-2.5 pt-2.5 border-t border-border-subtle">
          <div className="flex flex-wrap items-center gap-2">
            {/* Switch Día | Semana */}
            <div className="inline-flex p-1 rounded-lg bg-elevated border border-border-subtle">
              {(
                [
                  { id: 'dia', label: 'Día' },
                  { id: 'semana', label: 'Semana' },
                ] as const
              ).map((item) => (
                <button
                  key={item.id}
                  type="button"
                  onClick={() => setViewMode(item.id)}
                  className={`h-7 px-3 rounded-md text-xs font-medium transition-colors ${
                    viewMode === item.id
                      ? 'bg-brand-primary text-white shadow-sm'
                      : 'text-ink-secondary hover:text-ink-primary'
                  }`}
                >
                  {item.label}
                </button>
              ))}
            </div>

            <Button
              variant="secondary"
              size="sm"
              onClick={() => setFecha(todayIso)}
            >
              Hoy
            </Button>

            <div className="inline-flex items-center gap-1">
              <button
                type="button"
                onClick={() => handleShiftDate(-1)}
                aria-label="Período anterior"
                className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-border-strong bg-surface text-ink-secondary hover:text-ink-primary hover:bg-elevated transition-colors"
              >
                <ChevronLeft className="h-4 w-4" aria-hidden="true" />
              </button>
              <input
                type="date"
                aria-label="Fecha del calendario"
                value={fecha}
                onChange={(e) => {
                  if (e.target.value) setFecha(e.target.value);
                }}
                className="h-9 rounded-lg border border-border-strong bg-surface px-2.5 text-xs font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              />
              <button
                type="button"
                onClick={() => handleShiftDate(1)}
                aria-label="Período siguiente"
                className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-border-strong bg-surface text-ink-secondary hover:text-ink-primary hover:bg-elevated transition-colors"
              >
                <ChevronRight className="h-4 w-4" aria-hidden="true" />
              </button>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            {viewMode === 'semana' && (
              <select
                aria-label="Filtrar por profesional"
                value={filtroPeluqueroSemana}
                onChange={(e) =>
                  setFiltroPeluqueroSemana(Number(e.target.value))
                }
                className="h-9 rounded-lg border border-border-strong bg-surface px-3 text-xs font-medium text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                <option value={0}>Todos los profesionales</option>
                {peluqueros.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nombre} {p.apellido}
                  </option>
                ))}
              </select>
            )}

            <select
              aria-label="Filtrar por estado"
              value={filtroEstado}
              onChange={(e) => setFiltroEstado(Number(e.target.value))}
              className="h-9 rounded-lg border border-border-strong bg-surface px-3 text-xs font-medium text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
            >
              <option value={0}>Todos los estados</option>
              <option value={EstadoTurnoEnum.Pendiente}>Pendiente</option>
              <option value={EstadoTurnoEnum.Confirmado}>Confirmado</option>
              <option value={EstadoTurnoEnum.EnProceso}>En proceso</option>
              <option value={EstadoTurnoEnum.Completado}>Completado</option>
              <option value={EstadoTurnoEnum.Cancelado}>Cancelado</option>
              <option value={EstadoTurnoEnum.NoAsistio}>No asistió</option>
            </select>
          </div>
        </div>
      </header>

      {/* Cuerpo del Calendario: Vista Día (Grilla Fresha) o Vista Semana */}
      <AsyncStateGuard
        isLoading={cargandoAgenda}
        isError={Boolean(errorAgenda)}
        errorMessage={errorAgenda?.message}
        correlationId={errorAgenda?.correlationId}
        isEmpty={peluqueros.length === 0}
        loadingText="Cargando calendario de profesionales..."
        emptyTitle="Sin profesionales activos"
        emptyDescription="No hay profesionales activos configurados en esta sucursal."
        onRetry={cargarDatosAgenda}
      >
        {viewMode === 'dia' ? (
          /* VISTA DÍA — GRILLA HORARIA VERTICAL ESTILO FRESHA (09:00–20:00) */
          <div className="bg-surface border border-border-subtle rounded-xl overflow-hidden">
            <div className="overflow-x-auto">
              <div
                className="min-w-full"
                style={{
                  minWidth: `${Math.max(
                    680,
                    56 + peluqueros.length * 195
                  )}px`,
                }}
              >
                {/* Fila de encabezados de profesionales */}
                <div className="flex border-b border-border-subtle bg-elevated/60 sticky top-0 z-20">
                  <div className="w-14 shrink-0 border-r border-border-subtle flex items-center justify-center text-[11px] font-mono-tabular font-medium text-ink-secondary">
                    Hora
                  </div>
                  <div
                    className="flex-1 grid divide-x divide-border-subtle"
                    style={{
                      gridTemplateColumns: `repeat(${peluqueros.length}, minmax(190px, 1fr))`,
                    }}
                  >
                    {peluqueros.map((pel) => {
                      const accent = pel.colorAgendaHex || '#4F46E5';
                      const count = turnosFiltrados.filter(
                        (t) => t.peluqueroId === pel.id
                      ).length;
                      const fullName = `${pel.nombre} ${pel.apellido}`.trim();
                      return (
                        <div
                          key={pel.id}
                          style={{ borderTopColor: accent }}
                          className="border-t-4 px-3 py-2.5 flex items-center justify-between gap-2 min-w-0"
                        >
                          <div className="flex items-center gap-2 min-w-0">
                            <span
                              style={{ backgroundColor: accent }}
                              className="h-7 w-7 rounded-full text-white text-[11px] font-semibold flex items-center justify-center shrink-0 shadow-sm"
                            >
                              {getInitials(fullName)}
                            </span>
                            <span className="text-xs font-semibold text-ink-primary truncate">
                              {fullName}
                            </span>
                          </div>
                          <span className="font-mono-tabular text-[11px] font-semibold px-2 py-0.5 rounded-md bg-surface border border-border-subtle text-ink-secondary shrink-0">
                            {count}
                          </span>
                        </div>
                      );
                    })}
                  </div>
                </div>

                {/* Cuerpo horario 09:00 – 20:00 */}
                <div className="flex relative">
                  {/* Eje de horas */}
                  <div
                    className="w-14 shrink-0 border-r border-border-subtle bg-elevated/30 relative select-none"
                    style={{ height: `${totalGridHeight}px` }}
                  >
                    {gridHours.slice(0, -1).map((hour, idx) => (
                      <div
                        key={hour}
                        className="absolute left-0 right-0 pr-2 text-right font-mono-tabular text-[11px] font-medium text-ink-secondary"
                        style={{ top: `${idx * PIXELS_PER_HOUR + 4}px` }}
                      >
                        {String(hour).padStart(2, '0')}:00
                      </div>
                    ))}
                  </div>

                  {/* Columnas de profesionales */}
                  <div
                    className="flex-1 grid divide-x divide-border-subtle relative"
                    style={{
                      gridTemplateColumns: `repeat(${peluqueros.length}, minmax(190px, 1fr))`,
                      height: `${totalGridHeight}px`,
                    }}
                  >
                    {/* Línea de hora actual cruzando todas las columnas */}
                    {nowLineTopPx !== null && (
                      <div
                        className="absolute left-0 right-0 z-20 flex items-center pointer-events-none"
                        style={{ top: `${nowLineTopPx}px` }}
                      >
                        <span className="h-2.5 w-2.5 -ml-1.5 rounded-full bg-red-600 dark:bg-rose-500 ring-2 ring-surface" />
                        <div className="h-0.5 flex-1 bg-red-600/80 dark:bg-rose-500/80" />
                      </div>
                    )}

                    {peluqueros.map((pel) => {
                      const accent = pel.colorAgendaHex || '#4F46E5';
                      const pelTurnos = turnosFiltrados.filter(
                        (t) => t.peluqueroId === pel.id
                      );
                      const positioned = computeColumnPositions(pelTurnos);

                      // Descanso laboral del profesional para el día actual
                      const horarioDia = (
                        horariosPorPeluquero[pel.id] || []
                      ).find(
                        (h) =>
                          h.diaSemana === currentDayOfWeek && h.activo !== false
                      );
                      const descStart = parseMinutesFromTimeOrIso(
                        horarioDia?.horaInicioDescanso
                      );
                      const descEnd = parseMinutesFromTimeOrIso(
                        horarioDia?.horaFinDescanso
                      );

                      // Bloqueos del día
                      const dayStartMs = new Date(`${fecha}T00:00:00`).getTime();
                      const dayEndMs = new Date(`${fecha}T23:59:59`).getTime();
                      const bloqueosDia = (bloqueosPorPeluquero[pel.id] || [])
                        .map((b) => {
                          const s = new Date(b.fechaInicio).getTime();
                          const e = new Date(b.fechaFin).getTime();
                          if (
                            Number.isNaN(s) ||
                            Number.isNaN(e) ||
                            e <= dayStartMs ||
                            s >= dayEndMs
                          ) {
                            return null;
                          }
                          const cs = new Date(Math.max(s, dayStartMs));
                          const ce = new Date(Math.min(e, dayEndMs));
                          const sm = cs.getHours() * 60 + cs.getMinutes();
                          const em = Math.max(
                            sm + 30,
                            ce.getHours() * 60 + ce.getMinutes()
                          );
                          return { bloqueo: b, startMin: sm, endMin: em };
                        })
                        .filter(
                          (
                            item
                          ): item is {
                            bloqueo: BloqueoHorarioDto;
                            startMin: number;
                            endMin: number;
                          } => item !== null
                        );

                      return (
                        <div
                          key={pel.id}
                          className="relative"
                          style={{ height: `${totalGridHeight}px` }}
                        >
                          {/* Celdas interactivas de 30 min para agendar con 1 clic */}
                          {Array.from({ length: totalSlots }, (_, slotIdx) => {
                            const slotMin = START_HOUR * 60 + slotIdx * 30;
                            const slotLabel = formatSlotTime(slotMin);
                            const isHourLine = slotIdx % 2 === 0;
                            return (
                              <button
                                key={slotIdx}
                                type="button"
                                onClick={() =>
                                  abrirModalNuevoTurno({
                                    peluqueroId: pel.id,
                                    fecha,
                                    hora: slotLabel,
                                  })
                                }
                                title={`Agendar con ${pel.nombre} a las ${slotLabel}`}
                                style={{
                                  top: `${slotIdx * SLOT_HEIGHT}px`,
                                  height: `${SLOT_HEIGHT}px`,
                                }}
                                className={`group absolute left-0 right-0 w-full text-left px-2 transition-colors hover:bg-brand-primary/10 ${
                                  isHourLine
                                    ? 'border-t border-border-subtle'
                                    : 'border-t border-dashed border-border-subtle/50'
                                }`}
                              >
                                <span className="opacity-0 group-hover:opacity-100 text-[10px] font-mono-tabular font-medium text-brand-primary transition-opacity">
                                  + {slotLabel}
                                </span>
                              </button>
                            );
                          })}

                          {/* Descanso sombreado */}
                          {descStart !== null &&
                            descEnd !== null &&
                            descEnd > descStart && (
                              <div
                                className="absolute left-1 right-1 z-[5] rounded-md border border-dashed border-border-strong bg-elevated/90 px-2 py-1 text-[11px] text-ink-secondary pointer-events-none overflow-hidden"
                                style={{
                                  top: `${
                                    ((Math.max(descStart, START_HOUR * 60) -
                                      START_HOUR * 60) /
                                      60) *
                                    PIXELS_PER_HOUR
                                  }px`,
                                  height: `${Math.max(
                                    24,
                                    ((Math.min(descEnd, END_HOUR * 60) -
                                      Math.max(descStart, START_HOUR * 60)) /
                                      60) *
                                      PIXELS_PER_HOUR
                                  )}px`,
                                }}
                              >
                                <span className="font-medium truncate block">
                                  Descanso
                                </span>
                              </div>
                            )}

                          {/* Bloqueos horarios sombreados */}
                          {bloqueosDia.map(({ bloqueo, startMin, endMin }) => {
                            const cs = Math.max(startMin, START_HOUR * 60);
                            const ce = Math.min(endMin, END_HOUR * 60);
                            if (ce <= cs) return null;
                            return (
                              <div
                                key={
                                  bloqueo.id ||
                                  `${bloqueo.fechaInicio}-${bloqueo.motivo}`
                                }
                                className={`absolute left-1 right-1 z-[6] rounded-md border px-2 py-1 text-[11px] pointer-events-none overflow-hidden ${SEMANTIC_COLORS.warning.surfaceClass}`}
                                style={{
                                  top: `${
                                    ((cs - START_HOUR * 60) / 60) *
                                    PIXELS_PER_HOUR
                                  }px`,
                                  height: `${Math.max(
                                    26,
                                    ((ce - cs) / 60) * PIXELS_PER_HOUR
                                  )}px`,
                                }}
                              >
                                <span
                                  className={`font-semibold truncate block ${SEMANTIC_COLORS.warning.textClass}`}
                                >
                                  Bloqueo: {bloqueo.motivo}
                                </span>
                              </div>
                            );
                          })}

                          {/* Bloques de turno proporcionales */}
                          {positioned.map(
                            ({ turno, startMin, endMin, lane, totalLanes }) => {
                              const clampedStart = Math.max(
                                START_HOUR * 60,
                                Math.min(END_HOUR * 60 - 30, startMin)
                              );
                              const top =
                                ((clampedStart - START_HOUR * 60) / 60) *
                                PIXELS_PER_HOUR;
                              const rawHeight =
                                ((endMin - startMin) / 60) * PIXELS_PER_HOUR;
                              const height = Math.max(54, rawHeight);
                              const cardAccent =
                                turno.colorAgendaHex || accent;
                              const serviciosTexto = (turno.servicios ?? [])
                                .map((s) => s.servicioNombre)
                                .join(' + ');

                              return (
                                <button
                                  key={turno.id}
                                  type="button"
                                  onClick={() => setTurnoActivo(turno)}
                                  style={{
                                    top: `${top + 2}px`,
                                    height: `${height - 4}px`,
                                    left: `calc(${
                                      (lane / totalLanes) * 100
                                    }% + 4px)`,
                                    width: `calc(${100 / totalLanes}% - 8px)`,
                                    borderLeftColor: cardAccent,
                                  }}
                                  className="absolute z-10 text-left rounded-lg border border-border-subtle border-l-4 bg-surface hover:bg-elevated/90 shadow-sm hover:shadow-md transition-all px-2.5 py-1.5 flex flex-col justify-between overflow-hidden"
                                >
                                  <div className="min-w-0 space-y-0.5">
                                    <div className="flex items-center justify-between gap-1">
                                      <span className="font-mono-tabular text-[11px] font-semibold text-ink-secondary truncate">
                                        {formatTimeOnly(turno.fechaHoraInicio)}
                                      </span>
                                      <Badge
                                        variant={getEstadoBadgeVariant(
                                          turno.estadoId
                                        )}
                                        className="text-[10px] px-1.5 py-0 leading-4"
                                      >
                                        {turno.estadoNombre}
                                      </Badge>
                                    </div>
                                    <p className="text-xs font-semibold text-ink-primary truncate">
                                      {turno.clienteNombreCompleto}
                                    </p>
                                    {serviciosTexto && height >= 64 && (
                                      <p className="text-[11px] text-ink-secondary truncate">
                                        {serviciosTexto}
                                      </p>
                                    )}
                                  </div>

                                  {height >= 84 && (
                                    <div className="flex items-center justify-between gap-1 pt-1">
                                      <span className="font-mono-tabular text-[11px] font-semibold text-ink-primary">
                                        {formatCurrency(
                                          turno.montoTotalEstimado,
                                          moneda
                                        )}
                                      </span>
                                      <div className="flex items-center gap-1">
                                        {turno.senaPagada && (
                                          <Badge
                                            variant="success"
                                            className="text-[10px] px-1.5 py-0"
                                          >
                                            Seña
                                          </Badge>
                                        )}
                                        {turno.tieneAlertaAlergia && (
                                          <AlertTriangle
                                            className={`h-3.5 w-3.5 ${SEMANTIC_COLORS.danger.textClass}`}
                                            aria-label="Alerta de alergia"
                                          />
                                        )}
                                      </div>
                                    </div>
                                  )}
                                </button>
                              );
                            }
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            </div>
          </div>
        ) : (
          /* VISTA SEMANA — GRILLA LUNES A SÁBADO */
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-3">
            {weekDays.map((day) => {
              const turnosDia = turnosFiltrados
                .filter((t) => extractDateKey(t.fechaHoraInicio) === day.iso)
                .sort((a, b) =>
                  a.fechaHoraInicio.localeCompare(b.fechaHoraInicio)
                );
              const isToday = day.iso === todayIso;

              return (
                <div
                  key={day.iso}
                  className={`bg-surface border rounded-xl overflow-hidden flex flex-col min-w-0 ${
                    isToday
                      ? 'border-brand-primary ring-1 ring-brand-primary/30'
                      : 'border-border-subtle'
                  }`}
                >
                  <div
                    className={`px-3 py-2.5 border-b border-border-subtle flex items-center justify-between gap-2 ${
                      isToday ? 'bg-brand-primary/10' : 'bg-elevated/60'
                    }`}
                  >
                    <div className="min-w-0">
                      <p
                        className={`text-xs font-semibold ${
                          isToday ? 'text-brand-primary' : 'text-ink-primary'
                        }`}
                      >
                        {day.weekday} {day.dayNumber}
                      </p>
                      <p className="text-[11px] text-ink-secondary">
                        {day.monthShort} · {turnosDia.length}
                      </p>
                    </div>
                    <button
                      type="button"
                      onClick={() =>
                        abrirModalNuevoTurno({
                          fecha: day.iso,
                          peluqueroId:
                            filtroPeluqueroSemana > 0
                              ? filtroPeluqueroSemana
                              : undefined,
                        })
                      }
                      aria-label={`Nuevo turno para ${day.weekday} ${day.dayNumber}`}
                      className="inline-flex h-7 w-7 items-center justify-center rounded-lg border border-border-strong bg-surface text-ink-secondary hover:text-brand-primary hover:bg-elevated transition-colors shrink-0"
                    >
                      <Plus className="h-3.5 w-3.5" aria-hidden="true" />
                    </button>
                  </div>

                  <div className="p-2.5 space-y-2 flex-1 min-h-[240px]">
                    {turnosDia.length === 0 ? (
                      <p className="text-xs text-ink-secondary/80 text-center py-8">
                        Sin turnos
                      </p>
                    ) : (
                      turnosDia.map((turno) => (
                        <button
                          key={turno.id}
                          type="button"
                          onClick={() => setTurnoActivo(turno)}
                          style={{
                            borderLeftColor:
                              turno.colorAgendaHex || '#4F46E5',
                          }}
                          className="w-full text-left rounded-lg border border-border-subtle border-l-4 bg-elevated/40 hover:bg-elevated p-2.5 space-y-1 transition-colors"
                        >
                          <div className="flex items-center justify-between gap-1">
                            <span className="font-mono-tabular text-xs font-semibold text-ink-primary">
                              {formatTimeOnly(turno.fechaHoraInicio)}
                            </span>
                            <Badge
                              variant={getEstadoBadgeVariant(turno.estadoId)}
                              className="text-[10px] px-1.5 py-0"
                            >
                              {turno.estadoNombre}
                            </Badge>
                          </div>
                          <p className="text-xs font-semibold text-ink-primary truncate">
                            {turno.clienteNombreCompleto}
                          </p>
                          <p className="text-[11px] text-ink-secondary truncate">
                            {turno.peluqueroNombreCompleto}
                          </p>
                        </button>
                      ))
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </AsyncStateGuard>

      {/* Modal de Acciones Rápidas sobre un Turno */}
      <Modal
        isOpen={Boolean(turnoActivo)}
        onClose={() => setTurnoActivo(null)}
        title={turnoActivo?.clienteNombreCompleto || 'Detalle del turno'}
        subtitle={
          turnoActivo
            ? `${turnoActivo.peluqueroNombreCompleto} · ${formatDateOnly(
                turnoActivo.fechaHoraInicio
              )} · ${formatTimeOnly(
                turnoActivo.fechaHoraInicio
              )} – ${formatTimeOnly(turnoActivo.fechaHoraFin)} hs`
            : undefined
        }
      >
        {turnoActivo && (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant={getEstadoBadgeVariant(turnoActivo.estadoId)}>
                {turnoActivo.estadoNombre}
              </Badge>
              {turnoActivo.senaPagada && (
                <Badge variant="success">Seña abonada</Badge>
              )}
              {turnoActivo.tieneAlertaAlergia && (
                <Badge variant="danger">
                  <AlertTriangle className="h-3 w-3" aria-hidden="true" />
                  Alerta de alergia
                </Badge>
              )}
              <span className="font-mono-tabular text-sm font-bold text-ink-primary ml-auto">
                {formatCurrency(turnoActivo.montoTotalEstimado, moneda)}
              </span>
            </div>

            <div className="rounded-lg bg-elevated p-3.5 space-y-1.5 text-sm">
              <p className="text-xs font-medium text-ink-secondary">
                Servicios programados
              </p>
              <p className="font-medium text-ink-primary">
                {(turnoActivo.servicios ?? [])
                  .map((s) => s.servicioNombre)
                  .join(' + ') || 'Servicio de salón'}
              </p>
              {turnoActivo.observaciones && (
                <p className="text-xs text-ink-secondary pt-1">
                  Observaciones: {turnoActivo.observaciones}
                </p>
              )}
            </div>

            {/* Transiciones rápidas de estado */}
            <div className="space-y-2">
              <p className="text-xs font-medium text-ink-secondary">
                Cambiar estado rápidamente
              </p>
              <div className="flex flex-wrap gap-2">
                {turnoActivo.estadoId === EstadoTurnoEnum.Pendiente && (
                  <Button
                    variant="secondary"
                    size="sm"
                    isLoading={procesandoAccion}
                    leftIcon={
                      <CheckCircle2 className="h-3.5 w-3.5" aria-hidden="true" />
                    }
                    onClick={() =>
                      handleCambioEstadoDirecto(
                        turnoActivo,
                        EstadoTurnoEnum.Confirmado
                      )
                    }
                  >
                    Confirmar
                  </Button>
                )}
                {(turnoActivo.estadoId === EstadoTurnoEnum.Pendiente ||
                  turnoActivo.estadoId === EstadoTurnoEnum.Confirmado) && (
                  <Button
                    variant="secondary"
                    size="sm"
                    isLoading={procesandoAccion}
                    leftIcon={<Play className="h-3.5 w-3.5" aria-hidden="true" />}
                    onClick={() =>
                      handleCambioEstadoDirecto(
                        turnoActivo,
                        EstadoTurnoEnum.EnProceso
                      )
                    }
                  >
                    En proceso
                  </Button>
                )}
                {turnoActivo.estadoId !== EstadoTurnoEnum.Completado &&
                  turnoActivo.estadoId !== EstadoTurnoEnum.Cancelado && (
                    <Button
                      variant="secondary"
                      size="sm"
                      isLoading={procesandoAccion}
                      leftIcon={
                        <CheckCircle2
                          className="h-3.5 w-3.5"
                          aria-hidden="true"
                        />
                      }
                      onClick={() =>
                        handleCambioEstadoDirecto(
                          turnoActivo,
                          EstadoTurnoEnum.Completado
                        )
                      }
                    >
                      Completar
                    </Button>
                  )}
                {turnoActivo.estadoId !== EstadoTurnoEnum.Cancelado &&
                  turnoActivo.estadoId !== EstadoTurnoEnum.Completado && (
                    <Button
                      variant="destructive"
                      destructiveTone="soft"
                      size="sm"
                      leftIcon={
                        <XCircle className="h-3.5 w-3.5" aria-hidden="true" />
                      }
                      onClick={() =>
                        handleCambioEstadoDirecto(
                          turnoActivo,
                          EstadoTurnoEnum.Cancelado
                        )
                      }
                    >
                      Cancelar turno
                    </Button>
                  )}
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    const target = turnoActivo;
                    setTurnoActivo(null);
                    setTurnoCambiarEstado(target);
                    setNuevoEstadoId(target.estadoId);
                  }}
                >
                  Más opciones...
                </Button>
              </div>
            </div>

            {/* Acciones principales */}
            <div className="flex flex-wrap items-center justify-between gap-2 pt-3 border-t border-border-subtle">
              <div className="flex flex-wrap items-center gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  leftIcon={
                    <CalendarClock className="h-3.5 w-3.5" aria-hidden="true" />
                  }
                  onClick={() => abrirModalReprogramar(turnoActivo)}
                >
                  Reprogramar
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  leftIcon={
                    <History className="h-3.5 w-3.5" aria-hidden="true" />
                  }
                  onClick={() => verHistorial(turnoActivo.id)}
                >
                  Historial
                </Button>
              </div>

              {(turnoActivo.estadoId === EstadoTurnoEnum.Completado ||
                turnoActivo.estadoId === EstadoTurnoEnum.Confirmado ||
                turnoActivo.estadoId === EstadoTurnoEnum.EnProceso) && (
                <Button
                  variant="primary"
                  size="sm"
                  leftIcon={
                    <CreditCard className="h-3.5 w-3.5" aria-hidden="true" />
                  }
                  onClick={() => handlePasarACobrarEnCaja(turnoActivo)}
                >
                  Cobrar en POS
                </Button>
              )}
            </div>
          </div>
        )}
      </Modal>

      {/* Modal Nuevo Turno con SearchCombobox de Clientes + Registro Express */}
      <Modal
        isOpen={modalNuevoOpen}
        onClose={() => setModalNuevoOpen(false)}
        title="Nuevo Turno"
        subtitle="Seleccioná cliente, profesional y servicios para verificar disponibilidad en vivo."
        maxWidthClass="max-w-2xl"
      >
        <form onSubmit={handleCrearTurno} className="space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-2 rounded-lg bg-elevated px-3.5 py-2.5 border border-border-subtle text-xs">
            <span className="text-ink-secondary">
              ¿Es un cliente nuevo en el salón?
            </span>
            <Button
              type="button"
              variant="secondary"
              size="sm"
              leftIcon={<UserPlus className="h-3.5 w-3.5" aria-hidden="true" />}
              onClick={() => setModoWalkIn((prev) => !prev)}
            >
              {modoWalkIn ? 'Elegir cliente existente' : 'Registro Express'}
            </Button>
          </div>

          {modoWalkIn ? (
            <div className="p-4 rounded-xl border border-brand-primary/40 bg-brand-primary/5 space-y-3">
              <p className="text-xs font-semibold text-brand-primary">
                Registro Express de cliente nuevo
              </p>
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
                <InputField
                  id="walkin-nombre"
                  label="Nombre *"
                  value={walkInNombre}
                  onChange={(e) => setWalkInNombre(e.target.value)}
                />
                <InputField
                  id="walkin-apellido"
                  label="Apellido *"
                  value={walkInApellido}
                  onChange={(e) => setWalkInApellido(e.target.value)}
                />
                <InputField
                  id="walkin-telefono"
                  label="Teléfono *"
                  value={walkInTelefono}
                  onChange={(e) => setWalkInTelefono(e.target.value)}
                />
              </div>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                isLoading={creandoWalkIn}
                onClick={handleCrearClienteExpress}
              >
                Guardar y seleccionar cliente
              </Button>
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <SearchCombobox<number>
                id="calendario-cliente-combobox"
                label="Cliente *"
                placeholder="Buscar por nombre o teléfono..."
                emptyMessage="No se encontraron clientes con ese criterio."
                options={clienteOptions}
                value={clienteId}
                onChange={(val) => setClienteId(val)}
                footerAction={
                  <button
                    type="button"
                    onClick={() => setModoWalkIn(true)}
                    className="w-full text-left text-xs font-medium text-brand-primary hover:underline inline-flex items-center gap-1.5"
                  >
                    <UserPlus className="h-3.5 w-3.5" aria-hidden="true" />
                    <span>Registrar cliente nuevo (Express)</span>
                  </button>
                }
              />

              <div className="flex flex-col gap-1.5">
                <label
                  htmlFor="calendario-nuevo-profesional"
                  className="text-xs font-medium text-ink-secondary"
                >
                  Profesional *
                </label>
                <select
                  id="calendario-nuevo-profesional"
                  value={peluqueroId}
                  onChange={(e) => setPeluqueroId(Number(e.target.value))}
                  className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
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

          {/* Fecha y Horario */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <InputField
              id="calendario-nuevo-fecha"
              label="Fecha del turno *"
              type="date"
              required
              value={nuevoTurnoFecha}
              onChange={(e) => setNuevoTurnoFecha(e.target.value)}
            />

            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="calendario-nuevo-horario"
                className="text-xs font-medium text-ink-secondary"
              >
                Horario{' '}
                {slotsDisponibles.length > 0
                  ? `(${slotsDisponibles.length} disponibles)`
                  : '*'}
              </label>
              {slotsDisponibles.length > 0 ? (
                <select
                  id="calendario-nuevo-horario"
                  value={horaInicio}
                  onChange={(e) => setHoraInicio(e.target.value)}
                  className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                >
                  {slotsDisponibles.map((slot) => (
                    <option key={slot} value={slot}>
                      {slot} hs
                    </option>
                  ))}
                </select>
              ) : (
                <input
                  id="calendario-nuevo-horario"
                  type="time"
                  value={horaInicio}
                  onChange={(e) => setHoraInicio(e.target.value)}
                  className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                />
              )}
            </div>
          </div>

          {/* Selección de servicios */}
          <div className="space-y-1.5">
            <label className="text-xs font-medium text-ink-secondary block">
              Servicios solicitados *
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 max-h-44 overflow-y-auto p-2.5 border border-border-subtle rounded-lg bg-elevated/40">
              {servicios.map((s) => {
                const checked = serviciosIds.includes(s.id);
                return (
                  <label
                    key={s.id}
                    className={`flex items-center justify-between gap-2 rounded-lg border px-2.5 py-2 text-xs cursor-pointer transition-colors ${
                      checked
                        ? 'border-brand-primary bg-brand-primary/10 text-ink-primary'
                        : 'border-border-subtle bg-surface text-ink-primary hover:bg-elevated'
                    }`}
                  >
                    <span className="flex items-center gap-2 min-w-0">
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={() =>
                          setServiciosIds((prev) =>
                            prev.includes(s.id)
                              ? prev.filter((id) => id !== s.id)
                              : [...prev, s.id]
                          )
                        }
                        className="rounded border-border-strong text-brand-primary focus:ring-brand-accent"
                      />
                      <span className="truncate font-medium">{s.nombre}</span>
                    </span>
                    <span className="font-mono-tabular text-[11px] text-ink-secondary shrink-0">
                      {s.duracionMinutos}m · {formatCurrency(s.precio, moneda)}
                    </span>
                  </label>
                );
              })}
            </div>
          </div>

          <InputField
            id="calendario-nuevo-observaciones"
            label="Observaciones o alergias"
            value={observaciones}
            onChange={(e) => setObservaciones(e.target.value)}
            placeholder="Notas útiles para el profesional"
          />

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setModalNuevoOpen(false)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              isLoading={guardandoTurno}
              disabled={!clienteId || !peluqueroId || serviciosIds.length === 0}
            >
              Confirmar Turno
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Reprogramar Turno */}
      <Modal
        isOpen={Boolean(turnoAReprogramar)}
        onClose={() => setTurnoAReprogramar(null)}
        title={`Reprogramar turno — ${turnoAReprogramar?.clienteNombreCompleto || ''}`}
        subtitle="Se verificará automáticamente la disponibilidad del profesional."
      >
        <form onSubmit={handleConfirmarReprogramacion} className="space-y-4">
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="reprog-profesional"
              className="text-xs font-medium text-ink-secondary"
            >
              Profesional asignado *
            </label>
            <select
              id="reprog-profesional"
              value={reprogPeluqueroId}
              onChange={(e) => setReprogPeluqueroId(Number(e.target.value))}
              className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
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
              id="reprog-fecha"
              label="Nueva fecha *"
              type="date"
              required
              value={reprogFecha}
              onChange={(e) => setReprogFecha(e.target.value)}
            />
            <InputField
              id="reprog-hora"
              label="Nuevo horario *"
              type="time"
              required
              value={reprogHora}
              onChange={(e) => setReprogHora(e.target.value)}
            />
          </div>

          <InputField
            id="reprog-obs"
            label="Motivo u observaciones"
            value={reprogObservaciones}
            onChange={(e) => setReprogObservaciones(e.target.value)}
          />

          <div className="flex justify-end gap-2 pt-1">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setTurnoAReprogramar(null)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              isLoading={guardandoReprog}
            >
              Guardar Reprogramación
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Cambiar Estado Completo */}
      <Modal
        isOpen={Boolean(turnoCambiarEstado)}
        onClose={() => setTurnoCambiarEstado(null)}
        title={`Actualizar estado — ${turnoCambiarEstado?.clienteNombreCompleto || ''}`}
      >
        <form onSubmit={handleConfirmarCambioEstado} className="space-y-4">
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="estado-turno-select"
              className="text-xs font-medium text-ink-secondary"
            >
              Nuevo estado *
            </label>
            <select
              id="estado-turno-select"
              value={nuevoEstadoId}
              onChange={(e) =>
                setNuevoEstadoId(Number(e.target.value) as EstadoTurnoEnum)
              }
              className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
            >
              <option value={EstadoTurnoEnum.Pendiente}>Pendiente</option>
              <option value={EstadoTurnoEnum.Confirmado}>
                Confirmado (cliente confirmó asistencia)
              </option>
              <option value={EstadoTurnoEnum.EnProceso}>
                En proceso (en sillón)
              </option>
              <option value={EstadoTurnoEnum.Completado}>
                Completado (atención finalizada)
              </option>
              <option value={EstadoTurnoEnum.Cancelado}>Cancelado</option>
              <option value={EstadoTurnoEnum.NoAsistio}>No asistió</option>
            </select>
          </div>

          {nuevoEstadoId === EstadoTurnoEnum.Cancelado && (
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="motivo-cancelacion-select"
                className="text-xs font-medium text-ink-secondary"
              >
                Motivo de cancelación *
              </label>
              <select
                id="motivo-cancelacion-select"
                value={motivoCancelacionId}
                onChange={(e) => setMotivoCancelacionId(Number(e.target.value))}
                className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
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
            id="obs-estado-turno"
            label="Observaciones"
            value={obsEstado}
            onChange={(e) => setObsEstado(e.target.value)}
            placeholder="Nota opcional"
          />

          <div className="flex justify-end gap-2 pt-1">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setTurnoCambiarEstado(null)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              isLoading={guardandoEstado}
            >
              Guardar Estado
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal de Bloqueos Horarios y Ausencias */}
      <Modal
        isOpen={modalBloqueoOpen}
        onClose={() => setModalBloqueoOpen(false)}
        title="Bloqueos Horarios y Ausencias"
        subtitle="Bloqueá horarios específicos para evitar superposiciones en la agenda."
        maxWidthClass="max-w-2xl"
      >
        <div className="space-y-5">
          <form
            onSubmit={handleCrearBloqueo}
            className="space-y-3.5 border-b border-border-subtle pb-5"
          >
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="bloqueo-profesional-select"
                className="text-xs font-medium text-ink-secondary"
              >
                Profesional *
              </label>
              <select
                id="bloqueo-profesional-select"
                value={bloqueoPeluqueroId}
                onChange={(e) =>
                  void cargarBloqueosDeEstilista(Number(e.target.value))
                }
                className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
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
                id="bloqueo-inicio"
                label="Inicio del bloqueo *"
                type="datetime-local"
                required
                value={bloqueoInicio}
                onChange={(e) => setBloqueoInicio(e.target.value)}
              />
              <InputField
                id="bloqueo-fin"
                label="Fin del bloqueo *"
                type="datetime-local"
                required
                value={bloqueoFin}
                onChange={(e) => setBloqueoFin(e.target.value)}
              />
            </div>

            <InputField
              id="bloqueo-motivo"
              label="Motivo *"
              required
              value={bloqueoMotivo}
              onChange={(e) => setBloqueoMotivo(e.target.value)}
              placeholder="Ej. Trámite personal / Ausencia programada"
            />

            <Button
              type="submit"
              variant="primary"
              isLoading={guardandoBloqueo}
            >
              Registrar Bloqueo
            </Button>
          </form>

          <div className="space-y-2.5">
            <div className="flex items-center justify-between">
              <h4 className="text-xs font-semibold text-ink-primary">
                Bloqueos activos del profesional
              </h4>
              <Badge variant="neutral" className="font-mono-tabular">
                {bloqueosLista.length}
              </Badge>
            </div>
            {bloqueosLista.length === 0 ? (
              <p className="text-xs text-ink-secondary py-2">
                Sin bloqueos activos para este profesional.
              </p>
            ) : (
              <div className="space-y-2">
                {bloqueosLista.map((b) => (
                  <div
                    key={b.id}
                    className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 p-3 rounded-lg border border-border-subtle bg-elevated/40 text-xs"
                  >
                    <div className="min-w-0">
                      <p className="font-mono-tabular font-semibold text-ink-primary">
                        {formatDateTime(b.fechaInicio)} →{' '}
                        {formatDateTime(b.fechaFin)}
                      </p>
                      <p className="text-ink-secondary mt-0.5">{b.motivo}</p>
                    </div>
                    <Button
                      variant="destructive"
                      destructiveTone="soft"
                      size="sm"
                      leftIcon={
                        <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
                      }
                      onClick={() => handleEliminarBloqueo(b.id)}
                    >
                      Eliminar
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </Modal>

      {/* Modal Historial de Estados del Turno */}
      <Modal
        isOpen={Boolean(historialTurno)}
        onClose={() => setHistorialTurno(null)}
        title="Historial de cambios del turno"
      >
        <div className="space-y-3 text-xs">
          {(historialTurno || []).length === 0 ? (
            <p className="text-ink-secondary py-4 text-center">
              Sin movimientos registrados para este turno.
            </p>
          ) : (
            (historialTurno || []).map((h) => (
              <div
                key={h.id}
                className="border-b border-border-subtle pb-2.5 last:border-b-0 space-y-1"
              >
                <div className="flex items-center justify-between gap-2">
                  <span className="font-mono-tabular font-semibold text-ink-primary">
                    {formatDateTime(h.fechaCambio)}
                  </span>
                  <Badge variant="neutral">{h.estadoNuevoNombre}</Badge>
                </div>
                {h.observaciones && (
                  <p className="text-ink-secondary">{h.observaciones}</p>
                )}
              </div>
            ))
          )}
        </div>
      </Modal>
    </div>
  );
}
