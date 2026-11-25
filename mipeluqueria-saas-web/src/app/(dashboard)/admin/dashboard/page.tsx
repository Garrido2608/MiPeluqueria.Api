// filepath: src/app/(dashboard)/admin/dashboard/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  AlertTriangle,
  Award,
  CalendarCheck,
  DollarSign,
  Receipt,
  RefreshCw,
  Scissors,
  TrendingUp,
  ArrowRight,
  PackageX,
} from 'lucide-react';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import {
  DashboardKpisDto,
  PeluqueroDto,
  ProductoDto,
} from '@/types/api.types';
import {
  dashboardService,
  peluquerosService,
  productosService,
} from '@/services/api.services';
import { useCatalogos } from '@/context/AppProviders';
import { useTheme } from '@/context/ThemeProvider';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import { formatCurrency } from '@/lib/formatters';
import {
  getPaymentMethodColor,
  SEMANTIC_COLORS,
} from '@/lib/semanticColors';
import { AsyncStateGuard, Badge, Button } from '@/components/ui';

type QuickPeriod = 'hoy' | 'semana' | 'mes' | 'custom';

interface PieTooltipEntry {
  name?: string;
  value?: number;
  payload?: {
    medioPagoNombre: string;
    monto: number;
    porcentaje: number;
    colorHex: string;
  };
}

interface BarTooltipEntry {
  value?: number;
  payload?: {
    peluqueroNombre: string;
    cantidadServicios: number;
    facturacionTotal: number;
    colorHex: string;
  };
}

interface CustomTooltipProps<TEntry> {
  active?: boolean;
  payload?: TEntry[];
  moneda: string;
}

function toLocalDateIso(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function getQuickRange(period: Exclude<QuickPeriod, 'custom'>): {
  desde: string;
  hasta: string;
} {
  const today = new Date();
  if (period === 'hoy') {
    const iso = toLocalDateIso(today);
    return { desde: iso, hasta: iso };
  }
  if (period === 'semana') {
    const dayOfWeek = today.getDay(); // 0=Dom, 1=Lun...
    const diffToMonday = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;
    const monday = new Date(today);
    monday.setDate(today.getDate() + diffToMonday);
    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);
    return { desde: toLocalDateIso(monday), hasta: toLocalDateIso(sunday) };
  }
  const firstDay = new Date(today.getFullYear(), today.getMonth(), 1);
  return { desde: toLocalDateIso(firstDay), hasta: toLocalDateIso(today) };
}

function PaymentTooltip({
  active,
  payload,
  moneda,
}: CustomTooltipProps<PieTooltipEntry>) {
  if (!active || !payload || payload.length === 0) return null;
  const item = payload[0]?.payload;
  if (!item) return null;

  return (
    <div className="rounded-lg border border-border-subtle bg-surface px-3 py-2 text-xs shadow-lg">
      <div className="flex items-center gap-2 font-semibold text-ink-primary">
        <span
          className="h-2.5 w-2.5 rounded-full shrink-0"
          style={{ backgroundColor: item.colorHex }}
        />
        <span>{item.medioPagoNombre}</span>
      </div>
      <p className="mt-1 font-mono-tabular font-semibold text-ink-primary">
        {formatCurrency(item.monto, moneda)}
      </p>
      <p className="font-mono-tabular text-[11px] text-ink-secondary">
        {item.porcentaje.toFixed(1)}% del total
      </p>
    </div>
  );
}

function RankingTooltip({
  active,
  payload,
  moneda,
}: CustomTooltipProps<BarTooltipEntry>) {
  if (!active || !payload || payload.length === 0) return null;
  const item = payload[0]?.payload;
  if (!item) return null;

  return (
    <div className="rounded-lg border border-border-subtle bg-surface px-3 py-2 text-xs shadow-lg">
      <div className="flex items-center gap-2 font-semibold text-ink-primary">
        <span
          className="h-2.5 w-2.5 rounded-full shrink-0"
          style={{ backgroundColor: item.colorHex }}
        />
        <span>{item.peluqueroNombre}</span>
      </div>
      <p className="mt-1 font-mono-tabular font-semibold text-ink-primary">
        {formatCurrency(item.facturacionTotal, moneda)}
      </p>
      <p className="font-mono-tabular text-[11px] text-ink-secondary">
        {item.cantidadServicios} servicio(s) realizados
      </p>
    </div>
  );
}

export default function AdminDashboardBiPage() {
  const { configSalon } = useCatalogos();
  const { theme } = useTheme();
  const moneda = configSalon?.moneda || configSalon?.monedaIso || 'ARS';

  const initialMonth = useMemo(() => getQuickRange('mes'), []);
  const [periodoActivo, setPeriodoActivo] = useState<QuickPeriod>('mes');
  const [fechaDesde, setFechaDesde] = useState<string>(initialMonth.desde);
  const [fechaHasta, setFechaHasta] = useState<string>(initialMonth.hasta);

  const [kpis, setKpis] = useState<DashboardKpisDto | null>(null);
  const [stockCritico, setStockCritico] = useState<ProductoDto[]>([]);
  const [peluqueros, setPeluqueros] = useState<PeluqueroDto[]>([]);
  const [cargando, setCargando] = useState<boolean>(true);
  const [errorCarga, setErrorCarga] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);

  const cargarDashboard = useCallback(async () => {
    setCargando(true);
    setErrorCarga(null);
    try {
      const [kpiRes, critRes, pelRes] = await Promise.all([
        dashboardService.getKpis(fechaDesde, fechaHasta),
        productosService.getStockCritico(),
        peluquerosService.getAll().catch(() => null),
      ]);
      if (kpiRes.success && kpiRes.data) setKpis(kpiRes.data);
      if (critRes.success) {
        setStockCritico(extractPagedItems<ProductoDto>(critRes.data));
      }
      if (pelRes?.success) {
        setPeluqueros(extractPagedItems<PeluqueroDto>(pelRes.data));
      }
    } catch (err) {
      setErrorCarga({
        message: extractErrorMessage(
          err,
          'No se pudieron cargar las métricas ejecutivas del tablero.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
    } finally {
      setCargando(false);
    }
  }, [fechaDesde, fechaHasta]);

  useEffect(() => {
    void cargarDashboard();
  }, [cargarDashboard]);

  const handleSelectQuickPeriod = (period: Exclude<QuickPeriod, 'custom'>) => {
    const range = getQuickRange(period);
    setPeriodoActivo(period);
    setFechaDesde(range.desde);
    setFechaHasta(range.hasta);
  };

  const peluqueroColorMap = useMemo(() => {
    const map = new Map<number, string>();
    peluqueros.forEach((p) => {
      if (p.colorAgendaHex) {
        map.set(p.id, p.colorAgendaHex);
      }
    });
    return map;
  }, [peluqueros]);

  const defaultBrandHex = theme === 'dark' ? '#6366F1' : '#4F46E5';
  const chartAxisColor = theme === 'dark' ? '#94A3B8' : '#64748B';
  const chartGridColor = theme === 'dark' ? '#1E293B' : '#E2E8F0';

  const mediosPagoData = useMemo(() => {
    const raw = extractPagedItems(
      kpis?.ingresosPorMedioPago ?? kpis?.recaudacionPorMedioPago
    );
    return raw.map((mp) => {
      const monto = mp.montoTotal ?? mp.totalRecaudado ?? 0;
      const nombre = mp.medioPagoNombre || mp.medioPago || 'Otros';
      const { hex, tone } = getPaymentMethodColor(nombre, theme);
      return {
        ...mp,
        medioPagoNombre: nombre,
        monto,
        porcentaje: mp.porcentaje ?? 0,
        colorHex: hex,
        tone,
      };
    });
  }, [kpis, theme]);

  const rankingData = useMemo(() => {
    const raw = extractPagedItems(kpis?.rankingPeluqueros);
    return raw.map((r) => ({
      ...r,
      peluqueroNombre: r.peluqueroNombre || r.nombrePeluquero || 'Profesional',
      cantidadServicios: r.cantidadServicios ?? r.serviciosRealizados ?? 0,
      facturacionTotal: r.facturacionTotal ?? r.totalFacturado ?? 0,
      colorHex: peluqueroColorMap.get(r.peluqueroId) || defaultBrandHex,
    }));
  }, [kpis, peluqueroColorMap, defaultBrandHex]);

  const cantidadStockCritico =
    kpis?.productosEnStockCritico ??
    kpis?.productosBajoStockCritico ??
    stockCritico.length;

  const totalRecaudadoMedios = useMemo(
    () => mediosPagoData.reduce((acc, item) => acc + item.monto, 0),
    [mediosPagoData]
  );

  return (
    <div className="space-y-5 max-w-[1440px] mx-auto">
      {/* Cabecera Ejecutiva con Filtros Rápidos */}
      <section className="rounded-xl border border-border-subtle bg-surface p-4 lg:p-5 shadow-sm">
        <div className="flex flex-col xl:flex-row xl:items-center xl:justify-between gap-4">
          <div className="min-w-0">
            <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary">
              Dashboard Ejecutivo
            </h1>
            <p className="text-xs sm:text-sm text-ink-secondary mt-0.5">
              Indicadores de facturación, productividad del equipo y alertas de
              reposición del salón.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2">
            {/* Segmented control de período rápido */}
            <div
              role="group"
              aria-label="Período rápido"
              className="inline-flex items-center rounded-lg border border-border-subtle bg-elevated p-0.5"
            >
              {(
                [
                  { id: 'hoy', label: 'Hoy' },
                  { id: 'semana', label: 'Esta semana' },
                  { id: 'mes', label: 'Este mes' },
                ] as const
              ).map((opt) => {
                const active = periodoActivo === opt.id;
                return (
                  <button
                    key={opt.id}
                    type="button"
                    onClick={() => handleSelectQuickPeriod(opt.id)}
                    className={`h-8 rounded-md px-3 text-xs font-medium transition-colors whitespace-nowrap ${
                      active
                        ? 'bg-surface text-ink-primary shadow-sm ring-1 ring-border-subtle'
                        : 'text-ink-secondary hover:text-ink-primary'
                    }`}
                  >
                    {opt.label}
                  </button>
                );
              })}
            </div>

            {/* Rango personalizado compacto */}
            <div className="flex items-center gap-1.5">
              <input
                type="date"
                aria-label="Fecha desde"
                value={fechaDesde}
                onChange={(e) => {
                  setFechaDesde(e.target.value);
                  setPeriodoActivo('custom');
                }}
                className="h-9 rounded-lg border border-border-strong bg-surface px-2.5 text-xs font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              />
              <span className="text-xs text-ink-secondary">a</span>
              <input
                type="date"
                aria-label="Fecha hasta"
                value={fechaHasta}
                onChange={(e) => {
                  setFechaHasta(e.target.value);
                  setPeriodoActivo('custom');
                }}
                className="h-9 rounded-lg border border-border-strong bg-surface px-2.5 text-xs font-mono-tabular text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              />
            </div>

            <Button
              variant="primary"
              size="sm"
              isLoading={cargando && Boolean(kpis)}
              leftIcon={<RefreshCw className="h-4 w-4" aria-hidden="true" />}
              onClick={cargarDashboard}
            >
              Actualizar
            </Button>
          </div>
        </div>
      </section>

      <AsyncStateGuard
        isLoading={cargando && !kpis}
        isError={Boolean(errorCarga && !kpis)}
        errorMessage={errorCarga?.message}
        correlationId={errorCarga?.correlationId}
        loadingText="Calculando métricas ejecutivas del período..."
        onRetry={cargarDashboard}
        className="space-y-5"
      >
        {/* 6 Tarjetas KPI Ejecutivas (Sin prefijos numéricos) */}
        <section
          aria-label="Indicadores clave"
          className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4"
        >
          {/* Facturación Total */}
          <article className="rounded-xl border border-border-subtle bg-surface p-4 lg:p-5 shadow-sm flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="text-xs font-medium text-ink-secondary">
                Facturación Total
              </p>
              <p className="mt-1.5 text-2xl sm:text-3xl font-bold font-mono-tabular text-ink-primary truncate">
                {formatCurrency(kpis?.facturacionTotal || 0, moneda)}
              </p>
              <p className="mt-1.5 text-xs text-ink-secondary">
                <span className="font-mono-tabular font-semibold text-ink-primary">
                  {kpis?.cantidadVentas || 0}
                </span>{' '}
                comprobantes emitidos
              </p>
            </div>
            <div
              className={`h-10 w-10 rounded-lg border flex items-center justify-center shrink-0 ${SEMANTIC_COLORS.cash.badgeClass}`}
            >
              <DollarSign className="h-5 w-5" aria-hidden="true" />
            </div>
          </article>

          {/* Ticket Promedio */}
          <article className="rounded-xl border border-border-subtle bg-surface p-4 lg:p-5 shadow-sm flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="text-xs font-medium text-ink-secondary">
                Ticket Promedio
              </p>
              <p className="mt-1.5 text-2xl sm:text-3xl font-bold font-mono-tabular text-ink-primary truncate">
                {formatCurrency(kpis?.ticketPromedio || 0, moneda)}
              </p>
              <p className="mt-1.5 text-xs text-ink-secondary">
                Insumos en stock crítico:{' '}
                <span
                  className={`font-mono-tabular font-semibold ${
                    cantidadStockCritico > 0
                      ? SEMANTIC_COLORS.danger.textClass
                      : 'text-ink-primary'
                  }`}
                >
                  {cantidadStockCritico}
                </span>
              </p>
            </div>
            <div
              className={`h-10 w-10 rounded-lg border flex items-center justify-center shrink-0 ${SEMANTIC_COLORS.digital.badgeClass}`}
            >
              <Receipt className="h-5 w-5" aria-hidden="true" />
            </div>
          </article>

          {/* Turnos Atendidos */}
          <article className="rounded-xl border border-border-subtle bg-surface p-4 lg:p-5 shadow-sm flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="text-xs font-medium text-ink-secondary">
                Turnos Atendidos
              </p>
              <p
                className={`mt-1.5 text-2xl sm:text-3xl font-bold font-mono-tabular ${SEMANTIC_COLORS.cash.textClass}`}
              >
                {kpis?.turnosCompletados || 0}
              </p>
              <p className="mt-1.5 text-xs text-ink-secondary">
                Pendientes hoy:{' '}
                <span className="font-mono-tabular font-semibold text-ink-primary">
                  {kpis?.turnosPendientesHoy || 0}
                </span>
              </p>
            </div>
            <div
              className={`h-10 w-10 rounded-lg border flex items-center justify-center shrink-0 ${SEMANTIC_COLORS.cash.badgeClass}`}
            >
              <CalendarCheck className="h-5 w-5" aria-hidden="true" />
            </div>
          </article>

          {/* Tasa de Cancelación */}
          <article className="rounded-xl border border-border-subtle bg-surface p-4 lg:p-5 shadow-sm flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="text-xs font-medium text-ink-secondary">
                Tasa de Cancelación
              </p>
              <p
                className={`mt-1.5 text-2xl sm:text-3xl font-bold font-mono-tabular ${
                  (kpis?.tasaCancelacionPorcentaje || 0) > 15
                    ? SEMANTIC_COLORS.danger.textClass
                    : SEMANTIC_COLORS.warning.textClass
                }`}
              >
                {(kpis?.tasaCancelacionPorcentaje || 0).toFixed(1)}%
              </p>
              <p className="mt-1.5 text-xs text-ink-secondary">
                <span className="font-mono-tabular font-semibold text-ink-primary">
                  {kpis?.turnosCancelados || 0}
                </span>{' '}
                turnos cancelados en el período
              </p>
            </div>
            <div
              className={`h-10 w-10 rounded-lg border flex items-center justify-center shrink-0 ${SEMANTIC_COLORS.warning.badgeClass}`}
            >
              <AlertTriangle className="h-5 w-5" aria-hidden="true" />
            </div>
          </article>

          {/* Servicio Top */}
          <article className="rounded-xl border border-border-subtle bg-surface p-4 lg:p-5 shadow-sm flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="text-xs font-medium text-ink-secondary">
                Servicio Destacado
              </p>
              <p
                className="mt-2 text-lg font-semibold text-ink-primary truncate"
                title={kpis?.servicioMasVendido || 'Sin datos en el período'}
              >
                {kpis?.servicioMasVendido || 'Sin datos en el período'}
              </p>
              <p className="mt-1.5 text-xs text-ink-secondary">
                Mayor demanda en el rango seleccionado
              </p>
            </div>
            <div className="h-10 w-10 rounded-lg border border-indigo-200 bg-indigo-50 text-indigo-700 dark:border-indigo-800/70 dark:bg-indigo-950/60 dark:text-indigo-300 flex items-center justify-center shrink-0">
              <Scissors className="h-5 w-5" aria-hidden="true" />
            </div>
          </article>

          {/* Peluquero Estrella */}
          <article className="rounded-xl border border-border-subtle bg-surface p-4 lg:p-5 shadow-sm flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="text-xs font-medium text-ink-secondary">
                Profesional Destacado
              </p>
              <p
                className="mt-2 text-lg font-semibold text-ink-primary truncate"
                title={kpis?.peluqueroEstrella || 'Sin datos en el período'}
              >
                {kpis?.peluqueroEstrella || 'Sin datos en el período'}
              </p>
              <p className="mt-1.5 text-xs text-ink-secondary">
                Líder en facturación del período
              </p>
            </div>
            <div
              className={`h-10 w-10 rounded-lg border flex items-center justify-center shrink-0 ${SEMANTIC_COLORS.courtesy.badgeClass}`}
            >
              <Award className="h-5 w-5" aria-hidden="true" />
            </div>
          </article>
        </section>

        {/* Gráficos Recharts: Ingresos por Medio de Pago + Ranking por Estilista */}
        <section className="grid grid-cols-1 lg:grid-cols-2 gap-5">
          {/* Donut Chart + Barras Semánticas de Medios de Pago */}
          <div className="rounded-xl border border-border-subtle bg-surface p-5 shadow-sm flex flex-col gap-4 min-w-0">
            <div className="flex items-center justify-between gap-2">
              <div>
                <h2 className="text-base font-semibold text-ink-primary">
                  Ingresos por Medio de Pago
                </h2>
                <p className="text-xs text-ink-secondary">
                  Distribución por canal de cobro con colorimetría unificada.
                </p>
              </div>
              <Badge variant="neutral" className="font-mono-tabular">
                {formatCurrency(totalRecaudadoMedios, moneda)}
              </Badge>
            </div>

            {mediosPagoData.length > 0 ? (
              <>
                <div className="h-56 w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={mediosPagoData}
                        dataKey="monto"
                        nameKey="medioPagoNombre"
                        cx="50%"
                        cy="50%"
                        innerRadius={54}
                        outerRadius={82}
                        paddingAngle={2}
                        stroke="none"
                      >
                        {mediosPagoData.map((entry, idx) => (
                          <Cell
                            key={`${entry.medioPagoNombre}-${idx}`}
                            fill={entry.colorHex}
                          />
                        ))}
                      </Pie>
                      <Tooltip
                        content={<PaymentTooltip moneda={moneda} />}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                </div>

                {/* Desglose en barras horizontales con el MISMO color del Donut */}
                <div className="space-y-3 pt-1">
                  {mediosPagoData.map((mp, idx) => (
                    <div
                      key={`${mp.medioPagoNombre}-${idx}`}
                      className="space-y-1.5 text-xs"
                    >
                      <div className="flex items-center justify-between gap-2">
                        <span className="flex items-center gap-2 font-medium text-ink-primary min-w-0 truncate">
                          <span
                            className="h-2.5 w-2.5 rounded-full shrink-0"
                            style={{ backgroundColor: mp.colorHex }}
                          />
                          <span className="truncate">{mp.medioPagoNombre}</span>
                        </span>
                        <span className="font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                          {formatCurrency(mp.monto, moneda)}{' '}
                          <span className="text-ink-secondary font-normal">
                            ({mp.porcentaje.toFixed(1)}%)
                          </span>
                        </span>
                      </div>
                      <div className="w-full h-2 rounded-full bg-elevated overflow-hidden">
                        <div
                          className="h-full rounded-full transition-all duration-300"
                          style={{
                            width: `${Math.min(100, Math.max(0, mp.porcentaje))}%`,
                            backgroundColor: mp.colorHex,
                          }}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </>
            ) : (
              <div className="flex-1 flex flex-col items-center justify-center py-12 text-center text-xs text-ink-secondary">
                <TrendingUp
                  className="h-8 w-8 text-ink-secondary/40 mb-2"
                  aria-hidden="true"
                />
                <p className="font-medium text-ink-primary">
                  Sin cobros registrados en este período
                </p>
                <p className="mt-0.5">
                  Probá ampliar el rango de fechas para visualizar la
                  distribución por medio de pago.
                </p>
              </div>
            )}
          </div>

          {/* Gráfico de Barras + Tabla Ranking por Estilista */}
          <div className="rounded-xl border border-border-subtle bg-surface p-5 shadow-sm flex flex-col gap-4 min-w-0">
            <div className="flex items-center justify-between gap-2">
              <div>
                <h2 className="text-base font-semibold text-ink-primary">
                  Ranking por Estilista
                </h2>
                <p className="text-xs text-ink-secondary">
                  Facturación acumulada y servicios completados por profesional.
                </p>
              </div>
              <Badge variant="info" className="font-mono-tabular">
                {rankingData.length} prof.
              </Badge>
            </div>

            {rankingData.length > 0 ? (
              <>
                <div className="h-56 w-full">
                  <ResponsiveContainer width="100%" height="100%">
                    <BarChart
                      data={rankingData}
                      margin={{ top: 8, right: 8, left: 0, bottom: 4 }}
                    >
                      <CartesianGrid
                        strokeDasharray="3 3"
                        vertical={false}
                        stroke={chartGridColor}
                      />
                      <XAxis
                        dataKey="peluqueroNombre"
                        tick={{ fontSize: 11, fill: chartAxisColor }}
                        axisLine={{ stroke: chartGridColor }}
                        tickLine={false}
                      />
                      <YAxis
                        tick={{ fontSize: 11, fill: chartAxisColor }}
                        axisLine={false}
                        tickLine={false}
                        width={62}
                        tickFormatter={(val: number) =>
                          val >= 1000 ? `${Math.round(val / 1000)}k` : String(val)
                        }
                      />
                      <Tooltip
                        cursor={{
                          fill:
                            theme === 'dark'
                              ? 'rgba(148, 163, 184, 0.08)'
                              : 'rgba(15, 23, 42, 0.04)',
                        }}
                        content={<RankingTooltip moneda={moneda} />}
                      />
                      <Bar
                        dataKey="facturacionTotal"
                        name="Facturación"
                        radius={[6, 6, 0, 0]}
                        maxBarSize={48}
                      >
                        {rankingData.map((entry) => (
                          <Cell
                            key={`bar-${entry.peluqueroId}`}
                            fill={entry.colorHex}
                          />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                </div>

                <div className="overflow-x-auto rounded-lg border border-border-subtle">
                  <table className="w-full text-left text-xs">
                    <thead className="bg-elevated text-ink-secondary border-b border-border-subtle">
                      <tr>
                        <th className="px-3 py-2 font-medium">Profesional</th>
                        <th className="px-3 py-2 font-medium text-right">
                          Servicios
                        </th>
                        <th className="px-3 py-2 font-medium text-right">
                          Facturación
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border-subtle">
                      {rankingData.map((r) => (
                        <tr
                          key={r.peluqueroId}
                          className="hover:bg-elevated/60 transition-colors"
                        >
                          <td className="px-3 py-2 font-medium text-ink-primary">
                            <span className="inline-flex items-center gap-2">
                              <span
                                className="h-2.5 w-2.5 rounded-full shrink-0"
                                style={{ backgroundColor: r.colorHex }}
                              />
                              <span>{r.peluqueroNombre}</span>
                            </span>
                          </td>
                          <td className="px-3 py-2 text-right font-mono-tabular text-ink-secondary">
                            {r.cantidadServicios}
                          </td>
                          <td className="px-3 py-2 text-right font-mono-tabular font-semibold text-ink-primary">
                            {formatCurrency(r.facturacionTotal, moneda)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            ) : (
              <div className="flex-1 flex flex-col items-center justify-center py-12 text-center text-xs text-ink-secondary">
                <Award
                  className="h-8 w-8 text-ink-secondary/40 mb-2"
                  aria-hidden="true"
                />
                <p className="font-medium text-ink-primary">
                  Sin ranking disponible en este período
                </p>
                <p className="mt-0.5">
                  Los datos aparecerán al registrar ventas y servicios
                  completados.
                </p>
              </div>
            )}
          </div>
        </section>

        {/* Alerta de Stock Crítico con acceso directo a Inventario */}
        {stockCritico.length > 0 && (
          <section
            aria-label="Alerta de reposición"
            className={`rounded-xl border p-4 lg:p-5 flex flex-col md:flex-row md:items-center justify-between gap-4 ${SEMANTIC_COLORS.danger.surfaceClass}`}
          >
            <div className="flex items-start gap-3 min-w-0">
              <div
                className={`h-9 w-9 rounded-lg border flex items-center justify-center shrink-0 mt-0.5 ${SEMANTIC_COLORS.danger.badgeClass}`}
              >
                <PackageX className="h-4 w-4" aria-hidden="true" />
              </div>
              <div className="min-w-0">
                <h3
                  className={`text-sm font-semibold ${SEMANTIC_COLORS.danger.textClass}`}
                >
                  Alerta de reposición: {stockCritico.length} artículo(s) con
                  stock bajo
                </h3>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  {stockCritico.slice(0, 6).map((p) => (
                    <Badge
                      key={p.id}
                      variant="danger"
                      className="font-mono-tabular"
                    >
                      {p.nombre} ({p.stockActual} u.)
                    </Badge>
                  ))}
                  {stockCritico.length > 6 && (
                    <Badge variant="neutral">
                      +{stockCritico.length - 6} más
                    </Badge>
                  )}
                </div>
              </div>
            </div>

            <Link
              href="/admin/inventario"
              className={`inline-flex items-center justify-center gap-2 h-9 px-4 rounded-lg text-xs font-medium transition-colors shrink-0 ${SEMANTIC_COLORS.danger.solidButtonClass}`}
            >
              <span>Emitir Orden de Compra</span>
              <ArrowRight className="h-3.5 w-3.5" aria-hidden="true" />
            </Link>
          </section>
        )}
      </AsyncStateGuard>
    </div>
  );
}
