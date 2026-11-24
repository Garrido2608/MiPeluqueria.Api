// filepath: src/app/(dashboard)/admin/servicios-recetas/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  ProductoDto,
  RecetaInsumoItemDto,
  ServicioDto,
} from '@/types/api.types';
import {
  productosService,
  recetasServicioService,
  serviciosService,
} from '@/services/api.services';
import { useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import { formatCurrency } from '@/lib/formatters';
import {
  AsyncStateGuard,
  Button,
  InputField,
  Modal,
} from '@/components/ui';

export default function AdminServiciosYRecetasPage() {
  const { configSalon, catalogos } = useCatalogos();
  const moneda = configSalon?.moneda || 'ARS';

  const [subTab, setSubTab] = useState<'catalogo' | 'recetas'>('catalogo');
  const [servicios, setServicios] = useState<ServicioDto[]>([]);
  const [insumosInternos, setInsumosInternos] = useState<ProductoDto[]>([]);
  const [servicioElegido, setServicioElegido] = useState<ServicioDto | null>(
    null
  );
  const [itemsReceta, setItemsReceta] = useState<RecetaInsumoItemDto[]>([]);
  const [turnoIdConsumoManual, setTurnoIdConsumoManual] = useState<number>(1);
  const [cargando, setCargando] = useState<boolean>(true);
  const [errorCarga, setErrorCarga] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);
  const initialSelectedRef = useRef<boolean>(false);

  // Modal ABM Servicio Comercial
  const [modalServOpen, setModalServOpen] = useState<boolean>(false);
  const [servEditando, setServEditando] = useState<ServicioDto | null>(null);
  const [categoriaId, setCategoriaId] = useState<number>(1);
  const [nombreServ, setNombreServ] = useState<string>('');
  const [descripcionServ, setDescripcionServ] = useState<string>('');
  const [precioServ, setPrecioServ] = useState<number>(15000);
  const [duracionMinutos, setDuracionMinutos] = useState<number>(45);
  const [activoServ, setActivoServ] = useState<boolean>(true);

  const seleccionarServicio = useCallback(async (serv: ServicioDto) => {
    setServicioElegido(serv);
    try {
      const res = await recetasServicioService.getPorServicio(serv.id);
      if (res.success && res.data) {
        setItemsReceta(
          extractPagedItems<RecetaInsumoItemDto>(
            res.data.items ?? res.data.insumos
          )
        );
      } else {
        setItemsReceta([]);
      }
    } catch {
      setItemsReceta([]);
    }
  }, []);

  const cargarDatos = useCallback(async () => {
    setCargando(true);
    setErrorCarga(null);
    try {
      const [sRes, pRes] = await Promise.all([
        serviciosService.getAll(),
        productosService.getAll(false),
      ]);
      if (sRes.success) {
        const servItems = extractPagedItems<ServicioDto>(sRes.data);
        setServicios(servItems);
        if (servItems.length > 0 && !initialSelectedRef.current) {
          initialSelectedRef.current = true;
          void seleccionarServicio(servItems[0]);
        }
      }
      if (pRes.success) {
        setInsumosInternos(extractPagedItems<ProductoDto>(pRes.data));
      }
    } catch (err) {
      setErrorCarga({
        message: extractErrorMessage(
          err,
          'No se pudo cargar el catálogo de servicios y recetas.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
    } finally {
      setCargando(false);
    }
  }, [seleccionarServicio]);

  useEffect(() => {
    void cargarDatos();
  }, [cargarDatos]);

  const handleGuardarServicio = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      categoriaId,
      nombre: nombreServ.trim(),
      descripcion: descripcionServ.trim() || undefined,
      precio: precioServ,
      duracionMinutos,
      activo: activoServ,
    };

    try {
      const res = servEditando
        ? await serviciosService.update(servEditando.id, payload)
        : await serviciosService.create(payload);

      if (res.success) {
        notifyApiSuccess(res, 'Servicio comercial guardado en el catálogo.');
        setModalServOpen(false);
        setServEditando(null);
        setFeedback('Servicio comercial guardado en el catálogo.');
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el servicio comercial.',
      });
    }
  };

  const handleEliminarServicio = async (serv: ServicioDto) => {
    try {
      const res = await serviciosService.delete(serv.id);
      if (res.success) {
        notifyApiSuccess(res, `Servicio "${serv.nombre}" dado de baja lógica.`);
        setFeedback(`Servicio "${serv.nombre}" dado de baja lógica.`);
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo dar de baja el servicio.',
      });
    }
  };

  const costoEstimadoTotal = useMemo(() => {
    return (itemsReceta ?? []).reduce((acc, item) => {
      const prod = (insumosInternos ?? []).find(
        (p) => p.id === item.productoId
      );
      const costoUnit = prod?.precioCosto || item.precioCostoUnitario || 0;
      return acc + costoUnit * item.cantidadUnidadesDescontar;
    }, 0);
  }, [itemsReceta, insumosInternos]);

  const margenNetoReal = (servicioElegido?.precio || 0) - costoEstimadoTotal;

  const guardarReceta = async () => {
    if (!servicioElegido) return;
    try {
      const res = await recetasServicioService.guardarReceta({
        servicioId: servicioElegido.id,
        items: itemsReceta,
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          `Receta técnica de "${servicioElegido.nombre}" guardada.`
        );
        setFeedback(
          `Receta técnica de "${servicioElegido.nombre}" guardada. Se descontará automáticamente al completar cada turno.`
        );
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar la receta técnica.',
      });
    }
  };

  const handleEjecutarConsumoGabinetePorTurno = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await recetasServicioService.consumirPorTurno(
        turnoIdConsumoManual
      );
      if (res.success) {
        setFeedback(
          `Consumo de gabinete del Turno #${turnoIdConsumoManual} aplicado en el Kardex (ConsumoInternoSalon).`
        );
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setFeedback(
        customErr?.message || 'Error al aplicar el consumo de gabinete al turno.'
      );
    }
  };

  return (
    <div className="p-6 space-y-6">
      <header className="bg-white border border-slate-200 rounded-lg p-5 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900">
            Catálogo de Servicios y Recetas Técnicas de Gabinete
          </h1>
          <p className="text-xs text-slate-500">
            ABM de servicios comerciales, configurador de insumos químicos con
            margen neto e imputación en Kardex.
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant={subTab === 'catalogo' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('catalogo')}
          >
            1. Catálogo de Servicios ({servicios.length})
          </Button>
          <Button
            variant={subTab === 'recetas' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('recetas')}
          >
            2. Recetas de Gabinete
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

      {subTab === 'catalogo' && (
        <div className="space-y-4">
          <div className="flex justify-end">
            <Button
              variant="editorial"
              size="sm"
              onClick={() => {
                setServEditando(null);
                setNombreServ('');
                setDescripcionServ('');
                setPrecioServ(15000);
                setDuracionMinutos(45);
                setActivoServ(true);
                setModalServOpen(true);
              }}
            >
              + Nuevo Servicio Comercial
            </Button>
          </div>

          <AsyncStateGuard
            isLoading={cargando}
            isError={Boolean(errorCarga)}
            errorMessage={errorCarga?.message}
            correlationId={errorCarga?.correlationId}
            isEmpty={(servicios ?? []).length === 0}
            loadingText="Cargando catálogo de servicios..."
            emptyTitle="Sin servicios registrados"
            emptyDescription="Agregá tu primer servicio comercial para habilitar reservas y recetas."
            onRetry={cargarDatos}
          >
            <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-100 text-slate-700 uppercase">
                  <tr>
                    <th className="p-3">Servicio</th>
                    <th className="p-3">Duración</th>
                    <th className="p-3">Precio Lista</th>
                    <th className="p-3">Estado</th>
                    <th className="p-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {(servicios ?? []).map((s) => (
                    <tr key={s.id}>
                      <td className="p-3 font-bold text-slate-900">
                        {s.nombre}
                        {s.descripcion && (
                          <span className="block font-normal text-slate-500">
                            {s.descripcion}
                          </span>
                        )}
                      </td>
                      <td className="p-3 font-mono">{s.duracionMinutos} min</td>
                      <td className="p-3 font-mono font-bold">
                        {formatCurrency(s.precio, moneda)}
                      </td>
                      <td className="p-3">
                        {s.activo ? 'Activo' : 'Inactivo'}
                      </td>
                      <td className="p-3 text-right space-x-1.5">
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => {
                            void seleccionarServicio(s);
                            setSubTab('recetas');
                          }}
                        >
                          Configurar Receta
                        </Button>
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => {
                            setServEditando(s);
                            setCategoriaId(s.categoriaId || 1);
                            setNombreServ(s.nombre);
                            setDescripcionServ(s.descripcion || '');
                            setPrecioServ(s.precio);
                            setDuracionMinutos(s.duracionMinutos);
                            setActivoServ(s.activo);
                            setModalServOpen(true);
                          }}
                        >
                          Editar
                        </Button>
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => handleEliminarServicio(s)}
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

      {subTab === 'recetas' && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
            <div className="lg:col-span-4 bg-white border border-slate-200 rounded-lg p-4 space-y-2">
              <h2 className="text-xs font-bold uppercase text-slate-500">
                Seleccioná un Servicio Comercial
              </h2>
              {servicios.map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => seleccionarServicio(s)}
                  className={`w-full text-left p-3 rounded border text-xs flex justify-between ${
                    servicioElegido?.id === s.id
                      ? 'bg-slate-900 text-white border-slate-900'
                      : 'bg-white text-slate-800 border-slate-200'
                  }`}
                >
                  <span className="font-bold">{s.nombre}</span>
                  <span className="font-mono">
                    {formatCurrency(s.precio, moneda)}
                  </span>
                </button>
              ))}
            </div>

            <div className="lg:col-span-8 bg-white border border-slate-200 rounded-lg p-5 space-y-4">
              {servicioElegido && (
                <>
                  <div className="flex flex-wrap justify-between items-center bg-slate-50 p-4 rounded border border-slate-200 text-xs">
                    <div>
                      <p className="font-bold text-sm text-slate-900">
                        Receta de Gabinete: {servicioElegido.nombre}
                      </p>
                      <p className="text-slate-500">
                        Precio Venta:{' '}
                        {formatCurrency(servicioElegido.precio, moneda)} • Costo
                        Técnico Insumos:{' '}
                        {formatCurrency(costoEstimadoTotal, moneda)}
                      </p>
                    </div>
                    <div className="text-right">
                      <span className="text-[11px] uppercase text-slate-500 block">
                        Margen Neto Real
                      </span>
                      <span className="font-mono text-base font-bold text-emerald-800">
                        {formatCurrency(margenNetoReal, moneda)}
                      </span>
                    </div>
                  </div>

                  <div className="space-y-2">
                    {itemsReceta.map((item, idx) => (
                      <div
                        key={idx}
                        className="grid grid-cols-12 gap-2 items-center text-xs"
                      >
                        <select
                          value={item.productoId}
                          onChange={(e) => {
                            const next = [...itemsReceta];
                            next[idx].productoId = Number(e.target.value);
                            setItemsReceta(next);
                          }}
                          className="col-span-5 rounded border border-slate-300 px-2 py-1.5"
                        >
                          {insumosInternos.map((ins) => (
                            <option key={ins.id} value={ins.id}>
                              {ins.nombre} (Costo:{' '}
                              {formatCurrency(ins.precioCosto, moneda)})
                            </option>
                          ))}
                        </select>
                        <input
                          type="number"
                          min={1}
                          value={item.cantidadUnidadesDescontar}
                          onChange={(e) => {
                            const next = [...itemsReceta];
                            next[idx].cantidadUnidadesDescontar = Number(
                              e.target.value
                            );
                            setItemsReceta(next);
                          }}
                          className="col-span-2 rounded border border-slate-300 px-2 py-1.5 font-mono"
                        />
                        <input
                          type="number"
                          step="0.1"
                          value={item.cantidadMedidaEstandar}
                          onChange={(e) => {
                            const next = [...itemsReceta];
                            next[idx].cantidadMedidaEstandar = Number(
                              e.target.value
                            );
                            setItemsReceta(next);
                          }}
                          className="col-span-2 rounded border border-slate-300 px-2 py-1.5 font-mono"
                        />
                        <select
                          value={item.unidadMedida}
                          onChange={(e) => {
                            const next = [...itemsReceta];
                            next[idx].unidadMedida = e.target.value;
                            setItemsReceta(next);
                          }}
                          className="col-span-2 rounded border border-slate-300 px-2 py-1.5"
                        >
                          <option value="gr">gr</option>
                          <option value="ml">ml</option>
                          <option value="Unid">Unid</option>
                          <option value="Ampolla">Ampolla</option>
                        </select>
                        <button
                          type="button"
                          onClick={() =>
                            setItemsReceta((prev) =>
                              prev.filter((_, i) => i !== idx)
                            )
                          }
                          className="col-span-1 text-red-600 font-bold"
                        >
                          ✕
                        </button>
                      </div>
                    ))}
                  </div>

                  <div className="flex justify-between pt-3 border-t border-slate-200">
                    <Button
                      type="button"
                      variant="secondary"
                      size="sm"
                      onClick={() =>
                        setItemsReceta((prev) => [
                          ...prev,
                          {
                            productoId: insumosInternos[0]?.id || 1,
                            cantidadUnidadesDescontar: 1,
                            cantidadMedidaEstandar: 60,
                            unidadMedida: 'gr',
                            descuentoAutomaticoAlCompletarTurno: true,
                          },
                        ])
                      }
                    >
                      + Agregar Insumo a la Fórmula
                    </Button>
                    <Button
                      type="button"
                      variant="editorial"
                      onClick={guardarReceta}
                    >
                      Guardar Receta Técnica
                    </Button>
                  </div>
                </>
              )}
            </div>
          </div>

          {/* Ejecutor Manual de Consumo de Gabinete por Turno (POST /api/recetas-servicio/turno/{turnoId}/consumir-gabinete) */}
          <form
            onSubmit={handleEjecutarConsumoGabinetePorTurno}
            className="bg-white border border-slate-200 rounded-lg p-5 flex flex-wrap items-end justify-between gap-4"
          >
            <div>
              <h3 className="text-sm font-bold text-slate-900">
                Imputación Manual de Consumo de Gabinete por Turno
              </h3>
              <p className="text-xs text-slate-500">
                Ejecuta{' '}
                <code>
                  POST
                  /api/recetas-servicio/turno/&#123;turnoId&#125;/consumir-gabinete
                </code>{' '}
                para descontar insumos en el Kardex de un turno específico.
              </p>
            </div>
            <div className="flex items-end gap-2">
              <InputField
                label="ID del Turno *"
                type="number"
                min={1}
                required
                value={turnoIdConsumoManual}
                onChange={(e) =>
                  setTurnoIdConsumoManual(Number(e.target.value))
                }
              />
              <Button type="submit" variant="primary">
                Aplicar Consumo en Kardex
              </Button>
            </div>
          </form>
        </div>
      )}

      {/* Modal ABM Servicio Comercial */}
      <Modal
        isOpen={modalServOpen}
        onClose={() => setModalServOpen(false)}
        title={
          servEditando ? 'Editar Servicio Comercial' : 'Nuevo Servicio Comercial'
        }
      >
        <form onSubmit={handleGuardarServicio} className="space-y-3">
          <div className="flex flex-col gap-1">
            <label className="text-xs font-semibold uppercase text-slate-700">
              Categoría del Servicio *
            </label>
            <select
              value={categoriaId}
              onChange={(e) => setCategoriaId(Number(e.target.value))}
              className="rounded border border-slate-300 px-3 py-2 text-sm"
            >
              {(
                catalogos?.categoriasServicio || [
                  { id: 1, nombre: 'Peluquería' },
                  { id: 2, nombre: 'Colorimetría' },
                  { id: 3, nombre: 'Barbería' },
                ]
              ).map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nombre}
                </option>
              ))}
            </select>
          </div>
          <InputField
            label="Nombre del Servicio *"
            required
            value={nombreServ}
            onChange={(e) => setNombreServ(e.target.value)}
          />
          <InputField
            label="Descripción"
            value={descripcionServ}
            onChange={(e) => setDescripcionServ(e.target.value)}
          />
          <div className="grid grid-cols-2 gap-3">
            <InputField
              label="Precio de Lista *"
              type="number"
              step="0.01"
              required
              value={precioServ}
              onChange={(e) => setPrecioServ(Number(e.target.value))}
            />
            <InputField
              label="Duración Estimada (Minutos) *"
              type="number"
              min={5}
              required
              value={duracionMinutos}
              onChange={(e) => setDuracionMinutos(Number(e.target.value))}
            />
          </div>
          <label className="flex items-center gap-2 text-xs font-semibold">
            <input
              type="checkbox"
              checked={activoServ}
              onChange={(e) => setActivoServ(e.target.checked)}
            />
            Servicio Activo en Portal Público y POS
          </label>
          <Button type="submit" variant="primary" className="w-full">
            Guardar Servicio
          </Button>
        </form>
      </Modal>
    </div>
  );
}