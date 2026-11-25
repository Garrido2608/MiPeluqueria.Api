// filepath: src/app/(dashboard)/admin/staff-liquidaciones/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import {
  Banknote,
  CalendarClock,
  CheckCircle2,
  FileSpreadsheet,
  Pencil,
  Percent,
  Plus,
  Printer,
  Trash2,
  Users,
  Wallet,
  XCircle,
} from 'lucide-react';
import {
  ComisionConfigDto,
  EstadoLiquidacionEnum,
  HorarioLaboralDto,
  LiquidacionDto,
  PeluqueroDto,
  ServicioDto,
} from '@/types/api.types';
import {
  comisionesService,
  liquidacionesService,
  peluquerosService,
  serviciosService,
} from '@/services/api.services';
import { useCajaPos, useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess, QUERY_KEYS } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import { formatCurrency } from '@/lib/formatters';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import {
  AsyncStateGuard,
  Button,
  ConfirmModal,
  InputField,
  Modal,
  NumericInput,
} from '@/components/ui';
import {
  PrintableReceiptData,
  PrintReceiptModal,
} from '@/components/ui/PrintReceiptModal';

const DIAS_SEMANA = [
  'Domingo',
  'Lunes',
  'Martes',
  'Miércoles',
  'Jueves',
  'Viernes',
  'Sábado',
];

function toLocalDateIso(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/**
 * Normaliza fechas provenientes de SQL Server / .NET (incluyendo fracciones de
 * 7 dígitos como `.1234567`, espacios o valores vacíos) a un ISO 8601 seguro
 * para `new Date(...)` y `PrintReceiptModal`.
 */
function normalizeSafeIsoForReceipt(raw?: string | null): string {
  if (!raw || typeof raw !== 'string') {
    return new Date().toISOString();
  }
  const trimmed = raw.trim();
  if (!trimmed || trimmed.startsWith('0001-01-01')) {
    return new Date().toISOString();
  }
  // Si viene solo fecha YYYY-MM-DD, fijar mediodía local para evitar corrimiento de día
  if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
    return `${trimmed}T12:00:00`;
  }
  const cleaned = trimmed
    .replace(' ', 'T')
    .replace(/(\.\d{3})\d+/, '$1');
  const parsed = new Date(cleaned);
  if (Number.isNaN(parsed.getTime()) || parsed.getFullYear() < 1970) {
    return new Date().toISOString();
  }
  return cleaned;
}

/**
 * Formatea una fecha a DD/MM/YYYY de forma segura sin corrimiento UTC-3 ni
 * errores de parseo ante milisegundos extendidos de SQL Server.
 */
function formatSafeDateOnly(raw?: string | null): string {
  if (!raw || typeof raw !== 'string') return '—';
  const trimmed = raw.trim();
  if (!trimmed || trimmed.startsWith('0001-01-01')) return '—';
  const datePrefixMatch = trimmed.match(/^(\d{4})-(\d{2})-(\d{2})/);
  if (datePrefixMatch) {
    const [, year, month, day] = datePrefixMatch;
    return `${day}/${month}/${year}`;
  }
  const normalized = normalizeSafeIsoForReceipt(trimmed);
  const parsed = new Date(normalized);
  if (Number.isNaN(parsed.getTime())) return '—';
  return new Intl.DateTimeFormat('es-AR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(parsed);
}

function getInitials(nombre: string, apellido: string): string {
  const n = (nombre || '').trim().charAt(0).toUpperCase();
  const a = (apellido || '').trim().charAt(0).toUpperCase();
  return `${n}${a}` || 'ST';
}

interface EspecialidadesChipsCellProps {
  especialidades: string[];
  isOpen: boolean;
  onToggle: () => void;
  onClose: () => void;
}

function EspecialidadesChipsCell({
  especialidades,
  isOpen,
  onToggle,
  onClose,
}: EspecialidadesChipsCellProps) {
  const popoverRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;
    const handleOutside = (event: MouseEvent) => {
      if (
        popoverRef.current &&
        !popoverRef.current.contains(event.target as Node)
      ) {
        onClose();
      }
    };
    const handleEsc = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose();
    };
    document.addEventListener('mousedown', handleOutside);
    document.addEventListener('keydown', handleEsc);
    return () => {
      document.removeEventListener('mousedown', handleOutside);
      document.removeEventListener('keydown', handleEsc);
    };
  }, [isOpen, onClose]);

  if (!especialidades || especialidades.length === 0) {
    return (
      <span
        className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${SEMANTIC_COLORS.neutral.badgeClass}`}
      >
        General
      </span>
    );
  }

  const visibles = especialidades.slice(0, 2);
  const restantes = especialidades.length - visibles.length;

  return (
    <div ref={popoverRef} className="relative flex flex-wrap items-center gap-1.5">
      {visibles.map((esp) => (
        <span
          key={esp}
          className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border max-w-[140px] truncate ${SEMANTIC_COLORS.neutral.badgeClass}`}
          title={esp}
        >
          {esp}
        </span>
      ))}

      {restantes > 0 && (
        <button
          type="button"
          onClick={onToggle}
          aria-expanded={isOpen}
          className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold border border-brand-primary/30 bg-brand-primary/10 text-brand-primary hover:bg-brand-primary/15 transition-colors"
        >
          +{restantes} más
        </button>
      )}

      {isOpen && restantes > 0 && (
        <div
          role="dialog"
          aria-label="Todas las especialidades"
          className="absolute left-0 top-full mt-1.5 z-30 w-60 rounded-xl border border-border-subtle bg-surface p-3 shadow-lg space-y-2"
        >
          <p className="text-xs font-semibold text-ink-primary">
            Especialidades asignadas ({especialidades.length})
          </p>
          <div className="flex flex-wrap gap-1.5">
            {especialidades.map((esp) => (
              <span
                key={esp}
                className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${SEMANTIC_COLORS.neutral.badgeClass}`}
              >
                {esp}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

export default function AdminStaffComisionesLiquidacionesPage() {
  const queryClient = useQueryClient();
  const { configSalon, catalogos } = useCatalogos();
  const { sesionCajaActual, refrescarEstadoCaja } = useCajaPos();
  const moneda = configSalon?.moneda || 'ARS';

  const [subTab, setSubTab] = useState<
    'profesionales' | 'comisiones' | 'liquidaciones'
  >('profesionales');
  const [peluqueros, setPeluqueros] = useState<PeluqueroDto[]>([]);
  const [servicios, setServicios] = useState<ServicioDto[]>([]);
  const [comisiones, setComisiones] = useState<ComisionConfigDto[]>([]);
  const [liquidaciones, setLiquidaciones] = useState<LiquidacionDto[]>([]);
  const [cargando, setCargando] = useState<boolean>(true);
  const [errorCarga, setErrorCarga] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);

  // Popover de especialidades colapsables (+N más)
  const [popoverEspPelId, setPopoverEspPelId] = useState<number | null>(null);

  // 1. Alta/Edición Profesional y Horarios con Descansos
  const [modalPelOpen, setModalPelOpen] = useState<boolean>(false);
  const [pelEditando, setPelEditando] = useState<PeluqueroDto | null>(null);
  const [nombrePel, setNombrePel] = useState('');
  const [apellidoPel, setApellidoPel] = useState('');
  const [emailPel, setEmailPel] = useState('');
  const [telefonoPel, setTelefonoPel] = useState('');
  const [colorHex, setColorHex] = useState('#4F46E5');
  const [especialidadesIds, setEspecialidadesIds] = useState<number[]>([]);
  const [guardandoPel, setGuardandoPel] = useState<boolean>(false);

  const [pelParaBaja, setPelParaBaja] = useState<PeluqueroDto | null>(null);
  const [eliminandoPel, setEliminandoPel] = useState<boolean>(false);

  const [pelHorariosModal, setPelHorariosModal] =
    useState<PeluqueroDto | null>(null);
  const [grillaHorarios, setGrillaHorarios] = useState<HorarioLaboralDto[]>([]);
  const [guardandoHorarios, setGuardandoHorarios] = useState<boolean>(false);

  // 2. Esquemas de Comisión
  const [comEditandoId, setComEditandoId] = useState<number | null>(null);
  const [comPeluqueroId, setComPeluqueroId] = useState<number>(1);
  const [comServicioId, setComServicioId] = useState<number | null>(null);
  const [porcentajeServ, setPorcentajeServ] = useState<number | null>(50);
  const [porcentajeProd, setPorcentajeProd] = useState<number | null>(10);
  const [guardandoComision, setGuardandoComision] = useState<boolean>(false);
  const [comisionParaEliminar, setComisionParaEliminar] =
    useState<ComisionConfigDto | null>(null);
  const [eliminandoComision, setEliminandoComision] = useState<boolean>(false);

  // 3. Liquidaciones, Impresión de Recibo y Pago
  const [peluqueroLiqId, setPeluqueroLiqId] = useState<number>(1);
  const [fechaDesde, setFechaDesde] = useState<string>(() => {
    const d = new Date();
    d.setDate(1);
    return toLocalDateIso(d);
  });
  const [fechaHasta, setFechaHasta] = useState<string>(() =>
    toLocalDateIso(new Date())
  );
  const [bonificaciones, setBonificaciones] = useState<number | null>(0);
  const [descuentos, setDescuentos] = useState<number | null>(0);
  const [generandoLiq, setGenerandoLiq] = useState<boolean>(false);

  const [cargandoReciboId, setCargandoReciboId] = useState<number | null>(null);
  const [reciboImprimir, setReciboImprimir] =
    useState<PrintableReceiptData | null>(null);

  const [liqParaPagar, setLiqParaPagar] = useState<LiquidacionDto | null>(null);
  const [imputarPagoEnCaja, setImputarPagoEnCaja] = useState<boolean>(true);
  const [pagandoLiq, setPagandoLiq] = useState<boolean>(false);

  const [liqParaAnular, setLiqParaAnular] = useState<LiquidacionDto | null>(
    null
  );
  const [anulandoLiq, setAnulandoLiq] = useState<boolean>(false);

  const peluqueroNombreMap = useMemo(() => {
    const map = new Map<number, string>();
    for (const p of peluqueros) {
      map.set(p.id, `${p.nombre} ${p.apellido}`.trim());
    }
    return map;
  }, [peluqueros]);

  const especialidadesCatalogo = useMemo(
    () =>
      catalogos?.especialidades && catalogos.especialidades.length > 0
        ? catalogos.especialidades
        : [
            { id: 1, nombre: 'Colorimetría' },
            { id: 2, nombre: 'Barbería Clásica' },
            { id: 3, nombre: 'Corte Femenino' },
          ],
    [catalogos?.especialidades]
  );

  const cargarDatos = useCallback(async () => {
    setCargando(true);
    setErrorCarga(null);
    try {
      const [pRes, sRes, cRes, lRes] = await Promise.all([
        peluquerosService.getAll(),
        serviciosService.getAll(),
        comisionesService.getAll(),
        liquidacionesService.getAll(),
      ]);
      if (pRes.success) {
        const pelItems = extractPagedItems<PeluqueroDto>(pRes.data);
        setPeluqueros(pelItems);
        if (pelItems.length > 0) {
          setPeluqueroLiqId((prev) =>
            pelItems.some((p) => p.id === prev) ? prev : pelItems[0].id
          );
          setComPeluqueroId((prev) =>
            pelItems.some((p) => p.id === prev) ? prev : pelItems[0].id
          );
        }
      }
      if (sRes.success) {
        setServicios(extractPagedItems<ServicioDto>(sRes.data));
      }
      if (cRes.success) {
        setComisiones(extractPagedItems<ComisionConfigDto>(cRes.data));
      }
      if (lRes.success) {
        setLiquidaciones(extractPagedItems<LiquidacionDto>(lRes.data));
      }
    } catch (err) {
      setErrorCarga({
        message: extractErrorMessage(
          err,
          'No se pudo cargar la información de staff y liquidaciones.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
      notifyApiError(err, {
        fallbackMessage: 'Error al cargar profesionales y liquidaciones.',
      });
    } finally {
      setCargando(false);
    }
  }, []);

  useEffect(() => {
    void cargarDatos();
  }, [cargarDatos]);

  const handleGuardarPeluquero = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      nombre: nombrePel.trim(),
      apellido: apellidoPel.trim(),
      email: emailPel.trim(),
      telefono: telefonoPel.trim() || undefined,
      colorAgendaHex: colorHex,
      activo: true,
      especialidadesIds,
    };
    setGuardandoPel(true);
    try {
      const res = pelEditando
        ? await peluquerosService.update(pelEditando.id, payload)
        : await peluquerosService.create(payload);
      if (res.success) {
        notifyApiSuccess(
          res,
          pelEditando
            ? 'Datos del profesional actualizados.'
            : 'Profesional registrado correctamente.'
        );
        setModalPelOpen(false);
        setPelEditando(null);
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el profesional.',
      });
    } finally {
      setGuardandoPel(false);
    }
  };

  const confirmarBajaPeluquero = async () => {
    if (!pelParaBaja) return;
    setEliminandoPel(true);
    try {
      const res = await peluquerosService.delete(pelParaBaja.id);
      if (res.success) {
        notifyApiSuccess(
          res,
          `El profesional "${pelParaBaja.nombre} ${pelParaBaja.apellido}" fue dado de baja.`
        );
        setPelParaBaja(null);
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo dar de baja al profesional.',
      });
    } finally {
      setEliminandoPel(false);
    }
  };

  const abrirEditorHorarios = async (pel: PeluqueroDto) => {
    setPelHorariosModal(pel);
    try {
      const res = await peluquerosService.getHorarios(pel.id);
      const horarios = extractPagedItems<HorarioLaboralDto>(res.data);
      if (res.success && horarios.length > 0) {
        setGrillaHorarios(horarios);
      } else {
        setGrillaHorarios(
          [1, 2, 3, 4, 5, 6].map((dia) => ({
            diaSemana: dia,
            horaInicio: '09:00',
            horaFin: '19:00',
            horaInicioDescanso: '13:00',
            horaFinDescanso: '14:00',
            activo: true,
          }))
        );
      }
    } catch {
      setGrillaHorarios([]);
    }
  };

  const guardarHorariosSemanales = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!pelHorariosModal) return;
    setGuardandoHorarios(true);
    try {
      const res = await peluquerosService.configurarHorarios(
        pelHorariosModal.id,
        {
          horarios: grillaHorarios,
        }
      );
      if (res.success) {
        notifyApiSuccess(
          res,
          `Horarios y descansos de ${pelHorariosModal.nombre} actualizados.`
        );
        setPelHorariosModal(null);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudieron actualizar los horarios.',
      });
    } finally {
      setGuardandoHorarios(false);
    }
  };

  const handleGuardarComision = async (e: React.FormEvent) => {
    e.preventDefault();
    const dto = {
      peluqueroId: comPeluqueroId,
      servicioId: comServicioId,
      porcentajeServicio: porcentajeServ ?? 0,
      porcentajeVentaProducto: porcentajeProd ?? 0,
    };
    setGuardandoComision(true);
    try {
      const res = comEditandoId
        ? await comisionesService.updateConfig(comEditandoId, dto)
        : await comisionesService.saveConfig(dto);
      if (res.success) {
        notifyApiSuccess(
          res,
          comEditandoId
            ? 'Regla de comisión actualizada correctamente.'
            : 'Regla de comisión guardada correctamente.'
        );
        setComEditandoId(null);
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar la regla de comisión.',
      });
    } finally {
      setGuardandoComision(false);
    }
  };

  const confirmarEliminarComision = async () => {
    if (!comisionParaEliminar) return;
    setEliminandoComision(true);
    try {
      const res = await comisionesService.deleteConfig(comisionParaEliminar.id);
      if (res.success) {
        notifyApiSuccess(res, 'Regla de comisión eliminada.');
        if (comEditandoId === comisionParaEliminar.id) {
          setComEditandoId(null);
        }
        setComisionParaEliminar(null);
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar la regla de comisión.',
      });
    } finally {
      setEliminandoComision(false);
    }
  };

  const handleGenerarLiquidacion = async (e: React.FormEvent) => {
    e.preventDefault();
    if (generandoLiq) return;
    setGenerandoLiq(true);
    try {
      const res = await liquidacionesService.generar({
        peluqueroId: peluqueroLiqId,
        fechaDesde,
        fechaHasta,
        bonificaciones: bonificaciones ?? 0,
        descuentos: descuentos ?? 0,
      });
      if (res.success) {
        notifyApiSuccess(res, 'Liquidación emitida correctamente.');
        setBonificaciones(0);
        setDescuentos(0);
        await queryClient.invalidateQueries({
          queryKey: QUERY_KEYS.liquidaciones(),
        });
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo emitir la liquidación.',
      });
    } finally {
      setGenerandoLiq(false);
    }
  };

  const abrirModalPagoLiquidacion = (liq: LiquidacionDto) => {
    setLiqParaPagar(liq);
    setImputarPagoEnCaja(Boolean(sesionCajaActual));
  };

  const confirmarPagoLiquidacion = async () => {
    if (!liqParaPagar) return;
    const cajaIdAImputar =
      imputarPagoEnCaja && sesionCajaActual ? sesionCajaActual.id : null;
    setPagandoLiq(true);
    try {
      const res = await liquidacionesService.pagar(
        liqParaPagar.id,
        cajaIdAImputar
      );
      if (res.success) {
        notifyApiSuccess(
          res,
          cajaIdAImputar
            ? `Liquidación #${liqParaPagar.id} pagada e imputada en la Caja #${cajaIdAImputar}.`
            : `Liquidación #${liqParaPagar.id} registrada como pagada.`
        );
        setLiqParaPagar(null);
        await Promise.all([
          queryClient.invalidateQueries({
            queryKey: QUERY_KEYS.liquidaciones(),
          }),
          queryClient.invalidateQueries({
            queryKey: QUERY_KEYS.cajaActual(),
          }),
          queryClient.invalidateQueries({
            queryKey: QUERY_KEYS.cajaHistorial(),
          }),
          refrescarEstadoCaja(),
          cargarDatos(),
        ]);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo registrar el pago de la liquidación.',
      });
    } finally {
      setPagandoLiq(false);
    }
  };

  const confirmarAnulacionLiquidacion = async () => {
    if (!liqParaAnular) return;
    setAnulandoLiq(true);
    try {
      const res = await liquidacionesService.anular(
        liqParaAnular.id,
        'Anulación administrativa de liquidación pendiente'
      );
      if (res.success) {
        notifyApiSuccess(
          res,
          `Liquidación #${liqParaAnular.id} anulada. Las comisiones volvieron a quedar disponibles para un nuevo cálculo.`
        );
        setLiqParaAnular(null);
        await queryClient.invalidateQueries({
          queryKey: QUERY_KEYS.liquidaciones(),
        });
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo anular la liquidación.',
      });
    } finally {
      setAnulandoLiq(false);
    }
  };

  const imprimirReciboSueldo = async (liqBase: LiquidacionDto) => {
    setCargandoReciboId(liqBase.id);
    try {
      const res = await liquidacionesService.getById(liqBase.id);
      const l = res.success && res.data ? res.data : liqBase;
      const detallesRaw = extractPagedItems(l.detalles ?? l.detallesLiquidacion);

      const renglones =
        detallesRaw.length > 0
          ? detallesRaw.map((d, idx) => ({
              descripcion:
                d.conceptoNombre ||
                d.conceptoDescripcion ||
                `Comisión #${d.detalleVentaId || d.turnoId || idx + 1}`,
              monto: Number(d.montoComisionCalculado ?? 0),
            }))
          : [
              {
                descripcion: 'Comisiones del período liquidado',
                monto: Number(l.totalServicios ?? 0) + Number(l.totalProductos ?? 0),
              },
            ];

      const profesionalNombre =
        l.peluqueroNombre ||
        l.peluqueroNombreCompleto ||
        peluqueroNombreMap.get(l.peluqueroId) ||
        `Profesional #${l.peluqueroId}`;

      const fechaDocumento = normalizeSafeIsoForReceipt(
        l.fechaPago || l.fechaEmision || l.fechaHasta
      );

      setReciboImprimir({
        titulo: 'RECIBO DE LIQUIDACIÓN DE HABERES',
        numeroComprobante: `LIQ-${l.id}`,
        fechaIso: fechaDocumento,
        clienteOProfesional: profesionalNombre,
        monedaIso: moneda,
        renglones,
        pagosODesglose: [
          { etiqueta: 'Subtotal Servicios', monto: Number(l.totalServicios ?? 0) },
          { etiqueta: 'Subtotal Productos', monto: Number(l.totalProductos ?? 0) },
          { etiqueta: 'Bonificaciones (+)', monto: Number(l.bonificaciones ?? 0) },
          {
            etiqueta: 'Descuentos / Adelantos (-)',
            monto: Number(l.descuentos ?? 0),
          },
        ],
        totalFinal: Number(l.totalPagar ?? 0),
        pieDePagina: `Período liquidado: ${formatSafeDateOnly(
          l.fechaDesde
        )} al ${formatSafeDateOnly(l.fechaHasta)} · Estado: ${
          l.estadoNombre || 'Emitida'
        }`,
      });
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo preparar el recibo de liquidación.',
      });
    } finally {
      setCargandoReciboId(null);
    }
  };

  const getEstadoLiquidacionBadge = (liq: LiquidacionDto) => {
    if (liq.estado === EstadoLiquidacionEnum.Pagado) {
      return {
        label: liq.estadoNombre || 'Pagado',
        badgeClass: SEMANTIC_COLORS.cash.badgeClass,
        dotClass: SEMANTIC_COLORS.cash.dotClass,
      };
    }
    if (liq.estado === EstadoLiquidacionEnum.Anulada) {
      return {
        label: liq.estadoNombre || 'Anulada',
        badgeClass: SEMANTIC_COLORS.danger.badgeClass,
        dotClass: SEMANTIC_COLORS.danger.dotClass,
      };
    }
    return {
      label: liq.estadoNombre || 'Pendiente',
      badgeClass: SEMANTIC_COLORS.warning.badgeClass,
      dotClass: SEMANTIC_COLORS.warning.dotClass,
    };
  };

  return (
    <div className="max-w-[1440px] mx-auto space-y-5">
      {/* Cabecera */}
      <header className="bg-surface border border-border-subtle rounded-xl p-4 sm:p-5 flex flex-col xl:flex-row xl:items-center xl:justify-between gap-4">
        <div className="min-w-0">
          <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary">
            Staff, Comisiones y Liquidaciones
          </h1>
          <p className="text-sm text-ink-secondary mt-0.5">
            Administrá tu equipo de estilistas, sus horarios con descansos,
            reglas de comisión y recibos de pago.
          </p>
        </div>

        <div
          role="tablist"
          aria-label="Secciones de staff y liquidaciones"
          className="inline-flex flex-wrap items-center rounded-lg bg-elevated p-1 border border-border-subtle self-start xl:self-auto gap-0.5"
        >
          <button
            type="button"
            role="tab"
            aria-selected={subTab === 'profesionales'}
            onClick={() => setSubTab('profesionales')}
            className={`inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-semibold transition-colors ${
              subTab === 'profesionales'
                ? 'bg-surface text-ink-primary shadow-sm border border-border-subtle'
                : 'text-ink-secondary hover:text-ink-primary'
            }`}
          >
            <Users className="w-3.5 h-3.5" />
            <span>Profesionales</span>
            <span className="font-mono-tabular text-[11px] px-1.5 py-0.5 rounded-full bg-elevated text-ink-secondary">
              {peluqueros.length}
            </span>
          </button>

          <button
            type="button"
            role="tab"
            aria-selected={subTab === 'comisiones'}
            onClick={() => setSubTab('comisiones')}
            className={`inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-semibold transition-colors ${
              subTab === 'comisiones'
                ? 'bg-surface text-ink-primary shadow-sm border border-border-subtle'
                : 'text-ink-secondary hover:text-ink-primary'
            }`}
          >
            <Percent className="w-3.5 h-3.5" />
            <span>Comisiones</span>
            <span className="font-mono-tabular text-[11px] px-1.5 py-0.5 rounded-full bg-elevated text-ink-secondary">
              {comisiones.length}
            </span>
          </button>

          <button
            type="button"
            role="tab"
            aria-selected={subTab === 'liquidaciones'}
            onClick={() => setSubTab('liquidaciones')}
            className={`inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-xs font-semibold transition-colors ${
              subTab === 'liquidaciones'
                ? 'bg-surface text-ink-primary shadow-sm border border-border-subtle'
                : 'text-ink-secondary hover:text-ink-primary'
            }`}
          >
            <FileSpreadsheet className="w-3.5 h-3.5" />
            <span>Liquidaciones</span>
            <span className="font-mono-tabular text-[11px] px-1.5 py-0.5 rounded-full bg-elevated text-ink-secondary">
              {liquidaciones.length}
            </span>
          </button>
        </div>
      </header>

      {/* PESTAÑA: PROFESIONALES Y HORARIOS */}
      {subTab === 'profesionales' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between gap-3">
            <p className="text-xs text-ink-secondary">
              Cada profesional cuenta con su color en agenda, especialidades y
              grilla semanal con descansos.
            </p>
            <Button
              variant="primary"
              size="md"
              onClick={() => {
                setPelEditando(null);
                setNombrePel('');
                setApellidoPel('');
                setEmailPel('');
                setTelefonoPel('');
                setColorHex('#4F46E5');
                setEspecialidadesIds([]);
                setModalPelOpen(true);
              }}
              className="shrink-0"
            >
              <Plus className="w-4 h-4" />
              Alta de Profesional
            </Button>
          </div>

          <AsyncStateGuard
            isLoading={cargando}
            isError={Boolean(errorCarga)}
            errorMessage={errorCarga?.message}
            correlationId={errorCarga?.correlationId}
            isEmpty={(peluqueros ?? []).length === 0}
            loadingText="Cargando plantel de estilistas..."
            emptyTitle="Sin profesionales registrados"
            emptyDescription="Registrá tu primer estilista con 'Alta de Profesional'."
            onRetry={cargarDatos}
          >
            <div className="bg-surface border border-border-subtle rounded-xl overflow-visible">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                    <tr>
                      <th className="px-4 py-3">Profesional</th>
                      <th className="px-4 py-3">Contacto</th>
                      <th className="px-4 py-3">Especialidades</th>
                      <th className="px-4 py-3 text-right">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {(peluqueros ?? []).map((p) => (
                      <tr
                        key={p.id}
                        className="hover:bg-elevated/60 transition-colors"
                      >
                        <td className="px-4 py-3">
                          <div className="flex items-center gap-3">
                            <span
                              className="w-9 h-9 rounded-full flex items-center justify-center text-xs font-bold text-white shrink-0 shadow-sm"
                              style={{
                                backgroundColor: p.colorAgendaHex || '#4F46E5',
                              }}
                              title={`Color en agenda: ${p.colorAgendaHex}`}
                            >
                              {getInitials(p.nombre, p.apellido)}
                            </span>
                            <div className="min-w-0">
                              <p className="font-semibold text-ink-primary truncate">
                                {p.nombre} {p.apellido}
                              </p>
                              <span className="font-mono-tabular text-[11px] text-ink-secondary">
                                {p.colorAgendaHex}
                              </span>
                            </div>
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <p className="text-xs text-ink-primary truncate max-w-[220px]">
                            {p.email}
                          </p>
                          <p className="font-mono-tabular text-xs text-ink-secondary mt-0.5">
                            {p.telefono || 'Sin teléfono'}
                          </p>
                        </td>
                        <td className="px-4 py-3">
                          <EspecialidadesChipsCell
                            especialidades={p.especialidadesNombres ?? []}
                            isOpen={popoverEspPelId === p.id}
                            onToggle={() =>
                              setPopoverEspPelId((prev) =>
                                prev === p.id ? null : p.id
                              )
                            }
                            onClose={() => setPopoverEspPelId(null)}
                          />
                        </td>
                        <td className="px-4 py-3 text-right">
                          <div className="inline-flex items-center justify-end gap-1.5">
                            <Button
                              variant="secondary"
                              size="sm"
                              onClick={() => void abrirEditorHorarios(p)}
                            >
                              <CalendarClock className="w-3.5 h-3.5" />
                              Horarios
                            </Button>
                            <Button
                              variant="secondary"
                              size="sm"
                              onClick={() => {
                                setPelEditando(p);
                                setNombrePel(p.nombre);
                                setApellidoPel(p.apellido);
                                setEmailPel(p.email);
                                setTelefonoPel(p.telefono || '');
                                setColorHex(p.colorAgendaHex || '#4F46E5');
                                setEspecialidadesIds(p.especialidadesIds || []);
                                setModalPelOpen(true);
                              }}
                            >
                              <Pencil className="w-3.5 h-3.5" />
                              Editar
                            </Button>
                            <Button
                              variant="destructive"
                              destructiveTone="soft"
                              size="sm"
                              onClick={() => setPelParaBaja(p)}
                            >
                              <Trash2 className="w-3.5 h-3.5" />
                              Baja
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </AsyncStateGuard>
        </div>
      )}

      {/* PESTAÑA: ESQUEMAS DE COMISIÓN */}
      {subTab === 'comisiones' && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
          <form
            onSubmit={handleGuardarComision}
            className="lg:col-span-5 bg-surface border border-border-subtle rounded-xl p-5 space-y-4"
          >
            <div>
              <h2 className="text-base font-semibold text-ink-primary">
                {comEditandoId
                  ? `Editar regla de comisión #${comEditandoId}`
                  : 'Nueva regla de comisión'}
              </h2>
              <p className="text-xs text-ink-secondary mt-0.5">
                Definí un porcentaje general por profesional o una excepción
                para un servicio puntual.
              </p>
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-ink-secondary">
                Profesional *
              </label>
              <select
                value={comPeluqueroId}
                onChange={(e) => setComPeluqueroId(Number(e.target.value))}
                className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                {peluqueros.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nombre} {p.apellido}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-ink-secondary">
                Alcance de la regla
              </label>
              <select
                value={comServicioId ?? ''}
                onChange={(e) =>
                  setComServicioId(
                    e.target.value ? Number(e.target.value) : null
                  )
                }
                className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                <option value="">Regla general (todos los servicios)</option>
                {servicios.map((s) => (
                  <option key={s.id} value={s.id}>
                    Servicio específico: {s.nombre}
                  </option>
                ))}
              </select>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <NumericInput
                label="Comisión servicios *"
                suffix="%"
                allowDecimals
                decimalPlaces={1}
                min={0}
                max={100}
                required
                value={porcentajeServ}
                onValueChange={(val) => setPorcentajeServ(val)}
              />
              <NumericInput
                label="Venta de productos *"
                suffix="%"
                allowDecimals
                decimalPlaces={1}
                min={0}
                max={100}
                required
                value={porcentajeProd}
                onValueChange={(val) => setPorcentajeProd(val)}
              />
            </div>

            <div className="flex items-center gap-2 pt-2">
              {comEditandoId && (
                <Button
                  type="button"
                  variant="secondary"
                  size="md"
                  onClick={() => {
                    setComEditandoId(null);
                    setComServicioId(null);
                    setPorcentajeServ(50);
                    setPorcentajeProd(10);
                  }}
                >
                  Cancelar
                </Button>
              )}
              <Button
                type="submit"
                variant="primary"
                size="md"
                isLoading={guardandoComision}
                className="flex-1"
              >
                {comEditandoId ? 'Actualizar regla' : 'Guardar regla'}
              </Button>
            </div>
          </form>

          <div className="lg:col-span-7 bg-surface border border-border-subtle rounded-xl overflow-hidden">
            <div className="px-5 py-4 border-b border-border-subtle flex items-center justify-between gap-2">
              <h2 className="text-base font-semibold text-ink-primary">
                Matriz de Comisiones Vigente
              </h2>
              <span className="font-mono-tabular text-xs text-ink-secondary">
                {comisiones.length} regla(s)
              </span>
            </div>

            <AsyncStateGuard
              isLoading={cargando}
              isError={Boolean(errorCarga)}
              errorMessage={errorCarga?.message}
              correlationId={errorCarga?.correlationId}
              isEmpty={(comisiones ?? []).length === 0}
              loadingText="Cargando reglas de comisión..."
              emptyTitle="Sin reglas configuradas"
              emptyDescription="Creá la primera regla desde el formulario izquierdo."
              onRetry={cargarDatos}
            >
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                    <tr>
                      <th className="px-4 py-3">Profesional</th>
                      <th className="px-4 py-3">Alcance</th>
                      <th className="px-4 py-3 text-right">% Servicio</th>
                      <th className="px-4 py-3 text-right">% Producto</th>
                      <th className="px-4 py-3 text-right">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {(comisiones ?? []).map((c) => {
                      const nombreReal =
                        c.peluqueroNombre ||
                        c.peluqueroNombreCompleto ||
                        peluqueroNombreMap.get(c.peluqueroId) ||
                        `Profesional #${c.peluqueroId}`;
                      const isEditing = comEditandoId === c.id;
                      return (
                        <tr
                          key={c.id}
                          className={`transition-colors ${
                            isEditing
                              ? 'bg-brand-primary/10'
                              : 'hover:bg-elevated/60'
                          }`}
                        >
                          <td className="px-4 py-3 font-semibold text-ink-primary">
                            <div className="flex items-center gap-2">
                              <span>{nombreReal}</span>
                              {isEditing && (
                                <span className="px-2 py-0.5 rounded-full text-[11px] font-semibold bg-brand-primary text-white">
                                  Editando
                                </span>
                              )}
                            </div>
                          </td>
                          <td className="px-4 py-3">
                            {c.servicioNombre ? (
                              <span
                                className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${SEMANTIC_COLORS.digital.badgeClass}`}
                              >
                                {c.servicioNombre}
                              </span>
                            ) : (
                              <span
                                className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${SEMANTIC_COLORS.neutral.badgeClass}`}
                              >
                                Regla general
                              </span>
                            )}
                          </td>
                          <td className="px-4 py-3 text-right font-mono-tabular font-semibold text-ink-primary">
                            {c.porcentajeServicio}%
                          </td>
                          <td className="px-4 py-3 text-right font-mono-tabular text-ink-secondary">
                            {c.porcentajeVentaProducto}%
                          </td>
                          <td className="px-4 py-3 text-right">
                            <div className="inline-flex items-center justify-end gap-1.5">
                              <Button
                                variant="secondary"
                                size="sm"
                                onClick={() => {
                                  setComEditandoId(c.id);
                                  setComPeluqueroId(c.peluqueroId);
                                  setComServicioId(c.servicioId || null);
                                  setPorcentajeServ(c.porcentajeServicio);
                                  setPorcentajeProd(c.porcentajeVentaProducto);
                                }}
                              >
                                Editar
                              </Button>
                              <Button
                                variant="destructive"
                                destructiveTone="soft"
                                size="sm"
                                onClick={() => setComisionParaEliminar(c)}
                              >
                                Eliminar
                              </Button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </AsyncStateGuard>
          </div>
        </div>
      )}

      {/* PESTAÑA: LIQUIDACIÓN DE HABERES */}
      {subTab === 'liquidaciones' && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
          <form
            onSubmit={handleGenerarLiquidacion}
            className="lg:col-span-4 bg-surface border border-border-subtle rounded-xl p-5 space-y-4"
          >
            <div>
              <h2 className="text-base font-semibold text-ink-primary">
                Liquidar Período de Comisiones
              </h2>
              <p className="text-xs text-ink-secondary mt-0.5">
                Calcula las ventas pendientes del profesional dentro del rango
                elegido y emite su recibo.
              </p>
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-ink-secondary">
                Profesional *
              </label>
              <select
                value={peluqueroLiqId}
                onChange={(e) => setPeluqueroLiqId(Number(e.target.value))}
                className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                {peluqueros.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nombre} {p.apellido}
                  </option>
                ))}
              </select>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <InputField
                label="Fecha desde *"
                type="date"
                required
                value={fechaDesde}
                onChange={(e) => setFechaDesde(e.target.value)}
              />
              <InputField
                label="Fecha hasta *"
                type="date"
                required
                value={fechaHasta}
                onChange={(e) => setFechaHasta(e.target.value)}
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <NumericInput
                label="Bonificaciones (+)"
                prefix="$"
                allowDecimals
                decimalPlaces={2}
                min={0}
                value={bonificaciones}
                onValueChange={(val) => setBonificaciones(val)}
              />
              <NumericInput
                label="Adelantos / Desc. (-)"
                prefix="$"
                allowDecimals
                decimalPlaces={2}
                min={0}
                value={descuentos}
                onValueChange={(val) => setDescuentos(val)}
              />
            </div>

            <Button
              type="submit"
              variant="primary"
              size="md"
              isLoading={generandoLiq}
              disabled={generandoLiq || peluqueros.length === 0}
              className="w-full"
            >
              Calcular y Emitir Liquidación
            </Button>
          </form>

          <div className="lg:col-span-8 bg-surface border border-border-subtle rounded-xl overflow-hidden">
            <div className="px-5 py-4 border-b border-border-subtle flex flex-wrap items-center justify-between gap-2">
              <h2 className="text-base font-semibold text-ink-primary">
                Recibos de Liquidación Emitidos
              </h2>
              {sesionCajaActual ? (
                <span
                  className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${SEMANTIC_COLORS.cash.badgeClass}`}
                >
                  <Wallet className="w-3.5 h-3.5" />
                  Caja #{sesionCajaActual.id} abierta para pagos
                </span>
              ) : (
                <span
                  className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium border ${SEMANTIC_COLORS.neutral.badgeClass}`}
                >
                  <Wallet className="w-3.5 h-3.5" />
                  Sin caja abierta (pago externo)
                </span>
              )}
            </div>

            <AsyncStateGuard
              isLoading={cargando}
              isError={Boolean(errorCarga)}
              errorMessage={errorCarga?.message}
              correlationId={errorCarga?.correlationId}
              isEmpty={(liquidaciones ?? []).length === 0}
              loadingText="Cargando recibos de liquidación..."
              emptyTitle="Sin liquidaciones emitidas"
              emptyDescription="Calculá una liquidación desde el formulario izquierdo para ver su recibo aquí."
              onRetry={cargarDatos}
            >
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                    <tr>
                      <th className="px-3.5 py-3">N°</th>
                      <th className="px-3.5 py-3">Profesional</th>
                      <th className="px-3.5 py-3">Período</th>
                      <th className="px-3.5 py-3 text-right">Total a pagar</th>
                      <th className="px-3.5 py-3">Estado</th>
                      <th className="px-3.5 py-3 text-right">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {(liquidaciones ?? []).map((l) => {
                      const badge = getEstadoLiquidacionBadge(l);
                      const profNombre =
                        l.peluqueroNombre ||
                        l.peluqueroNombreCompleto ||
                        peluqueroNombreMap.get(l.peluqueroId) ||
                        `Profesional #${l.peluqueroId}`;

                      return (
                        <tr
                          key={l.id}
                          className="hover:bg-elevated/60 transition-colors"
                        >
                          <td className="px-3.5 py-3 font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                            #{l.id}
                          </td>
                          <td className="px-3.5 py-3 font-medium text-ink-primary whitespace-nowrap">
                            {profNombre}
                          </td>
                          <td className="px-3.5 py-3 font-mono-tabular text-xs text-ink-secondary whitespace-nowrap">
                            {formatSafeDateOnly(l.fechaDesde)} –{' '}
                            {formatSafeDateOnly(l.fechaHasta)}
                          </td>
                          <td className="px-3.5 py-3 text-right font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                            {formatCurrency(l.totalPagar, moneda)}
                          </td>
                          <td className="px-3.5 py-3 whitespace-nowrap">
                            <span
                              className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium border ${badge.badgeClass}`}
                            >
                              <span
                                className={`w-1.5 h-1.5 rounded-full ${badge.dotClass}`}
                              />
                              {badge.label}
                            </span>
                          </td>
                          <td className="px-3.5 py-3 text-right whitespace-nowrap">
                            <div className="inline-flex items-center justify-end gap-1.5">
                              <Button
                                variant="secondary"
                                size="sm"
                                isLoading={cargandoReciboId === l.id}
                                onClick={() => void imprimirReciboSueldo(l)}
                              >
                                <Printer className="w-3.5 h-3.5" />
                                Recibo PDF
                              </Button>

                              {l.estado === EstadoLiquidacionEnum.Pendiente && (
                                <>
                                  <Button
                                    variant="secondary"
                                    size="sm"
                                    onClick={() => abrirModalPagoLiquidacion(l)}
                                    className="border-emerald-300 text-emerald-700 hover:bg-emerald-50 dark:border-emerald-800 dark:text-emerald-300 dark:hover:bg-emerald-950/50"
                                  >
                                    <Banknote className="w-3.5 h-3.5" />
                                    Pagar
                                  </Button>
                                  <Button
                                    variant="destructive"
                                    destructiveTone="soft"
                                    size="sm"
                                    onClick={() => setLiqParaAnular(l)}
                                  >
                                    <XCircle className="w-3.5 h-3.5" />
                                    Anular
                                  </Button>
                                </>
                              )}
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </AsyncStateGuard>
          </div>
        </div>
      )}

      {/* Modal Grilla Semanal de Horarios y Descansos */}
      <Modal
        isOpen={Boolean(pelHorariosModal)}
        onClose={() => setPelHorariosModal(null)}
        title={`Jornada y Descansos — ${pelHorariosModal?.nombre ?? ''} ${
          pelHorariosModal?.apellido ?? ''
        }`}
        subtitle="Definí la franja laboral y el descanso de cada día para calcular disponibilidad real en agenda."
        maxWidthClass="max-w-3xl"
      >
        <form onSubmit={guardarHorariosSemanales} className="space-y-3">
          <div className="grid grid-cols-12 gap-2 text-xs font-medium text-ink-secondary border-b border-border-subtle pb-2 px-1">
            <span className="col-span-3">Día</span>
            <span className="col-span-2">Entrada</span>
            <span className="col-span-2">Salida</span>
            <span className="col-span-2">Inicio desc.</span>
            <span className="col-span-2">Fin desc.</span>
            <span className="col-span-1 text-center">Activo</span>
          </div>

          <div className="space-y-2">
            {grillaHorarios.map((h, idx) => (
              <div
                key={`${h.diaSemana}-${idx}`}
                className="grid grid-cols-12 gap-2 items-center border-b border-border-subtle/70 pb-2 text-sm"
              >
                <span className="col-span-3 font-medium text-ink-primary">
                  {DIAS_SEMANA[h.diaSemana] || `Día ${h.diaSemana}`}
                </span>
                <input
                  type="time"
                  value={h.horaInicio.slice(0, 5)}
                  onChange={(e) => {
                    const next = [...grillaHorarios];
                    next[idx] = { ...next[idx], horaInicio: e.target.value };
                    setGrillaHorarios(next);
                  }}
                  className="col-span-2 h-9 rounded-lg border border-border-strong bg-surface px-2 text-xs font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none"
                />
                <input
                  type="time"
                  value={h.horaFin.slice(0, 5)}
                  onChange={(e) => {
                    const next = [...grillaHorarios];
                    next[idx] = { ...next[idx], horaFin: e.target.value };
                    setGrillaHorarios(next);
                  }}
                  className="col-span-2 h-9 rounded-lg border border-border-strong bg-surface px-2 text-xs font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none"
                />
                <input
                  type="time"
                  value={(h.horaInicioDescanso || '13:00').slice(0, 5)}
                  onChange={(e) => {
                    const next = [...grillaHorarios];
                    next[idx] = {
                      ...next[idx],
                      horaInicioDescanso: e.target.value,
                    };
                    setGrillaHorarios(next);
                  }}
                  className="col-span-2 h-9 rounded-lg border border-border-strong bg-surface px-2 text-xs font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none"
                />
                <input
                  type="time"
                  value={(h.horaFinDescanso || '14:00').slice(0, 5)}
                  onChange={(e) => {
                    const next = [...grillaHorarios];
                    next[idx] = {
                      ...next[idx],
                      horaFinDescanso: e.target.value,
                    };
                    setGrillaHorarios(next);
                  }}
                  className="col-span-2 h-9 rounded-lg border border-border-strong bg-surface px-2 text-xs font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none"
                />
                <div className="col-span-1 flex justify-center">
                  <input
                    type="checkbox"
                    checked={h.activo ?? true}
                    onChange={(e) => {
                      const next = [...grillaHorarios];
                      next[idx] = { ...next[idx], activo: e.target.checked };
                      setGrillaHorarios(next);
                    }}
                    className="h-4 w-4 rounded border-border-strong accent-brand-primary"
                  />
                </div>
              </div>
            ))}
          </div>

          <div className="flex items-center justify-end gap-2 pt-3 border-t border-border-subtle">
            <Button
              type="button"
              variant="secondary"
              size="md"
              onClick={() => setPelHorariosModal(null)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="md"
              isLoading={guardandoHorarios}
            >
              Guardar horarios y descansos
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Alta / Edición de Profesional */}
      <Modal
        isOpen={modalPelOpen}
        onClose={() => setModalPelOpen(false)}
        title={pelEditando ? 'Editar Profesional' : 'Alta de Profesional'}
        subtitle="Completá los datos de contacto, color de agenda y especialidades."
      >
        <form onSubmit={handleGuardarPeluquero} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <InputField
              label="Nombre *"
              required
              value={nombrePel}
              onChange={(e) => setNombrePel(e.target.value)}
            />
            <InputField
              label="Apellido *"
              required
              value={apellidoPel}
              onChange={(e) => setApellidoPel(e.target.value)}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <InputField
              label="Email *"
              type="email"
              required
              value={emailPel}
              onChange={(e) => setEmailPel(e.target.value)}
            />
            <InputField
              label="Teléfono"
              value={telefonoPel}
              onChange={(e) => setTelefonoPel(e.target.value)}
            />
          </div>

          <div className="flex items-center gap-3">
            <div className="flex-1">
              <InputField
                label="Color identificatorio en agenda *"
                type="color"
                value={colorHex}
                onChange={(e) => setColorHex(e.target.value)}
                className="h-10 cursor-pointer p-1"
              />
            </div>
            <div className="pt-5">
              <span
                className="inline-flex items-center gap-2 px-3 h-10 rounded-lg border border-border-subtle bg-elevated text-xs font-mono-tabular text-ink-primary"
              >
                <span
                  className="w-4 h-4 rounded-full border border-white/40"
                  style={{ backgroundColor: colorHex }}
                />
                {colorHex.toUpperCase()}
              </span>
            </div>
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-medium text-ink-secondary">
              Especialidades asignadas
            </label>
            <div className="grid grid-cols-2 gap-2 p-3 border border-border-subtle rounded-lg bg-elevated/40 text-sm">
              {especialidadesCatalogo.map((esp) => (
                <label
                  key={esp.id}
                  className="flex items-center gap-2 cursor-pointer select-none text-ink-primary"
                >
                  <input
                    type="checkbox"
                    checked={especialidadesIds.includes(esp.id)}
                    onChange={() =>
                      setEspecialidadesIds((prev) =>
                        prev.includes(esp.id)
                          ? prev.filter((id) => id !== esp.id)
                          : [...prev, esp.id]
                      )
                    }
                    className="h-4 w-4 rounded border-border-strong accent-brand-primary"
                  />
                  <span className="text-xs">{esp.nombre}</span>
                </label>
              ))}
            </div>
          </div>

          <div className="flex items-center justify-end gap-2 pt-3 border-t border-border-subtle">
            <Button
              type="button"
              variant="secondary"
              size="md"
              onClick={() => setModalPelOpen(false)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="md"
              isLoading={guardandoPel}
            >
              {pelEditando ? 'Guardar cambios' : 'Registrar profesional'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Confirmación de Pago de Liquidación (con opción de imputar en Caja abierta) */}
      <ConfirmModal
        isOpen={Boolean(liqParaPagar)}
        onClose={() => setLiqParaPagar(null)}
        onConfirm={() => void confirmarPagoLiquidacion()}
        title={`Confirmar pago — Liquidación #${liqParaPagar?.id ?? ''}`}
        confirmLabel="Confirmar pago"
        isLoading={pagandoLiq}
      >
        <div className="space-y-3">
          <div className="rounded-lg border border-border-subtle bg-elevated/60 p-3 flex items-center justify-between gap-3">
            <div>
              <p className="text-xs text-ink-secondary">Profesional</p>
              <p className="text-sm font-semibold text-ink-primary">
                {liqParaPagar?.peluqueroNombre ||
                  liqParaPagar?.peluqueroNombreCompleto ||
                  (liqParaPagar
                    ? peluqueroNombreMap.get(liqParaPagar.peluqueroId)
                    : '') ||
                  'Profesional'}
              </p>
            </div>
            <div className="text-right">
              <p className="text-xs text-ink-secondary">Importe a abonar</p>
              <p
                className={`font-mono-tabular text-base font-bold ${SEMANTIC_COLORS.cash.textClass}`}
              >
                {formatCurrency(liqParaPagar?.totalPagar ?? 0, moneda)}
              </p>
            </div>
          </div>

          {sesionCajaActual ? (
            <label className="flex items-start gap-2.5 rounded-lg border border-border-subtle bg-surface p-3 cursor-pointer select-none">
              <input
                type="checkbox"
                checked={imputarPagoEnCaja}
                onChange={(e) => setImputarPagoEnCaja(e.target.checked)}
                className="mt-0.5 h-4 w-4 rounded border-border-strong accent-brand-primary"
              />
              <span className="text-xs leading-relaxed text-ink-primary">
                <strong className="block font-semibold">
                  Imputar egreso en la Caja Abierta actual (Caja #
                  {sesionCajaActual.id})
                </strong>
                <span className="text-ink-secondary">
                  Si desmarcás esta opción, la liquidación quedará registrada
                  como pagada sin descontar efectivo del cajón actual.
                </span>
              </span>
            </label>
          ) : (
            <div
              className={`rounded-lg border p-3 text-xs flex items-start gap-2 ${SEMANTIC_COLORS.neutral.surfaceClass}`}
            >
              <CheckCircle2 className="w-4 h-4 shrink-0 mt-0.5 text-ink-secondary" />
              <span className="text-ink-secondary">
                No hay una caja abierta en este momento. La liquidación se
                marcará como pagada fuera de caja (transferencia o fondo
                externo).
              </span>
            </div>
          )}
        </div>
      </ConfirmModal>

      {/* Confirmación de Anulación de Liquidación */}
      <ConfirmModal
        isOpen={Boolean(liqParaAnular)}
        onClose={() => setLiqParaAnular(null)}
        onConfirm={() => void confirmarAnulacionLiquidacion()}
        title={`Anular liquidación #${liqParaAnular?.id ?? ''}`}
        confirmLabel="Confirmar anulación"
        variant="danger"
        isLoading={anulandoLiq}
      >
        <p className="text-ink-secondary">
          ¿Querés anular la liquidación pendiente de{' '}
          <strong className="text-ink-primary">
            {liqParaAnular?.peluqueroNombre ||
              liqParaAnular?.peluqueroNombreCompleto ||
              'este profesional'}
          </strong>
          ? Las comisiones incluidas volverán a quedar disponibles para un nuevo
          cálculo.
        </p>
      </ConfirmModal>

      {/* Confirmación de Baja de Profesional */}
      <ConfirmModal
        isOpen={Boolean(pelParaBaja)}
        onClose={() => setPelParaBaja(null)}
        onConfirm={() => void confirmarBajaPeluquero()}
        title="Dar de baja profesional"
        confirmLabel="Confirmar baja"
        variant="danger"
        isLoading={eliminandoPel}
      >
        <p className="text-ink-secondary">
          ¿Querés dar de baja a{' '}
          <strong className="text-ink-primary">
            {pelParaBaja?.nombre} {pelParaBaja?.apellido}
          </strong>
          ? Se conservará su historial de turnos, ventas y liquidaciones.
        </p>
      </ConfirmModal>

      {/* Confirmación de Eliminación de Regla de Comisión */}
      <ConfirmModal
        isOpen={Boolean(comisionParaEliminar)}
        onClose={() => setComisionParaEliminar(null)}
        onConfirm={() => void confirmarEliminarComision()}
        title="Eliminar regla de comisión"
        confirmLabel="Eliminar regla"
        variant="danger"
        isLoading={eliminandoComision}
      >
        <p className="text-ink-secondary">
          ¿Querés eliminar esta regla de comisión? Las liquidaciones ya emitidas
          no se verán afectadas.
        </p>
      </ConfirmModal>

      {/* Modal Vista Previa / Impresión de Recibo en 1 Hoja */}
      <PrintReceiptModal
        data={reciboImprimir}
        onClose={() => setReciboImprimir(null)}
        nombreSalon={configSalon?.nombreComercial}
        cuitSalon={configSalon?.cuit}
      />
    </div>
  );
}
