// filepath: src/app/(dashboard)/recepcion/pos/page.tsx
'use client';

import React, { Suspense, useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import {
  ClienteDto,
  CrearDetalleVentaItemDto,
  CrearPagoVentaItemDto,
  PeluqueroDto,
  ProductoDto,
  ServicioDto,
  TipoItemVentaEnum,
  TurnoDto,
  VentaResponseDto,
} from '@/types/api.types';
import {
  clientesService,
  peluquerosService,
  productosService,
  serviciosService,
  turnosService,
  ventasService,
} from '@/services/api.services';
import { useCajaPos, useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import { formatCurrency, formatDateTime } from '@/lib/formatters';
import {
  AsyncStateGuard,
  Badge,
  Button,
  InputField,
  Modal,
  PageHeader,
} from '@/components/ui';
import {
  PrintableReceiptData,
  PrintReceiptModal,
} from '@/components/ui/PrintReceiptModal';

function generateUuid(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

function PuntoDeVentaPosContent() {
  const searchParams = useSearchParams();
  const turnoIdQueryParam = searchParams.get('turnoId');

  const { configSalon, catalogos } = useCatalogos();
  const {
    sesionCajaActual,
    cajaAbierta,
    posPreload,
    limpiarPreloadPos,
    refrescarEstadoCaja,
  } = useCajaPos();

  const moneda = configSalon?.moneda || 'ARS';
  const mediosPago = useMemo(
    () =>
      catalogos?.mediosPago?.length
        ? catalogos.mediosPago
        : [
            { id: 1, nombre: 'Efectivo', esEfectivoFisico: true },
            { id: 2, nombre: 'Tarjeta de Débito', esEfectivoFisico: false },
            { id: 3, nombre: 'Tarjeta de Crédito', esEfectivoFisico: false },
            { id: 4, nombre: 'Mercado Pago / QR', esEfectivoFisico: false },
            { id: 5, nombre: 'Transferencia Bancaria', esEfectivoFisico: false },
          ],
    [catalogos?.mediosPago]
  );

  // Catálogos operativos del POS
  const [servicios, setServicios] = useState<ServicioDto[]>([]);
  const [productosVenta, setProductosVenta] = useState<ProductoDto[]>([]);
  const [peluqueros, setPeluqueros] = useState<PeluqueroDto[]>([]);
  const [clientes, setClientes] = useState<ClienteDto[]>([]);
  const [ventasSesion, setVentasSesion] = useState<VentaResponseDto[]>([]);
  const [cargandoCatalogos, setCargandoCatalogos] = useState<boolean>(true);
  const [errorCatalogos, setErrorCatalogos] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);

  // Filtros del selector izquierdo
  const [tabCatalogo, setTabCatalogo] = useState<'servicios' | 'productos'>(
    'servicios'
  );
  const [busquedaItem, setBusquedaItem] = useState<string>('');
  const [peluqueroDefaultId, setPeluqueroDefaultId] = useState<number>(0);

  // Estado del Carrito y Pre-carga desde Turno (?turnoId={id})
  const [turnoVinculado, setTurnoVinculado] = useState<TurnoDto | null>(null);
  const [clienteId, setClienteId] = useState<number | null>(null);
  const [renglones, setRenglones] = useState<CrearDetalleVentaItemDto[]>([]);
  const [pagos, setPagos] = useState<CrearPagoVentaItemDto[]>([]);

  // Estado para agregar un nuevo medio de pago en el multipago
  const [medioPagoSeleccionadoId, setMedioPagoSeleccionadoId] =
    useState<number>(1);
  const [importePagoInput, setImportePagoInput] = useState<string>('');
  const [referenciaPagoInput, setReferenciaPagoInput] = useState<string>('');

  // Clave de Idempotencia Financiera (UUID v4)
  const [idempotencyKey, setIdempotencyKey] = useState<string>(() =>
    generateUuid()
  );
  const [ultimoErrorRed, setUltimoErrorRed] = useState<boolean>(false);
  const [procesandoCobro, setProcesandoCobro] = useState<boolean>(false);

  // Comprobante térmico y anulación
  const [comprobanteImprimir, setComprobanteImprimir] =
    useState<PrintableReceiptData | null>(null);
  const [ventaAAnular, setVentaAAnular] = useState<VentaResponseDto | null>(
    null
  );
  const [motivoAnulacion, setMotivoAnulacion] = useState<string>(
    'Error de carga en medio de pago'
  );
  const [anulando, setAnulando] = useState<boolean>(false);

  const rotarIdempotencyKey = useCallback(() => {
    setIdempotencyKey(generateUuid());
    setUltimoErrorRed(false);
  }, []);

  const cargarVentasDeSesion = useCallback(async () => {
    if (!sesionCajaActual?.id) {
      setVentasSesion([]);
      return;
    }
    try {
      const res = await ventasService.getPorSesion(sesionCajaActual.id);
      if (res.success) {
        setVentasSesion(extractPagedItems<VentaResponseDto>(res.data));
      }
    } catch {
      // Silencioso si no hay ventas
    }
  }, [sesionCajaActual?.id]);

  // Carga inicial de catálogos para el facturador
  const cargarCatalogosPos = useCallback(async () => {
    setCargandoCatalogos(true);
    setErrorCatalogos(null);
    try {
      const [servRes, prodRes, pelRes, cliRes] = await Promise.all([
        serviciosService.getAll(true),
        productosService.getAll(false),
        peluquerosService.getAll(true),
        clientesService.getAll(1, 100),
      ]);
      if (servRes.success) {
        setServicios(
          extractPagedItems<ServicioDto>(servRes.data).filter((s) => s.activo)
        );
      }
      if (prodRes.success) {
        setProductosVenta(
          extractPagedItems<ProductoDto>(prodRes.data).filter(
            (p) => p.esParaVenta && p.stockActual > 0
          )
        );
      }
      if (pelRes.success) {
        const pelItems = extractPagedItems<PeluqueroDto>(pelRes.data);
        setPeluqueros(pelItems);
        if (pelItems.length > 0) {
          setPeluqueroDefaultId((prev) => (prev > 0 ? prev : pelItems[0].id));
        }
      }
      if (cliRes.success) {
        setClientes(extractPagedItems<ClienteDto>(cliRes.data));
      }
    } catch (err) {
      setErrorCatalogos({
        message: extractErrorMessage(
          err,
          'No se pudo cargar el catálogo de facturación del POS.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
    } finally {
      setCargandoCatalogos(false);
    }
  }, []);

  useEffect(() => {
    void cargarCatalogosPos();
  }, [cargarCatalogosPos]);

  useEffect(() => {
    cargarVentasDeSesion();
  }, [cargarVentasDeSesion]);

  // Pre-carga desde ?turnoId={id} o desde posPreload en Context
  useEffect(() => {
    const idTarget = turnoIdQueryParam
      ? Number(turnoIdQueryParam)
      : posPreload?.turnoId || 0;

    if (!idTarget || Number.isNaN(idTarget) || idTarget <= 0) {
      return;
    }

    let cancelado = false;
    async function precargarTurnoEnCarrito() {
      try {
        const res = await turnosService.getById(idTarget);
        if (cancelado || !res.success || !res.data) return;
        const turno = res.data;
        setTurnoVinculado(turno);
        setClienteId(turno.clienteId);
        if (turno.peluqueroId) {
          setPeluqueroDefaultId(turno.peluqueroId);
        }

        const detalles = (
          turno.serviciosDetalle ||
          turno.servicios ||
          []
        ).map((s) => ({
          tipoItem: TipoItemVentaEnum.Servicio,
          itemId: s.servicioId,
          nombreItem: s.servicioNombre,
          cantidad: 1,
          precioUnitario: s.precioHistorico,
          peluqueroId: turno.peluqueroId,
        }));

        setRenglones(detalles);

        const subtotalInicial = detalles.reduce(
          (acc, item) => acc + item.cantidad * item.precioUnitario,
          0
        );
        const descuentoSena =
          turno.senaPagada && turno.montoSena ? turno.montoSena : 0;
        const saldoPendiente =
          typeof turno.saldoPendienteCobro === 'number'
            ? turno.saldoPendienteCobro
            : Math.max(0, subtotalInicial - descuentoSena);

        const medioInicial = mediosPago[0];
        if (medioInicial && saldoPendiente > 0) {
          setPagos([
            {
              medioPagoId: medioInicial.id,
              medioPagoNombre: medioInicial.nombre,
              importe: saldoPendiente,
              referenciaOperacion: null,
            },
          ]);
        } else {
          setPagos([]);
        }
        rotarIdempotencyKey();
        if (posPreload) {
          limpiarPreloadPos();
        }
      } catch (err) {
        notifyApiError(err, {
          fallbackMessage: `No se pudo pre-cargar el turno #${idTarget}.`,
        });
      }
    }

    precargarTurnoEnCarrito();
    return () => {
      cancelado = true;
    };
  }, [
    turnoIdQueryParam,
    posPreload,
    mediosPago,
    limpiarPreloadPos,
    rotarIdempotencyKey,
  ]);

  // Cálculos matemáticos del carrito y descuento de Seña Online
  const subtotalCarrito = useMemo(
    () =>
      renglones.reduce(
        (acc, item) => acc + item.cantidad * item.precioUnitario,
        0
      ),
    [renglones]
  );

  const montoSenaAbonada = useMemo(() => {
    if (turnoVinculado && turnoVinculado.senaPagada && turnoVinculado.montoSena) {
      return turnoVinculado.montoSena;
    }
    return 0;
  }, [turnoVinculado]);

  const saldoNetoACobrar = useMemo(() => {
    if (
      turnoVinculado &&
      turnoVinculado.senaPagada &&
      typeof turnoVinculado.saldoPendienteCobro === 'number' &&
      subtotalCarrito === turnoVinculado.montoTotalEstimado
    ) {
      return Math.max(0, turnoVinculado.saldoPendienteCobro);
    }
    return Math.max(0, subtotalCarrito - montoSenaAbonada);
  }, [turnoVinculado, subtotalCarrito, montoSenaAbonada]);

  const totalPagosCargados = useMemo(
    () => pagos.reduce((acc, p) => acc + p.importe, 0),
    [pagos]
  );

  const saldoFaltanteCubrir = useMemo(
    () => Math.max(0, Number((saldoNetoACobrar - totalPagosCargados).toFixed(2))),
    [saldoNetoACobrar, totalPagosCargados]
  );

  // Sincroniza el input de pago rápido con el saldo faltante
  useEffect(() => {
    setImportePagoInput(
      saldoFaltanteCubrir > 0 ? String(saldoFaltanteCubrir) : ''
    );
  }, [saldoFaltanteCubrir]);

  const agregarServicioAlCarrito = (serv: ServicioDto) => {
    setRenglones((prev) => {
      const idx = prev.findIndex(
        (r) =>
          r.tipoItem === TipoItemVentaEnum.Servicio &&
          r.itemId === serv.id &&
          r.peluqueroId === (peluqueroDefaultId || null)
      );
      if (idx >= 0) {
        const copia = [...prev];
        copia[idx] = { ...copia[idx], cantidad: copia[idx].cantidad + 1 };
        return copia;
      }
      return [
        ...prev,
        {
          tipoItem: TipoItemVentaEnum.Servicio,
          itemId: serv.id,
          nombreItem: serv.nombre,
          cantidad: 1,
          precioUnitario: serv.precio,
          peluqueroId: peluqueroDefaultId || null,
        },
      ];
    });
    rotarIdempotencyKey();
  };

  const agregarProductoAlCarrito = (prod: ProductoDto) => {
    setRenglones((prev) => {
      const idx = prev.findIndex(
        (r) =>
          r.tipoItem === TipoItemVentaEnum.Producto && r.itemId === prod.id
      );
      if (idx >= 0) {
        const actual = prev[idx];
        if (actual.cantidad + 1 > prod.stockActual) {
          return prev;
        }
        const copia = [...prev];
        copia[idx] = { ...actual, cantidad: actual.cantidad + 1 };
        return copia;
      }
      return [
        ...prev,
        {
          tipoItem: TipoItemVentaEnum.Producto,
          itemId: prod.id,
          nombreItem: prod.nombre,
          cantidad: 1,
          precioUnitario: prod.precioVenta,
          peluqueroId: peluqueroDefaultId || null,
        },
      ];
    });
    rotarIdempotencyKey();
  };

  const cambiarCantidadRenglon = (index: number, nuevaCantidad: number) => {
    if (nuevaCantidad <= 0) {
      eliminarRenglon(index);
      return;
    }
    setRenglones((prev) =>
      prev.map((item, idx) =>
        idx === index ? { ...item, cantidad: nuevaCantidad } : item
      )
    );
    rotarIdempotencyKey();
  };

  const cambiarPeluqueroRenglon = (
    index: number,
    nuevoPeluqueroId: number | null
  ) => {
    setRenglones((prev) =>
      prev.map((item, idx) =>
        idx === index ? { ...item, peluqueroId: nuevoPeluqueroId } : item
      )
    );
    rotarIdempotencyKey();
  };

  const eliminarRenglon = (index: number) => {
    setRenglones((prev) => prev.filter((_, idx) => idx !== index));
    rotarIdempotencyKey();
  };

  const limpiarCarritoCompleto = () => {
    setTurnoVinculado(null);
    setClienteId(null);
    setRenglones([]);
    setPagos([]);
    rotarIdempotencyKey();
    limpiarPreloadPos();
  };

  const handleAgregarPago = () => {
    const importe = Number(importePagoInput);
    if (Number.isNaN(importe) || importe <= 0) return;

    const medio = mediosPago.find((m) => m.id === medioPagoSeleccionadoId);
    setPagos((prev) => [
      ...prev,
      {
        medioPagoId: medioPagoSeleccionadoId,
        medioPagoNombre: medio?.nombre || `Medio #${medioPagoSeleccionadoId}`,
        importe,
        referenciaOperacion: referenciaPagoInput.trim() || null,
      },
    ]);
    setReferenciaPagoInput('');
    rotarIdempotencyKey();
  };

  const handleQuitarPago = (index: number) => {
    setPagos((prev) => prev.filter((_, idx) => idx !== index));
    rotarIdempotencyKey();
  };

  const handleCubrirTodoEnUnPago = (medioId: number) => {
    if (saldoNetoACobrar <= 0) return;
    const medio = mediosPago.find((m) => m.id === medioId);
    setPagos([
      {
        medioPagoId: medioId,
        medioPagoNombre: medio?.nombre || `Medio #${medioId}`,
        importe: saldoNetoACobrar,
        referenciaOperacion: null,
      },
    ]);
    rotarIdempotencyKey();
  };

  const armarTicketImprimible = (venta: VentaResponseDto): PrintableReceiptData => {
    const desglose: { etiqueta: string; monto: number }[] = [];
    if (venta.descuentoSenaAplicado > 0) {
      desglose.push({
        etiqueta: 'Seña Online Abonada',
        monto: -venta.descuentoSenaAplicado,
      });
    }
    (venta.pagos || []).forEach((p) => {
      desglose.push({
        etiqueta: `Pago (${p.medioPagoNombre})`,
        monto: p.importe,
      });
    });

    return {
      titulo: 'TICKET DE VENTA POS',
      numeroComprobante: String(venta.id).padStart(6, '0'),
      fechaIso: venta.fecha,
      clienteOProfesional: venta.clienteNombre || 'Consumidor Final',
      monedaIso: moneda,
      renglones: (venta.detalles || []).map((d) => ({
        descripcion: d.conceptoNombre || d.itemNombre,
        cantidad: d.cantidad,
        monto: d.subtotal,
      })),
      pagosODesglose: desglose,
      totalFinal: venta.total,
      pieDePagina: `Idempotency-Key: ${venta.idempotencyKey || idempotencyKey}`,
    };
  };

  const handleConfirmarVenta = async () => {
    if (!sesionCajaActual?.id) return;
    if (renglones.length === 0) return;

    setProcesandoCobro(true);
    try {
      const res = await ventasService.registrar(
        {
          sesionCajaId: sesionCajaActual.id,
          clienteId: clienteId || null,
          turnoId: turnoVinculado?.id || null,
          idempotencyKey,
          detalles: renglones,
          pagos,
        },
        idempotencyKey
      );

      if (res.success && res.data) {
        notifyApiSuccess(res, `Venta #${res.data.id} facturada correctamente.`);
        setComprobanteImprimir(armarTicketImprimible(res.data));
        // En cuanto responde 200/201 OK limpiamos el carrito y rotamos el UUID de idempotencia
        setTurnoVinculado(null);
        setClienteId(null);
        setRenglones([]);
        setPagos([]);
        rotarIdempotencyKey();
        limpiarPreloadPos();
        await Promise.all([refrescarEstadoCaja(), cargarVentasDeSesion()]);
      }
    } catch (err: unknown) {
      const status = (err as { statusCode?: number })?.statusCode ?? 500;
      notifyApiError(err, {
        fallbackMessage: 'Error al procesar el cobro en el POS.',
      });
      if (status === 400) {
        // Si fue error de validación 400, regenera UUID al corregir
        rotarIdempotencyKey();
      } else if (status >= 500 || status === 0) {
        // Si fue timeout o corte de red, conserva el MISMO idempotencyKey para reintento seguro
        setUltimoErrorRed(true);
      }
    } finally {
      setProcesandoCobro(false);
    }
  };

  const handleConfirmarAnulacion = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!ventaAAnular) return;
    setAnulando(true);
    try {
      const res = await ventasService.anular(ventaAAnular.id, {
        motivo: motivoAnulacion,
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          `Venta #${ventaAAnular.id} anulada y stock revertido.`
        );
        setVentaAAnular(null);
        await Promise.all([refrescarEstadoCaja(), cargarVentasDeSesion()]);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo anular la venta.',
      });
    } finally {
      setAnulando(false);
    }
  };

  return (
    <div className="flex-1 flex flex-col min-h-0">
      <PageHeader
        eyebrow="Mostrador & Facturación Multipago • Header X-Idempotency-Key"
        title="Punto de Venta (POS) y Facturador Idempotente"
        subtitle={`Clave de Idempotencia Activa: ${idempotencyKey.slice(0, 18)}… • Previene cobros duplicados ante micro-cortes`}
        actions={
          <div className="flex items-center gap-2">
            <Badge variant={cajaAbierta ? 'success' : 'danger'}>
              {cajaAbierta
                ? `SESIÓN CAJA #${sesionCajaActual?.id} ABIERTA`
                : 'CAJA CERRADA'}
            </Badge>
            <Link
              href="/recepcion/calendario"
              className="px-3 py-1.5 rounded border border-slate-300 bg-white text-xs font-semibold text-slate-700 hover:bg-slate-50"
            >
              ← Ir a Agenda
            </Link>
            <Link
              href="/recepcion/caja"
              className="px-3 py-1.5 rounded border border-slate-300 bg-white text-xs font-semibold text-slate-700 hover:bg-slate-50"
            >
              Arqueo de Caja
            </Link>
          </div>
        }
      />

      {!cajaAbierta && (
        <div className="mx-6 mt-4 p-4 rounded-lg bg-amber-50 border border-amber-300 flex items-center justify-between">
          <div className="text-xs text-amber-950">
            <strong>Atención:</strong> No hay una sesión de caja abierta en esta
            sucursal. Para facturar en el POS debés abrir la caja primero.
          </div>
          <Link
            href="/recepcion/caja"
            className="px-3 py-1.5 rounded bg-amber-900 text-white text-xs font-bold hover:bg-amber-950"
          >
            Abrir Caja Ahora →
          </Link>
        </div>
      )}

      <div className="p-6 grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* COLUMNA IZQUIERDA (7 cols): Catálogo de Servicios y Productos Retail */}
        <div className="lg:col-span-7 space-y-6">
          <div className="bg-white border border-slate-200 rounded-lg p-4 space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex gap-2">
                <Button
                  size="sm"
                  variant={tabCatalogo === 'servicios' ? 'primary' : 'secondary'}
                  onClick={() => setTabCatalogo('servicios')}
                >
                  Servicios ({servicios.length})
                </Button>
                <Button
                  size="sm"
                  variant={tabCatalogo === 'productos' ? 'primary' : 'secondary'}
                  onClick={() => setTabCatalogo('productos')}
                >
                  Productos Retail ({productosVenta.length})
                </Button>
              </div>

              <div className="flex items-center gap-2">
                <label className="text-xs font-semibold text-slate-600">
                  Profesional por defecto:
                </label>
                <select
                  value={peluqueroDefaultId}
                  onChange={(e) => setPeluqueroDefaultId(Number(e.target.value))}
                  className="rounded border border-slate-300 px-2.5 py-1.5 text-xs"
                >
                  <option value={0}>Sin asignar</option>
                  {peluqueros.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nombreCompleto || `${p.nombre} ${p.apellido}`}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <input
              type="search"
              placeholder={
                tabCatalogo === 'servicios'
                  ? 'Buscar servicio por nombre...'
                  : 'Buscar producto por nombre o código de barras...'
              }
              value={busquedaItem}
              onChange={(e) => setBusquedaItem(e.target.value)}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
            />

            <AsyncStateGuard
              isLoading={cargandoCatalogos}
              isError={Boolean(errorCatalogos)}
              errorMessage={errorCatalogos?.message}
              correlationId={errorCatalogos?.correlationId}
              isEmpty={
                tabCatalogo === 'servicios'
                  ? (servicios ?? []).filter((s) =>
                      s.nombre
                        .toLowerCase()
                        .includes(busquedaItem.toLowerCase())
                    ).length === 0
                  : (productosVenta ?? []).filter(
                      (p) =>
                        p.nombre
                          .toLowerCase()
                          .includes(busquedaItem.toLowerCase()) ||
                        (p.codigoBarras || '')
                          .toLowerCase()
                          .includes(busquedaItem.toLowerCase())
                    ).length === 0
              }
              loadingText="Cargando catálogo de facturación..."
              emptyTitle="Sin ítems coincidentes"
              emptyDescription="No se encontraron servicios o productos disponibles para la búsqueda actual."
              onRetry={cargarCatalogosPos}
            >
              {tabCatalogo === 'servicios' ? (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 max-h-96 overflow-y-auto pr-1">
                  {(servicios ?? [])
                    .filter((s) =>
                      s.nombre
                        .toLowerCase()
                        .includes(busquedaItem.toLowerCase())
                    )
                    .map((serv) => (
                      <button
                        key={serv.id}
                        type="button"
                        onClick={() => agregarServicioAlCarrito(serv)}
                        className="text-left p-3 rounded-lg border border-slate-200 hover:border-amber-800 hover:bg-amber-50/30 transition-colors flex items-center justify-between gap-2"
                      >
                        <div>
                          <p className="text-xs font-bold text-slate-900">
                            {serv.nombre}
                          </p>
                          <p className="text-[11px] text-slate-500">
                            {serv.duracionMinutos} min •{' '}
                            {serv.categoriaNombre || 'Servicio'}
                          </p>
                        </div>
                        <span className="font-mono text-xs font-bold text-slate-900 shrink-0">
                          {formatCurrency(serv.precio, moneda)}
                        </span>
                      </button>
                    ))}
                </div>
              ) : (
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 max-h-96 overflow-y-auto pr-1">
                  {(productosVenta ?? [])
                    .filter(
                      (p) =>
                        p.nombre
                          .toLowerCase()
                          .includes(busquedaItem.toLowerCase()) ||
                        (p.codigoBarras || '')
                          .toLowerCase()
                          .includes(busquedaItem.toLowerCase())
                    )
                    .map((prod) => (
                      <button
                        key={prod.id}
                        type="button"
                        onClick={() => agregarProductoAlCarrito(prod)}
                        className="text-left p-3 rounded-lg border border-slate-200 hover:border-amber-800 hover:bg-amber-50/30 transition-colors flex items-center justify-between gap-2"
                      >
                        <div>
                          <p className="text-xs font-bold text-slate-900">
                            {prod.nombre}
                          </p>
                          <p className="text-[11px] text-slate-500">
                            Stock disp.: {prod.stockActual} u. • SKU:{' '}
                            {prod.codigoBarras || 'S/C'}
                          </p>
                        </div>
                        <span className="font-mono text-xs font-bold text-slate-900 shrink-0">
                          {formatCurrency(prod.precioVenta, moneda)}
                        </span>
                      </button>
                    ))}
                </div>
              )}
            </AsyncStateGuard>
          </div>

          {/* Historial de Ventas de la Sesión Actual */}
          <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
            <div className="px-4 py-3 border-b border-slate-200 flex items-center justify-between">
              <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700">
                Tickets Emitidos en la Sesión Actual ({ventasSesion.length})
              </h3>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-xs">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-[11px] uppercase text-slate-500">
                    <th className="py-2.5 px-3">Ticket</th>
                    <th className="py-2.5 px-3">Hora</th>
                    <th className="py-2.5 px-3">Cliente</th>
                    <th className="py-2.5 px-3 text-right">Seña Desc.</th>
                    <th className="py-2.5 px-3 text-right">Total Cobrado</th>
                    <th className="py-2.5 px-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {ventasSesion.length === 0 ? (
                    <tr>
                      <td
                        colSpan={6}
                        className="py-6 text-center text-slate-400"
                      >
                        Todavía no se emitieron ventas en esta sesión de caja.
                      </td>
                    </tr>
                  ) : (
                    ventasSesion.map((v) => (
                      <tr key={v.id} className="hover:bg-slate-50">
                        <td className="py-2.5 px-3 font-mono font-bold">
                          #{v.id}
                          {v.turnoId ? ` (T#${v.turnoId})` : ''}
                        </td>
                        <td className="py-2.5 px-3 text-slate-500">
                          {formatDateTime(v.fecha)}
                        </td>
                        <td className="py-2.5 px-3 font-medium">
                          {v.clienteNombre}
                        </td>
                        <td className="py-2.5 px-3 text-right font-mono text-emerald-700">
                          {v.descuentoSenaAplicado > 0
                            ? `-${formatCurrency(v.descuentoSenaAplicado, moneda)}`
                            : '—'}
                        </td>
                        <td className="py-2.5 px-3 text-right font-mono font-bold">
                          {formatCurrency(v.total, moneda)}
                        </td>
                        <td className="py-2.5 px-3 text-right space-x-1.5">
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() =>
                              setComprobanteImprimir(armarTicketImprimible(v))
                            }
                          >
                            Imprimir
                          </Button>
                          {!v.anulada && (
                            <Button
                              size="sm"
                              variant="danger"
                              onClick={() => setVentaAAnular(v)}
                            >
                              Anular
                            </Button>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>

        {/* COLUMNA DERECHA (5 cols): Carrito, Descuento de Seña Online y Multipago */}
        <div className="lg:col-span-5 space-y-4">
          <div className="bg-white border border-slate-200 rounded-lg p-5 space-y-4 shadow-sm">
            <div className="flex items-center justify-between border-b border-slate-200 pb-3">
              <div>
                <h2 className="text-sm font-bold uppercase tracking-wider text-slate-800">
                  Ticket POS en Curso
                </h2>
                {turnoVinculado && (
                  <p className="text-xs text-amber-900 font-semibold mt-0.5">
                    Vinculado al Turno #{turnoVinculado.id} —{' '}
                    {turnoVinculado.clienteNombreCompleto}
                  </p>
                )}
              </div>
              {renglones.length > 0 && (
                <button
                  type="button"
                  onClick={limpiarCarritoCompleto}
                  className="text-xs text-red-600 hover:underline font-semibold"
                >
                  Vaciar Carrito
                </button>
              )}
            </div>

            {/* Selector de Cliente */}
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-slate-700 block mb-1">
                Cliente Asociado
              </label>
              <select
                value={clienteId ?? 0}
                onChange={(e) => {
                  const val = Number(e.target.value);
                  setClienteId(val > 0 ? val : null);
                  rotarIdempotencyKey();
                }}
                className="w-full rounded-md border border-slate-300 px-3 py-2 text-xs"
              >
                <option value={0}>Consumidor Final (Mostrador)</option>
                {clientes.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nombreCompleto || `${c.nombre} ${c.apellido}`} ({c.telefono})
                  </option>
                ))}
              </select>
            </div>

            {/* Lista de Ítems del Carrito */}
            <div className="space-y-2 max-h-64 overflow-y-auto">
              {renglones.length === 0 ? (
                <div className="py-8 text-center text-xs text-slate-400 border border-dashed border-slate-200 rounded-lg">
                  Seleccioná servicios o productos del panel izquierdo, o facturá
                  un turno desde el Calendario.
                </div>
              ) : (
                renglones.map((item, idx) => (
                  <div
                    key={`${item.tipoItem}-${item.itemId}-${idx}`}
                    className="p-2.5 rounded border border-slate-200 bg-slate-50/70 space-y-2 text-xs"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <span className="text-[10px] font-bold uppercase px-1.5 py-0.5 rounded bg-slate-200 text-slate-700 mr-1.5">
                          {item.tipoItem === TipoItemVentaEnum.Servicio
                            ? 'SERV'
                            : 'PROD'}
                        </span>
                        <strong className="text-slate-900">
                          {item.nombreItem || `Ítem #${item.itemId}`}
                        </strong>
                      </div>
                      <button
                        type="button"
                        onClick={() => eliminarRenglon(idx)}
                        className="text-slate-400 hover:text-red-600 font-bold"
                      >
                        ✕
                      </button>
                    </div>

                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <div className="flex items-center gap-1.5">
                        <button
                          type="button"
                          onClick={() =>
                            cambiarCantidadRenglon(idx, item.cantidad - 1)
                          }
                          className="w-6 h-6 rounded border border-slate-300 bg-white font-bold"
                        >
                          -
                        </button>
                        <span className="font-mono font-bold px-1.5">
                          {item.cantidad}
                        </span>
                        <button
                          type="button"
                          onClick={() =>
                            cambiarCantidadRenglon(idx, item.cantidad + 1)
                          }
                          className="w-6 h-6 rounded border border-slate-300 bg-white font-bold"
                        >
                          +
                        </button>
                        <span className="text-slate-500 font-mono ml-1">
                          x {formatCurrency(item.precioUnitario, moneda)}
                        </span>
                      </div>

                      <select
                        value={item.peluqueroId ?? 0}
                        onChange={(e) => {
                          const val = Number(e.target.value);
                          cambiarPeluqueroRenglon(idx, val > 0 ? val : null);
                        }}
                        className="rounded border border-slate-300 bg-white px-2 py-1 text-[11px]"
                      >
                        <option value={0}>Sin comisión</option>
                        {peluqueros.map((p) => (
                          <option key={p.id} value={p.id}>
                            Comisión: {p.nombre} {p.apellido}
                          </option>
                        ))}
                      </select>

                      <span className="font-mono font-bold text-slate-900">
                        {formatCurrency(
                          item.cantidad * item.precioUnitario,
                          moneda
                        )}
                      </span>
                    </div>
                  </div>
                ))
              )}
            </div>

            {/* Resumen Financiero + Descuento Visual de Seña Online */}
            <div className="border-t border-slate-200 pt-3 space-y-1.5 text-xs">
              <div className="flex justify-between text-slate-600">
                <span>Subtotal Carrito:</span>
                <span className="font-mono font-semibold">
                  {formatCurrency(subtotalCarrito, moneda)}
                </span>
              </div>

              {turnoVinculado?.senaPagada && montoSenaAbonada > 0 && (
                <div className="flex justify-between items-center p-2 rounded bg-emerald-50 border border-emerald-200 text-emerald-900 font-bold">
                  <span>
                    Seña Online Abonada: -
                    {formatCurrency(montoSenaAbonada, moneda)}
                  </span>
                  <span className="font-mono">
                    -{formatCurrency(montoSenaAbonada, moneda)}
                  </span>
                </div>
              )}

              <div className="flex justify-between text-base font-bold text-slate-900 pt-1 border-t border-slate-100">
                <span>Saldo Neto a Cobrar:</span>
                <span className="font-mono">
                  {formatCurrency(saldoNetoACobrar, moneda)}
                </span>
              </div>
            </div>

            {/* Sección de Multipago (Split Payment) */}
            <div className="border-t border-slate-200 pt-3 space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="text-xs font-bold uppercase tracking-wider text-slate-700">
                  Desglose de Medios de Pago
                </h3>
                <div className="flex gap-1">
                  {mediosPago.slice(0, 3).map((m) => (
                    <button
                      key={m.id}
                      type="button"
                      onClick={() => handleCubrirTodoEnUnPago(m.id)}
                      className="px-2 py-0.5 rounded border border-slate-300 bg-slate-50 text-[10px] font-semibold hover:bg-slate-100"
                    >
                      100% {m.nombre}
                    </button>
                  ))}
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-12 gap-2 items-end">
                <div className="sm:col-span-5">
                  <label className="text-[11px] font-semibold text-slate-600 block mb-1">
                    Medio
                  </label>
                  <select
                    value={medioPagoSeleccionadoId}
                    onChange={(e) =>
                      setMedioPagoSeleccionadoId(Number(e.target.value))
                    }
                    className="w-full rounded border border-slate-300 px-2.5 py-2 text-xs"
                  >
                    {mediosPago.map((m) => (
                      <option key={m.id} value={m.id}>
                        {m.nombre}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="sm:col-span-4">
                  <label className="text-[11px] font-semibold text-slate-600 block mb-1">
                    Importe
                  </label>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    value={importePagoInput}
                    onChange={(e) => setImportePagoInput(e.target.value)}
                    className="w-full rounded border border-slate-300 px-2.5 py-2 text-xs font-mono"
                  />
                </div>
                <div className="sm:col-span-3">
                  <Button
                    type="button"
                    size="sm"
                    variant="secondary"
                    className="w-full"
                    onClick={handleAgregarPago}
                  >
                    + Sumar
                  </Button>
                </div>
              </div>

              <InputField
                label="Nro. Cupón / Referencia Operación Digital (Opcional)"
                value={referenciaPagoInput}
                onChange={(e) => setReferenciaPagoInput(e.target.value)}
                placeholder="Ej. MP-99482716"
              />

              {pagos.length > 0 && (
                <div className="space-y-1.5 pt-1">
                  {pagos.map((p, idx) => (
                    <div
                      key={`${p.medioPagoId}-${idx}`}
                      className="flex items-center justify-between px-3 py-1.5 rounded bg-slate-100 text-xs"
                    >
                      <span>
                        <strong>{p.medioPagoNombre}</strong>
                        {p.referenciaOperacion
                          ? ` (Ref: ${p.referenciaOperacion})`
                          : ''}
                      </span>
                      <div className="flex items-center gap-2">
                        <span className="font-mono font-bold">
                          {formatCurrency(p.importe, moneda)}
                        </span>
                        <button
                          type="button"
                          onClick={() => handleQuitarPago(idx)}
                          className="text-red-600 font-bold"
                        >
                          ✕
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              <div className="flex justify-between text-xs font-semibold pt-1">
                <span>Diferencia por cubrir:</span>
                <span
                  className={`font-mono font-bold ${
                    saldoFaltanteCubrir === 0
                      ? 'text-emerald-700'
                      : 'text-amber-800'
                  }`}
                >
                  {formatCurrency(saldoFaltanteCubrir, moneda)}
                </span>
              </div>
            </div>

            {ultimoErrorRed && (
              <div className="p-3 rounded bg-amber-50 border border-amber-300 text-xs text-amber-950">
                Se detectó un fallo de red. Podés presionar{' '}
                <strong>Reintentar Cobro Seguro</strong>: se enviará la misma
                clave <code>{idempotencyKey.slice(0, 12)}…</code> para evitar
                duplicar la venta.
              </div>
            )}

            <Button
              type="button"
              variant="editorial"
              className="w-full"
              isLoading={procesandoCobro}
              disabled={
                !cajaAbierta ||
                renglones.length === 0 ||
                pagos.length === 0 ||
                Math.abs(totalPagosCargados - saldoNetoACobrar) > 0.05
              }
              onClick={handleConfirmarVenta}
            >
              {ultimoErrorRed
                ? 'Reintentar Cobro Seguro (Idempotente)'
                : `Confirmar y Emitir Ticket (${formatCurrency(
                    saldoNetoACobrar,
                    moneda
                  )})`}
            </Button>
          </div>
        </div>
      </div>

      {/* Modal de Anulación de Venta */}
      <Modal
        isOpen={!!ventaAAnular}
        onClose={() => setVentaAAnular(null)}
        title={`Anular Venta POS #${ventaAAnular?.id}`}
        subtitle="Revertirá automáticamente el ingreso en caja, las comisiones y el stock en Kardex."
      >
        <form onSubmit={handleConfirmarAnulacion} className="space-y-4">
          <InputField
            label="Motivo de Anulación *"
            required
            value={motivoAnulacion}
            onChange={(e) => setMotivoAnulacion(e.target.value)}
          />
          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setVentaAAnular(null)}
            >
              Cancelar
            </Button>
            <Button type="submit" variant="danger" isLoading={anulando}>
              Confirmar Anulación
            </Button>
          </div>
        </form>
      </Modal>

      <PrintReceiptModal
        data={comprobanteImprimir}
        onClose={() => setComprobanteImprimir(null)}
        nombreSalon={configSalon?.nombreComercial}
        cuitSalon={configSalon?.cuit}
      />
    </div>
  );
}

export default function PuntoDeVentaPosPage() {
  return (
    <Suspense
      fallback={
        <div className="p-6 text-sm text-slate-500">
          Cargando Punto de Venta (POS)...
        </div>
      }
    >
      <PuntoDeVentaPosContent />
    </Suspense>
  );
}
