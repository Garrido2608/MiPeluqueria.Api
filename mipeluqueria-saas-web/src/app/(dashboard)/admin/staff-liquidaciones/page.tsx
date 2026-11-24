// filepath: src/app/(dashboard)/admin/staff-liquidaciones/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
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
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import { formatCurrency, formatDateOnly } from '@/lib/formatters';
import {
  AsyncStateGuard,
  Button,
  InputField,
  Modal,
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

export default function AdminStaffComisionesLiquidacionesPage() {
  const { configSalon, catalogos } = useCatalogos();
  const { sesionCajaActual } = useCajaPos();
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
  const [feedback, setFeedback] = useState<string | null>(null);

  // 1. ABM Peluquero y Horarios con Descansos
  const [modalPelOpen, setModalPelOpen] = useState<boolean>(false);
  const [pelEditando, setPelEditando] = useState<PeluqueroDto | null>(null);
  const [nombrePel, setNombrePel] = useState('');
  const [apellidoPel, setApellidoPel] = useState('');
  const [emailPel, setEmailPel] = useState('');
  const [telefonoPel, setTelefonoPel] = useState('');
  const [colorHex, setColorHex] = useState('#1E40AF');
  const [especialidadesIds, setEspecialidadesIds] = useState<number[]>([]);

  const [pelHorariosModal, setPelHorariosModal] =
    useState<PeluqueroDto | null>(null);
  const [grillaHorarios, setGrillaHorarios] = useState<HorarioLaboralDto[]>([]);

  // 2. ABM Esquemas de Comisión (soporta POST y PUT)
  const [comEditandoId, setComEditandoId] = useState<number | null>(null);
  const [comPeluqueroId, setComPeluqueroId] = useState<number>(1);
  const [comServicioId, setComServicioId] = useState<number | null>(null);
  const [porcentajeServ, setPorcentajeServ] = useState<number>(50);
  const [porcentajeProd, setPorcentajeProd] = useState<number>(10);

  // 3. Liquidaciones e Impresión de Recibo
  const [peluqueroLiqId, setPeluqueroLiqId] = useState<number>(1);
  const [fechaDesde, setFechaDesde] = useState<string>(() => {
    const d = new Date();
    d.setDate(1);
    return d.toISOString().split('T')[0];
  });
  const [fechaHasta, setFechaHasta] = useState<string>(
    () => new Date().toISOString().split('T')[0]
  );
  const [bonificaciones, setBonificaciones] = useState<number>(0);
  const [descuentos, setDescuentos] = useState<number>(0);
  const [reciboImprimir, setReciboImprimir] =
    useState<PrintableReceiptData | null>(null);

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
          setPeluqueroLiqId((prev) => (prev > 0 ? prev : pelItems[0].id));
          setComPeluqueroId((prev) => (prev > 0 ? prev : pelItems[0].id));
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
    try {
      const res = pelEditando
        ? await peluquerosService.update(pelEditando.id, payload)
        : await peluquerosService.create(payload);
      if (res.success) {
        notifyApiSuccess(res, 'Legajo del profesional guardado.');
        setModalPelOpen(false);
        setPelEditando(null);
        setFeedback('Legajo del profesional guardado.');
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el profesional.',
      });
    }
  };

  const handleEliminarPeluquero = async (pel: PeluqueroDto) => {
    try {
      const res = await peluquerosService.delete(pel.id);
      if (res.success) {
        notifyApiSuccess(
          res,
          `Profesional "${pel.nombre} ${pel.apellido}" dado de baja.`
        );
        setFeedback(
          `Profesional "${pel.nombre} ${pel.apellido}" dado de baja.`
        );
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo dar de baja al profesional.',
      });
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
          `Horarios semanales de ${pelHorariosModal.nombre} actualizados.`
        );
        setPelHorariosModal(null);
        setFeedback(
          `Horarios semanales y descansos de ${pelHorariosModal.nombre} actualizados.`
        );
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudieron actualizar los horarios.',
      });
    }
  };

  const handleGuardarComision = async (e: React.FormEvent) => {
    e.preventDefault();
    const dto = {
      peluqueroId: comPeluqueroId,
      servicioId: comServicioId,
      porcentajeServicio: porcentajeServ,
      porcentajeVentaProducto: porcentajeProd,
    };
    try {
      const res = comEditandoId
        ? await comisionesService.updateConfig(comEditandoId, dto)
        : await comisionesService.saveConfig(dto);
      if (res.success) {
        notifyApiSuccess(res, 'Regla de comisión guardada.');
        setComEditandoId(null);
        setFeedback('Regla de comisión guardada.');
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar la regla de comisión.',
      });
    }
  };

  const handleEliminarComision = async (id: number) => {
    try {
      const res = await comisionesService.deleteConfig(id);
      if (res.success) {
        notifyApiSuccess(res, 'Regla de comisión eliminada.');
        setFeedback('Regla de comisión eliminada.');
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar la regla de comisión.',
      });
    }
  };

  const handleGenerarLiquidacion = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await liquidacionesService.generar({
        peluqueroId: peluqueroLiqId,
        fechaDesde,
        fechaHasta,
        bonificaciones,
        descuentos,
      });
      if (res.success) {
        notifyApiSuccess(res, 'Liquidación de comisiones emitida.');
        setFeedback(
          'Liquidación emitida. Los renglones de venta quedaron bloqueados en DetallesLiquidacion.'
        );
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo generar la liquidación.',
      });
    }
  };

  const handlePagarLiquidacion = async (id: number) => {
    try {
      const res = await liquidacionesService.pagar(id, sesionCajaActual?.id);
      if (res.success) {
        notifyApiSuccess(
          res,
          `Liquidación #${id} abonada${
            sesionCajaActual
              ? ` e imputada en Caja #${sesionCajaActual.id}`
              : ''
          }.`
        );
        setFeedback(
          `Liquidación #${id} abonada${
            sesionCajaActual
              ? ` e imputada en Caja #${sesionCajaActual.id}`
              : ''
          }.`
        );
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo abonar la liquidación.',
      });
    }
  };

  const handleAnularLiquidacion = async (id: number) => {
    try {
      const res = await liquidacionesService.anular(
        id,
        'Anulación administrativa de liquidación pendiente'
      );
      if (res.success) {
        notifyApiSuccess(
          res,
          `Liquidación #${id} anulada y comisiones liberadas para nuevo cálculo.`
        );
        setFeedback(
          `Liquidación #${id} anulada y comisiones liberadas para nuevo cálculo.`
        );
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo anular la liquidación.',
      });
    }
  };

  const imprimirReciboSueldo = async (id: number) => {
    try {
      const res = await liquidacionesService.getById(id);
      if (res.success && res.data) {
        const l = res.data;
        setReciboImprimir({
          titulo: 'RECIBO DE LIQUIDACIÓN DE COMISIONES',
          numeroComprobante: `LIQ-${l.id}`,
          fechaIso: l.fechaPago || l.fechaHasta,
          clienteOProfesional: l.peluqueroNombreCompleto,
          monedaIso: moneda,
          renglones: extractPagedItems(l.detallesLiquidacion).map((d) => ({
            descripcion:
              d.conceptoDescripcion ||
              `Ítem #${d.detalleVentaId || d.turnoId}`,
            monto: d.montoComisionCalculado,
          })),
          pagosODesglose: [
            { etiqueta: 'Subtotal Servicios', monto: l.totalServicios },
            { etiqueta: 'Subtotal Productos', monto: l.totalProductos },
            { etiqueta: 'Bonificaciones (+)', monto: l.bonificaciones || 0 },
            {
              etiqueta: 'Descuentos / Adelantos (-)',
              monto: l.descuentos || 0,
            },
          ],
          totalFinal: l.totalPagar,
          pieDePagina: `Período liquidado: ${formatDateOnly(
            l.fechaDesde
          )} al ${formatDateOnly(l.fechaHasta)}`,
        });
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo generar el recibo de liquidación.',
      });
    }
  };

  return (
    <div className="p-6 space-y-6">
      <header className="bg-white border border-slate-200 rounded-lg p-5 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900">
            Staff Profesional, Esquemas de Comisión y Liquidación de Sueldos
          </h1>
          <p className="text-xs text-slate-500">
            Gestión de estilistas, horarios con descansos, matriz de porcentajes
            y recibos imprimibles.
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant={subTab === 'profesionales' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('profesionales')}
          >
            1. Profesionales y Horarios ({peluqueros.length})
          </Button>
          <Button
            variant={subTab === 'comisiones' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('comisiones')}
          >
            2. Esquemas de Comisión ({comisiones.length})
          </Button>
          <Button
            variant={subTab === 'liquidaciones' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('liquidaciones')}
          >
            3. Liquidación de Haberes ({liquidaciones.length})
          </Button>
        </div>
      </header>

      {feedback && (
        <div className="p-3 rounded bg-slate-900 text-white text-xs flex justify-between">
          <span>{feedback}</span>
          <button type="button" onClick={() => setFeedback(null)}>
            ✕
          </button>
        </div>
      )}

      {subTab === 'profesionales' && (
        <div className="space-y-4">
          <div className="flex justify-end">
            <Button
              variant="editorial"
              size="sm"
              onClick={() => {
                setPelEditando(null);
                setNombrePel('');
                setApellidoPel('');
                setEmailPel('');
                setTelefonoPel('');
                setColorHex('#1E40AF');
                setEspecialidadesIds([]);
                setModalPelOpen(true);
              }}
            >
              + Alta de Profesional
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
            emptyDescription="Registrá tu primer estilista con '+ Alta de Profesional'."
            onRetry={cargarDatos}
          >
            <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-100 text-slate-700 uppercase">
                  <tr>
                    <th className="p-3">Color</th>
                    <th className="p-3">Profesional</th>
                    <th className="p-3">Contacto</th>
                    <th className="p-3">Especialidades</th>
                    <th className="p-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {(peluqueros ?? []).map((p) => (
                    <tr key={p.id}>
                      <td className="p-3">
                        <span
                          className="inline-block w-6 h-6 rounded-full border border-slate-300"
                          style={{ backgroundColor: p.colorAgendaHex }}
                        />
                      </td>
                      <td className="p-3 font-bold text-slate-900">
                        {p.nombre} {p.apellido}
                      </td>
                      <td className="p-3 font-mono">
                        {p.email} • {p.telefono || '—'}
                      </td>
                      <td className="p-3">
                        {(p.especialidadesNombres ?? []).join(', ') || 'General'}
                      </td>
                      <td className="p-3 text-right space-x-1.5">
                        <Button
                          variant="primary"
                          size="sm"
                          onClick={() => abrirEditorHorarios(p)}
                        >
                          Horarios y Descansos
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
                            setColorHex(p.colorAgendaHex);
                            setEspecialidadesIds(p.especialidadesIds || []);
                            setModalPelOpen(true);
                          }}
                        >
                          Editar
                        </Button>
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => handleEliminarPeluquero(p)}
                        >
                          Baja
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </AsyncStateGuard>
        </div>
      )}

      {subTab === 'comisiones' && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          <form
            onSubmit={handleGuardarComision}
            className="lg:col-span-5 bg-white border border-slate-200 rounded-lg p-5 space-y-4"
          >
            <h2 className="text-base font-bold">
              {comEditandoId
                ? `Editar Regla de Comisión #${comEditandoId}`
                : 'Configurar % General o Excepción por Servicio'}
            </h2>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-semibold uppercase text-slate-700">
                Profesional *
              </label>
              <select
                value={comPeluqueroId}
                onChange={(e) => setComPeluqueroId(Number(e.target.value))}
                className="rounded border border-slate-300 px-3 py-2 text-sm"
              >
                {peluqueros.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nombre} {p.apellido}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-semibold uppercase text-slate-700">
                Alcance de la Regla
              </label>
              <select
                value={comServicioId || ''}
                onChange={(e) =>
                  setComServicioId(
                    e.target.value ? Number(e.target.value) : null
                  )
                }
                className="rounded border border-slate-300 px-3 py-2 text-sm"
              >
                <option value="">Regla General (Todos los servicios)</option>
                {servicios.map((s) => (
                  <option key={s.id} value={s.id}>
                    Excepción Específica: {s.nombre}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <InputField
                label="% Comisión Servicios *"
                type="number"
                step="0.5"
                required
                value={porcentajeServ}
                onChange={(e) => setPorcentajeServ(Number(e.target.value))}
              />
              <InputField
                label="% Venta Productos *"
                type="number"
                step="0.5"
                required
                value={porcentajeProd}
                onChange={(e) => setPorcentajeProd(Number(e.target.value))}
              />
            </div>
            <div className="flex gap-2">
              {comEditandoId && (
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setComEditandoId(null)}
                >
                  Cancelar
                </Button>
              )}
              <Button type="submit" variant="primary" className="flex-1">
                {comEditandoId ? 'Actualizar Regla (PUT)' : 'Guardar Regla'}
              </Button>
            </div>
          </form>

          <div className="lg:col-span-7 bg-white border border-slate-200 rounded-lg p-5">
            <h2 className="text-base font-bold mb-3">
              Matriz de Comisiones Vigente
            </h2>
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-600">
                <tr>
                  <th className="p-2">Profesional</th>
                  <th className="p-2">Servicio</th>
                  <th className="p-2">% Servicio</th>
                  <th className="p-2">% Producto</th>
                  <th className="p-2 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {comisiones.map((c) => (
                  <tr key={c.id}>
                    <td className="p-2 font-bold">
                      {c.peluqueroNombreCompleto ||
                        `Peluquero #${c.peluqueroId}`}
                    </td>
                    <td className="p-2">
                      {c.servicioNombre || 'Regla General Base'}
                    </td>
                    <td className="p-2 font-mono font-bold">
                      {c.porcentajeServicio}%
                    </td>
                    <td className="p-2 font-mono">
                      {c.porcentajeVentaProducto}%
                    </td>
                    <td className="p-2 text-right space-x-1.5">
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
                        variant="danger"
                        size="sm"
                        onClick={() => handleEliminarComision(c.id)}
                      >
                        Eliminar
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {subTab === 'liquidaciones' && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          <form
            onSubmit={handleGenerarLiquidacion}
            className="lg:col-span-5 bg-white border border-slate-200 rounded-lg p-5 space-y-4"
          >
            <h2 className="text-base font-bold">
              Liquidar Período de Comisiones
            </h2>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-semibold uppercase text-slate-700">
                Profesional *
              </label>
              <select
                value={peluqueroLiqId}
                onChange={(e) => setPeluqueroLiqId(Number(e.target.value))}
                className="rounded border border-slate-300 px-3 py-2 text-sm"
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
                label="Fecha Desde *"
                type="date"
                required
                value={fechaDesde}
                onChange={(e) => setFechaDesde(e.target.value)}
              />
              <InputField
                label="Fecha Hasta *"
                type="date"
                required
                value={fechaHasta}
                onChange={(e) => setFechaHasta(e.target.value)}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <InputField
                label="Bonificaciones (+)"
                type="number"
                value={bonificaciones}
                onChange={(e) => setBonificaciones(Number(e.target.value))}
              />
              <InputField
                label="Adelantos / Descuentos (-)"
                type="number"
                value={descuentos}
                onChange={(e) => setDescuentos(Number(e.target.value))}
              />
            </div>
            <Button type="submit" variant="primary" className="w-full">
              Calcular y Emitir Liquidación
            </Button>
          </form>

          <div className="lg:col-span-7 bg-white border border-slate-200 rounded-lg p-5 space-y-3">
            <h2 className="text-base font-bold">
              Recibos de Liquidación Emitidos
            </h2>
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
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-50 text-slate-600">
                  <tr>
                    <th className="p-2">ID</th>
                    <th className="p-2">Profesional</th>
                    <th className="p-2">Período</th>
                    <th className="p-2">Total Pagar</th>
                    <th className="p-2">Estado</th>
                    <th className="p-2 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {(liquidaciones ?? []).map((l) => (
                    <tr key={l.id}>
                      <td className="p-2 font-mono font-bold">#{l.id}</td>
                      <td className="p-2 font-semibold">
                        {l.peluqueroNombreCompleto}
                      </td>
                      <td className="p-2">
                        {formatDateOnly(l.fechaDesde)} -{' '}
                        {formatDateOnly(l.fechaHasta)}
                      </td>
                      <td className="p-2 font-mono font-bold">
                        {formatCurrency(l.totalPagar, moneda)}
                      </td>
                      <td className="p-2">{l.estadoNombre || l.estado}</td>
                      <td className="p-2 text-right space-x-1">
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => imprimirReciboSueldo(l.id)}
                        >
                          🖨️ Recibo PDF
                        </Button>
                        {l.estado === EstadoLiquidacionEnum.Pendiente && (
                          <>
                            <Button
                              variant="editorial"
                              size="sm"
                              onClick={() => handlePagarLiquidacion(l.id)}
                            >
                              Pagar
                            </Button>
                            <Button
                              variant="danger"
                              size="sm"
                              onClick={() => handleAnularLiquidacion(l.id)}
                            >
                              Anular
                            </Button>
                          </>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </AsyncStateGuard>
          </div>
        </div>
      )}

      {/* Modal Grilla Semanal con Horarios Laborales + Inicio/Fin de Descanso */}
      <Modal
        isOpen={!!pelHorariosModal}
        onClose={() => setPelHorariosModal(null)}
        title={`Configurar Jornada y Descansos — ${pelHorariosModal?.nombre} ${pelHorariosModal?.apellido}`}
        maxWidthClass="max-w-3xl"
      >
        <form
          onSubmit={guardarHorariosSemanales}
          className="space-y-3 text-xs"
        >
          <div className="grid grid-cols-12 gap-2 font-bold text-slate-500 uppercase border-b pb-2">
            <span className="col-span-3">Día</span>
            <span className="col-span-2">Entrada</span>
            <span className="col-span-2">Salida</span>
            <span className="col-span-2">Inicio Desc.</span>
            <span className="col-span-2">Fin Desc.</span>
            <span className="col-span-1">Activo</span>
          </div>
          {grillaHorarios.map((h, idx) => (
            <div
              key={idx}
              className="grid grid-cols-12 gap-2 items-center border-b border-slate-100 pb-2"
            >
              <span className="col-span-3 font-bold">
                {DIAS_SEMANA[h.diaSemana] || `Día ${h.diaSemana}`}
              </span>
              <input
                type="time"
                value={h.horaInicio.slice(0, 5)}
                onChange={(e) => {
                  const next = [...grillaHorarios];
                  next[idx].horaInicio = e.target.value;
                  setGrillaHorarios(next);
                }}
                className="col-span-2 rounded border border-slate-300 px-1.5 py-1 font-mono"
              />
              <input
                type="time"
                value={h.horaFin.slice(0, 5)}
                onChange={(e) => {
                  const next = [...grillaHorarios];
                  next[idx].horaFin = e.target.value;
                  setGrillaHorarios(next);
                }}
                className="col-span-2 rounded border border-slate-300 px-1.5 py-1 font-mono"
              />
              <input
                type="time"
                value={(h.horaInicioDescanso || '13:00').slice(0, 5)}
                onChange={(e) => {
                  const next = [...grillaHorarios];
                  next[idx].horaInicioDescanso = e.target.value;
                  setGrillaHorarios(next);
                }}
                className="col-span-2 rounded border border-slate-300 px-1.5 py-1 font-mono"
              />
              <input
                type="time"
                value={(h.horaFinDescanso || '14:00').slice(0, 5)}
                onChange={(e) => {
                  const next = [...grillaHorarios];
                  next[idx].horaFinDescanso = e.target.value;
                  setGrillaHorarios(next);
                }}
                className="col-span-2 rounded border border-slate-300 px-1.5 py-1 font-mono"
              />
              <input
                type="checkbox"
                checked={h.activo ?? true}
                onChange={(e) => {
                  const next = [...grillaHorarios];
                  next[idx].activo = e.target.checked;
                  setGrillaHorarios(next);
                }}
                className="col-span-1"
              />
            </div>
          ))}
          <Button type="submit" variant="editorial" className="w-full">
            Guardar Grilla Horaria y Descansos
          </Button>
        </form>
      </Modal>

      <Modal
        isOpen={modalPelOpen}
        onClose={() => setModalPelOpen(false)}
        title={pelEditando ? 'Editar Profesional' : 'Alta de Profesional'}
      >
        <form onSubmit={handleGuardarPeluquero} className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
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
          <div className="grid grid-cols-2 gap-3">
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
          <InputField
            label="Color Identificatorio en Agenda (Hex) *"
            type="color"
            value={colorHex}
            onChange={(e) => setColorHex(e.target.value)}
          />
          <div className="space-y-1">
            <label className="text-xs font-semibold uppercase text-slate-700">
              Especialidades Asignadas
            </label>
            <div className="grid grid-cols-2 gap-2 p-2 border border-slate-200 rounded text-xs">
              {(
                catalogos?.especialidades || [
                  { id: 1, nombre: 'Colorimetría' },
                  { id: 2, nombre: 'Barbería Clásica' },
                  { id: 3, nombre: 'Corte Femenino' },
                ]
              ).map((esp) => (
                <label key={esp.id} className="flex items-center gap-2">
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
                  />
                  <span>{esp.nombre}</span>
                </label>
              ))}
            </div>
          </div>
          <Button type="submit" variant="primary" className="w-full">
            Guardar Profesional
          </Button>
        </form>
      </Modal>

      <PrintReceiptModal
        data={reciboImprimir}
        onClose={() => setReciboImprimir(null)}
        nombreSalon={configSalon?.nombreComercial}
        cuitSalon={configSalon?.cuit}
      />
    </div>
  );
}