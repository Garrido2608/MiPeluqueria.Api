// filepath: src/app/(dashboard)/recepcion/caja/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
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
import { formatCurrency, formatDateTime } from '@/lib/formatters';
import {
  AsyncStateGuard,
  Badge,
  Button,
  InputField,
  Modal,
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

  // Estados de Apertura, Movimientos Manuales y Cierre
  const [montoInicial, setMontoInicial] = useState<number>(15000);
  const [montoMovimiento, setMontoMovimiento] = useState<number>(0);
  const [conceptoMovimiento, setConceptoMovimiento] = useState<string>('');
  const [tipoEgreso, setTipoEgreso] = useState<string>('EgresoInsumoMenor');
  const [montoFisicoCierre, setMontoFisicoCierre] = useState<number>(0);
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
    cargarHistorial();
  }, [cargarHistorial]);

  const handleAbrirCaja = async (e: React.FormEvent) => {
    e.preventDefault();
    setProcesando(true);
    try {
      const res = await cajaService.abrir({ montoInicial });
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
    if (
      !sesionCajaActual ||
      montoMovimiento <= 0 ||
      !conceptoMovimiento.trim()
    ) {
      return;
    }

    setProcesando(true);
    try {
      const payload = {
        sesionCajaId: sesionCajaActual.id,
        monto: montoMovimiento,
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
          `Movimiento manual de ${modo} imputado en la caja #${sesionCajaActual.id}.`
        );
        setMontoMovimiento(0);
        setConceptoMovimiento('');
        await refrescarEstadoCaja();
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
    titulo: 'ACTA DE ARQUEO DE CAJA',
    numeroComprobante: String(sesion.id).padStart(5, '0'),
    fechaIso: sesion.fechaCierre || sesion.fechaApertura,
    clienteOProfesional:
      sesion.usuarioCierreNombre ||
      sesion.usuarioAperturaNombre ||
      `Cajero #${sesion.usuarioAperturaId}`,
    monedaIso: moneda,
    renglones: [
      { descripcion: 'Fondo Fijo Inicial', monto: sesion.montoInicial },
      {
        descripcion: '+ Ventas en Efectivo Físico',
        monto: sesion.totalVentasEfectivo,
      },
      {
        descripcion: '+ Ingresos Manuales',
        monto: sesion.totalIngresosManuales,
      },
      {
        descripcion: '- Egresos / Alivios de Caja',
        monto: -sesion.totalEgresosManuales,
      },
    ],
    pagosODesglose: [
      {
        etiqueta: 'Efectivo Físico Esperado en Cajón',
        monto: sesion.montoFinalEsperado,
      },
      {
        etiqueta: 'Efectivo Físico Real Contado',
        monto: sesion.montoFinalReal ?? 0,
      },
      {
        etiqueta: 'Diferencia de Arqueo (Sobrante/Faltante)',
        monto: sesion.diferencia ?? 0,
      },
      {
        etiqueta: 'Recaudación Digital Acumulada (Informativo)',
        monto: sesion.totalVentasDigital,
      },
    ],
    totalFinal: sesion.montoFinalReal ?? sesion.montoFinalEsperado,
    pieDePagina: sesion.observacionesCierre
      ? `Observaciones: ${sesion.observacionesCierre}`
      : 'Arqueo de Tesorería Auditado',
  });

  const handleCerrarCaja = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!sesionCajaActual) return;

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

  return (
    <div className="flex-1 flex flex-col min-h-0">
      <PageHeader
        eyebrow="Tesorería & Arqueo de Billetes • CajaController"
        title="Control de Caja Diaria y Arqueo Ciego"
        subtitle="Fórmula estricta de arqueo: MontoFinalEsperado = MontoInicial + VentasEfectivo + IngresosManuales - EgresosManuales"
        actions={
          <Badge variant={cajaAbierta ? 'success' : 'danger'}>
            {cajaAbierta
              ? `CAJA ABIERTA #${sesionCajaActual?.id}`
              : 'CAJA CERRADA'}
          </Badge>
        }
      />

      <div className="p-6 space-y-6">
        {/* Dos Tarjetas Independientes (Efectivo Físico en Cajón vs Recaudación Digital) */}
        {cajaAbierta && sesionCajaActual && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="bg-white border-2 border-emerald-700 rounded-lg p-5 space-y-2 shadow-sm">
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold uppercase tracking-wider text-emerald-900">
                  1. Efectivo Físico Esperado en Cajón
                </span>
                <Badge variant="success">Solo Billetes Físicos</Badge>
              </div>
              <p className="text-2xl font-mono font-extrabold text-slate-900">
                {formatCurrency(sesionCajaActual.montoFinalEsperado, moneda)}
              </p>
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-2 border-t border-slate-100 text-[11px] text-slate-600">
                <div>
                  <span className="block text-slate-400">Fondo Inicial</span>
                  <strong className="font-mono">
                    {formatCurrency(sesionCajaActual.montoInicial, moneda)}
                  </strong>
                </div>
                <div>
                  <span className="block text-slate-400">+ Ventas Efectivo</span>
                  <strong className="font-mono text-emerald-700">
                    {formatCurrency(
                      sesionCajaActual.totalVentasEfectivo,
                      moneda
                    )}
                  </strong>
                </div>
                <div>
                  <span className="block text-slate-400">+ Ingresos Man.</span>
                  <strong className="font-mono text-emerald-700">
                    {formatCurrency(
                      sesionCajaActual.totalIngresosManuales,
                      moneda
                    )}
                  </strong>
                </div>
                <div>
                  <span className="block text-slate-400">- Egresos Man.</span>
                  <strong className="font-mono text-red-700">
                    -{formatCurrency(
                      sesionCajaActual.totalEgresosManuales,
                      moneda
                    )}
                  </strong>
                </div>
              </div>
            </div>

            <div className="bg-white border border-slate-200 rounded-lg p-5 space-y-2 shadow-sm">
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold uppercase tracking-wider text-sky-900">
                  2. Recaudación Digital Acumulada
                </span>
                <Badge variant="info">Tarjetas / QR / Transferencias</Badge>
              </div>
              <p className="text-2xl font-mono font-extrabold text-slate-900">
                {formatCurrency(sesionCajaActual.totalVentasDigital, moneda)}
              </p>
              <p className="text-xs text-slate-500 pt-2 border-t border-slate-100">
                Acreditado directamente en cuentas bancarias/pasarelas
                digitales. No se suma al arqueo de billetes físicos del cajón.
              </p>
            </div>
          </div>
        )}

        {!cajaAbierta ? (
          <form
            onSubmit={handleAbrirCaja}
            className="max-w-md bg-white border border-slate-200 rounded-lg p-6 space-y-4 shadow-sm"
          >
            <h2 className="text-base font-bold text-slate-900">
              Apertura de Turno de Caja
            </h2>
            <InputField
              label="Fondo Fijo Inicial en Efectivo *"
              type="number"
              step="0.01"
              required
              value={montoInicial}
              onChange={(e) => setMontoInicial(Number(e.target.value))}
            />
            <Button
              type="submit"
              variant="primary"
              className="w-full"
              isLoading={procesando}
            >
              Abrir Sesión de Caja
            </Button>
          </form>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-white border border-slate-200 rounded-lg p-6 space-y-4">
              <h2 className="text-base font-bold text-slate-900">
                Registrar Ingreso / Egreso Manual
              </h2>
              <InputField
                label="Importe en Efectivo *"
                type="number"
                step="0.01"
                value={montoMovimiento}
                onChange={(e) => setMontoMovimiento(Number(e.target.value))}
              />
              <InputField
                label="Concepto / Comprobante *"
                value={conceptoMovimiento}
                onChange={(e) => setConceptoMovimiento(e.target.value)}
                placeholder="Ej. Pago cadetería / Retiro alivio de caja / Cambio"
              />
              <div>
                <label className="text-xs font-semibold uppercase tracking-wider text-slate-700 block mb-1">
                  Clasificación Operativa
                </label>
                <select
                  value={tipoEgreso}
                  onChange={(e) => setTipoEgreso(e.target.value)}
                  className="w-full rounded border border-slate-300 px-3 py-2 text-xs"
                >
                  <option value="EgresoInsumoMenor">Egreso Insumo Menor</option>
                  <option value="RetiroDuenio">
                    Retiro de Alivio / Dueño
                  </option>
                  <option value="PagoDelivery">
                    Pago Cadetería / Delivery
                  </option>
                </select>
              </div>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="secondary"
                  className="flex-1"
                  isLoading={procesando}
                  onClick={() => handleMovimiento('ingreso')}
                >
                  + Registrar Ingreso
                </Button>
                <Button
                  type="button"
                  variant="danger"
                  className="flex-1"
                  isLoading={procesando}
                  onClick={() => handleMovimiento('egreso')}
                >
                  - Registrar Egreso
                </Button>
              </div>
            </div>

            <form
              onSubmit={handleCerrarCaja}
              className="bg-white border border-slate-200 rounded-lg p-6 space-y-4"
            >
              <h2 className="text-base font-bold text-slate-900">
                Cierre y Arqueo Físico de Billetes
              </h2>
              <InputField
                label="Efectivo Físico Contado en Cajón (MontoFinalReal) *"
                type="number"
                step="0.01"
                required
                value={montoFisicoCierre}
                onChange={(e) => setMontoFisicoCierre(Number(e.target.value))}
              />
              <InputField
                label="Observaciones de Cierre / Justificación de Diferencia"
                value={observacionesCierre}
                onChange={(e) => setObservacionesCierre(e.target.value)}
                placeholder="Ej. Arqueo conforme sin diferencias."
              />
              <Button
                type="submit"
                variant="editorial"
                className="w-full"
                isLoading={procesando}
              >
                Ejecutar Arqueo y Cerrar Caja #{sesionCajaActual?.id}
              </Button>
            </form>
          </div>
        )}

        {/* Movimientos de la Sesión Abierta Actual */}
        {cajaAbierta &&
          sesionCajaActual &&
          sesionCajaActual.movimientos &&
          sesionCajaActual.movimientos.length > 0 && (
            <div className="bg-white border border-slate-200 rounded-lg p-5">
              <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700 mb-3">
                Libro Diario de Movimientos de la Sesión Actual (#
                {sesionCajaActual.id})
              </h3>
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-50 text-slate-600">
                    <tr>
                      <th className="p-2">Hora</th>
                      <th className="p-2">Tipo</th>
                      <th className="p-2">Concepto</th>
                      <th className="p-2 text-right">Importe</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {sesionCajaActual.movimientos.map((m) => (
                      <tr key={m.id}>
                        <td className="p-2 text-slate-500">
                          {formatDateTime(m.fecha)}
                        </td>
                        <td className="p-2 font-semibold">{m.tipo}</td>
                        <td className="p-2">{m.concepto}</td>
                        <td className="p-2 text-right font-mono font-bold">
                          {formatCurrency(m.monto, moneda)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

        {/* Historial Paginado de Arqueos */}
        <div className="space-y-3">
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
            <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
              <div className="px-5 py-3.5 border-b border-slate-200 flex items-center justify-between">
                <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700">
                  Historial de Sesiones y Diferencias de Arqueo
                </h3>
                <span className="text-xs text-slate-500">
                  Página {pageNumber} de {totalPages}
                </span>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-50 text-slate-600 border-b border-slate-200">
                    <tr>
                      <th className="p-3">Sesión #</th>
                      <th className="p-3">Apertura / Cierre</th>
                      <th className="p-3 text-right">Fondo Inicial</th>
                      <th className="p-3 text-right">Efectivo Esperado</th>
                      <th className="p-3 text-right">Real Contado</th>
                      <th className="p-3 text-right">Digital Acum.</th>
                      <th className="p-3 text-right">Diferencia</th>
                      <th className="p-3 text-right">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {(historialCajas ?? []).map((c) => (
                      <tr key={c.id} className="hover:bg-slate-50">
                        <td className="p-3 font-mono font-bold">#{c.id}</td>
                        <td className="p-3">
                          <p>{formatDateTime(c.fechaApertura)}</p>
                          {c.fechaCierre && (
                            <p className="text-[11px] text-slate-400">
                              Cierre: {formatDateTime(c.fechaCierre)}
                            </p>
                          )}
                        </td>
                        <td className="p-3 text-right font-mono">
                          {formatCurrency(c.montoInicial, moneda)}
                        </td>
                        <td className="p-3 text-right font-mono font-semibold">
                          {c.montoFinalEsperado != null
                            ? formatCurrency(c.montoFinalEsperado, moneda)
                            : '—'}
                        </td>
                        <td className="p-3 text-right font-mono">
                          {c.montoFinalReal != null
                            ? formatCurrency(c.montoFinalReal, moneda)
                            : '—'}
                        </td>
                        <td className="p-3 text-right font-mono text-sky-800">
                          {formatCurrency(c.totalVentasDigital, moneda)}
                        </td>
                        <td
                          className={`p-3 text-right font-mono font-bold ${
                            (c.diferencia || 0) < 0
                              ? 'text-red-700'
                              : 'text-emerald-800'
                          }`}
                        >
                          {c.diferencia != null
                            ? formatCurrency(c.diferencia, moneda)
                            : 'Abierta'}
                        </td>
                        <td className="p-3 text-right space-x-1.5">
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() =>
                              handleInspeccionarSesionHistorica(c.id)
                            }
                          >
                            Ver Detalle
                          </Button>
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() => setActaImprimible(armarActaArqueo(c))}
                          >
                            Acta PDF
                          </Button>
                        </td>
                      </tr>
                    ))}
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
        </div>
      </div>

      {/* Modal de Inspección de Sesión Histórica (GET /api/caja/{id}) */}
      <Modal
        isOpen={!!sesionInspeccionada}
        onClose={() => setSesionInspeccionada(null)}
        title={`Detalle de Sesión de Caja #${sesionInspeccionada?.id}`}
        subtitle={`Apertura: ${formatDateTime(
          sesionInspeccionada?.fechaApertura || ''
        )}`}
        maxWidthClass="max-w-2xl"
      >
        {sesionInspeccionada && (
          <div className="space-y-4 text-xs">
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 bg-slate-50 p-3 rounded border border-slate-200">
              <div>
                <span className="text-slate-500 block">Efectivo Esperado</span>
                <strong className="font-mono text-sm">
                  {formatCurrency(
                    sesionInspeccionada.montoFinalEsperado,
                    moneda
                  )}
                </strong>
              </div>
              <div>
                <span className="text-slate-500 block">Real Contado</span>
                <strong className="font-mono text-sm">
                  {sesionInspeccionada.montoFinalReal != null
                    ? formatCurrency(sesionInspeccionada.montoFinalReal, moneda)
                    : 'Abierta'}
                </strong>
              </div>
              <div>
                <span className="text-slate-500 block">Recaudación Digital</span>
                <strong className="font-mono text-sm text-sky-900">
                  {formatCurrency(
                    sesionInspeccionada.totalVentasDigital,
                    moneda
                  )}
                </strong>
              </div>
            </div>

            {sesionInspeccionada.observacionesCierre && (
              <p className="p-2.5 rounded bg-amber-50 border border-amber-200 text-amber-950">
                <strong>Observaciones de Cierre:</strong>{' '}
                {sesionInspeccionada.observacionesCierre}
              </p>
            )}

            <div>
              <h4 className="font-bold uppercase text-slate-700 mb-2">
                Movimientos Registrados (
                {(sesionInspeccionada.movimientos ?? []).length})
              </h4>
              <div className="max-h-60 overflow-y-auto border border-slate-200 rounded">
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-50 border-b border-slate-200">
                    <tr>
                      <th className="p-2">Fecha</th>
                      <th className="p-2">Tipo</th>
                      <th className="p-2">Concepto</th>
                      <th className="p-2 text-right">Monto</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {(sesionInspeccionada.movimientos ?? []).map((m) => (
                      <tr key={m.id}>
                        <td className="p-2">{formatDateTime(m.fecha)}</td>
                        <td className="p-2 font-semibold">{m.tipo}</td>
                        <td className="p-2">{m.concepto}</td>
                        <td className="p-2 text-right font-mono font-bold">
                          {formatCurrency(m.monto, moneda)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
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
