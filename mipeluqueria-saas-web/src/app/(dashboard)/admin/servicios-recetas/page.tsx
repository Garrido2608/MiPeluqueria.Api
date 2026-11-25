// filepath: src/app/(dashboard)/admin/servicios-recetas/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  ChevronDown,
  ChevronRight,
  Clock,
  FlaskConical,
  Pencil,
  Plus,
  Scissors,
  Search,
  Sparkles,
  Trash2,
  TrendingUp,
} from 'lucide-react';
import {
  ProductoDto,
  RecetaInsumoItemDto,
  ServicioDto,
  TurnoDto,
} from '@/types/api.types';
import {
  productosService,
  recetasServicioService,
  serviciosService,
  turnosService,
} from '@/services/api.services';
import { useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import { formatCurrency, formatPercentage } from '@/lib/formatters';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import {
  AsyncStateGuard,
  Button,
  ConfirmModal,
  InputField,
  Modal,
  NumericInput,
  SearchCombobox,
  SearchComboboxOption,
} from '@/components/ui';

function formatShortTurnoDate(iso?: string | null): string {
  if (!iso) return 'Sin fecha';
  const normalized = iso.trim().replace(' ', 'T').replace(/(\.\d{3})\d+/, '$1');
  const parsed = new Date(normalized);
  if (Number.isNaN(parsed.getTime())) return 'Sin fecha';
  return new Intl.DateTimeFormat('es-AR', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(parsed);
}

export default function AdminServiciosYRecetasPage() {
  const { configSalon, catalogos } = useCatalogos();
  const moneda = configSalon?.moneda || 'ARS';

  const [subTab, setSubTab] = useState<'catalogo' | 'recetas'>('catalogo');
  const [servicios, setServicios] = useState<ServicioDto[]>([]);
  const [insumosInternos, setInsumosInternos] = useState<ProductoDto[]>([]);
  const [turnosRecientes, setTurnosRecientes] = useState<TurnoDto[]>([]);
  const [servicioElegido, setServicioElegido] = useState<ServicioDto | null>(
    null
  );
  const [itemsReceta, setItemsReceta] = useState<RecetaInsumoItemDto[]>([]);
  const [cargandoReceta, setCargandoReceta] = useState<boolean>(false);
  const [guardandoReceta, setGuardandoReceta] = useState<boolean>(false);

  // Búsqueda y filtros del catálogo
  const [busquedaCatalogo, setBusquedaCatalogo] = useState<string>('');
  const [filtroCategoriaId, setFiltroCategoriaId] = useState<number | 'all'>(
    'all'
  );
  const [busquedaRecetaServ, setBusquedaRecetaServ] = useState<string>('');

  // Acordeón colapsado de ajuste manual por turno
  const [ajusteManualAbierto, setAjusteManualAbierto] =
    useState<boolean>(false);
  const [turnoIdConsumoManual, setTurnoIdConsumoManual] = useState<
    number | null
  >(null);
  const [aplicandoConsumoManual, setAplicandoConsumoManual] =
    useState<boolean>(false);

  const [cargando, setCargando] = useState<boolean>(true);
  const [errorCarga, setErrorCarga] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);
  const initialSelectedRef = useRef<boolean>(false);

  // Modal Crear / Editar Servicio
  const [modalServOpen, setModalServOpen] = useState<boolean>(false);
  const [servEditando, setServEditando] = useState<ServicioDto | null>(null);
  const [categoriaId, setCategoriaId] = useState<number>(1);
  const [nombreServ, setNombreServ] = useState<string>('');
  const [descripcionServ, setDescripcionServ] = useState<string>('');
  const [precioServ, setPrecioServ] = useState<number | null>(15000);
  const [duracionMinutos, setDuracionMinutos] = useState<number | null>(45);
  const [activoServ, setActivoServ] = useState<boolean>(true);
  const [guardandoServ, setGuardandoServ] = useState<boolean>(false);

  // Confirmación de baja
  const [servParaBaja, setServParaBaja] = useState<ServicioDto | null>(null);
  const [eliminandoServ, setEliminandoServ] = useState<boolean>(false);

  const categoriasServicio = useMemo(
    () =>
      catalogos?.categoriasServicio && catalogos.categoriasServicio.length > 0
        ? catalogos.categoriasServicio
        : [
            { id: 1, nombre: 'Peluquería' },
            { id: 2, nombre: 'Colorimetría' },
            { id: 3, nombre: 'Barbería' },
          ],
    [catalogos?.categoriasServicio]
  );

  const categoriaNombreMap = useMemo(() => {
    const map = new Map<number, string>();
    for (const cat of categoriasServicio) {
      map.set(cat.id, cat.nombre);
    }
    return map;
  }, [categoriasServicio]);

  const seleccionarServicio = useCallback(async (serv: ServicioDto) => {
    setServicioElegido(serv);
    setCargandoReceta(true);
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
    } finally {
      setCargandoReceta(false);
    }
  }, []);

  const cargarDatos = useCallback(async () => {
    setCargando(true);
    setErrorCarga(null);
    try {
      const [sRes, pRes, tRes] = await Promise.all([
        serviciosService.getAll(),
        productosService.getAll(false),
        turnosService
          .getPaged({ pageNumber: 1, pageSize: 40 })
          .catch(() => null),
      ]);

      if (sRes.success) {
        const servItems = extractPagedItems<ServicioDto>(sRes.data);
        setServicios(servItems);
        if (servItems.length > 0 && !initialSelectedRef.current) {
          initialSelectedRef.current = true;
          void seleccionarServicio(servItems[0]);
        } else if (servicioElegido) {
          const updated = servItems.find((s) => s.id === servicioElegido.id);
          if (updated) setServicioElegido(updated);
        }
      }
      if (pRes.success) {
        setInsumosInternos(extractPagedItems<ProductoDto>(pRes.data));
      }
      if (tRes && tRes.success) {
        setTurnosRecientes(extractPagedItems<TurnoDto>(tRes.data));
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
  }, [seleccionarServicio, servicioElegido]);

  useEffect(() => {
    void cargarDatos();
    // Carga inicial única del módulo
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const serviciosFiltrados = useMemo(() => {
    const q = busquedaCatalogo.trim().toLowerCase();
    return (servicios ?? []).filter((s) => {
      if (filtroCategoriaId !== 'all' && s.categoriaId !== filtroCategoriaId) {
        return false;
      }
      if (!q) return true;
      const catNombre =
        s.categoriaNombre || categoriaNombreMap.get(s.categoriaId) || '';
      return (
        s.nombre.toLowerCase().includes(q) ||
        (s.descripcion || '').toLowerCase().includes(q) ||
        catNombre.toLowerCase().includes(q)
      );
    });
  }, [servicios, busquedaCatalogo, filtroCategoriaId, categoriaNombreMap]);

  const serviciosParaReceta = useMemo(() => {
    const q = busquedaRecetaServ.trim().toLowerCase();
    if (!q) return servicios ?? [];
    return (servicios ?? []).filter((s) => s.nombre.toLowerCase().includes(q));
  }, [servicios, busquedaRecetaServ]);

  const opcionesTurnosRecientes = useMemo<SearchComboboxOption<number>[]>(() => {
    return (turnosRecientes ?? []).map((t) => {
      const listaServicios =
        (t.servicios && t.servicios.length > 0
          ? t.servicios.join(', ')
          : t.serviciosDetalle?.map((d) => d.servicioNombre).join(', ')) ||
        'Servicio agendado';
      const fechaFmt = formatShortTurnoDate(t.fechaHoraInicio);
      return {
        value: t.id,
        label: `#${t.id} · ${t.clienteNombre || 'Cliente'} · ${fechaFmt}`,
        subtitle: `${listaServicios}${
          t.peluqueroNombre ? ` — ${t.peluqueroNombre}` : ''
        }`,
        keywords: `${t.id} ${t.clienteNombre || ''} ${
          t.peluqueroNombre || ''
        } ${listaServicios}`,
      };
    });
  }, [turnosRecientes]);

  const abrirModalNuevoServicio = () => {
    setServEditando(null);
    setCategoriaId(categoriasServicio[0]?.id || 1);
    setNombreServ('');
    setDescripcionServ('');
    setPrecioServ(15000);
    setDuracionMinutos(45);
    setActivoServ(true);
    setModalServOpen(true);
  };

  const abrirModalEditarServicio = (s: ServicioDto) => {
    setServEditando(s);
    setCategoriaId(s.categoriaId || categoriasServicio[0]?.id || 1);
    setNombreServ(s.nombre);
    setDescripcionServ(s.descripcion || '');
    setPrecioServ(s.precio);
    setDuracionMinutos(s.duracionMinutos);
    setActivoServ(s.activo);
    setModalServOpen(true);
  };

  const handleGuardarServicio = async (e: React.FormEvent) => {
    e.preventDefault();
    const precioFinal = precioServ ?? 0;
    const duracionFinal = duracionMinutos ?? 30;
    const payload = {
      categoriaId,
      nombre: nombreServ.trim(),
      descripcion: descripcionServ.trim() || undefined,
      precio: precioFinal,
      duracionMinutos: duracionFinal,
      activo: activoServ,
    };

    setGuardandoServ(true);
    try {
      const res = servEditando
        ? await serviciosService.update(servEditando.id, payload)
        : await serviciosService.create(payload);

      if (res.success) {
        notifyApiSuccess(
          res,
          servEditando
            ? 'Servicio actualizado correctamente.'
            : 'Servicio agregado al catálogo.'
        );
        setModalServOpen(false);
        setServEditando(null);
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el servicio.',
      });
    } finally {
      setGuardandoServ(false);
    }
  };

  const confirmarBajaServicio = async () => {
    if (!servParaBaja) return;
    setEliminandoServ(true);
    try {
      const res = await serviciosService.delete(servParaBaja.id);
      if (res.success) {
        notifyApiSuccess(
          res,
          `El servicio "${servParaBaja.nombre}" fue dado de baja.`
        );
        setServParaBaja(null);
        await cargarDatos();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo dar de baja el servicio.',
      });
    } finally {
      setEliminandoServ(false);
    }
  };

  const costoEstimadoTotal = useMemo(() => {
    return (itemsReceta ?? []).reduce((acc, item) => {
      const prod = (insumosInternos ?? []).find(
        (p) => p.id === item.productoId
      );
      const costoUnit = prod?.precioCosto || item.precioCostoUnitario || 0;
      return acc + costoUnit * (item.cantidadUnidadesDescontar || 0);
    }, 0);
  }, [itemsReceta, insumosInternos]);

  const precioServicioActual = servicioElegido?.precio || 0;
  const margenNetoReal = precioServicioActual - costoEstimadoTotal;
  const rentabilidadPorcentaje =
    precioServicioActual > 0
      ? (margenNetoReal / precioServicioActual) * 100
      : 0;

  const guardarReceta = async () => {
    if (!servicioElegido) return;
    setGuardandoReceta(true);
    try {
      const res = await recetasServicioService.guardarReceta({
        servicioId: servicioElegido.id,
        items: itemsReceta,
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          `Receta técnica de "${servicioElegido.nombre}" guardada. Los insumos se descontarán automáticamente al completar cada turno.`
        );
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar la receta técnica.',
      });
    } finally {
      setGuardandoReceta(false);
    }
  };

  const handleEjecutarConsumoGabinetePorTurno = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!turnoIdConsumoManual) return;
    setAplicandoConsumoManual(true);
    try {
      const res = await recetasServicioService.consumirPorTurno(
        turnoIdConsumoManual
      );
      if (res.success) {
        notifyApiSuccess(
          res,
          `Descuento de insumos aplicado correctamente al turno #${turnoIdConsumoManual}.`
        );
        setTurnoIdConsumoManual(null);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage:
          'No se pudo aplicar el descuento manual de insumos para el turno seleccionado.',
      });
    } finally {
      setAplicandoConsumoManual(false);
    }
  };

  return (
    <div className="max-w-[1440px] mx-auto space-y-5">
      {/* Cabecera principal */}
      <header className="bg-surface border border-border-subtle rounded-xl p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div className="min-w-0">
          <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary">
            Servicios y Recetas de Gabinete
          </h1>
          <p className="text-sm text-ink-secondary mt-0.5">
            Administrá el catálogo del salón, sus precios, duraciones y los
            insumos que se descuentan por receta.
          </p>
        </div>

        {/* Pestañas segmentadas sin prefijos numéricos */}
        <div
          role="tablist"
          aria-label="Secciones de servicios y recetas"
          className="inline-flex items-center rounded-lg bg-elevated p-1 border border-border-subtle self-start sm:self-auto shrink-0"
        >
          <button
            type="button"
            role="tab"
            aria-selected={subTab === 'catalogo'}
            onClick={() => setSubTab('catalogo')}
            className={`inline-flex items-center gap-2 rounded-md px-3 py-1.5 text-xs font-semibold transition-colors ${
              subTab === 'catalogo'
                ? 'bg-surface text-ink-primary shadow-sm border border-border-subtle'
                : 'text-ink-secondary hover:text-ink-primary'
            }`}
          >
            <Scissors className="w-3.5 h-3.5" />
            <span>Catálogo de Servicios</span>
            <span className="font-mono-tabular text-[11px] px-1.5 py-0.5 rounded-full bg-elevated text-ink-secondary">
              {servicios.length}
            </span>
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={subTab === 'recetas'}
            onClick={() => setSubTab('recetas')}
            className={`inline-flex items-center gap-2 rounded-md px-3 py-1.5 text-xs font-semibold transition-colors ${
              subTab === 'recetas'
                ? 'bg-surface text-ink-primary shadow-sm border border-border-subtle'
                : 'text-ink-secondary hover:text-ink-primary'
            }`}
          >
            <FlaskConical className="w-3.5 h-3.5" />
            <span>Recetas de Gabinete</span>
          </button>
        </div>
      </header>

      {/* PESTAÑA: CATÁLOGO DE SERVICIOS */}
      {subTab === 'catalogo' && (
        <div className="space-y-4">
          {/* Toolbar de filtros y único CTA primario */}
          <div className="bg-surface border border-border-subtle rounded-xl p-3.5 flex flex-wrap items-center justify-between gap-3">
            <div className="flex flex-wrap items-center gap-2.5 flex-1 min-w-[240px]">
              <div className="relative flex-1 min-w-[200px] max-w-sm">
                <Search
                  className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-ink-secondary"
                  aria-hidden="true"
                />
                <input
                  type="text"
                  value={busquedaCatalogo}
                  onChange={(e) => setBusquedaCatalogo(e.target.value)}
                  placeholder="Buscar servicio o categoría..."
                  className="h-9 w-full rounded-lg border border-border-strong bg-surface pl-9 pr-3 text-sm text-ink-primary placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                />
              </div>

              <select
                aria-label="Filtrar por categoría"
                value={filtroCategoriaId}
                onChange={(e) =>
                  setFiltroCategoriaId(
                    e.target.value === 'all' ? 'all' : Number(e.target.value)
                  )
                }
                className="h-9 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                <option value="all">Todas las categorías</option>
                {categoriasServicio.map((cat) => (
                  <option key={cat.id} value={cat.id}>
                    {cat.nombre}
                  </option>
                ))}
              </select>
            </div>

            <Button
              variant="primary"
              size="md"
              onClick={abrirModalNuevoServicio}
              className="shrink-0"
            >
              <Plus className="w-4 h-4" />
              Nuevo Servicio
            </Button>
          </div>

          <AsyncStateGuard
            isLoading={cargando}
            isError={Boolean(errorCarga)}
            errorMessage={errorCarga?.message}
            correlationId={errorCarga?.correlationId}
            isEmpty={serviciosFiltrados.length === 0}
            loadingText="Cargando catálogo de servicios..."
            emptyTitle={
              servicios.length === 0
                ? 'Todavía no hay servicios registrados'
                : 'Sin coincidencias para tu búsqueda'
            }
            emptyDescription={
              servicios.length === 0
                ? 'Agregá tu primer servicio para habilitar turnos, cobros y recetas de insumos.'
                : 'Probá limpiando los filtros o buscando otro término.'
            }
            onRetry={cargarDatos}
          >
            <div className="bg-surface border border-border-subtle rounded-xl overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm">
                  <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                    <tr>
                      <th className="px-4 py-3">Servicio</th>
                      <th className="px-4 py-3">Categoría</th>
                      <th className="px-4 py-3">Duración</th>
                      <th className="px-4 py-3 text-right">Precio de lista</th>
                      <th className="px-4 py-3">Estado</th>
                      <th className="px-4 py-3 text-right">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {serviciosFiltrados.map((s) => {
                      const catNombre =
                        s.categoriaNombre ||
                        categoriaNombreMap.get(s.categoriaId) ||
                        'General';
                      return (
                        <tr
                          key={s.id}
                          className="hover:bg-elevated/60 transition-colors"
                        >
                          <td className="px-4 py-3">
                            <p className="font-semibold text-ink-primary">
                              {s.nombre}
                            </p>
                            {s.descripcion && (
                              <p className="text-xs text-ink-secondary mt-0.5 line-clamp-1">
                                {s.descripcion}
                              </p>
                            )}
                          </td>
                          <td className="px-4 py-3">
                            <span
                              className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border ${SEMANTIC_COLORS.neutral.badgeClass}`}
                            >
                              {catNombre}
                            </span>
                          </td>
                          <td className="px-4 py-3">
                            <span className="inline-flex items-center gap-1 font-mono-tabular text-xs text-ink-secondary">
                              <Clock className="w-3.5 h-3.5" />
                              {s.duracionMinutos} min
                            </span>
                          </td>
                          <td className="px-4 py-3 text-right font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                            {formatCurrency(s.precio, moneda)}
                          </td>
                          <td className="px-4 py-3">
                            <span
                              className={`inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-medium border ${
                                s.activo
                                  ? SEMANTIC_COLORS.cash.badgeClass
                                  : SEMANTIC_COLORS.neutral.badgeClass
                              }`}
                            >
                              <span
                                className={`w-1.5 h-1.5 rounded-full ${
                                  s.activo
                                    ? SEMANTIC_COLORS.cash.dotClass
                                    : SEMANTIC_COLORS.neutral.dotClass
                                }`}
                              />
                              {s.activo ? 'Activo' : 'Inactivo'}
                            </span>
                          </td>
                          <td className="px-4 py-3 text-right">
                            <div className="inline-flex items-center justify-end gap-1.5">
                              <Button
                                variant="secondary"
                                size="sm"
                                onClick={() => {
                                  void seleccionarServicio(s);
                                  setSubTab('recetas');
                                }}
                              >
                                <FlaskConical className="w-3.5 h-3.5" />
                                Receta
                              </Button>
                              <Button
                                variant="secondary"
                                size="sm"
                                onClick={() => abrirModalEditarServicio(s)}
                              >
                                <Pencil className="w-3.5 h-3.5" />
                                Editar
                              </Button>
                              <Button
                                variant="destructive"
                                destructiveTone="soft"
                                size="sm"
                                onClick={() => setServParaBaja(s)}
                              >
                                <Trash2 className="w-3.5 h-3.5" />
                                Baja
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
        </div>
      )}

      {/* PESTAÑA: RECETAS DE GABINETE */}
      {subTab === 'recetas' && (
        <div className="space-y-5">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
            {/* Selector izquierdo de servicios */}
            <aside className="lg:col-span-4 bg-surface border border-border-subtle rounded-xl p-4 space-y-3">
              <div className="flex items-center justify-between gap-2">
                <h2 className="text-sm font-semibold text-ink-primary">
                  Servicios del salón
                </h2>
                <span className="text-xs text-ink-secondary font-mono-tabular">
                  {serviciosParaReceta.length}
                </span>
              </div>

              <div className="relative">
                <Search
                  className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-ink-secondary"
                  aria-hidden="true"
                />
                <input
                  type="text"
                  value={busquedaRecetaServ}
                  onChange={(e) => setBusquedaRecetaServ(e.target.value)}
                  placeholder="Filtrar servicio..."
                  className="h-9 w-full rounded-lg border border-border-strong bg-surface pl-8 pr-3 text-xs text-ink-primary placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                />
              </div>

              <div className="space-y-1.5 max-h-[460px] overflow-y-auto pr-0.5">
                {serviciosParaReceta.map((s) => {
                  const isSelected = servicioElegido?.id === s.id;
                  return (
                    <button
                      key={s.id}
                      type="button"
                      onClick={() => void seleccionarServicio(s)}
                      className={`w-full text-left px-3 py-2.5 rounded-lg border text-sm transition-colors flex items-center justify-between gap-2 ${
                        isSelected
                          ? 'bg-brand-primary/10 border-brand-primary text-ink-primary ring-1 ring-brand-primary/30'
                          : 'bg-surface border-border-subtle text-ink-primary hover:bg-elevated'
                      }`}
                    >
                      <span className="font-medium truncate">{s.nombre}</span>
                      <span className="font-mono-tabular text-xs font-semibold text-ink-secondary shrink-0">
                        {formatCurrency(s.precio, moneda)}
                      </span>
                    </button>
                  );
                })}
                {serviciosParaReceta.length === 0 && (
                  <p className="text-xs text-ink-secondary text-center py-6">
                    No se encontraron servicios.
                  </p>
                )}
              </div>
            </aside>

            {/* Editor de receta derecha */}
            <section className="lg:col-span-8 bg-surface border border-border-subtle rounded-xl p-5 space-y-5">
              {servicioElegido ? (
                <>
                  {/* Resumen financiero y Margen Neto Real en Esmeralda + % */}
                  <div className="rounded-xl border border-border-subtle bg-elevated/70 p-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                    <div className="min-w-0 space-y-1">
                      <p className="text-xs font-medium text-ink-secondary">
                        Receta de insumos del servicio
                      </p>
                      <h2 className="text-base sm:text-lg font-semibold text-ink-primary truncate">
                        {servicioElegido.nombre}
                      </h2>
                      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-ink-secondary">
                        <span>
                          Precio lista:{' '}
                          <strong className="font-mono-tabular text-ink-primary">
                            {formatCurrency(precioServicioActual, moneda)}
                          </strong>
                        </span>
                        <span>
                          Costo de insumos:{' '}
                          <strong className="font-mono-tabular text-ink-primary">
                            {formatCurrency(costoEstimadoTotal, moneda)}
                          </strong>
                        </span>
                      </div>
                    </div>

                    <div className="sm:text-right shrink-0 flex sm:flex-col items-center sm:items-end justify-between gap-2 pt-2 sm:pt-0 border-t sm:border-t-0 border-border-subtle">
                      <span className="text-xs font-medium text-ink-secondary">
                        Margen neto real
                      </span>
                      <div className="flex items-center gap-2">
                        <span
                          className={`font-mono-tabular text-xl font-bold ${
                            margenNetoReal >= 0
                              ? SEMANTIC_COLORS.cash.textClass
                              : SEMANTIC_COLORS.danger.textClass
                          }`}
                        >
                          {formatCurrency(margenNetoReal, moneda)}
                        </span>
                        <span
                          className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold font-mono-tabular border ${
                            margenNetoReal >= 0
                              ? SEMANTIC_COLORS.cash.badgeClass
                              : SEMANTIC_COLORS.danger.badgeClass
                          }`}
                        >
                          <TrendingUp className="w-3 h-3" />
                          {formatPercentage(rentabilidadPorcentaje, 1)}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* Grilla de insumos con encabezados explícitos */}
                  {cargandoReceta ? (
                    <div className="py-8 text-center text-sm text-ink-secondary">
                      Cargando fórmula de insumos...
                    </div>
                  ) : itemsReceta.length === 0 ? (
                    <div className="rounded-lg border border-dashed border-border-strong p-6 text-center space-y-1">
                      <Sparkles className="w-5 h-5 mx-auto text-ink-secondary" />
                      <p className="text-sm font-medium text-ink-primary">
                        Sin insumos configurados para este servicio
                      </p>
                      <p className="text-xs text-ink-secondary">
                        Agregá los productos de gabinete que se consumen en cada
                        atención para calcular el margen real y descontar stock.
                      </p>
                    </div>
                  ) : (
                    <div className="space-y-2.5">
                      {/* Encabezados claros de columnas */}
                      <div className="hidden sm:grid grid-cols-12 gap-2.5 px-1 text-xs font-medium text-ink-secondary">
                        <span className="col-span-5">Insumo</span>
                        <span className="col-span-2">Unid. a descontar</span>
                        <span className="col-span-2">Medida técnica</span>
                        <span className="col-span-2">Unidad</span>
                        <span className="col-span-1 text-right">Quitar</span>
                      </div>

                      {itemsReceta.map((item, idx) => (
                        <div
                          key={`${item.productoId}-${idx}`}
                          className="grid grid-cols-1 sm:grid-cols-12 gap-2.5 items-center rounded-lg border border-border-subtle bg-surface p-2.5 sm:p-2"
                        >
                          <div className="sm:col-span-5">
                            <label className="sm:hidden block text-xs font-medium text-ink-secondary mb-1">
                              Insumo
                            </label>
                            <select
                              aria-label="Insumo"
                              value={item.productoId}
                              onChange={(e) => {
                                const next = [...itemsReceta];
                                next[idx] = {
                                  ...next[idx],
                                  productoId: Number(e.target.value),
                                };
                                setItemsReceta(next);
                              }}
                              className="h-9 w-full rounded-lg border border-border-strong bg-surface px-2.5 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                            >
                              {insumosInternos.map((ins) => (
                                <option key={ins.id} value={ins.id}>
                                  {ins.nombre} (Costo:{' '}
                                  {formatCurrency(ins.precioCosto, moneda)})
                                </option>
                              ))}
                            </select>
                          </div>

                          <div className="sm:col-span-2">
                            <label className="sm:hidden block text-xs font-medium text-ink-secondary mb-1">
                              Unid. a descontar
                            </label>
                            <NumericInput
                              aria-label="Unidades a descontar"
                              allowDecimals={false}
                              min={1}
                              emptyValueOnBlur={1}
                              value={item.cantidadUnidadesDescontar}
                              onValueChange={(val) => {
                                const next = [...itemsReceta];
                                next[idx] = {
                                  ...next[idx],
                                  cantidadUnidadesDescontar: val ?? 0,
                                };
                                setItemsReceta(next);
                              }}
                              className="h-9 text-sm"
                            />
                          </div>

                          <div className="sm:col-span-2">
                            <label className="sm:hidden block text-xs font-medium text-ink-secondary mb-1">
                              Medida técnica
                            </label>
                            <NumericInput
                              aria-label="Medida técnica"
                              allowDecimals
                              decimalPlaces={1}
                              min={0}
                              emptyValueOnBlur={0}
                              value={item.cantidadMedidaEstandar}
                              onValueChange={(val) => {
                                const next = [...itemsReceta];
                                next[idx] = {
                                  ...next[idx],
                                  cantidadMedidaEstandar: val ?? 0,
                                };
                                setItemsReceta(next);
                              }}
                              className="h-9 text-sm"
                            />
                          </div>

                          <div className="sm:col-span-2">
                            <label className="sm:hidden block text-xs font-medium text-ink-secondary mb-1">
                              Unidad
                            </label>
                            <select
                              aria-label="Unidad de medida"
                              value={item.unidadMedida}
                              onChange={(e) => {
                                const next = [...itemsReceta];
                                next[idx] = {
                                  ...next[idx],
                                  unidadMedida: e.target.value,
                                };
                                setItemsReceta(next);
                              }}
                              className="h-9 w-full rounded-lg border border-border-strong bg-surface px-2.5 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                            >
                              <option value="gr">gr</option>
                              <option value="ml">ml</option>
                              <option value="Unid">Unid</option>
                              <option value="Ampolla">Ampolla</option>
                            </select>
                          </div>

                          <div className="sm:col-span-1 flex justify-end">
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              aria-label="Quitar insumo"
                              onClick={() =>
                                setItemsReceta((prev) =>
                                  prev.filter((_, i) => i !== idx)
                                )
                              }
                              className="text-red-600 hover:text-red-700 dark:text-rose-400"
                            >
                              <Trash2 className="w-4 h-4" />
                            </Button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Acciones de receta */}
                  <div className="flex flex-wrap items-center justify-between gap-3 pt-4 border-t border-border-subtle">
                    <Button
                      type="button"
                      variant="secondary"
                      size="sm"
                      disabled={insumosInternos.length === 0}
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
                      <Plus className="w-4 h-4" />
                      Agregar insumo a la fórmula
                    </Button>

                    <Button
                      type="button"
                      variant="primary"
                      size="md"
                      isLoading={guardandoReceta}
                      onClick={guardarReceta}
                    >
                      Guardar Receta Técnica
                    </Button>
                  </div>
                </>
              ) : (
                <div className="py-10 text-center text-sm text-ink-secondary">
                  Seleccioná un servicio de la lista para ver o editar su receta
                  de insumos.
                </div>
              )}
            </section>
          </div>

          {/* Acordeón colapsado: Ajuste manual de consumo por turno */}
          <div className="bg-surface border border-border-subtle rounded-xl overflow-hidden">
            <button
              type="button"
              onClick={() => setAjusteManualAbierto((prev) => !prev)}
              aria-expanded={ajusteManualAbierto}
              className="w-full px-5 py-3.5 flex items-center justify-between gap-3 text-left hover:bg-elevated/60 transition-colors"
            >
              <div className="flex items-center gap-2.5 min-w-0">
                {ajusteManualAbierto ? (
                  <ChevronDown className="w-4 h-4 text-ink-secondary shrink-0" />
                ) : (
                  <ChevronRight className="w-4 h-4 text-ink-secondary shrink-0" />
                )}
                <div className="min-w-0">
                  <h3 className="text-sm font-semibold text-ink-primary">
                    Ajuste manual de consumo por turno
                  </h3>
                  <p className="text-xs text-ink-secondary truncate">
                    Los insumos se descuentan automáticamente al completar cada
                    turno. Usá esta opción solo para imputaciones puntuales.
                  </p>
                </div>
              </div>
              <span className="text-xs font-medium text-ink-secondary shrink-0">
                {ajusteManualAbierto ? 'Ocultar' : 'Desplegar'}
              </span>
            </button>

            {ajusteManualAbierto && (
              <form
                onSubmit={handleEjecutarConsumoGabinetePorTurno}
                className="px-5 pb-5 pt-3 border-t border-border-subtle flex flex-col sm:flex-row sm:items-end gap-3"
              >
                <div className="flex-1 min-w-0">
                  <SearchCombobox
                    label="Turno reciente"
                    placeholder="Buscá por cliente, servicio o número de turno..."
                    emptyMessage="No hay turnos recientes disponibles."
                    options={opcionesTurnosRecientes}
                    value={turnoIdConsumoManual}
                    onChange={(val) => setTurnoIdConsumoManual(val)}
                  />
                </div>
                <Button
                  type="submit"
                  variant="secondary"
                  size="md"
                  disabled={!turnoIdConsumoManual}
                  isLoading={aplicandoConsumoManual}
                  className="shrink-0"
                >
                  Aplicar descuento de insumos
                </Button>
              </form>
            )}
          </div>
        </div>
      )}

      {/* Modal Crear / Editar Servicio */}
      <Modal
        isOpen={modalServOpen}
        onClose={() => setModalServOpen(false)}
        title={servEditando ? 'Editar Servicio' : 'Nuevo Servicio'}
        subtitle="Definí precio de lista, duración estimada y disponibilidad en agenda."
      >
        <form onSubmit={handleGuardarServicio} className="space-y-4">
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium text-ink-secondary">
              Categoría *
            </label>
            <select
              value={categoriaId}
              onChange={(e) => setCategoriaId(Number(e.target.value))}
              className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
            >
              {categoriasServicio.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nombre}
                </option>
              ))}
            </select>
          </div>

          <InputField
            label="Nombre del servicio *"
            required
            placeholder="Ej. Corte femenino + Brushing"
            value={nombreServ}
            onChange={(e) => setNombreServ(e.target.value)}
          />

          <InputField
            label="Descripción"
            placeholder="Detalle opcional visible para el cliente"
            value={descripcionServ}
            onChange={(e) => setDescripcionServ(e.target.value)}
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <NumericInput
              label="Precio de lista *"
              prefix="$"
              allowDecimals
              decimalPlaces={2}
              min={0}
              required
              value={precioServ}
              onValueChange={(val) => setPrecioServ(val)}
            />
            <NumericInput
              label="Duración estimada *"
              suffix="min"
              allowDecimals={false}
              min={5}
              max={600}
              required
              value={duracionMinutos}
              onValueChange={(val) => setDuracionMinutos(val)}
            />
          </div>

          <label className="flex items-center gap-2.5 text-sm text-ink-primary cursor-pointer select-none pt-1">
            <input
              type="checkbox"
              checked={activoServ}
              onChange={(e) => setActivoServ(e.target.checked)}
              className="h-4 w-4 rounded border-border-strong accent-brand-primary"
            />
            <span>Servicio activo para reservas y punto de venta</span>
          </label>

          <div className="flex items-center justify-end gap-2 pt-3 border-t border-border-subtle">
            <Button
              type="button"
              variant="secondary"
              size="md"
              onClick={() => setModalServOpen(false)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="md"
              isLoading={guardandoServ}
            >
              {servEditando ? 'Guardar cambios' : 'Crear servicio'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Confirmación de baja de servicio */}
      <ConfirmModal
        isOpen={Boolean(servParaBaja)}
        onClose={() => setServParaBaja(null)}
        onConfirm={() => void confirmarBajaServicio()}
        title="Dar de baja servicio"
        confirmLabel="Confirmar baja"
        variant="danger"
        isLoading={eliminandoServ}
      >
        <p className="text-ink-secondary">
          ¿Querés dar de baja{' '}
          <strong className="text-ink-primary">{servParaBaja?.nombre}</strong>?
          Dejará de estar disponible para nuevos turnos y cobros, conservando el
          historial de atenciones previas.
        </p>
      </ConfirmModal>
    </div>
  );
}
