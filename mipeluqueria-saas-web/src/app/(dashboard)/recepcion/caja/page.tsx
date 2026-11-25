// filepath: src/app/(dashboard)/recepcion/caja/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
import {
  ArrowDownCircle,
  ArrowUpCircle,
  Banknote,
  CreditCard,
  Eye,
  FileText,
  History,
  Lock,
  Unlock,
} from 'lucide-react';
import { SesionCajaDto } from '@/types/api.types';
import { cajaService } from '@/services/api.services';
import { useCajaPos, useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
  extractPaginationMeta,
} from '@/lib/apiHelpers';
import {
  formatCurrency,
  formatDateOnly,
  formatDateTime,
} from '@/lib/formatters';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import {
  AsyncStateGuard,
  Badge,
  Button,
  InputField,
  Modal,
  NumericInput,
  PageHeader,
  PaginationBar,
} from '@/components/ui';
import {
  PrintableReceiptData,
  PrintReceiptModal,
} from '@/components/ui/PrintReceiptModal';

export default function ControlCajaDiariaPage() {
  const { sesionCajaActual, cajaAbierta, refrescarEstadoCaja } = useCajaPos();
  const { configSalon } = useCatalogos();
  const moneda = configSalon?.moneda || 'ARS';

  // Estados de Apertura, Movimientos Manuales y Cierre (NumericInput sin 0 pegajoso)
  const [montoInicial, setMontoInicial] = useState<number | null>(15000);
  const [montoMovimiento, setMontoMovimiento] = useState<number | null>(null);
  const [conceptoMovimiento, setConceptoMovimiento] = useState<string>('');
  const [tipoEgreso, setTipoEgreso] = useState<string>('EgresoInsumoMenor');
  const [montoFisicoCierre, setMontoFisicoCierre] = useState<number | null>(null);
  const [observacionesCierre, setObservacionesCierre] = useState<string>('');
  const [procesando, setProcesando] = useState<boolean>(false);

  // Historial Paginado y Detalle Bajo Demanda (GET /api/caja/{id})
  const [historialCajas, setHistorialCajas] = useState<SesionCajaDto[]>([]);
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [totalHistorial, setTotalHistorial] = useState<number>(0);
  const [cargandoHistorial, setCargandoHistorial] = useState<boolean>(true);
  const [errorHistorial, setErrorHistorial] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);
  const [sesionInspeccionada, setSesionInspeccionada] =
    useState<SesionCajaDto | null>(null);
  const [actaImprimible, setActaImprimible] =
    useState<PrintableReceiptData | null>(null);

  const cargarHistorial = useCallback(async () => {
    setCargandoHistorial(true);
    setErrorHistorial(null);
    try {
      const res = await cajaService.getHistorialPaged({
        pageNumber,
        pageSize: 10,
      });
      if (res.success) {
        const items = extractPagedItems<SesionCajaDto>(res.data);
        const meta = extractPaginationMeta(res.data, {
          pageNumber,
          pageSize: 10,
        });
        setHistorialCajas(items);
        setTotalPages(meta.totalPages);
        setTotalHistorial(meta.totalCount);
      }
    } catch (err) {
      setHistorialCajas([]);
      setErrorHistorial({
        message: extractErrorMessage(
          err,
          'No se pudo cargar el historial de sesiones de caja.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
    } finally {
      setCargandoHistorial(false);
    }
  }, [pageNumber]);

  useEffect(() => {
    void cargarHistorial();
  }, [cargarHistorial]);

  const handleAbrirCaja = async (e: React.FormEvent) => {
    e.preventDefault();
    const valorInicial = montoInicial ?? 0;
    if (valorInicial < 0) return;

    setProcesando(true);
    try {
      const res = await cajaService.abrir({ montoInicial: valorInicial });
      if (res.success) {
        notifyApiSuccess(res, 'Sesión de caja abierta correctamente.');
        await Promise.all([refrescarEstadoCaja(), cargarHistorial()]);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo abrir la sesión de caja.',
      });
    } finally {
      setProcesando(false);
    }
  };

  const handleMovimiento = async (modo: 'ingreso' | 'egreso') => {
    const importe = montoMovimiento ?? 0;
    if (!sesionCajaActual || importe <= 0 || !conceptoMovimiento.trim()) {
      return;
    }

    setProcesando(true);
    try {
      const payload = {
        sesionCajaId: sesionCajaActual.id,
        monto: importe,
        concepto: conceptoMovimiento.trim(),
        tipo: modo === 'ingreso' ? 'IngresoManual' : tipoEgreso,
      };

      const res =
        modo === 'ingreso'
          ? await cajaService.registrarIngreso(payload)
          : await cajaService.registrarEgreso(payload);

      if (res.success) {
        notifyApiSuccess(
          res,
          `Movimiento manual de ${modo} registrado en la caja #${sesionCajaActual.id}.`
        );
        setMontoMovimiento(null);
        setConceptoMovimiento('');
        await Promise.all([refrescarEstadoCaja(), cargarHistorial()]);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'Error al registrar el movimiento manual en caja.',
      });
    } finally {
      setProcesando(false);
    }
  };

  const armarActaArqueo = (sesion: SesionCajaDto): PrintableReceiptData => ({
    titulo: 'Acta de Arqueo de Caja',
    numeroComprobante: String(sesion.id).padStart(5, '0'),
    fechaIso: sesion.fechaCierre || sesion.fechaApertura,
    clienteOProfesional:
      sesion.usuarioCierreNombre ||
      sesion.usuarioAperturaNombre ||
      `Cajero #${sesion.usuarioAperturaId}`,
    monedaIso: moneda,
    renglones: [
      { descripcion: 'Fondo fijo inicial', monto: sesion.montoInicial },
      {
        descripcion: 'Ventas en efectivo',
        monto: sesion.totalVentasEfectivo,
      },
      {
        descripcion: 'Ingresos manuales',
        monto: sesion.totalIngresosManuales,
      },
      {
        descripcion: 'Egresos y retiros de caja',
        monto: -sesion.totalEgresosManuales,
      },
    ],
    pagosODesglose: [
      {
        etiqueta: 'Efectivo esperado en cajón',
        monto: sesion.montoFinalEsperado,
      },
      {
        etiqueta: 'Efectivo físico contado',
        monto: sesion.montoFinalReal ?? 0,
      },
      {
        etiqueta: 'Diferencia de arqueo',
        monto: sesion.diferencia ?? 0,
      },
      {
        etiqueta: 'Recaudación digital acumulada (informativo)',
        monto: sesion.totalVentasDigital,
      },
    ],
    totalFinal: sesion.montoFinalReal ?? sesion.montoFinalEsperado,
    pieDePagina: sesion.observacionesCierre
      ? `Observaciones: ${sesion.observacionesCierre}`
      : 'Acta de arqueo diario emitida por el sistema',
  });

  const handleCerrarCaja = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!sesionCajaActual || montoFisicoCierre === null) return;

    setProcesando(true);
    try {
      const res = await cajaService.cerrar({
        sesionCajaId: sesionCajaActual.id,
        montoFinalReal: montoFisicoCierre,
        observacionesCierre: observacionesCierre.trim() || undefined,
      });

      if (res.success && res.data) {
        notifyApiSuccess(
          res,
          `Caja #${res.data.id} cerrada. Diferencia: ${formatCurrency(
            res.data.diferencia || 0,
            moneda
          )}`
        );
        setActaImprimible(armarActaArqueo(res.data));
        setMontoFisicoCierre(null);
        setObservacionesCierre('');
        await Promise.all([refrescarEstadoCaja(), cargarHistorial()]);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo cerrar la sesión de caja.',
      });
    } finally {
      setProcesando(false);
    }
  };

  const handleInspeccionarSesionHistorica = async (id: number) => {
    try {
      const res = await cajaService.getById(id);
      if (res.success && res.data) {
        setSesionInspeccionada(res.data);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: `No se pudo obtener el detalle de la sesión #${id}.`,
      });
    }
  };

  const cajaBadgeLabel =
    cajaAbierta && sesionCajaActual
      ? `Caja del ${formatDateOnly(sesionCajaActual.fechaApertura)} (#${sesionCajaActual.id})`
      : 'Caja cerrada';

  return (
    <div className="flex-1 flex flex-col min-h-0 min-w-0">
      <PageHeader
        title="Caja Diaria"
        subtitle="Control de efectivo en cajón, movimientos manuales y cierre de turno."
        actions={
          <Badge variant={cajaAbierta ? 'success' : 'danger'}>
            <span
              className={`h-1.5 w-1.5 rounded-full ${
                cajaAbierta
                  ? SEMANTIC_COLORS.cash.dotClass
                  : SEMANTIC_COLORS.danger.dotClass
              }`}
              aria-hidden="true"
            />
            <span className="font-mono-tabular">{cajaBadgeLabel}</span>
          </Badge>
        }
      />

      <div className="p-4 lg:p-5 space-y-5 max-w-[1440px] w-full mx-auto min-w-0">
        {/* Dos Tarjetas KPI con colores semánticos fijos: Verde (Efectivo) y Azul (Digital) */}
        {cajaAbierta && sesionCajaActual && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* KPI 1: Efectivo en Cajón (Esmeralda) */}
            <div
              className={`rounded-xl border p-5 space-y-3 shadow-sm ${SEMANTIC_COLORS.cash.surfaceClass}`}
            >
              <div className="flex items-center justify-between gap-2">
                <div className="flex items-center gap-2 min-w-0">
                  <div className="h-8 w-8 rounded-lg bg-emerald-600/15 dark:bg-emerald-500/20 flex items-center justify-center shrink-0">
                    <Banknote
                      className={`h-4 w-4 ${SEMANTIC_COLORS.cash.textClass}`}
                      aria-hidden="true"
                    />
                  </div>
                  <span
                    className={`text-sm font-semibold truncate ${SEMANTIC_COLORS.cash.textClass}`}
                  >
                    Efectivo en Cajón
                  </span>
                </div>
                <Badge variant="success">Billetes físicos</Badge>
              </div>

              <p className="text-2xl sm:text-3xl font-bold font-mono-tabular text-ink-primary">
                {formatCurrency(sesionCajaActual.montoFinalEsperado, moneda)}
              </p>

              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 pt-3 border-t border-emerald-200/70 dark:border-emerald-800/60 text-xs">
                <div>
                  <span className="block text-ink-secondary">Fondo inicial</span>
                  <strong className="font-mono-tabular font-semibold text-ink-primary">
                    {formatCurrency(sesionCajaActual.montoInicial, moneda)}
                  </strong>
                </div>
                <div>
                  <span className="block text-ink-secondary">+ Ventas efvo.</span>
                  <strong
                    className={`font-mono-tabular font-semibold ${SEMANTIC_COLORS.cash.textClass}`}
                  >
                    {formatCurrency(
                      sesionCajaActual.totalVentasEfectivo,
                      moneda
                    )}
                  </strong>
                </div>
                <div>
                  <span className="block text-ink-secondary">+ Ingresos</span>
                  <strong
                    className={`font-mono-tabular font-semibold ${SEMANTIC_COLORS.cash.textClass}`}
                  >
                    {formatCurrency(
                      sesionCajaActual.totalIngresosManuales,
                      moneda
                    )}
                  </strong>
                </div>
                <div>
                  <span className="block text-ink-secondary">- Egresos</span>
                  <strong
                    className={`font-mono-tabular font-semibold ${SEMANTIC_COLORS.danger.textClass}`}
                  >
                    -{formatCurrency(
                      sesionCajaActual.totalEgresosManuales,
                      moneda
                    )}
                  </strong>
                </div>
              </div>
            </div>

            {/* KPI 2: Recaudación Digital (Azul Cielo) */}
            <div
              className={`rounded-xl border p-5 space-y-3 shadow-sm flex flex-col justify-between ${SEMANTIC_COLORS.digital.surfaceClass}`}
            >
              <div className="space-y-3">
                <div className="flex items-center justify-between gap-2">
                  <div className="flex items-center gap-2 min-w-0">
                    <div className="h-8 w-8 rounded-lg bg-sky-600/15 dark:bg-sky-500/20 flex items-center justify-center shrink-0">
                      <CreditCard
                        className={`h-4 w-4 ${SEMANTIC_COLORS.digital.textClass}`}
                        aria-hidden="true"
                      />
                    </div>
                    <span
                      className={`text-sm font-semibold truncate ${SEMANTIC_COLORS.digital.textClass}`}
                    >
                      Recaudación Digital
                    </span>
                  </div>
                  <Badge variant="info">Tarjetas · QR · Transferencias</Badge>
                </div>

                <p className="text-2xl sm:text-3xl font-bold font-mono-tabular text-ink-primary">
                  {formatCurrency(sesionCajaActual.totalVentasDigital, moneda)}
                </p>
              </div>

              <p className="text-xs text-ink-secondary pt-3 border-t border-sky-200/70 dark:border-sky-800/60">
                Cobros acreditados en cuentas bancarias o billeteras digitales. No
                se incluyen en el recuento físico de billetes del cajón.
              </p>
            </div>
          </div>
        )}

        {/* Operativa: Apertura o Movimientos + Cierre */}
        {!cajaAbierta ? (
          <form
            onSubmit={handleAbrirCaja}
            className="max-w-md bg-surface border border-border-subtle rounded-xl p-5 space-y-4 shadow-sm"
          >
            <div className="flex items-center gap-2">
              <Unlock
                className="h-4 w-4 text-brand-primary"
                aria-hidden="true"
              />
              <h2 className="text-base font-semibold text-ink-primary">
                Apertura de turno de caja
              </h2>
            </div>
            <NumericInput
              id="caja-fondo-inicial"
              label="Fondo fijo inicial en efectivo *"
              prefix="$"
              value={montoInicial}
              onValueChange={setMontoInicial}
              placeholder="0"
              required
            />
            <Button
              type="submit"
              variant="primary"
              className="w-full"
              isLoading={procesando}
            >
              Abrir sesión de caja
            </Button>
          </form>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
            {/* Tarjeta Ingresos / Egresos manuales */}
            <div className="bg-surface border border-border-subtle rounded-xl p-5 space-y-4 shadow-sm">
              <h2 className="text-base font-semibold text-ink-primary">
                Registrar ingreso o egreso manual
              </h2>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <NumericInput
                  id="caja-monto-movimiento"
                  label="Importe en efectivo *"
                  prefix="$"
                  value={montoMovimiento}
                  onValueChange={setMontoMovimiento}
                  placeholder="0"
                />
                <div className="flex flex-col gap-1.5">
                  <label
                    htmlFor="caja-tipo-egreso"
                    className="text-xs font-medium text-ink-secondary"
                  >
                    Categoría (para egresos)
                  </label>
                  <select
                    id="caja-tipo-egreso"
                    value={tipoEgreso}
                    onChange={(e) => setTipoEgreso(e.target.value)}
                    className="h-10 w-full rounded-lg border border-border-strong bg-surface text-ink-primary px-3 text-sm focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                  >
                    <option value="EgresoInsumoMenor">Insumo menor</option>
                    <option value="RetiroDuenio">Retiro de alivio / Dueño</option>
                    <option value="PagoDelivery">Cadetería / Delivery</option>
                  </select>
                </div>
              </div>

              <InputField
                label="Concepto o comprobante *"
                value={conceptoMovimiento}
                onChange={(e) => setConceptoMovimiento(e.target.value)}
                placeholder="Ej. Pago cadetería, retiro parcial o cambio de billetes"
              />

              <div className="flex flex-col sm:flex-row gap-2.5 pt-1">
                <Button
                  type="button"
                  variant="secondary"
                  className="flex-1"
                  leftIcon={
                    <ArrowUpCircle
                      className={`h-4 w-4 ${SEMANTIC_COLORS.cash.textClass}`}
                      aria-hidden="true"
                    />
                  }
                  disabled={
                    procesando ||
                    (montoMovimiento ?? 0) <= 0 ||
                    !conceptoMovimiento.trim()
                  }
                  isLoading={procesando}
                  onClick={() => handleMovimiento('ingreso')}
                >
                  Registrar ingreso
                </Button>
                <Button
                  type="button"
                  variant="destructive"
                  destructiveTone="solid"
                  className="flex-1"
                  leftIcon={
                    <ArrowDownCircle className="h-4 w-4" aria-hidden="true" />
                  }
                  disabled={
                    procesando ||
                    (montoMovimiento ?? 0) <= 0 ||
                    !conceptoMovimiento.trim()
                  }
                  isLoading={procesando}
                  onClick={() => handleMovimiento('egreso')}
                >
                  Registrar egreso
                </Button>
              </div>
            </div>

            {/* Tarjeta Cierre y Arqueo */}
            <form
              onSubmit={handleCerrarCaja}
              className="bg-surface border border-border-subtle rounded-xl p-5 space-y-4 shadow-sm flex flex-col justify-between"
            >
              <div className="space-y-4">
                <div className="flex items-center gap-2">
                  <Lock
                    className="h-4 w-4 text-ink-secondary"
                    aria-hidden="true"
                  />
                  <h2 className="text-base font-semibold text-ink-primary">
                    Cierre y arqueo físico de billetes
                  </h2>
                </div>

                <NumericInput
                  id="caja-monto-contado"
                  label="Efectivo físico contado en cajón *"
                  prefix="$"
                  value={montoFisicoCierre}
                  onValueChange={setMontoFisicoCierre}
                  placeholder="0"
                  required
                />

                <InputField
                  label="Observaciones de cierre (opcional)"
                  value={observacionesCierre}
                  onChange={(e) => setObservacionesCierre(e.target.value)}
                  placeholder="Ej. Arqueo conforme sin diferencias."
                />
              </div>

              <Button
                type="submit"
                variant="primary"
                className="w-full"
                disabled={montoFisicoCierre === null}
                isLoading={procesando}
              >
                Ejecutar arqueo y cerrar caja #{sesionCajaActual?.id}
              </Button>
            </form>
          </div>
        )}

        {/* Movimientos de la Sesión Abierta Actual */}
        {cajaAbierta &&
          sesionCajaActual &&
          sesionCajaActual.movimientos &&
          sesionCajaActual.movimientos.length > 0 && (
            <section className="bg-surface border border-border-subtle rounded-xl overflow-hidden shadow-sm">
              <div className="px-4 py-3 border-b border-border-subtle flex items-center justify-between">
                <h3 className="text-sm font-semibold text-ink-primary">
                  Movimientos de la sesión actual (#{sesionCajaActual.id})
                </h3>
                <Badge variant="neutral">
                  <span className="font-mono-tabular">
                    {sesionCajaActual.movimientos.length}
                  </span>
                </Badge>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse text-xs">
                  <thead>
                    <tr className="bg-elevated/80 border-b border-border-subtle text-ink-secondary">
                      <th className="py-2.5 px-4 font-medium">Fecha y hora</th>
                      <th className="py-2.5 px-4 font-medium">Tipo</th>
                      <th className="py-2.5 px-4 font-medium">Concepto</th>
                      <th className="py-2.5 px-4 font-medium text-right">
                        Importe
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {sesionCajaActual.movimientos.map((m) => (
                      <tr
                        key={m.id}
                        className="hover:bg-elevated/50 transition-colors"
                      >
                        <td className="py-2.5 px-4 font-mono-tabular text-ink-secondary whitespace-nowrap">
                          {formatDateTime(m.fecha)}
                        </td>
                        <td className="py-2.5 px-4 font-medium text-ink-primary whitespace-nowrap">
                          {m.tipo}
                        </td>
                        <td className="py-2.5 px-4 text-ink-primary">
                          {m.concepto}
                        </td>
                        <td className="py-2.5 px-4 text-right font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                          {formatCurrency(m.monto, moneda)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}

        {/* Historial Paginado de Sesiones (sin scroll interno atrapado) */}
        <section className="space-y-3">
          <AsyncStateGuard
            isLoading={cargandoHistorial}
            isError={Boolean(errorHistorial)}
            errorMessage={errorHistorial?.message}
            correlationId={errorHistorial?.correlationId}
            isEmpty={(historialCajas ?? []).length === 0}
            loadingText="Cargando historial de sesiones de caja..."
            emptyTitle="Sin sesiones históricas registradas"
            emptyDescription="Aún no se registraron aperturas ni cierres de caja en esta sucursal."
            onRetry={cargarHistorial}
          >
            <div className="bg-surface border border-border-subtle rounded-xl overflow-hidden shadow-sm">
              <div className="px-4 py-3.5 border-b border-border-subtle flex items-center justify-between gap-2">
                <div className="flex items-center gap-2">
                  <History
                    className="h-4 w-4 text-ink-secondary"
                    aria-hidden="true"
                  />
                  <h2 className="text-sm font-semibold text-ink-primary">
                    Historial de sesiones y arqueos
                  </h2>
                </div>
                <span className="text-xs text-ink-secondary font-mono-tabular">
                  {totalHistorial} sesiones
                </span>
              </div>

              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse text-xs">
                  <thead>
                    <tr className="bg-elevated/80 border-b border-border-subtle text-ink-secondary">
                      <th className="py-3 px-4 font-medium">Sesión</th>
                      <th className="py-3 px-3 font-medium">Horario</th>
                      <th className="py-3 px-3 font-medium text-right">
                        Fondo inicial
                      </th>
                      <th className="py-3 px-3 font-medium text-right">
                        Efectivo esp.
                      </th>
                      <th className="py-3 px-3 font-medium text-right">
                        Real contado
                      </th>
                      <th className="py-3 px-3 font-medium text-right">
                        Digital
                      </th>
                      <th className="py-3 px-3 font-medium text-right">
                        Diferencia
                      </th>
                      <th className="py-3 px-4 font-medium text-right">
                        Acciones
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {(historialCajas ?? []).map((c) => {
                      const esAbierta = c.estadoAbierta || c.fechaCierre == null;
                      return (
                        <tr
                          key={c.id}
                          className="hover:bg-elevated/50 transition-colors"
                        >
                          <td className="py-3 px-4 whitespace-nowrap">
                            <span className="font-mono-tabular font-semibold text-ink-primary">
                              Sesión #{c.id} — {formatDateOnly(c.fechaApertura)}
                            </span>
                          </td>
                          <td className="py-3 px-3 text-ink-secondary whitespace-nowrap">
                            <p className="font-mono-tabular">
                              {formatDateTime(c.fechaApertura)}
                            </p>
                            {c.fechaCierre && (
                              <p className="text-[11px] text-ink-secondary/80 font-mono-tabular">
                                Cierre: {formatDateTime(c.fechaCierre)}
                              </p>
                            )}
                          </td>
                          <td className="py-3 px-3 text-right font-mono-tabular text-ink-primary whitespace-nowrap">
                            {formatCurrency(c.montoInicial, moneda)}
                          </td>
                          <td className="py-3 px-3 text-right font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                            {c.montoFinalEsperado != null
                              ? formatCurrency(c.montoFinalEsperado, moneda)
                              : '—'}
                          </td>
                          <td className="py-3 px-3 text-right font-mono-tabular text-ink-primary whitespace-nowrap">
                            {c.montoFinalReal != null
                              ? formatCurrency(c.montoFinalReal, moneda)
                              : '—'}
                          </td>
                          <td
                            className={`py-3 px-3 text-right font-mono-tabular font-medium whitespace-nowrap ${SEMANTIC_COLORS.digital.textClass}`}
                          >
                            {formatCurrency(c.totalVentasDigital, moneda)}
                          </td>
                          <td className="py-3 px-3 text-right whitespace-nowrap">
                            {esAbierta || c.diferencia == null ? (
                              <Badge variant="success">Abierta</Badge>
                            ) : (
                              <span
                                className={`font-mono-tabular font-semibold ${
                                  c.diferencia < 0
                                    ? SEMANTIC_COLORS.danger.textClass
                                    : SEMANTIC_COLORS.cash.textClass
                                }`}
                              >
                                {formatCurrency(c.diferencia, moneda)}
                              </span>
                            )}
                          </td>
                          <td className="py-3 px-4 text-right whitespace-nowrap">
                            <div className="inline-flex items-center gap-1.5">
                              <Button
                                size="sm"
                                variant="secondary"
                                leftIcon={
                                  <Eye className="h-3.5 w-3.5" aria-hidden="true" />
                                }
                                onClick={() =>
                                  handleInspeccionarSesionHistorica(c.id)
                                }
                              >
                                Detalle
                              </Button>
                              <Button
                                size="sm"
                                variant="secondary"
                                leftIcon={
                                  <FileText
                                    className="h-3.5 w-3.5"
                                    aria-hidden="true"
                                  />
                                }
                                onClick={() =>
                                  setActaImprimible(armarActaArqueo(c))
                                }
                              >
                                Acta PDF
                              </Button>
                            </div>
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          </AsyncStateGuard>

          <PaginationBar
            pageNumber={pageNumber}
            totalPages={totalPages}
            totalCount={totalHistorial}
            itemLabel="sesiones"
            isLoading={cargandoHistorial}
            onPageChange={setPageNumber}
          />
        </section>
      </div>

      {/* Modal de Inspección de Sesión Histórica */}
      <Modal
        isOpen={Boolean(sesionInspeccionada)}
        onClose={() => setSesionInspeccionada(null)}
        title={`Sesión #${sesionInspeccionada?.id ?? ''} — ${formatDateOnly(
          sesionInspeccionada?.fechaApertura
        )}`}
        subtitle={`Apertura: ${formatDateTime(
          sesionInspeccionada?.fechaApertura || ''
        )}`}
        maxWidthClass="max-w-2xl"
      >
        {sesionInspeccionada && (
          <div className="space-y-4 text-xs">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div
                className={`p-3 rounded-lg border ${SEMANTIC_COLORS.cash.surfaceClass}`}
              >
                <span className="text-ink-secondary block">
                  Efectivo esperado
                </span>
                <strong className="font-mono-tabular text-sm font-bold text-ink-primary">
                  {formatCurrency(
                    sesionInspeccionada.montoFinalEsperado,
                    moneda
                  )}
                </strong>
              </div>
              <div className="p-3 rounded-lg border border-border-subtle bg-elevated/60">
                <span className="text-ink-secondary block">Real contado</span>
                <strong className="font-mono-tabular text-sm font-bold text-ink-primary">
                  {sesionInspeccionada.montoFinalReal != null
                    ? formatCurrency(sesionInspeccionada.montoFinalReal, moneda)
                    : 'Sesión abierta'}
                </strong>
              </div>
              <div
                className={`p-3 rounded-lg border ${SEMANTIC_COLORS.digital.surfaceClass}`}
              >
                <span className="text-ink-secondary block">
                  Recaudación digital
                </span>
                <strong
                  className={`font-mono-tabular text-sm font-bold ${SEMANTIC_COLORS.digital.textClass}`}
                >
                  {formatCurrency(
                    sesionInspeccionada.totalVentasDigital,
                    moneda
                  )}
                </strong>
              </div>
            </div>

            {sesionInspeccionada.observacionesCierre && (
              <div
                className={`p-3 rounded-lg border text-ink-primary ${SEMANTIC_COLORS.warning.surfaceClass}`}
              >
                <strong>Observaciones de cierre:</strong>{' '}
                {sesionInspeccionada.observacionesCierre}
              </div>
            )}

            <div className="space-y-2">
              <h4 className="font-semibold text-ink-primary">
                Movimientos registrados (
                {(sesionInspeccionada.movimientos ?? []).length})
              </h4>
              {(sesionInspeccionada.movimientos ?? []).length === 0 ? (
                <p className="text-ink-secondary py-4 text-center border border-dashed border-border-strong rounded-lg">
                  No se registraron movimientos manuales en esta sesión.
                </p>
              ) : (
                <div className="border border-border-subtle rounded-lg overflow-hidden">
                  <table className="w-full text-left border-collapse text-xs">
                    <thead className="bg-elevated/80 border-b border-border-subtle text-ink-secondary">
                      <tr>
                        <th className="p-2.5 font-medium">Fecha</th>
                        <th className="p-2.5 font-medium">Tipo</th>
                        <th className="p-2.5 font-medium">Concepto</th>
                        <th className="p-2.5 font-medium text-right">Monto</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border-subtle">
                      {(sesionInspeccionada.movimientos ?? []).map((m) => (
                        <tr key={m.id}>
                          <td className="p-2.5 font-mono-tabular text-ink-secondary whitespace-nowrap">
                            {formatDateTime(m.fecha)}
                          </td>
                          <td className="p-2.5 font-medium text-ink-primary whitespace-nowrap">
                            {m.tipo}
                          </td>
                          <td className="p-2.5 text-ink-primary">
                            {m.concepto}
                          </td>
                          <td className="p-2.5 text-right font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                            {formatCurrency(m.monto, moneda)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        )}
      </Modal>

      <PrintReceiptModal
        data={actaImprimible}
        onClose={() => setActaImprimible(null)}
        nombreSalon={configSalon?.nombreComercial}
        cuitSalon={configSalon?.cuit}
      />
    </div>
  );
}
