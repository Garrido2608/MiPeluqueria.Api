// filepath: src/app/(dashboard)/peluquero/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  ArrowLeft,
  CalendarDays,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Clock,
  ExternalLink,
  FileText,
  FlaskConical,
  Image as ImageIcon,
  Pencil,
  Play,
  Plus,
  Printer,
  Receipt,
  Search,
  Sparkles,
  Trash2,
  Upload,
  Wallet,
} from 'lucide-react';
import {
  BloqueoHorarioDto,
  ClienteDto,
  ComisionConfigDto,
  EstadoTurnoEnum,
  FichaTecnicaDto,
  HorarioLaboralDto,
  LiquidacionDto,
  TurnoDto,
} from '@/types/api.types';
import {
  bloqueosService,
  clientesService,
  comisionesService,
  fichasTecnicasService,
  liquidacionesService,
  peluquerosService,
  recetasServicioService,
  turnosService,
} from '@/services/api.services';
import { useAuth, useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import { uploadImageToCloudinary } from '@/lib/cloudinary';
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
} from '@/components/ui';
import {
  PrintableReceiptData,
  PrintReceiptModal,
} from '@/components/ui/PrintReceiptModal';

type TabKey = 'agenda' | 'fichas' | 'comisiones' | 'horarios';
type AgendaMode = 'hoy' | 'manana' | 'semana';

const DIAS_SEMANA = [
  'Domingo',
  'Lunes',
  'Martes',
  'Miércoles',
  'Jueves',
  'Viernes',
  'Sábado',
];

const DIAS_SEMANA_CORTOS = ['Dom', 'Lun', 'Mar', 'Mié', 'Jue', 'Vie', 'Sáb'];
const ORDEN_DIAS_LABORALES = [1, 2, 3, 4, 5, 6, 0];
const PIXELS_PER_HOUR = 64;

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

function formatHourLabel(hour: number): string {
  return `${String(hour).padStart(2, '0')}:00`;
}

function getInitials(fullName: string): string {
  const words = fullName.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return 'CL';
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  return `${words[0][0]}${words[1][0]}`.toUpperCase();
}

function formatShortDayHeader(isoDate: string): {
  weekday: string;
  dayNumber: string;
  monthShort: string;
} {
  const d = parseLocalDateIso(isoDate);
  const weekday = DIAS_SEMANA_CORTOS[d.getDay()] || '';
  const dayNumber = String(d.getDate()).padStart(2, '0');
  const monthShort = new Intl.DateTimeFormat('es-AR', { month: 'short' })
    .format(d)
    .replace(/\.$/, '');
  return {
    weekday,
    dayNumber,
    monthShort:
      monthShort.charAt(0).toUpperCase() + monthShort.slice(1).toLowerCase(),
  };
}

function getWeekDaysMonToSat(anchorIso: string): {
  iso: string;
  weekday: string;
  dayNumber: string;
  monthShort: string;
  dayOfWeek: number;
}[] {
  const d = parseLocalDateIso(anchorIso);
  const dow = d.getDay();
  const diffToMon = dow === 0 ? -6 : 1 - dow;
  d.setDate(d.getDate() + diffToMon);
  return Array.from({ length: 6 }, (_, index) => {
    const dayDate = new Date(d.getFullYear(), d.getMonth(), d.getDate() + index, 12, 0, 0);
    const iso = toLocalDateIso(dayDate);
    const formatted = formatShortDayHeader(iso);
    return {
      iso,
      ...formatted,
      dayOfWeek: dayDate.getDay(),
    };
  });
}

function getEstadoTurnoBadgeVariant(
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

function getEstadoLiquidacionBadgeVariant(
  estadoTexto?: string | null
): NonNullable<BadgeProps['variant']> {
  const norm = (estadoTexto || '').toLowerCase();
  if (norm.includes('pagad')) return 'success';
  if (norm.includes('anulad') || norm.includes('atras')) return 'danger';
  if (norm.includes('pend')) return 'warning';
  return 'neutral';
}

function formatPercent(value?: number | null): string {
  const n = typeof value === 'number' && Number.isFinite(value) ? value : 0;
  return Number.isInteger(n) ? `${n}%` : `${n.toFixed(1)}%`;
}

interface PositionedTurno {
  turno: TurnoDto;
  startMin: number;
  endMin: number;
  lane: number;
  totalLanes: number;
}

function computeTimelinePositions(turnos: TurnoDto[]): PositionedTurno[] {
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
      const endMin = startMin + Math.max(durationMin, 30);
      return { turno, startMin, endMin };
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
    let assignedLane = laneEnds.findIndex((end) => end <= item.startMin);
    if (assignedLane === -1) {
      assignedLane = laneEnds.length;
      laneEnds.push(item.endMin);
    } else {
      laneEnds[assignedLane] = item.endMin;
    }
    placed.push({ ...item, lane: assignedLane });
  }

  return placed.map((item) => {
    const overlapping = placed.filter(
      (other) => other.startMin < item.endMin && other.endMin > item.startMin
    );
    const maxLane = overlapping.reduce((acc, o) => Math.max(acc, o.lane), 0);
    return {
      ...item,
      totalLanes: Math.max(1, maxLane + 1),
    };
  });
}

export default function PortalPeluqueroPage() {
  const { user } = useAuth();
  const { configSalon } = useCatalogos();
  const moneda = configSalon?.moneda || 'ARS';
  const miPeluqueroId = user?.peluqueroId || 1;

  const [activeTab, setActiveTab] = useState<TabKey>('agenda');

  // 1. Estado Mi Agenda (Hoy | Mañana | Esta semana)
  const [todayIso, setTodayIso] = useState<string>(() => toLocalDateIso(new Date()));
  const [agendaMode, setAgendaMode] = useState<AgendaMode>('hoy');
  const [fechaAgenda, setFechaAgenda] = useState<string>(() =>
    toLocalDateIso(new Date())
  );
  const [nowMinutes, setNowMinutes] = useState<number | null>(null);
  const [misTurnos, setMisTurnos] = useState<TurnoDto[]>([]);
  const [cargandoAgenda, setCargandoAgenda] = useState<boolean>(true);
  const [errorAgenda, setErrorAgenda] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);
  const [turnoDetalleModal, setTurnoDetalleModal] = useState<TurnoDto | null>(
    null
  );
  const [actualizandoTurnoId, setActualizandoTurnoId] = useState<number | null>(
    null
  );

  // 2. Estado Fichas Técnicas Capilares (Master-Detail + Mobile-First)
  const [busquedaCliente, setBusquedaCliente] = useState<string>('');
  const [clientesLista, setClientesLista] = useState<ClienteDto[]>([]);
  const [cargandoClientes, setCargandoClientes] = useState<boolean>(false);
  const [clienteSeleccionado, setClienteSeleccionado] =
    useState<ClienteDto | null>(null);
  const [mobileShowDetail, setMobileShowDetail] = useState<boolean>(false);
  const [fichasCliente, setFichasCliente] = useState<FichaTecnicaDto[]>([]);
  const [cargandoFichas, setCargandoFichas] = useState<boolean>(false);
  const [modalFichaOpen, setModalFichaOpen] = useState<boolean>(false);
  const [fichaEditando, setFichaEditando] = useState<FichaTecnicaDto | null>(
    null
  );
  const [turnoAsociadoId, setTurnoAsociadoId] = useState<number | null>(null);
  const [formulaTintura, setFormulaTintura] = useState<string>('');
  const [notasAlergias, setNotasAlergias] = useState<string>('');
  const [productosUsados, setProductosUsados] = useState<string>('');
  const [fotoUrl, setFotoUrl] = useState<string>('');
  const [subiendoFoto, setSubiendoFoto] = useState<boolean>(false);
  const [guardandoFicha, setGuardandoFicha] = useState<boolean>(false);

  // 3. Estado Mis Comisiones y Liquidaciones
  const [misComisionesConfig, setMisComisionesConfig] = useState<
    ComisionConfigDto[]
  >([]);
  const [misLiquidaciones, setMisLiquidaciones] = useState<LiquidacionDto[]>(
    []
  );
  const [cargandoComisiones, setCargandoComisiones] = useState<boolean>(false);
  const [reciboDetalle, setReciboDetalle] = useState<LiquidacionDto | null>(
    null
  );
  const [reciboImprimir, setReciboImprimir] =
    useState<PrintableReceiptData | null>(null);

  // 4. Estado Mis Horarios y Bloqueos
  const [misHorarios, setMisHorarios] = useState<HorarioLaboralDto[]>([]);
  const [misBloqueos, setMisBloqueos] = useState<BloqueoHorarioDto[]>([]);
  const [bloqueoInicio, setBloqueoInicio] = useState<string>('');
  const [bloqueoFin, setBloqueoFin] = useState<string>('');
  const [bloqueoMotivo, setBloqueoMotivo] = useState<string>('');
  const [guardandoBloqueo, setGuardandoBloqueo] = useState<boolean>(false);

  // Reloj en vivo para línea de hora actual (sin desfase SSR)
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

  const weekDays = useMemo(() => getWeekDaysMonToSat(fechaAgenda), [fechaAgenda]);

  // Cargar horarios y bloqueos del profesional (se usan tanto en Agenda como en Horarios)
  const cargarHorariosYBloqueos = useCallback(async () => {
    try {
      const [horRes, bloqRes] = await Promise.all([
        peluquerosService.getHorarios(miPeluqueroId),
        bloqueosService.getPorPeluquero(miPeluqueroId),
      ]);
      if (horRes.success) {
        setMisHorarios(extractPagedItems<HorarioLaboralDto>(horRes.data));
      }
      if (bloqRes.success) {
        setMisBloqueos(extractPagedItems<BloqueoHorarioDto>(bloqRes.data));
      }
    } catch {
      // Silencioso en carga secundaria de agenda
    }
  }, [miPeluqueroId]);

  useEffect(() => {
    void cargarHorariosYBloqueos();
  }, [cargarHorariosYBloqueos]);

  // Cargar turnos según modo (día puntual vs rango semanal)
  const cargarAgenda = useCallback(async () => {
    setCargandoAgenda(true);
    setErrorAgenda(null);
    try {
      const res =
        agendaMode === 'semana'
          ? await turnosService.getByRange(
              weekDays[0].iso,
              weekDays[weekDays.length - 1].iso,
              miPeluqueroId
            )
          : await turnosService.getAll(fechaAgenda, miPeluqueroId);

      if (res.success) {
        setMisTurnos(extractPagedItems<TurnoDto>(res.data));
      } else {
        setMisTurnos([]);
      }
    } catch (err) {
      setMisTurnos([]);
      setErrorAgenda({
        message: extractErrorMessage(
          err,
          'No se pudieron cargar tus turnos asignados.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
    } finally {
      setCargandoAgenda(false);
    }
  }, [agendaMode, fechaAgenda, miPeluqueroId, weekDays]);

  useEffect(() => {
    if (activeTab === 'agenda') {
      void cargarAgenda();
    } else if (activeTab === 'comisiones') {
      setCargandoComisiones(true);
      Promise.all([
        comisionesService.getByPeluquero(miPeluqueroId),
        liquidacionesService.getPorPeluquero(miPeluqueroId),
      ])
        .then(([comRes, liqRes]) => {
          if (comRes.success) {
            setMisComisionesConfig(
              extractPagedItems<ComisionConfigDto>(comRes.data)
            );
          }
          if (liqRes.success) {
            setMisLiquidaciones(
              extractPagedItems<LiquidacionDto>(liqRes.data)
            );
          }
        })
        .catch((err) => {
          notifyApiError(err, {
            fallbackMessage: 'No se pudieron consultar tus comisiones y recibos.',
          });
        })
        .finally(() => setCargandoComisiones(false));
    } else if (activeTab === 'horarios') {
      void cargarHorariosYBloqueos();
    }
  }, [activeTab, cargarAgenda, cargarHorariosYBloqueos, miPeluqueroId]);

  // Carga inicial + búsqueda instantánea con debounce en Fichas Técnicas
  const fetchClientes = useCallback(async (query: string) => {
    setCargandoClientes(true);
    try {
      const res = await clientesService.getAll(1, 40, query.trim() || undefined);
      if (res.success) {
        setClientesLista(extractPagedItems<ClienteDto>(res.data));
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo obtener la lista de clientes.',
      });
    } finally {
      setCargandoClientes(false);
    }
  }, []);

  useEffect(() => {
    if (activeTab !== 'fichas') return;
    const handle = window.setTimeout(() => {
      void fetchClientes(busquedaCliente);
    }, 220);
    return () => window.clearTimeout(handle);
  }, [activeTab, busquedaCliente, fetchClientes]);

  const handleSelectAgendaMode = (mode: AgendaMode) => {
    setAgendaMode(mode);
    if (mode === 'hoy') {
      setFechaAgenda(todayIso);
    } else if (mode === 'manana') {
      setFechaAgenda(addDaysIso(todayIso, 1));
    }
  };

  const handleShiftAgendaDate = (delta: number) => {
    const step = agendaMode === 'semana' ? delta * 7 : delta;
    const nextDate = addDaysIso(fechaAgenda, step);
    setFechaAgenda(nextDate);
    if (agendaMode !== 'semana') {
      if (nextDate === todayIso) setAgendaMode('hoy');
      else if (nextDate === addDaysIso(todayIso, 1)) setAgendaMode('manana');
    }
  };

  // Acciones rápidas de turno
  const cambiarEstadoRapido = async (
    turno: TurnoDto,
    nuevoEstado: EstadoTurnoEnum
  ) => {
    setActualizandoTurnoId(turno.id);
    try {
      const res = await turnosService.cambiarEstado(turno.id, {
        nuevoEstadoId: nuevoEstado,
        observaciones:
          nuevoEstado === EstadoTurnoEnum.Completado
            ? 'Atención finalizada por el profesional.'
            : 'Atención iniciada en puesto de trabajo.',
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          nuevoEstado === EstadoTurnoEnum.Completado
            ? 'Trabajo finalizado y consumo de insumos actualizado.'
            : 'Atención iniciada.'
        );
        setTurnoDetalleModal(null);
        await cargarAgenda();
      }
    } catch (err: unknown) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo actualizar el estado del turno.',
      });
    } finally {
      setActualizandoTurnoId(null);
    }
  };

  const handleAplicarConsumoGabineteManual = async (turnoId: number) => {
    setActualizandoTurnoId(turnoId);
    try {
      const res = await recetasServicioService.consumirPorTurno(turnoId);
      if (res.success) {
        notifyApiSuccess(
          res,
          'Consumo de insumos del servicio registrado correctamente.'
        );
      }
    } catch (err: unknown) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo registrar el consumo de insumos.',
      });
    } finally {
      setActualizandoTurnoId(null);
    }
  };

  const seleccionarClienteFicha = async (
    cli: ClienteDto,
    openMobileDetail: boolean = true
  ) => {
    setClienteSeleccionado(cli);
    if (openMobileDetail) {
      setMobileShowDetail(true);
    }
    setCargandoFichas(true);
    try {
      const res = await fichasTecnicasService.getPorCliente(cli.id);
      if (res.success) {
        setFichasCliente(extractPagedItems<FichaTecnicaDto>(res.data));
      } else {
        setFichasCliente([]);
      }
    } catch (err) {
      setFichasCliente([]);
      notifyApiError(err, {
        fallbackMessage: 'No se pudieron obtener las fichas técnicas del cliente.',
      });
    } finally {
      setCargandoFichas(false);
    }
  };

  const abrirFichaDesdeTurno = async (turno: TurnoDto) => {
    setTurnoDetalleModal(null);
    setActiveTab('fichas');
    setTurnoAsociadoId(turno.id);
    try {
      const cliRes = await clientesService.getById(turno.clienteId);
      if (cliRes.success && cliRes.data) {
        await seleccionarClienteFicha(cliRes.data, true);
        return;
      }
    } catch {
      // Usa datos disponibles del turno si no se pudo consultar el cliente completo
    }
    const fallbackCli: ClienteDto = {
      id: turno.clienteId,
      nombre: turno.clienteNombreCompleto,
      apellido: '',
      telefono: turno.clienteTelefono || '—',
      fechaNacimiento: '1995-01-01',
      fechaRegistro: new Date().toISOString(),
      activo: true,
    };
    await seleccionarClienteFicha(fallbackCli, true);
  };

  const handleSubirFotoFicha = async (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setSubiendoFoto(true);
    try {
      const uploadedResult = await uploadImageToCloudinary(file);
      setFotoUrl(uploadedResult.url);
    } finally {
      setSubiendoFoto(false);
      e.target.value = '';
    }
  };

  const guardarFichaTecnica = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!clienteSeleccionado) return;

    setGuardandoFicha(true);
    const payload = {
      clienteId: clienteSeleccionado.id,
      peluqueroId: miPeluqueroId,
      turnoId: turnoAsociadoId,
      formulaAplicada: formulaTintura,
      detalleFormulaTintura: formulaTintura,
      notasSensibilidadAlergias: notasAlergias,
      productosUtilizados: productosUsados,
      fotoResultadoUrl: fotoUrl || undefined,
    };

    try {
      const res = fichaEditando
        ? await fichasTecnicasService.actualizar(fichaEditando.id, payload)
        : await fichasTecnicasService.crear(payload);

      if (res.success) {
        notifyApiSuccess(res, 'Ficha técnica guardada correctamente.');
        setModalFichaOpen(false);
        setFichaEditando(null);
        setFormulaTintura('');
        setNotasAlergias('');
        setProductosUsados('');
        setFotoUrl('');
        await seleccionarClienteFicha(clienteSeleccionado, true);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar la ficha técnica.',
      });
    } finally {
      setGuardandoFicha(false);
    }
  };

  const handleEliminarFicha = async (fichaId: number) => {
    if (!clienteSeleccionado) return;
    try {
      const res = await fichasTecnicasService.eliminar(fichaId);
      if (res.success) {
        notifyApiSuccess(res, 'Ficha técnica eliminada.');
        await seleccionarClienteFicha(clienteSeleccionado, true);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar la ficha técnica.',
      });
    }
  };

  const abrirDetalleRecibo = async (liquidacionId: number) => {
    try {
      const res = await liquidacionesService.getById(liquidacionId);
      if (res.success && res.data) {
        setReciboDetalle(res.data);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo obtener el desglose del recibo.',
      });
    }
  };

  const imprimirMiRecibo = async (liquidacionId: number) => {
    try {
      const res = await liquidacionesService.getById(liquidacionId);
      if (res.success && res.data) {
        const l = res.data;
        setReciboImprimir({
          titulo: 'RECIBO DE COMISIONES PROFESIONAL',
          numeroComprobante: `LIQ-${l.id}`,
          fechaIso: l.fechaPago || l.fechaHasta,
          clienteOProfesional: l.peluqueroNombreCompleto || l.peluqueroNombre,
          monedaIso: moneda,
          renglones: extractPagedItems(l.detallesLiquidacion).map((d) => ({
            descripcion:
              d.conceptoNombre ||
              d.conceptoDescripcion ||
              `Servicio #${d.detalleVentaId || d.turnoId || d.id}`,
            monto: d.montoComisionCalculado,
          })),
          pagosODesglose: [
            { etiqueta: 'Subtotal Servicios', monto: l.totalServicios },
            { etiqueta: 'Subtotal Productos', monto: l.totalProductos },
            { etiqueta: 'Bonificaciones (+)', monto: l.bonificaciones || 0 },
            {
              etiqueta: 'Adelantos / Descuentos (-)',
              monto: l.descuentos || 0,
            },
          ],
          totalFinal: l.totalPagar,
          pieDePagina: `Período: ${formatDateOnly(
            l.fechaDesde
          )} al ${formatDateOnly(l.fechaHasta)}`,
        });
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo generar el recibo imprimible.',
      });
    }
  };

  const registrarBloqueoAusencia = async (e: React.FormEvent) => {
    e.preventDefault();
    if (new Date(bloqueoFin) <= new Date(bloqueoInicio)) {
      notifyApiError(
        { message: 'La fecha y hora de fin deben ser posteriores al inicio.' },
        { fallbackMessage: 'Revisá el horario de inicio y fin.' }
      );
      return;
    }
    setGuardandoBloqueo(true);
    try {
      const res = await bloqueosService.crear({
        peluqueroId: miPeluqueroId,
        fechaInicio: bloqueoInicio,
        fechaFin: bloqueoFin,
        motivo: bloqueoMotivo,
      });
      if (res.success) {
        notifyApiSuccess(res, 'Bloqueo horario registrado en tu agenda.');
        setBloqueoInicio('');
        setBloqueoFin('');
        setBloqueoMotivo('');
        await cargarHorariosYBloqueos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo registrar el bloqueo horario.',
      });
    } finally {
      setGuardandoBloqueo(false);
    }
  };

  const handleEliminarMiBloqueo = async (bloqueoId?: number) => {
    if (!bloqueoId) return;
    try {
      const res = await bloqueosService.eliminar(bloqueoId);
      if (res.success) {
        notifyApiSuccess(res, 'Bloqueo horario eliminado.');
        await cargarHorariosYBloqueos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar el bloqueo horario.',
      });
    }
  };

  // Cálculos de línea horaria vertical diaria (09:00–20:00 por defecto, expansible si hay turnos fuera)
  const dayTimelineConfig = useMemo(() => {
    const posicionados = computeTimelinePositions(misTurnos);
    const minStart = posicionados.reduce(
      (min, p) => Math.min(min, Math.floor(p.startMin / 60)),
      9
    );
    const maxEnd = posicionados.reduce(
      (max, p) => Math.max(max, Math.ceil(p.endMin / 60)),
      20
    );
    const startHour = Math.max(7, Math.min(9, minStart));
    const endHour = Math.min(23, Math.max(20, maxEnd));
    const hours = Array.from(
      { length: endHour - startHour + 1 },
      (_, idx) => startHour + idx
    );
    const totalHeight = (endHour - startHour) * PIXELS_PER_HOUR;

    const dow = parseLocalDateIso(fechaAgenda).getDay();
    const horarioDia = misHorarios.find(
      (h) => h.diaSemana === dow && h.activo !== false
    );
    const descansoInicio = parseMinutesFromTimeOrIso(
      horarioDia?.horaInicioDescanso
    );
    const descansoFin = parseMinutesFromTimeOrIso(horarioDia?.horaFinDescanso);

    const dayStartMs = new Date(`${fechaAgenda}T00:00:00`).getTime();
    const dayEndMs = new Date(`${fechaAgenda}T23:59:59`).getTime();

    const bloqueosDelDia = misBloqueos
      .map((b) => {
        const bStart = new Date(b.fechaInicio).getTime();
        const bEnd = new Date(b.fechaFin).getTime();
        if (
          Number.isNaN(bStart) ||
          Number.isNaN(bEnd) ||
          bEnd <= dayStartMs ||
          bStart >= dayEndMs
        ) {
          return null;
        }
        const clampedStart = new Date(Math.max(bStart, dayStartMs));
        const clampedEnd = new Date(Math.min(bEnd, dayEndMs));
        const startMin = clampedStart.getHours() * 60 + clampedStart.getMinutes();
        const endMin = Math.max(
          startMin + 30,
          clampedEnd.getHours() * 60 + clampedEnd.getMinutes()
        );
        return { bloqueo: b, startMin, endMin };
      })
      .filter(
        (item): item is { bloqueo: BloqueoHorarioDto; startMin: number; endMin: number } =>
          item !== null
      );

    return {
      posicionados,
      startHour,
      endHour,
      hours,
      totalHeight,
      descanso:
        descansoInicio !== null &&
        descansoFin !== null &&
        descansoFin > descansoInicio
          ? {
              startMin: descansoInicio,
              endMin: descansoFin,
              label: `${formatTimeOnly(horarioDia?.horaInicioDescanso)} – ${formatTimeOnly(horarioDia?.horaFinDescanso)}`,
            }
          : null,
      bloqueosDelDia,
    };
  }, [fechaAgenda, misBloqueos, misHorarios, misTurnos]);

  // Alertas de alergias destacadas para el cliente seleccionado en Fichas Técnicas
  const alertasAlergiasCliente = useMemo(() => {
    return fichasCliente
      .map((f) => (f.notasSensibilidadAlergias || '').trim())
      .filter((txt) => txt.length > 0);
  }, [fichasCliente]);

  // Filtro instantáneo adicional en cliente para respuesta sin latencia al escribir
  const clientesFiltrados = useMemo(() => {
    const q = busquedaCliente.trim().toLowerCase();
    if (!q) return clientesLista;
    return clientesLista.filter((c) => {
      const text = `${c.nombre} ${c.apellido} ${c.telefono || ''}`.toLowerCase();
      return text.includes(q);
    });
  }, [busquedaCliente, clientesLista]);

  const horariosOrdenados = useMemo(() => {
    return [...misHorarios].sort(
      (a, b) =>
        ORDEN_DIAS_LABORALES.indexOf(a.diaSemana) -
        ORDEN_DIAS_LABORALES.indexOf(b.diaSemana)
    );
  }, [misHorarios]);

  const totalGanadoPeriodo = useMemo(
    () => misLiquidaciones.reduce((acc, l) => acc + (l.totalPagar || 0), 0),
    [misLiquidaciones]
  );

  const tabs: readonly { id: TabKey; label: string; icon: React.ReactNode }[] = [
    {
      id: 'agenda',
      label: 'Mi Agenda',
      icon: <CalendarDays className="h-4 w-4" aria-hidden="true" />,
    },
    {
      id: 'fichas',
      label: 'Fichas Técnicas',
      icon: <FileText className="h-4 w-4" aria-hidden="true" />,
    },
    {
      id: 'comisiones',
      label: 'Comisiones',
      icon: <Wallet className="h-4 w-4" aria-hidden="true" />,
    },
    {
      id: 'horarios',
      label: 'Horarios',
      icon: <Clock className="h-4 w-4" aria-hidden="true" />,
    },
  ];

  const nowTopPx =
    nowMinutes !== null &&
    fechaAgenda === todayIso &&
    nowMinutes >= dayTimelineConfig.startHour * 60 &&
    nowMinutes <= dayTimelineConfig.endHour * 60
      ? ((nowMinutes - dayTimelineConfig.startHour * 60) / 60) * PIXELS_PER_HOUR
      : null;

  return (
    <div className="max-w-[1440px] mx-auto w-full space-y-5">
      {/* Cabecera compacta + Selector de pestañas */}
      <header className="bg-surface border border-border-subtle rounded-xl p-4 sm:p-5 flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
        <div className="min-w-0">
          <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary truncate">
            Mi Puesto{user?.username ? ` — ${user.username}` : ''}
          </h1>
          <p className="text-xs sm:text-sm text-ink-secondary mt-0.5">
            Gestioná tu agenda, historial técnico capilar, comisiones y horarios.
          </p>
        </div>

        <div
          role="tablist"
          aria-label="Secciones de Mi Puesto"
          className="grid grid-cols-2 sm:inline-flex gap-1 p-1 rounded-xl bg-elevated border border-border-subtle shrink-0"
        >
          {tabs.map((tab) => {
            const isActive = activeTab === tab.id;
            return (
              <button
                key={tab.id}
                type="button"
                role="tab"
                aria-selected={isActive}
                onClick={() => setActiveTab(tab.id)}
                className={`inline-flex items-center justify-center gap-1.5 h-9 px-3 rounded-lg text-xs font-medium transition-colors whitespace-nowrap ${
                  isActive
                    ? 'bg-brand-primary text-white shadow-sm'
                    : 'text-ink-secondary hover:text-ink-primary hover:bg-surface/80'
                }`}
              >
                {tab.icon}
                <span>{tab.label}</span>
              </button>
            );
          })}
        </div>
      </header>

      {/* ====================================================================
          PESTAÑA 1: MI AGENDA (HOY | MAÑANA | ESTA SEMANA + TIMELINE VERTICAL)
         ==================================================================== */}
      {activeTab === 'agenda' && (
        <section className="space-y-4">
          {/* Toolbar de Agenda */}
          <div className="bg-surface border border-border-subtle rounded-xl p-3.5 sm:p-4 flex flex-col md:flex-row md:items-center justify-between gap-3">
            <div className="flex flex-wrap items-center gap-2">
              <div className="inline-flex p-1 rounded-lg bg-elevated border border-border-subtle">
                {(
                  [
                    { id: 'hoy', label: 'Hoy' },
                    { id: 'manana', label: 'Mañana' },
                    { id: 'semana', label: 'Esta semana' },
                  ] as const
                ).map((item) => (
                  <button
                    key={item.id}
                    type="button"
                    onClick={() => handleSelectAgendaMode(item.id)}
                    className={`h-8 px-3 rounded-md text-xs font-medium transition-colors ${
                      agendaMode === item.id
                        ? 'bg-brand-primary text-white shadow-sm'
                        : 'text-ink-secondary hover:text-ink-primary'
                    }`}
                  >
                    {item.label}
                  </button>
                ))}
              </div>

              <div className="inline-flex items-center gap-1">
                <button
                  type="button"
                  onClick={() => handleShiftAgendaDate(-1)}
                  aria-label="Fecha anterior"
                  className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-border-strong bg-surface text-ink-secondary hover:text-ink-primary hover:bg-elevated transition-colors"
                >
                  <ChevronLeft className="h-4 w-4" aria-hidden="true" />
                </button>
                <input
                  type="date"
                  aria-label="Seleccionar fecha de agenda"
                  value={fechaAgenda}
                  onChange={(e) => {
                    const nextVal = e.target.value;
                    if (!nextVal) return;
                    setFechaAgenda(nextVal);
                    if (nextVal === todayIso) setAgendaMode('hoy');
                    else if (nextVal === addDaysIso(todayIso, 1))
                      setAgendaMode('manana');
                    else if (agendaMode !== 'semana') setAgendaMode('hoy');
                  }}
                  className="h-9 rounded-lg border border-border-strong bg-surface px-3 text-xs font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                />
                <button
                  type="button"
                  onClick={() => handleShiftAgendaDate(1)}
                  aria-label="Fecha siguiente"
                  className="inline-flex h-9 w-9 items-center justify-center rounded-lg border border-border-strong bg-surface text-ink-secondary hover:text-ink-primary hover:bg-elevated transition-colors"
                >
                  <ChevronRight className="h-4 w-4" aria-hidden="true" />
                </button>
              </div>
            </div>

            <div className="flex items-center gap-2 text-xs text-ink-secondary">
              <Badge variant="info" className="font-mono-tabular">
                {misTurnos.length}{' '}
                {misTurnos.length === 1 ? 'turno' : 'turnos'}
              </Badge>
              {agendaMode === 'semana' && (
                <span className="font-mono-tabular">
                  {formatDateOnly(weekDays[0].iso)} –{' '}
                  {formatDateOnly(weekDays[weekDays.length - 1].iso)}
                </span>
              )}
            </div>
          </div>

          <AsyncStateGuard
            isLoading={cargandoAgenda}
            isError={Boolean(errorAgenda)}
            errorMessage={errorAgenda?.message}
            correlationId={errorAgenda?.correlationId}
            isEmpty={false}
            loadingText="Cargando agenda..."
            onRetry={cargarAgenda}
          >
            {/* VISTA DIARIA: TIMELINE VERTICAL REAL 09:00–20:00 */}
            {agendaMode !== 'semana' ? (
              <div className="bg-surface border border-border-subtle rounded-xl overflow-hidden">
                {misTurnos.length === 0 && (
                  <div className="px-4 py-3 border-b border-border-subtle bg-elevated/60 flex items-center justify-between gap-2 text-xs text-ink-secondary">
                    <span>
                      No tenés turnos programados para el{' '}
                      <strong className="text-ink-primary font-mono-tabular">
                        {formatDateOnly(fechaAgenda)}
                      </strong>
                      .
                    </span>
                    {dayTimelineConfig.descanso && (
                      <span className="font-mono-tabular hidden sm:inline">
                        Descanso: {dayTimelineConfig.descanso.label}
                      </span>
                    )}
                  </div>
                )}

                <div className="flex relative">
                  {/* Eje vertical de horas */}
                  <div
                    className="w-14 sm:w-16 shrink-0 border-r border-border-subtle bg-elevated/40 select-none relative"
                    style={{ height: `${dayTimelineConfig.totalHeight}px` }}
                  >
                    {dayTimelineConfig.hours.slice(0, -1).map((hour, idx) => (
                      <div
                        key={hour}
                        className="absolute left-0 right-0 pr-2 text-right font-mono-tabular text-[11px] font-medium text-ink-secondary"
                        style={{ top: `${idx * PIXELS_PER_HOUR + 4}px` }}
                      >
                        {formatHourLabel(hour)}
                      </div>
                    ))}
                  </div>

                  {/* Pista vertical de eventos */}
                  <div
                    className="relative flex-1 min-w-0"
                    style={{ height: `${dayTimelineConfig.totalHeight}px` }}
                  >
                    {/* Líneas horarias y medias horas */}
                    {dayTimelineConfig.hours.slice(0, -1).map((hour, idx) => (
                      <div
                        key={hour}
                        className="absolute left-0 right-0 border-t border-border-subtle"
                        style={{
                          top: `${idx * PIXELS_PER_HOUR}px`,
                          height: `${PIXELS_PER_HOUR}px`,
                        }}
                      >
                        <div className="h-1/2 border-b border-dashed border-border-subtle/50" />
                      </div>
                    ))}

                    {/* Franja sombreada de descanso laboral */}
                    {dayTimelineConfig.descanso && (
                      <div
                        className="absolute left-1 right-1 rounded-lg border border-dashed border-border-strong bg-elevated/85 px-3 py-1.5 flex items-center justify-between text-xs text-ink-secondary pointer-events-none"
                        style={{
                          top: `${
                            ((dayTimelineConfig.descanso.startMin -
                              dayTimelineConfig.startHour * 60) /
                              60) *
                            PIXELS_PER_HOUR
                          }px`,
                          height: `${Math.max(
                            28,
                            ((dayTimelineConfig.descanso.endMin -
                              dayTimelineConfig.descanso.startMin) /
                              60) *
                              PIXELS_PER_HOUR
                          )}px`,
                        }}
                      >
                        <span className="font-medium truncate">
                          Descanso programado
                        </span>
                        <span className="font-mono-tabular text-[11px] shrink-0">
                          {dayTimelineConfig.descanso.label}
                        </span>
                      </div>
                    )}

                    {/* Franjas sombreadas de bloqueos/ausencias del día */}
                    {dayTimelineConfig.bloqueosDelDia.map(
                      ({ bloqueo, startMin, endMin }) => {
                        const clampedStart = Math.max(
                          startMin,
                          dayTimelineConfig.startHour * 60
                        );
                        const clampedEnd = Math.min(
                          endMin,
                          dayTimelineConfig.endHour * 60
                        );
                        if (clampedEnd <= clampedStart) return null;
                        const top =
                          ((clampedStart - dayTimelineConfig.startHour * 60) /
                            60) *
                          PIXELS_PER_HOUR;
                        const height = Math.max(
                          32,
                          ((clampedEnd - clampedStart) / 60) * PIXELS_PER_HOUR
                        );
                        return (
                          <div
                            key={bloqueo.id || `${bloqueo.fechaInicio}-${bloqueo.motivo}`}
                            className={`absolute left-1 right-1 rounded-lg border px-3 py-1.5 text-xs pointer-events-none ${SEMANTIC_COLORS.warning.surfaceClass}`}
                            style={{ top: `${top}px`, height: `${height}px` }}
                          >
                            <p
                              className={`font-semibold truncate ${SEMANTIC_COLORS.warning.textClass}`}
                            >
                              Bloqueo: {bloqueo.motivo}
                            </p>
                          </div>
                        );
                      }
                    )}

                    {/* Línea de hora actual */}
                    {nowTopPx !== null && (
                      <div
                        className="absolute left-0 right-0 z-20 flex items-center pointer-events-none"
                        style={{ top: `${nowTopPx}px` }}
                      >
                        <span className="h-2.5 w-2.5 -ml-1.5 rounded-full bg-red-600 dark:bg-rose-500 ring-2 ring-surface" />
                        <div className="h-0.5 flex-1 bg-red-600 dark:bg-rose-500" />
                      </div>
                    )}

                    {/* Tarjetas proporcionales de turno */}
                    {dayTimelineConfig.posicionados.map(
                      ({ turno, startMin, endMin, lane, totalLanes }) => {
                        const top =
                          ((startMin - dayTimelineConfig.startHour * 60) / 60) *
                          PIXELS_PER_HOUR;
                        const computedHeight =
                          ((endMin - startMin) / 60) * PIXELS_PER_HOUR;
                        const height = Math.max(76, computedHeight);
                        const accentColor = turno.colorAgendaHex || '#4F46E5';
                        const serviciosTexto =
                          (turno.servicios ?? [])
                            .map((s) => s.servicioNombre)
                            .join(' + ') || 'Servicio de salón';

                        return (
                          <div
                            key={turno.id}
                            style={{
                              top: `${top + 2}px`,
                              minHeight: `${height - 4}px`,
                              left: `calc(${(lane / totalLanes) * 100}% + 6px)`,
                              width: `calc(${100 / totalLanes}% - 12px)`,
                              borderLeftColor: accentColor,
                            }}
                            className="absolute z-10 rounded-xl border border-border-subtle border-l-4 bg-surface shadow-sm hover:shadow-md transition-shadow p-3 flex flex-col sm:flex-row sm:items-center justify-between gap-2.5 overflow-hidden"
                          >
                            <div className="min-w-0 space-y-1">
                              <div className="flex flex-wrap items-center gap-1.5">
                                <span className="font-mono-tabular text-xs font-semibold text-ink-primary bg-elevated px-2 py-0.5 rounded-md">
                                  {formatTimeOnly(turno.fechaHoraInicio)} –{' '}
                                  {formatTimeOnly(turno.fechaHoraFin)}
                                </span>
                                <Badge
                                  variant={getEstadoTurnoBadgeVariant(
                                    turno.estadoId
                                  )}
                                >
                                  {turno.estadoNombre}
                                </Badge>
                                {turno.tieneAlertaAlergia && (
                                  <Badge variant="danger">
                                    <AlertTriangle
                                      className="h-3 w-3"
                                      aria-hidden="true"
                                    />
                                    Alergia
                                  </Badge>
                                )}
                              </div>
                              <p className="text-sm font-semibold text-ink-primary truncate">
                                {turno.clienteNombreCompleto}
                              </p>
                              <p className="text-xs text-ink-secondary truncate">
                                {serviciosTexto}
                              </p>
                              {(turno.notasAlergiaCliente ||
                                turno.observaciones) && (
                                <p className="text-xs text-amber-800 dark:text-amber-300 truncate">
                                  Nota:{' '}
                                  {turno.notasAlergiaCliente ||
                                    turno.observaciones}
                                </p>
                              )}
                            </div>

                            <div className="flex flex-wrap items-center gap-1.5 shrink-0">
                              {(turno.estadoId === EstadoTurnoEnum.Pendiente ||
                                turno.estadoId ===
                                  EstadoTurnoEnum.Confirmado) && (
                                <Button
                                  variant="primary"
                                  size="sm"
                                  isLoading={actualizandoTurnoId === turno.id}
                                  leftIcon={
                                    <Play
                                      className="h-3.5 w-3.5"
                                      aria-hidden="true"
                                    />
                                  }
                                  onClick={() =>
                                    cambiarEstadoRapido(
                                      turno,
                                      EstadoTurnoEnum.EnProceso
                                    )
                                  }
                                >
                                  Iniciar
                                </Button>
                              )}
                              {turno.estadoId === EstadoTurnoEnum.EnProceso && (
                                <Button
                                  variant="primary"
                                  size="sm"
                                  isLoading={actualizandoTurnoId === turno.id}
                                  leftIcon={
                                    <CheckCircle2
                                      className="h-3.5 w-3.5"
                                      aria-hidden="true"
                                    />
                                  }
                                  onClick={() =>
                                    cambiarEstadoRapido(
                                      turno,
                                      EstadoTurnoEnum.Completado
                                    )
                                  }
                                >
                                  Finalizar
                                </Button>
                              )}
                              {turno.estadoId ===
                                EstadoTurnoEnum.Completado && (
                                <Button
                                  variant="secondary"
                                  size="sm"
                                  isLoading={actualizandoTurnoId === turno.id}
                                  leftIcon={
                                    <FlaskConical
                                      className="h-3.5 w-3.5"
                                      aria-hidden="true"
                                    />
                                  }
                                  onClick={() =>
                                    handleAplicarConsumoGabineteManual(turno.id)
                                  }
                                >
                                  Insumos
                                </Button>
                              )}
                              <Button
                                variant="secondary"
                                size="sm"
                                leftIcon={
                                  <FileText
                                    className="h-3.5 w-3.5"
                                    aria-hidden="true"
                                  />
                                }
                                onClick={() => abrirFichaDesdeTurno(turno)}
                              >
                                Ficha
                              </Button>
                            </div>
                          </div>
                        );
                      }
                    )}
                  </div>
                </div>
              </div>
            ) : (
              /* VISTA SEMANAL: COLUMNAS LUNES A SÁBADO */
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-3">
                {weekDays.map((day) => {
                  const turnosDia = misTurnos
                    .filter(
                      (t) => extractDateKey(t.fechaHoraInicio) === day.iso
                    )
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
                              isToday
                                ? 'text-brand-primary'
                                : 'text-ink-primary'
                            }`}
                          >
                            {day.weekday} {day.dayNumber}
                          </p>
                          <p className="text-[11px] text-ink-secondary">
                            {day.monthShort}
                          </p>
                        </div>
                        <span className="font-mono-tabular text-xs font-semibold px-2 py-0.5 rounded-md bg-surface border border-border-subtle text-ink-secondary">
                          {turnosDia.length}
                        </span>
                      </div>

                      <div className="p-2.5 space-y-2 flex-1 min-h-[220px]">
                        {turnosDia.length === 0 ? (
                          <p className="text-xs text-ink-secondary/80 text-center py-8">
                            Sin turnos
                          </p>
                        ) : (
                          turnosDia.map((turno) => (
                            <button
                              key={turno.id}
                              type="button"
                              onClick={() => setTurnoDetalleModal(turno)}
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
                                  variant={getEstadoTurnoBadgeVariant(
                                    turno.estadoId
                                  )}
                                  className="text-[10px] px-1.5 py-0"
                                >
                                  {turno.estadoNombre}
                                </Badge>
                              </div>
                              <p className="text-xs font-semibold text-ink-primary truncate">
                                {turno.clienteNombreCompleto}
                              </p>
                              <p className="text-[11px] text-ink-secondary truncate">
                                {(turno.servicios ?? [])
                                  .map((s) => s.servicioNombre)
                                  .join(' + ') || 'Servicio'}
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
        </section>
      )}

      {/* ====================================================================
          PESTAÑA 2: FICHAS TÉCNICAS CAPILARES (MASTER-DETAIL + MOBILE 390x844)
         ==================================================================== */}
      {activeTab === 'fichas' && (
        <section className="grid grid-cols-1 md:grid-cols-12 gap-4 items-start">
          {/* PANEL IZQUIERDO (MASTER): Buscador + Lista Vertical de Clientes */}
          <div
            className={`md:col-span-4 bg-surface border border-border-subtle rounded-xl overflow-hidden flex flex-col ${
              mobileShowDetail ? 'hidden md:flex' : 'flex'
            }`}
          >
            <div className="p-3.5 border-b border-border-subtle space-y-2.5 bg-elevated/40">
              <div className="flex items-center justify-between gap-2">
                <h2 className="text-sm font-semibold text-ink-primary">
                  Clientes
                </h2>
                <span className="text-xs font-mono-tabular text-ink-secondary">
                  {clientesFiltrados.length}
                </span>
              </div>
              <div className="relative">
                <Search
                  className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-secondary"
                  aria-hidden="true"
                />
                <input
                  type="search"
                  aria-label="Buscar cliente"
                  placeholder="Buscar por nombre o teléfono..."
                  value={busquedaCliente}
                  onChange={(e) => setBusquedaCliente(e.target.value)}
                  className="h-10 w-full rounded-lg border border-border-strong bg-surface pl-9 pr-3 text-sm text-ink-primary placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                />
              </div>
            </div>

            <div className="divide-y divide-border-subtle max-h-[62vh] md:max-h-[68vh] overflow-y-auto">
              {cargandoClientes && clientesFiltrados.length === 0 ? (
                <p className="p-6 text-center text-xs text-ink-secondary">
                  Buscando clientes...
                </p>
              ) : clientesFiltrados.length === 0 ? (
                <p className="p-6 text-center text-xs text-ink-secondary">
                  No se encontraron clientes para esa búsqueda.
                </p>
              ) : (
                clientesFiltrados.map((cli) => {
                  const nombreCompleto = `${cli.nombre} ${cli.apellido}`.trim();
                  const selected = clienteSeleccionado?.id === cli.id;
                  return (
                    <button
                      key={cli.id}
                      type="button"
                      onClick={() => seleccionarClienteFicha(cli, true)}
                      className={`w-full px-3.5 py-3 text-left flex items-center gap-3 transition-colors ${
                        selected
                          ? 'bg-brand-primary/10 border-l-4 border-l-brand-primary'
                          : 'hover:bg-elevated/70 border-l-4 border-l-transparent'
                      }`}
                    >
                      <span
                        className={`h-9 w-9 rounded-full flex items-center justify-center text-xs font-semibold shrink-0 ${
                          selected
                            ? 'bg-brand-primary text-white'
                            : 'bg-elevated text-ink-primary border border-border-subtle'
                        }`}
                      >
                        {getInitials(nombreCompleto)}
                      </span>
                      <div className="min-w-0 flex-1">
                        <p className="text-sm font-medium text-ink-primary truncate">
                          {nombreCompleto}
                        </p>
                        <p className="text-xs font-mono-tabular text-ink-secondary truncate">
                          {cli.telefono || 'Sin teléfono'}
                        </p>
                      </div>
                      <ChevronRight
                        className="h-4 w-4 text-ink-secondary/70 shrink-0"
                        aria-hidden="true"
                      />
                    </button>
                  );
                })
              )}
            </div>
          </div>

          {/* PANEL DERECHO (DETAIL): Historial Técnico + Banner Alergias + Botón Volver Móvil */}
          <div
            className={`md:col-span-8 space-y-4 min-w-0 ${
              mobileShowDetail ? 'block' : 'hidden md:block'
            }`}
          >
            {!clienteSeleccionado ? (
              <div className="bg-surface border border-border-subtle rounded-xl p-10 text-center space-y-2">
                <div className="mx-auto h-11 w-11 rounded-xl bg-elevated flex items-center justify-center text-ink-secondary">
                  <FileText className="h-5 w-5" aria-hidden="true" />
                </div>
                <h3 className="text-base font-semibold text-ink-primary">
                  Seleccioná un cliente
                </h3>
                <p className="text-xs sm:text-sm text-ink-secondary max-w-md mx-auto">
                  Elegí un cliente de la lista para consultar sus fórmulas
                  anteriores, alertas de alergias y cargar nuevas fichas
                  técnicas desde el sillón.
                </p>
              </div>
            ) : (
              <>
                {/* Botón Volver en móvil (< md) */}
                <div className="md:hidden">
                  <Button
                    variant="ghost"
                    size="sm"
                    leftIcon={
                      <ArrowLeft className="h-4 w-4" aria-hidden="true" />
                    }
                    onClick={() => setMobileShowDetail(false)}
                  >
                    Volver a clientes
                  </Button>
                </div>

                {/* Encabezado del cliente seleccionado */}
                <div className="bg-surface border border-border-subtle rounded-xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                  <div className="flex items-center gap-3 min-w-0">
                    <span className="h-11 w-11 rounded-full bg-brand-primary text-white flex items-center justify-center text-sm font-semibold shrink-0">
                      {getInitials(
                        `${clienteSeleccionado.nombre} ${clienteSeleccionado.apellido}`
                      )}
                    </span>
                    <div className="min-w-0">
                      <h2 className="text-base sm:text-lg font-semibold text-ink-primary truncate">
                        {clienteSeleccionado.nombre}{' '}
                        {clienteSeleccionado.apellido}
                      </h2>
                      <div className="flex flex-wrap items-center gap-1.5 mt-1">
                        <Badge variant="neutral">
                          Cabello:{' '}
                          {clienteSeleccionado.tipoCabelloNombre || 'Estándar'}
                        </Badge>
                        <Badge variant="neutral">
                          Rostro:{' '}
                          {clienteSeleccionado.tipoRostroNombre || 'Estándar'}
                        </Badge>
                      </div>
                    </div>
                  </div>

                  <Button
                    variant="primary"
                    size="md"
                    className="w-full sm:w-auto shrink-0"
                    leftIcon={<Plus className="h-4 w-4" aria-hidden="true" />}
                    onClick={() => {
                      setFichaEditando(null);
                      setFormulaTintura('');
                      setNotasAlergias('');
                      setProductosUsados('');
                      setFotoUrl('');
                      setModalFichaOpen(true);
                    }}
                  >
                    Nueva Ficha Técnica
                  </Button>
                </div>

                {/* Banner rojo destacado de alergias / sensibilidad */}
                {alertasAlergiasCliente.length > 0 && (
                  <div
                    role="alert"
                    className={`rounded-xl border p-3.5 sm:p-4 flex items-start gap-3 ${SEMANTIC_COLORS.danger.surfaceClass}`}
                  >
                    <AlertTriangle
                      className={`h-5 w-5 shrink-0 mt-0.5 ${SEMANTIC_COLORS.danger.textClass}`}
                      aria-hidden="true"
                    />
                    <div className="min-w-0 space-y-1">
                      <p
                        className={`text-xs font-semibold uppercase tracking-wide ${SEMANTIC_COLORS.danger.textClass}`}
                      >
                        Alerta de Sensibilidad / Alergias
                      </p>
                      <p className="text-sm text-ink-primary break-words">
                        {alertasAlergiasCliente[0]}
                      </p>
                    </div>
                  </div>
                )}

                {/* Listado de Fichas Técnicas */}
                {cargandoFichas ? (
                  <div className="bg-surface border border-border-subtle rounded-xl p-8 text-center text-xs text-ink-secondary">
                    Cargando historial técnico...
                  </div>
                ) : fichasCliente.length === 0 ? (
                  <div className="bg-surface border border-border-subtle rounded-xl p-8 text-center space-y-2">
                    <Sparkles
                      className="h-6 w-6 mx-auto text-ink-secondary"
                      aria-hidden="true"
                    />
                    <p className="text-sm font-semibold text-ink-primary">
                      Aún no hay fichas técnicas registradas
                    </p>
                    <p className="text-xs text-ink-secondary">
                      Cargá la primera fórmula capilar de este cliente para
                      tenerla siempre a mano.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {fichasCliente.map((ficha) => (
                      <article
                        key={ficha.id}
                        className="bg-surface border border-border-subtle rounded-xl p-4 space-y-3"
                      >
                        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border-subtle pb-2.5">
                          <div className="flex flex-wrap items-center gap-2">
                            <span className="font-mono-tabular text-xs font-semibold text-ink-primary">
                              {formatDateTime(
                                ficha.fechaRegistro || ficha.fecha
                              )}
                            </span>
                            {ficha.turnoId && (
                              <Badge variant="info" className="font-mono-tabular">
                                Turno #{ficha.turnoId}
                              </Badge>
                            )}
                          </div>
                          <div className="flex items-center gap-1.5">
                            <Button
                              variant="secondary"
                              size="sm"
                              leftIcon={
                                <Pencil
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                              }
                              onClick={() => {
                                setFichaEditando(ficha);
                                setTurnoAsociadoId(ficha.turnoId || null);
                                setFormulaTintura(
                                  ficha.detalleFormulaTintura ||
                                    ficha.formulaAplicada ||
                                    ''
                                );
                                setNotasAlergias(
                                  ficha.notasSensibilidadAlergias ||
                                    ficha.observaciones ||
                                    ''
                                );
                                setProductosUsados(
                                  ficha.productosUtilizados || ''
                                );
                                setFotoUrl(ficha.fotoResultadoUrl || '');
                                setModalFichaOpen(true);
                              }}
                            >
                              Editar
                            </Button>
                            <Button
                              variant="destructive"
                              destructiveTone="soft"
                              size="sm"
                              leftIcon={
                                <Trash2
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                              }
                              onClick={() => handleEliminarFicha(ficha.id)}
                            >
                              Eliminar
                            </Button>
                          </div>
                        </div>

                        <div className="space-y-2 text-sm">
                          <div>
                            <span className="text-xs font-medium text-ink-secondary block">
                              Fórmula aplicada
                            </span>
                            <p className="text-ink-primary font-medium mt-0.5 break-words">
                              {ficha.detalleFormulaTintura ||
                                ficha.formulaAplicada ||
                                '—'}
                            </p>
                          </div>

                          {ficha.productosUtilizados && (
                            <div>
                              <span className="text-xs font-medium text-ink-secondary block">
                                Productos utilizados
                              </span>
                              <p className="text-xs sm:text-sm text-ink-primary mt-0.5 break-words">
                                {ficha.productosUtilizados}
                              </p>
                            </div>
                          )}

                          {(ficha.notasSensibilidadAlergias ||
                            ficha.observaciones) && (
                            <div
                              className={`rounded-lg border px-3 py-2 text-xs font-medium ${SEMANTIC_COLORS.danger.surfaceClass} ${SEMANTIC_COLORS.danger.textClass}`}
                            >
                              Sensibilidad / Alergias:{' '}
                              {ficha.notasSensibilidadAlergias ||
                                ficha.observaciones}
                            </div>
                          )}

                          {ficha.fotoResultadoUrl && (
                            <div className="pt-1 flex items-center gap-3">
                              {/* eslint-disable-next-line @next/next/no-img-element */}
                              <img
                                src={ficha.fotoResultadoUrl}
                                alt="Resultado capilar"
                                className="h-14 w-14 rounded-lg object-cover border border-border-subtle shrink-0 bg-elevated"
                              />
                              <a
                                href={ficha.fotoResultadoUrl}
                                target="_blank"
                                rel="noreferrer"
                                className="inline-flex items-center gap-1.5 text-xs font-medium text-brand-primary hover:underline"
                              >
                                <ImageIcon
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                                <span>Ver fotografía completa</span>
                                <ExternalLink
                                  className="h-3 w-3"
                                  aria-hidden="true"
                                />
                              </a>
                            </div>
                          )}
                        </div>
                      </article>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>
        </section>
      )}

      {/* ====================================================================
          PESTAÑA 3: MIS COMISIONES Y RECIBOS
         ==================================================================== */}
      {activeTab === 'comisiones' && (
        <section className="space-y-5">
          {/* Resumen KPI */}
          <div className="bg-surface border border-border-subtle rounded-xl p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div>
              <span className="text-xs font-medium text-ink-secondary">
                Total acumulado en liquidaciones
              </span>
              <p className="text-2xl sm:text-3xl font-bold font-mono-tabular text-ink-primary mt-1">
                {formatCurrency(totalGanadoPeriodo, moneda)}
              </p>
            </div>
            <Badge variant="info" className="font-mono-tabular self-start sm:self-center">
              {misLiquidaciones.length}{' '}
              {misLiquidaciones.length === 1
                ? 'recibo emitido'
                : 'recibos emitidos'}
            </Badge>
          </div>

          {/* Esquema de Comisiones Vigente */}
          <div className="space-y-2.5">
            <h2 className="text-base font-semibold text-ink-primary">
              Mi esquema de comisiones vigente
            </h2>
            {cargandoComisiones ? (
              <div className="bg-surface border border-border-subtle rounded-xl p-6 text-center text-xs text-ink-secondary">
                Cargando esquema de comisiones...
              </div>
            ) : misComisionesConfig.length === 0 ? (
              <div className="bg-surface border border-border-subtle rounded-xl p-5 text-xs text-ink-secondary">
                No tenés reglas de comisión personalizadas configuradas aún.
              </div>
            ) : (
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
                {misComisionesConfig.map((cfg) => {
                  const pctProd =
                    cfg.porcentajeVentaProducto ?? cfg.porcentajeProducto ?? 0;
                  return (
                    <div
                      key={cfg.id}
                      className="bg-surface border border-border-subtle rounded-xl p-4 space-y-3"
                    >
                      <div className="flex items-center justify-between gap-2">
                        <span className="text-xs font-semibold text-brand-primary truncate">
                          {cfg.servicioNombre || 'Esquema general del profesional'}
                        </span>
                        <Badge variant="success">Vigente</Badge>
                      </div>
                      <div className="grid grid-cols-2 gap-3 pt-1">
                        <div className="rounded-lg bg-elevated p-2.5">
                          <span className="text-[11px] text-ink-secondary block">
                            Servicios
                          </span>
                          <span className="text-xl font-bold font-mono-tabular text-ink-primary">
                            {formatPercent(cfg.porcentajeServicio)}
                          </span>
                        </div>
                        <div className="rounded-lg bg-elevated p-2.5">
                          <span className="text-[11px] text-ink-secondary block">
                            Productos
                          </span>
                          <span className="text-xl font-bold font-mono-tabular text-ink-primary">
                            {formatPercent(pctProd)}
                          </span>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Historial de Recibos */}
          <div className="bg-surface border border-border-subtle rounded-xl overflow-hidden">
            <div className="px-4 py-3 border-b border-border-subtle flex items-center justify-between">
              <h2 className="text-base font-semibold text-ink-primary">
                Recibos de liquidación
              </h2>
            </div>

            {misLiquidaciones.length === 0 ? (
              <p className="p-6 text-center text-xs text-ink-secondary">
                Todavía no hay recibos de liquidación registrados.
              </p>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                    <tr>
                      <th className="px-4 py-3">Recibo</th>
                      <th className="px-4 py-3">Período</th>
                      <th className="px-4 py-3 text-right">Servicios</th>
                      <th className="px-4 py-3 text-right">Productos</th>
                      <th className="px-4 py-3 text-right">Total neto</th>
                      <th className="px-4 py-3">Estado</th>
                      <th className="px-4 py-3 text-right">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {misLiquidaciones.map((liq) => (
                      <tr
                        key={liq.id}
                        className="hover:bg-elevated/60 transition-colors"
                      >
                        <td className="px-4 py-3 font-mono-tabular font-semibold text-ink-primary">
                          #{liq.id}
                        </td>
                        <td className="px-4 py-3 font-mono-tabular text-xs text-ink-secondary whitespace-nowrap">
                          {formatDateOnly(liq.fechaDesde)} –{' '}
                          {formatDateOnly(liq.fechaHasta)}
                        </td>
                        <td className="px-4 py-3 text-right font-mono-tabular text-ink-secondary">
                          {formatCurrency(liq.totalServicios, moneda)}
                        </td>
                        <td className="px-4 py-3 text-right font-mono-tabular text-ink-secondary">
                          {formatCurrency(liq.totalProductos, moneda)}
                        </td>
                        <td className="px-4 py-3 text-right font-mono-tabular font-semibold text-ink-primary">
                          {formatCurrency(liq.totalPagar, moneda)}
                        </td>
                        <td className="px-4 py-3">
                          <Badge
                            variant={getEstadoLiquidacionBadgeVariant(
                              liq.estadoNombre || String(liq.estado)
                            )}
                          >
                            {liq.estadoNombre || liq.estado}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-right whitespace-nowrap">
                          <div className="inline-flex items-center gap-1.5">
                            <Button
                              variant="secondary"
                              size="sm"
                              leftIcon={
                                <Receipt
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                              }
                              onClick={() => abrirDetalleRecibo(liq.id)}
                            >
                              Desglose
                            </Button>
                            <Button
                              variant="secondary"
                              size="sm"
                              leftIcon={
                                <Printer
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                              }
                              onClick={() => imprimirMiRecibo(liq.id)}
                            >
                              Recibo
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </section>
      )}

      {/* ====================================================================
          PESTAÑA 4: MIS HORARIOS Y AUSENCIAS
         ==================================================================== */}
      {activeTab === 'horarios' && (
        <section className="space-y-5">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
            {/* Jornada Semanal */}
            <div className="bg-surface border border-border-subtle rounded-xl p-5 space-y-3">
              <h2 className="text-base font-semibold text-ink-primary">
                Jornada semanal y descansos
              </h2>
              {horariosOrdenados.length === 0 ? (
                <p className="text-xs text-ink-secondary py-4">
                  Aún no tenés horarios laborales asignados.
                </p>
              ) : (
                <div className="divide-y divide-border-subtle">
                  {horariosOrdenados.map((h, idx) => (
                    <div
                      key={h.id ?? idx}
                      className="py-2.5 flex items-center justify-between gap-3 text-sm"
                    >
                      <span className="font-medium text-ink-primary">
                        {DIAS_SEMANA[h.diaSemana] || `Día ${h.diaSemana}`}
                      </span>
                      {h.activo === false ? (
                        <Badge variant="neutral">Franco</Badge>
                      ) : (
                        <div className="text-right">
                          <span className="font-mono-tabular text-xs font-semibold text-ink-primary">
                            {formatTimeOnly(h.horaInicio)} –{' '}
                            {formatTimeOnly(h.horaFin)} hs
                          </span>
                          {h.horaInicioDescanso && h.horaFinDescanso && (
                            <span className="block font-mono-tabular text-[11px] text-ink-secondary">
                              Descanso: {formatTimeOnly(h.horaInicioDescanso)} –{' '}
                              {formatTimeOnly(h.horaFinDescanso)}
                            </span>
                          )}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Formulario Bloqueo / Ausencia */}
            <form
              onSubmit={registrarBloqueoAusencia}
              className="bg-surface border border-border-subtle rounded-xl p-5 space-y-4"
            >
              <div>
                <h2 className="text-base font-semibold text-ink-primary">
                  Registrar ausencia o bloqueo
                </h2>
                <p className="text-xs text-ink-secondary mt-0.5">
                  Bloqueá una franja horaria para que no se asignen nuevos
                  turnos en ese período.
                </p>
              </div>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <InputField
                  id="peluquero-bloqueo-inicio"
                  label="Fecha y hora de inicio *"
                  type="datetime-local"
                  required
                  value={bloqueoInicio}
                  onChange={(e) => setBloqueoInicio(e.target.value)}
                />
                <InputField
                  id="peluquero-bloqueo-fin"
                  label="Fecha y hora de fin *"
                  type="datetime-local"
                  required
                  value={bloqueoFin}
                  onChange={(e) => setBloqueoFin(e.target.value)}
                />
              </div>
              <InputField
                id="peluquero-bloqueo-motivo"
                label="Motivo *"
                required
                maxLength={200}
                value={bloqueoMotivo}
                onChange={(e) => setBloqueoMotivo(e.target.value)}
                placeholder="Ej. Turno médico, capacitación o trámite personal"
              />
              <Button
                type="submit"
                variant="primary"
                className="w-full"
                isLoading={guardandoBloqueo}
              >
                Bloquear horario en agenda
              </Button>
            </form>
          </div>

          {/* Bloqueos Registrados */}
          <div className="bg-surface border border-border-subtle rounded-xl p-5 space-y-3">
            <div className="flex items-center justify-between gap-2">
              <h2 className="text-base font-semibold text-ink-primary">
                Mis bloqueos registrados
              </h2>
              <Badge variant="neutral" className="font-mono-tabular">
                {misBloqueos.length}
              </Badge>
            </div>
            {misBloqueos.length === 0 ? (
              <p className="text-xs text-ink-secondary py-2">
                No tenés bloqueos horarios registrados actualmente.
              </p>
            ) : (
              <div className="divide-y divide-border-subtle">
                {misBloqueos.map((b) => (
                  <div
                    key={b.id}
                    className="py-3 flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-sm"
                  >
                    <div className="min-w-0">
                      <p className="font-mono-tabular text-xs font-semibold text-ink-primary">
                        {formatDateTime(b.fechaInicio)} →{' '}
                        {formatDateTime(b.fechaFin)}
                      </p>
                      <p className="text-xs text-ink-secondary mt-0.5">
                        {b.motivo}
                      </p>
                    </div>
                    <Button
                      variant="destructive"
                      destructiveTone="soft"
                      size="sm"
                      leftIcon={
                        <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
                      }
                      onClick={() => handleEliminarMiBloqueo(b.id)}
                    >
                      Eliminar
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </section>
      )}

      {/* Modal de Acciones Rápidas (desde vista semanal) */}
      <Modal
        isOpen={Boolean(turnoDetalleModal)}
        onClose={() => setTurnoDetalleModal(null)}
        title={turnoDetalleModal?.clienteNombreCompleto || 'Detalle del turno'}
        subtitle={
          turnoDetalleModal
            ? `${formatDateOnly(turnoDetalleModal.fechaHoraInicio)} · ${formatTimeOnly(
                turnoDetalleModal.fechaHoraInicio
              )} – ${formatTimeOnly(turnoDetalleModal.fechaHoraFin)} hs`
            : undefined
        }
      >
        {turnoDetalleModal && (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center gap-2">
              <Badge
                variant={getEstadoTurnoBadgeVariant(turnoDetalleModal.estadoId)}
              >
                {turnoDetalleModal.estadoNombre}
              </Badge>
              {turnoDetalleModal.tieneAlertaAlergia && (
                <Badge variant="danger">Alerta de alergia</Badge>
              )}
              <span className="font-mono-tabular text-xs font-semibold text-ink-primary ml-auto">
                {formatCurrency(turnoDetalleModal.montoTotalEstimado, moneda)}
              </span>
            </div>

            <div className="rounded-lg bg-elevated p-3 space-y-1 text-sm">
              <span className="text-xs font-medium text-ink-secondary block">
                Servicios
              </span>
              <p className="font-medium text-ink-primary">
                {(turnoDetalleModal.servicios ?? [])
                  .map((s) => s.servicioNombre)
                  .join(' + ') || 'Servicio de salón'}
              </p>
              {(turnoDetalleModal.notasAlergiaCliente ||
                turnoDetalleModal.observaciones) && (
                <p className="text-xs text-ink-secondary pt-1">
                  Nota:{' '}
                  {turnoDetalleModal.notasAlergiaCliente ||
                    turnoDetalleModal.observaciones}
                </p>
              )}
            </div>

            <div className="flex flex-wrap items-center justify-end gap-2 pt-2">
              <Button
                variant="secondary"
                size="sm"
                leftIcon={
                  <FileText className="h-3.5 w-3.5" aria-hidden="true" />
                }
                onClick={() => abrirFichaDesdeTurno(turnoDetalleModal)}
              >
                Ver Ficha Técnica
              </Button>
              {(turnoDetalleModal.estadoId === EstadoTurnoEnum.Pendiente ||
                turnoDetalleModal.estadoId === EstadoTurnoEnum.Confirmado) && (
                <Button
                  variant="primary"
                  size="sm"
                  isLoading={actualizandoTurnoId === turnoDetalleModal.id}
                  leftIcon={<Play className="h-3.5 w-3.5" aria-hidden="true" />}
                  onClick={() =>
                    cambiarEstadoRapido(
                      turnoDetalleModal,
                      EstadoTurnoEnum.EnProceso
                    )
                  }
                >
                  Iniciar atención
                </Button>
              )}
              {turnoDetalleModal.estadoId === EstadoTurnoEnum.EnProceso && (
                <Button
                  variant="primary"
                  size="sm"
                  isLoading={actualizandoTurnoId === turnoDetalleModal.id}
                  leftIcon={
                    <CheckCircle2 className="h-3.5 w-3.5" aria-hidden="true" />
                  }
                  onClick={() =>
                    cambiarEstadoRapido(
                      turnoDetalleModal,
                      EstadoTurnoEnum.Completado
                    )
                  }
                >
                  Finalizar trabajo
                </Button>
              )}
              {turnoDetalleModal.estadoId === EstadoTurnoEnum.Completado && (
                <Button
                  variant="primary"
                  size="sm"
                  isLoading={actualizandoTurnoId === turnoDetalleModal.id}
                  leftIcon={
                    <FlaskConical className="h-3.5 w-3.5" aria-hidden="true" />
                  }
                  onClick={() =>
                    handleAplicarConsumoGabineteManual(turnoDetalleModal.id)
                  }
                >
                  Registrar consumo de insumos
                </Button>
              )}
            </div>
          </div>
        )}
      </Modal>

      {/* Modal Nueva / Editar Ficha Técnica */}
      <Modal
        isOpen={modalFichaOpen}
        onClose={() => setModalFichaOpen(false)}
        title={
          fichaEditando
            ? 'Editar Ficha Técnica Capilar'
            : 'Nueva Ficha Técnica Capilar'
        }
        subtitle={
          clienteSeleccionado
            ? `${clienteSeleccionado.nombre} ${clienteSeleccionado.apellido}`.trim()
            : undefined
        }
      >
        <form onSubmit={guardarFichaTecnica} className="space-y-4">
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="ficha-formula"
              className="text-xs font-medium text-ink-secondary"
            >
              Fórmula de tintura / tratamiento aplicado *
            </label>
            <textarea
              id="ficha-formula"
              required
              rows={3}
              value={formulaTintura}
              onChange={(e) => setFormulaTintura(e.target.value)}
              className="w-full rounded-lg border border-border-strong bg-surface px-3 py-2 text-sm text-ink-primary placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              placeholder="Ej. 60g Tono 7.1 + 90ml Oxidante 20 Vol (35 min)"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="ficha-alergias"
              className="text-xs font-medium text-ink-secondary"
            >
              Sensibilidad o alergias en cuero cabelludo
            </label>
            <textarea
              id="ficha-alergias"
              rows={2}
              value={notasAlergias}
              onChange={(e) => setNotasAlergias(e.target.value)}
              placeholder="Ej. Cuero cabelludo sensible al amoníaco"
              className="w-full rounded-lg border border-border-strong bg-surface px-3 py-2 text-sm text-ink-primary placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
            />
          </div>

          <InputField
            id="ficha-productos"
            label="Productos utilizados"
            value={productosUsados}
            onChange={(e) => setProductosUsados(e.target.value)}
            placeholder="Ej. Máscara nutritiva + protector térmico"
          />

          <div className="space-y-2">
            <label className="text-xs font-medium text-ink-secondary block">
              Foto del resultado (opcional)
            </label>
            <div className="flex flex-wrap items-center gap-3">
              <label className="inline-flex h-9 cursor-pointer items-center gap-2 rounded-lg border border-border-strong bg-surface px-3 text-xs font-medium text-ink-primary hover:bg-elevated transition-colors">
                <Upload className="h-3.5 w-3.5" aria-hidden="true" />
                <span>{subiendoFoto ? 'Subiendo foto...' : 'Subir imagen'}</span>
                <input
                  type="file"
                  accept="image/*"
                  disabled={subiendoFoto}
                  onChange={handleSubirFotoFicha}
                  className="sr-only"
                />
              </label>
              {fotoUrl && (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={fotoUrl}
                  alt="Vista previa"
                  className="h-10 w-10 rounded-lg object-cover border border-border-subtle bg-elevated"
                />
              )}
            </div>
            <InputField
              id="ficha-foto-url"
              label="O pegar enlace de fotografía"
              value={fotoUrl}
              onChange={(e) => setFotoUrl(e.target.value)}
              placeholder="https://..."
            />
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setModalFichaOpen(false)}
            >
              Cancelar
            </Button>
            <Button type="submit" variant="primary" isLoading={guardandoFicha}>
              Guardar Ficha Técnica
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Detalle de Recibo de Liquidación */}
      <Modal
        isOpen={Boolean(reciboDetalle)}
        onClose={() => setReciboDetalle(null)}
        title={`Detalle de Recibo #${reciboDetalle?.id ?? ''}`}
        maxWidthClass="max-w-2xl"
      >
        {reciboDetalle && (
          <div className="space-y-4 text-sm">
            <div className="flex flex-wrap items-center justify-between gap-2 bg-elevated p-3.5 rounded-lg border border-border-subtle">
              <span className="text-xs text-ink-secondary">
                Período:{' '}
                <strong className="font-mono-tabular text-ink-primary">
                  {formatDateOnly(reciboDetalle.fechaDesde)}
                </strong>{' '}
                al{' '}
                <strong className="font-mono-tabular text-ink-primary">
                  {formatDateOnly(reciboDetalle.fechaHasta)}
                </strong>
              </span>
              <span className="font-mono-tabular font-bold text-ink-primary">
                Total neto: {formatCurrency(reciboDetalle.totalPagar, moneda)}
              </span>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="border-b border-border-subtle text-ink-secondary">
                  <tr>
                    <th className="py-2 pr-3">Referencia</th>
                    <th className="py-2 pr-3">Concepto</th>
                    <th className="py-2 text-right">Comisión</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border-subtle">
                  {(reciboDetalle.detallesLiquidacion || []).map((det) => (
                    <tr key={det.id}>
                      <td className="py-2.5 pr-3 font-mono-tabular text-ink-secondary">
                        #{det.detalleVentaId || det.turnoId || det.id}
                      </td>
                      <td className="py-2.5 pr-3 text-ink-primary">
                        {det.conceptoNombre ||
                          det.conceptoDescripcion ||
                          'Comisión por servicio / producto'}
                      </td>
                      <td className="py-2.5 text-right font-mono-tabular font-semibold text-ink-primary">
                        {formatCurrency(det.montoComisionCalculado, moneda)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </Modal>

      {/* Modal de Impresión de Recibo */}
      <PrintReceiptModal
        data={reciboImprimir}
        onClose={() => setReciboImprimir(null)}
        nombreSalon={configSalon?.nombreComercial}
        cuitSalon={configSalon?.cuit}
      />
    </div>
  );
}
