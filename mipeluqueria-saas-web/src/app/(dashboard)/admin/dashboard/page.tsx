// filepath: src/app/(dashboard)/admin/dashboard/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
import Link from 'next/link';
import {
  Bar,
  BarChart,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { DashboardKpisDto, ProductoDto } from '@/types/api.types';
import { dashboardService, productosService } from '@/services/api.services';
import { useCatalogos } from '@/context/AppProviders';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import { formatCurrency } from '@/lib/formatters';
import { AsyncStateGuard, Button } from '@/components/ui';

const CHART_COLORS = [
  '#0F172A',
  '#78350F',
  '#065F46',
  '#1E40AF',
  '#9A3412',
  '#475569',
];

export default function AdminDashboardBiPage() {
  const { configSalon } = useCatalogos();
  const moneda = configSalon?.moneda || 'ARS';

  const [fechaDesde, setFechaDesde] = useState<string>(() => {
    const d = new Date();
    d.setDate(1);
    return d.toISOString().split('T')[0];
  });
  const [fechaHasta, setFechaHasta] = useState<string>(
    () => new Date().toISOString().split('T')[0]
  );

  const [kpis, setKpis] = useState<DashboardKpisDto | null>(null);
  const [stockCritico, setStockCritico] = useState<ProductoDto[]>([]);
  const [cargando, setCargando] = useState<boolean>(true);
  const [errorCarga, setErrorCarga] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);

  const cargarDashboard = useCallback(async () => {
    setCargando(true);
    setErrorCarga(null);
    try {
      const [kpiRes, critRes] = await Promise.all([
        dashboardService.getKpis(fechaDesde, fechaHasta),
        productosService.getStockCritico(),
      ]);
      if (kpiRes.success && kpiRes.data) setKpis(kpiRes.data);
      if (critRes.success) {
        setStockCritico(extractPagedItems<ProductoDto>(critRes.data));
      }
    } catch (err) {
      setErrorCarga({
        message: extractErrorMessage(
          err,
          'No se pudieron cargar las métricas ejecutivas del Dashboard.'
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

  const mediosPagoData = extractPagedItems(kpis?.ingresosPorMedioPago);
  const rankingData = extractPagedItems(kpis?.rankingPeluqueros);
  const cantidadStockCritico =
    kpis?.productosEnStockCritico ??
    kpis?.productosBajoStockCritico ??
    (stockCritico ?? []).length;

  return (
    <div className="min-h-screen bg-slate-50 p-6 space-y-6">
      <header className="bg-white border border-slate-200 rounded-lg p-5 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900">
            Dashboard Ejecutivo (Business Intelligence)
          </h1>
          <p className="text-xs text-slate-500">
            Métricas consolidadas en tiempo real por rango de fechas y alertas de
            reposición.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <input
            type="date"
            value={fechaDesde}
            onChange={(e) => setFechaDesde(e.target.value)}
            className="rounded border border-slate-300 px-3 py-1.5 text-xs"
          />
          <span className="text-xs text-slate-400">a</span>
          <input
            type="date"
            value={fechaHasta}
            onChange={(e) => setFechaHasta(e.target.value)}
            className="rounded border border-slate-300 px-3 py-1.5 text-xs"
          />
          <Button variant="primary" size="sm" onClick={cargarDashboard}>
            Actualizar KPIs
          </Button>
        </div>
      </header>

      {/* 6 KPI Cards Ejecutivas */}
      <AsyncStateGuard
        isLoading={cargando && !kpis}
        isError={Boolean(errorCarga && !kpis)}
        errorMessage={errorCarga?.message}
        correlationId={errorCarga?.correlationId}
        loadingText="Calculando métricas ejecutivas en tiempo real..."
        onRetry={cargarDashboard}
        className="space-y-6"
      >
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <div className="bg-white border border-slate-200 rounded-lg p-5">
          <span className="text-xs font-bold uppercase text-slate-500">
            1. Facturación Total
          </span>
          <p className="text-2xl font-mono font-bold text-slate-900 mt-1">
            {formatCurrency(kpis?.facturacionTotal || 0, moneda)}
          </p>
          <p className="text-xs text-slate-500 mt-1">
            {kpis?.cantidadVentas || 0} tickets emitidos
          </p>
        </div>

        <div className="bg-white border border-slate-200 rounded-lg p-5">
          <span className="text-xs font-bold uppercase text-slate-500">
            2. Ticket Promedio
          </span>
          <p className="text-2xl font-mono font-bold text-slate-900 mt-1">
            {formatCurrency(kpis?.ticketPromedio || 0, moneda)}
          </p>
          <p className="text-xs text-slate-500 mt-1">
            Insumos en Stock Crítico: {cantidadStockCritico}
          </p>
        </div>

        <div className="bg-white border border-slate-200 rounded-lg p-5">
          <span className="text-xs font-bold uppercase text-slate-500">
            3. Turnos Atendidos
          </span>
          <p className="text-2xl font-mono font-bold text-emerald-800 mt-1">
            {kpis?.turnosCompletados || 0}
          </p>
          <p className="text-xs text-slate-500 mt-1">
            Pendientes hoy: {kpis?.turnosPendientesHoy || 0}
          </p>
        </div>

        <div className="bg-white border border-slate-200 rounded-lg p-5">
          <span className="text-xs font-bold uppercase text-slate-500">
            4. Tasa de Cancelación
          </span>
          <p className="text-2xl font-mono font-bold text-amber-800 mt-1">
            {(kpis?.tasaCancelacionPorcentaje || 0).toFixed(1)}%
          </p>
        </div>

        <div className="bg-white border border-slate-200 rounded-lg p-5">
          <span className="text-xs font-bold uppercase text-slate-500">
            5. Servicio Top
          </span>
          <p className="text-lg font-bold text-slate-900 mt-1">
            {kpis?.servicioMasVendido || 'Sin datos en el período'}
          </p>
        </div>

        <div className="bg-white border border-slate-200 rounded-lg p-5">
          <span className="text-xs font-bold uppercase text-slate-500">
            6. Peluquero Estrella
          </span>
          <p className="text-lg font-bold text-slate-900 mt-1">
            {kpis?.peluqueroEstrella || 'Sin datos en el período'}
          </p>
        </div>
      </div>

      {/* Gráficos Recharts: Medio de Pago y Ranking de Estilistas */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white border border-slate-200 rounded-lg p-5 space-y-4">
          <h3 className="text-sm font-bold uppercase text-slate-700">
            Ingresos Desglosados por Medio de Pago
          </h3>

          {mediosPagoData.length > 0 && (
            <div className="h-56 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={mediosPagoData}
                    dataKey="totalRecaudado"
                    nameKey="medioPagoNombre"
                    cx="50%"
                    cy="50%"
                    outerRadius={75}
                    label={({ name, percent }) =>
                      `${name} (${((percent || 0) * 100).toFixed(0)}%)`
                    }
                  >
                    {mediosPagoData.map((_, idx) => (
                      <Cell
                        key={`cell-${idx}`}
                        fill={CHART_COLORS[idx % CHART_COLORS.length]}
                      />
                    ))}
                  </Pie>
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value, moneda)}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
          )}

          <div className="space-y-2">
            {mediosPagoData.map((mp, idx) => (
              <div key={idx} className="space-y-1 text-xs">
                <div className="flex justify-between font-semibold">
                  <span>{mp.medioPagoNombre}</span>
                  <span className="font-mono">
                    {formatCurrency(mp.montoTotal, moneda)} (
                    {mp.porcentaje.toFixed(1)}%)
                  </span>
                </div>
                <div className="w-full h-2 bg-slate-100 rounded overflow-hidden">
                  <div
                    className="h-full bg-slate-900"
                    style={{ width: `${Math.min(100, mp.porcentaje)}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-white border border-slate-200 rounded-lg p-5 space-y-4">
          <h3 className="text-sm font-bold uppercase text-slate-700">
            Ranking Comparativo por Estilista
          </h3>

          {rankingData.length > 0 && (
            <div className="h-56 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={rankingData}>
                  <XAxis dataKey="peluqueroNombre" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value, moneda)}
                  />
                  <Bar
                    dataKey="facturacionTotal"
                    name="Facturación"
                    fill="#78350F"
                    radius={[4, 4, 0, 0]}
                  />
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}

          <table className="w-full text-left text-xs">
            <thead className="bg-slate-50 text-slate-600">
              <tr>
                <th className="p-2">Profesional</th>
                <th className="p-2">Servicios</th>
                <th className="p-2 text-right">Facturación</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {rankingData.map((r) => (
                <tr key={r.peluqueroId}>
                  <td className="p-2 font-bold">{r.peluqueroNombre}</td>
                  <td className="p-2 font-mono">{r.cantidadServicios}</td>
                  <td className="p-2 text-right font-mono font-bold">
                    {formatCurrency(r.facturacionTotal, moneda)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Widget de Alerta Roja: Productos en Stock Crítico */}
      {stockCritico.length > 0 && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-5 flex items-center justify-between">
          <div>
            <h3 className="text-sm font-bold text-red-900">
              ▲ Alerta Roja: {stockCritico.length} producto(s) en Stock Crítico
              (StockActual ≤ StockMinimoAlerta)
            </h3>
            <p className="text-xs text-red-700 mt-1">
              {stockCritico
                .map((p) => `${p.nombre} (${p.stockActual} u.)`)
                .join(' • ')}
            </p>
          </div>
          <Link
            href="/admin/inventario"
            className="px-4 py-2 rounded bg-red-800 text-white text-xs font-semibold hover:bg-red-900 shrink-0"
          >
            Emitir Orden de Compra
          </Link>
        </div>
      )}
      </AsyncStateGuard>
    </div>
  );
}
