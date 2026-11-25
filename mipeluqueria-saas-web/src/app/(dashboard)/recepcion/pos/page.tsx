// filepath: src/app/(dashboard)/recepcion/pos/page.tsx
'use client';

import React, { Suspense, useCallback, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import Link from 'next/link';
import {
  AlertTriangle,
  Banknote,
  Calendar,
  CreditCard,
  Minus,
  Package,
  Plus,
  Printer,
  Receipt,
  Scissors,
  Search,
  ShoppingCart,
  Trash2,
  Wallet,
  X,
} from 'lucide-react';
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

function isCashPaymentMethod(
  nombre: string,
  esEfectivoFisico?: boolean | null
): boolean {
  if (esEfectivoFisico === true) return true;
  const normalized = (nombre || '').toLowerCase();
  return normalized.includes('efectivo') || normalized.includes('cash');
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
  const [importePagoInput, setImportePagoInput] = useState<number | null>(null);
  const [referenciaPagoInput, setReferenciaPagoInput] = useState<string>('');

  // Clave de Idempotencia Financiera (UUID v4) — lógica interna intacta
  const [idempotencyKey, setIdempotencyKey] = useState<string>(() =>
    generateUuid()
  );
  const [ultimoErrorRed, setUltimoErrorRed] = useState<boolean>(false);
  const [procesandoCobro, setProcesandoCobro] = useState<boolean>(false);

  // Comprobante y anulación
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
    void cargarVentasDeSesion();
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

    void precargarTurnoEnCarrito();
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
    setImportePagoInput(saldoFaltanteCubrir > 0 ? saldoFaltanteCubrir : null);
  }, [saldoFaltanteCubrir]);

  const serviciosFiltrados = useMemo(() => {
    const query = busquedaItem.trim().toLowerCase();
    if (!query) return servicios;
    return servicios.filter((s) => s.nombre.toLowerCase().includes(query));
  }, [servicios, busquedaItem]);

  const productosFiltrados = useMemo(() => {
    const query = busquedaItem.trim().toLowerCase();
    if (!query) return productosVenta;
    return productosVenta.filter(
      (p) =>
        p.nombre.toLowerCase().includes(query) ||
        (p.codigoBarras || '').toLowerCase().includes(query)
    );
  }, [productosVenta, busquedaItem]);

  const atajosPago = useMemo(() => {
    const efectivo = mediosPago.find((m) =>
      isCashPaymentMethod(m.nombre, m.esEfectivoFisico)
    );
    const digital =
      mediosPago.find(
        (m) =>
          !isCashPaymentMethod(m.nombre, m.esEfectivoFisico) &&
          (m.nombre.toLowerCase().includes('mercado') ||
            m.nombre.toLowerCase().includes('qr') ||
            m.nombre.toLowerCase().includes('transfer'))
      ) ||
      mediosPago.find((m) => !isCashPaymentMethod(m.nombre, m.esEfectivoFisico));

    const extras = mediosPago.filter(
      (m) => m.id !== efectivo?.id && m.id !== digital?.id
    );

    return [
      ...(efectivo ? [{ medio: efectivo, isCash: true }] : []),
      ...(digital ? [{ medio: digital, isCash: false }] : []),
      ...extras.slice(0, 1).map((m) => ({
        medio: m,
        isCash: isCashPaymentMethod(m.nombre, m.esEfectivoFisico),
      })),
    ];
  }, [mediosPago]);

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
    const importe = importePagoInput ?? 0;
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
        etiqueta: 'Seña abonada previamente',
        monto: -venta.descuentoSenaAplicado,
      });
    }
    (venta.pagos || []).forEach((p) => {
      desglose.push({
        etiqueta: `Pago — ${p.medioPagoNombre}`,
        monto: p.importe,
      });
    });

    return {
      titulo: 'Ticket de Venta',
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
      pieDePagina: 'Gracias por tu visita — Comprobante no válido como factura fiscal',
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
        fallbackMessage: 'Error al procesar el cobro en el punto de venta.',
      });
      if (status === 400) {
        rotarIdempotencyKey();
      } else if (status >= 500 || status === 0) {
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

  const cajaBadgeLabel =
    cajaAbierta && sesionCajaActual
      ? `Caja del ${formatDateOnly(sesionCajaActual.fechaApertura)} (#${sesionCajaActual.id})`
      : 'Caja cerrada';

  return (
    <div className="flex-1 flex flex-col min-h-0 min-w-0">
      <PageHeader
        title="Punto de Venta"
        subtitle="Cobro de servicios, productos retail y turnos con soporte multipago."
        actions={
          <div className="flex flex-wrap items-center gap-2">
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
            <Link
              href="/recepcion/calendario"
              className="inline-flex h-9 items-center gap-1.5 rounded-lg border border-border-strong bg-surface px-3 text-xs font-medium text-ink-primary hover:bg-elevated transition-colors"
            >
              <Calendar className="h-4 w-4 text-ink-secondary" aria-hidden="true" />
              <span>Agenda</span>
            </Link>
            <Link
              href="/recepcion/caja"
              className="inline-flex h-9 items-center gap-1.5 rounded-lg border border-border-strong bg-surface px-3 text-xs font-medium text-ink-primary hover:bg-elevated transition-colors"
            >
              <Wallet className="h-4 w-4 text-ink-secondary" aria-hidden="true" />
              <span>Caja Diaria</span>
            </Link>
          </div>
        }
      />

      <div className="p-4 lg:p-5 space-y-5 max-w-[1440px] w-full mx-auto min-w-0">
        {!cajaAbierta && (
          <div
            className={`rounded-xl border p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3 ${SEMANTIC_COLORS.warning.surfaceClass}`}
          >
            <div className="flex items-start gap-2.5 text-xs text-ink-primary">
              <AlertTriangle
                className={`h-4 w-4 shrink-0 mt-0.5 ${SEMANTIC_COLORS.warning.textClass}`}
                aria-hidden="true"
              />
              <p>
                <strong className="font-semibold">Caja cerrada:</strong> No hay
                una sesión de caja abierta en esta sucursal. Abrí la caja para
                habilitar el cobro en el mostrador.
              </p>
            </div>
            <Link
              href="/recepcion/caja"
              className={`inline-flex h-9 items-center justify-center rounded-lg px-3.5 text-xs font-medium shrink-0 transition-colors ${SEMANTIC_COLORS.warning.solidButtonClass}`}
            >
              Abrir Caja Ahora
            </Link>
          </div>
        )}

        {/* Layout 60/40 optimizado para 1280x720 */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start min-w-0">
          {/* COLUMNA IZQUIERDA (7 cols ~58%): Catálogo + Tickets de la sesión */}
          <div className="lg:col-span-7 space-y-5 min-w-0">
            <section className="bg-surface border border-border-subtle rounded-xl p-4 space-y-4 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3">
                {/* Segmented control Servicios / Productos */}
                <div
                  role="tablist"
                  aria-label="Catálogo de venta"
                  className="inline-flex rounded-lg bg-elevated p-1 border border-border-subtle"
                >
                  <button
                    type="button"
                    role="tab"
                    aria-selected={tabCatalogo === 'servicios'}
                    onClick={() => setTabCatalogo('servicios')}
                    className={`inline-flex items-center gap-1.5 h-8 px-3 rounded-md text-xs font-medium transition-colors ${
                      tabCatalogo === 'servicios'
                        ? 'bg-surface text-ink-primary shadow-sm border border-border-subtle'
                        : 'text-ink-secondary hover:text-ink-primary'
                    }`}
                  >
                    <Scissors className="h-3.5 w-3.5" aria-hidden="true" />
                    <span>Servicios</span>
                    <span className="font-mono-tabular text-[11px] opacity-75">
                      ({servicios.length})
                    </span>
                  </button>
                  <button
                    type="button"
                    role="tab"
                    aria-selected={tabCatalogo === 'productos'}
                    onClick={() => setTabCatalogo('productos')}
                    className={`inline-flex items-center gap-1.5 h-8 px-3 rounded-md text-xs font-medium transition-colors ${
                      tabCatalogo === 'productos'
                        ? 'bg-surface text-ink-primary shadow-sm border border-border-subtle'
                        : 'text-ink-secondary hover:text-ink-primary'
                    }`}
                  >
                    <Package className="h-3.5 w-3.5" aria-hidden="true" />
                    <span>Productos Retail</span>
                    <span className="font-mono-tabular text-[11px] opacity-75">
                      ({productosVenta.length})
                    </span>
                  </button>
                </div>

                {/* Profesional por defecto */}
                <div className="flex items-center gap-2 min-w-0">
                  <label
                    htmlFor="pos-peluquero-default"
                    className="text-xs font-medium text-ink-secondary whitespace-nowrap"
                  >
                    Profesional:
                  </label>
                  <select
                    id="pos-peluquero-default"
                    value={peluqueroDefaultId}
                    onChange={(e) => setPeluqueroDefaultId(Number(e.target.value))}
                    className="h-9 rounded-lg border border-border-strong bg-surface text-ink-primary px-2.5 text-xs focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
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

              {/* Buscador */}
              <div className="relative">
                <Search
                  className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-secondary"
                  aria-hidden="true"
                />
                <input
                  type="search"
                  placeholder={
                    tabCatalogo === 'servicios'
                      ? 'Buscar servicio por nombre...'
                      : 'Buscar producto por nombre o código de barras...'
                  }
                  value={busquedaItem}
                  onChange={(e) => setBusquedaItem(e.target.value)}
                  className="h-10 w-full rounded-lg border border-border-strong bg-surface pl-9 pr-3 text-sm text-ink-primary placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                />
              </div>

              <AsyncStateGuard
                isLoading={cargandoCatalogos}
                isError={Boolean(errorCatalogos)}
                errorMessage={errorCatalogos?.message}
                correlationId={errorCatalogos?.correlationId}
                isEmpty={
                  tabCatalogo === 'servicios'
                    ? serviciosFiltrados.length === 0
                    : productosFiltrados.length === 0
                }
                loadingText="Cargando catálogo de facturación..."
                emptyTitle="Sin ítems coincidentes"
                emptyDescription="No se encontraron servicios o productos disponibles para la búsqueda actual."
                onRetry={cargarCatalogosPos}
              >
                {tabCatalogo === 'servicios' ? (
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 max-h-[340px] overflow-y-auto pr-1">
                    {serviciosFiltrados.map((serv) => (
                      <button
                        key={serv.id}
                        type="button"
                        onClick={() => agregarServicioAlCarrito(serv)}
                        className="group text-left p-3 rounded-lg border border-border-subtle bg-surface hover:border-brand-primary hover:bg-elevated/60 transition-colors flex items-center justify-between gap-2 min-w-0"
                      >
                        <div className="min-w-0">
                          <p className="text-sm font-medium text-ink-primary truncate group-hover:text-brand-primary transition-colors">
                            {serv.nombre}
                          </p>
                          <p className="text-xs text-ink-secondary truncate mt-0.5">
                            <span className="font-mono-tabular">
                              {serv.duracionMinutos} min
                            </span>{' '}
                            · {serv.categoriaNombre || 'Servicio'}
                          </p>
                        </div>
                        <span className="font-mono-tabular text-xs font-semibold text-ink-primary bg-elevated border border-border-subtle px-2 py-1 rounded-md shrink-0">
                          {formatCurrency(serv.precio, moneda)}
                        </span>
                      </button>
                    ))}
                  </div>
                ) : (
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5 max-h-[340px] overflow-y-auto pr-1">
                    {productosFiltrados.map((prod) => (
                      <button
                        key={prod.id}
                        type="button"
                        onClick={() => agregarProductoAlCarrito(prod)}
                        className="group text-left p-3 rounded-lg border border-border-subtle bg-surface hover:border-brand-primary hover:bg-elevated/60 transition-colors flex items-center justify-between gap-2 min-w-0"
                      >
                        <div className="min-w-0">
                          <p className="text-sm font-medium text-ink-primary truncate group-hover:text-brand-primary transition-colors">
                            {prod.nombre}
                          </p>
                          <p className="text-xs text-ink-secondary truncate mt-0.5">
                            Stock:{' '}
                            <span className="font-mono-tabular font-medium">
                              {prod.stockActual} u.
                            </span>{' '}
                            · SKU: {prod.codigoBarras || 'S/C'}
                          </p>
                        </div>
                        <span className="font-mono-tabular text-xs font-semibold text-ink-primary bg-elevated border border-border-subtle px-2 py-1 rounded-md shrink-0">
                          {formatCurrency(prod.precioVenta, moneda)}
                        </span>
                      </button>
                    ))}
                  </div>
                )}
              </AsyncStateGuard>
            </section>

            {/* Historial de Ventas de la Sesión Actual */}
            <section className="bg-surface border border-border-subtle rounded-xl overflow-hidden shadow-sm">
              <div className="px-4 py-3 border-b border-border-subtle flex items-center justify-between gap-2">
                <div className="flex items-center gap-2">
                  <Receipt
                    className="h-4 w-4 text-ink-secondary"
                    aria-hidden="true"
                  />
                  <h2 className="text-sm font-semibold text-ink-primary">
                    Tickets emitidos en la sesión
                  </h2>
                </div>
                <Badge variant="neutral">
                  <span className="font-mono-tabular">{ventasSesion.length}</span>
                </Badge>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full text-left border-collapse text-xs">
                  <thead>
                    <tr className="bg-elevated/80 border-b border-border-subtle text-ink-secondary">
                      <th className="py-2.5 px-3 font-medium">Ticket</th>
                      <th className="py-2.5 px-3 font-medium">Fecha y hora</th>
                      <th className="py-2.5 px-3 font-medium">Cliente</th>
                      <th className="py-2.5 px-3 font-medium text-right">Seña</th>
                      <th className="py-2.5 px-3 font-medium text-right">Total</th>
                      <th className="py-2.5 px-3 font-medium text-right">Acciones</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {ventasSesion.length === 0 ? (
                      <tr>
                        <td
                          colSpan={6}
                          className="py-8 text-center text-ink-secondary"
                        >
                          Todavía no se emitieron ventas en esta sesión de caja.
                        </td>
                      </tr>
                    ) : (
                      ventasSesion.map((v) => (
                        <tr
                          key={v.id}
                          className="hover:bg-elevated/50 transition-colors"
                        >
                          <td className="py-2.5 px-3 font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                            #{v.id}
                            {v.turnoId ? (
                              <span className="ml-1 text-[11px] text-ink-secondary font-normal">
                                (T#{v.turnoId})
                              </span>
                            ) : null}
                          </td>
                          <td className="py-2.5 px-3 font-mono-tabular text-ink-secondary whitespace-nowrap">
                            {formatDateTime(v.fecha)}
                          </td>
                          <td className="py-2.5 px-3 font-medium text-ink-primary max-w-[160px] truncate">
                            {v.clienteNombre || 'Consumidor Final'}
                          </td>
                          <td
                            className={`py-2.5 px-3 text-right font-mono-tabular whitespace-nowrap ${SEMANTIC_COLORS.cash.textClass}`}
                          >
                            {v.descuentoSenaAplicado > 0
                              ? `-${formatCurrency(v.descuentoSenaAplicado, moneda)}`
                              : '—'}
                          </td>
                          <td className="py-2.5 px-3 text-right font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                            {formatCurrency(v.total, moneda)}
                          </td>
                          <td className="py-2.5 px-3 text-right whitespace-nowrap">
                            <div className="inline-flex items-center gap-1.5">
                              <Button
                                size="sm"
                                variant="secondary"
                                leftIcon={
                                  <Printer className="h-3.5 w-3.5" aria-hidden="true" />
                                }
                                onClick={() =>
                                  setComprobanteImprimir(armarTicketImprimible(v))
                                }
                              >
                                Imprimir
                              </Button>
                              {!v.anulada && (
                                <Button
                                  size="sm"
                                  variant="destructive"
                                  destructiveTone="soft"
                                  onClick={() => setVentaAAnular(v)}
                                >
                                  Anular
                                </Button>
                              )}
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          </div>

          {/* COLUMNA DERECHA (5 cols ~42%): Ticket en curso y Multipago */}
          <div className="lg:col-span-5 min-w-0">
            <section className="bg-surface border border-border-subtle rounded-xl p-4 sm:p-5 space-y-4 shadow-sm">
              <div className="flex items-start justify-between gap-2 border-b border-border-subtle pb-3">
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <ShoppingCart
                      className="h-4 w-4 text-brand-primary shrink-0"
                      aria-hidden="true"
                    />
                    <h2 className="text-base font-semibold text-ink-primary">
                      Ticket en curso
                    </h2>
                  </div>
                  {turnoVinculado && (
                    <p className="text-xs text-brand-primary font-medium mt-1 truncate">
                      Turno #{turnoVinculado.id} —{' '}
                      {turnoVinculado.clienteNombreCompleto}
                    </p>
                  )}
                </div>
                {renglones.length > 0 && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    leftIcon={<Trash2 className="h-3.5 w-3.5" aria-hidden="true" />}
                    onClick={limpiarCarritoCompleto}
                    className="text-red-600 dark:text-rose-400 hover:text-red-700"
                  >
                    Vaciar
                  </Button>
                )}
              </div>

              {/* Selector de Cliente */}
              <div className="flex flex-col gap-1.5">
                <label
                  htmlFor="pos-cliente-select"
                  className="text-xs font-medium text-ink-secondary"
                >
                  Cliente asociado
                </label>
                <select
                  id="pos-cliente-select"
                  value={clienteId ?? 0}
                  onChange={(e) => {
                    const val = Number(e.target.value);
                    setClienteId(val > 0 ? val : null);
                    rotarIdempotencyKey();
                  }}
                  className="h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
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
              <div className="space-y-2 max-h-[230px] overflow-y-auto pr-0.5">
                {renglones.length === 0 ? (
                  <div className="py-7 px-4 text-center text-xs text-ink-secondary border border-dashed border-border-strong rounded-lg bg-elevated/40">
                    Seleccioná servicios o productos del catálogo, o iniciá el
                    cobro desde un turno en la Agenda.
                  </div>
                ) : (
                  renglones.map((item, idx) => (
                    <div
                      key={`${item.tipoItem}-${item.itemId}-${idx}`}
                      className="p-2.5 rounded-lg border border-border-subtle bg-elevated/50 space-y-2 text-xs"
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div className="flex items-center gap-1.5 min-w-0">
                          <Badge
                            variant={
                              item.tipoItem === TipoItemVentaEnum.Servicio
                                ? 'info'
                                : 'courtesy'
                            }
                            className="text-[10px] px-1.5 py-0"
                          >
                            {item.tipoItem === TipoItemVentaEnum.Servicio
                              ? 'Servicio'
                              : 'Producto'}
                          </Badge>
                          <strong className="text-ink-primary font-medium truncate">
                            {item.nombreItem || `Ítem #${item.itemId}`}
                          </strong>
                        </div>
                        <button
                          type="button"
                          onClick={() => eliminarRenglon(idx)}
                          aria-label="Quitar ítem"
                          className="inline-flex h-6 w-6 items-center justify-center rounded-md text-ink-secondary hover:text-red-600 dark:hover:text-rose-400 hover:bg-surface transition-colors shrink-0"
                        >
                          <X className="h-3.5 w-3.5" aria-hidden="true" />
                        </button>
                      </div>

                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <div className="flex items-center gap-1">
                          <button
                            type="button"
                            onClick={() =>
                              cambiarCantidadRenglon(idx, item.cantidad - 1)
                            }
                            aria-label="Restar cantidad"
                            className="inline-flex h-7 w-7 items-center justify-center rounded-md border border-border-strong bg-surface text-ink-primary hover:bg-elevated"
                          >
                            <Minus className="h-3 w-3" aria-hidden="true" />
                          </button>
                          <span className="font-mono-tabular font-semibold px-2 text-ink-primary">
                            {item.cantidad}
                          </span>
                          <button
                            type="button"
                            onClick={() =>
                              cambiarCantidadRenglon(idx, item.cantidad + 1)
                            }
                            aria-label="Sumar cantidad"
                            className="inline-flex h-7 w-7 items-center justify-center rounded-md border border-border-strong bg-surface text-ink-primary hover:bg-elevated"
                          >
                            <Plus className="h-3 w-3" aria-hidden="true" />
                          </button>
                          <span className="text-ink-secondary font-mono-tabular ml-1">
                            × {formatCurrency(item.precioUnitario, moneda)}
                          </span>
                        </div>

                        <select
                          aria-label="Profesional asignado a comisión"
                          value={item.peluqueroId ?? 0}
                          onChange={(e) => {
                            const val = Number(e.target.value);
                            cambiarPeluqueroRenglon(idx, val > 0 ? val : null);
                          }}
                          className="h-7 rounded-md border border-border-strong bg-surface text-ink-primary px-2 text-[11px] max-w-[140px]"
                        >
                          <option value={0}>Sin comisión</option>
                          {peluqueros.map((p) => (
                            <option key={p.id} value={p.id}>
                              {p.nombre} {p.apellido}
                            </option>
                          ))}
                        </select>

                        <span className="font-mono-tabular font-semibold text-ink-primary ml-auto">
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

              {/* Resumen Financiero + Descuento de Seña */}
              <div className="border-t border-border-subtle pt-3 space-y-2 text-xs">
                <div className="flex justify-between text-ink-secondary">
                  <span>Subtotal</span>
                  <span className="font-mono-tabular font-medium text-ink-primary">
                    {formatCurrency(subtotalCarrito, moneda)}
                  </span>
                </div>

                {turnoVinculado?.senaPagada && montoSenaAbonada > 0 && (
                  <div
                    className={`flex justify-between items-center px-3 py-2 rounded-lg border font-medium ${SEMANTIC_COLORS.cash.surfaceClass} ${SEMANTIC_COLORS.cash.textClass}`}
                  >
                    <span>Seña abonada previamente</span>
                    <span className="font-mono-tabular font-semibold">
                      -{formatCurrency(montoSenaAbonada, moneda)}
                    </span>
                  </div>
                )}

                <div className="flex justify-between items-baseline pt-2 border-t border-border-subtle">
                  <span className="text-sm font-semibold text-ink-primary">
                    Saldo neto a cobrar
                  </span>
                  <span className="font-mono-tabular text-xl font-bold text-ink-primary">
                    {formatCurrency(saldoNetoACobrar, moneda)}
                  </span>
                </div>
              </div>

              {/* Sección de Multipago */}
              <div className="border-t border-border-subtle pt-3 space-y-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <h3 className="text-xs font-semibold text-ink-primary">
                    Medios de pago
                  </h3>
                  <div className="flex flex-wrap gap-1.5">
                    {atajosPago.map(({ medio, isCash }) => (
                      <button
                        key={medio.id}
                        type="button"
                        disabled={saldoNetoACobrar <= 0}
                        onClick={() => handleCubrirTodoEnUnPago(medio.id)}
                        className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-[11px] font-medium transition-colors disabled:opacity-40 disabled:cursor-not-allowed ${
                          isCash
                            ? SEMANTIC_COLORS.cash.softButtonClass
                            : SEMANTIC_COLORS.digital.softButtonClass
                        }`}
                      >
                        {isCash ? (
                          <Banknote className="h-3.5 w-3.5" aria-hidden="true" />
                        ) : (
                          <CreditCard className="h-3.5 w-3.5" aria-hidden="true" />
                        )}
                        <span>100% {medio.nombre}</span>
                      </button>
                    ))}
                  </div>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-12 gap-2 items-end">
                  <div className="sm:col-span-5 flex flex-col gap-1.5">
                    <label
                      htmlFor="pos-medio-pago"
                      className="text-xs font-medium text-ink-secondary"
                    >
                      Medio
                    </label>
                    <select
                      id="pos-medio-pago"
                      value={medioPagoSeleccionadoId}
                      onChange={(e) =>
                        setMedioPagoSeleccionadoId(Number(e.target.value))
                      }
                      className="h-10 w-full rounded-lg border border-border-strong bg-surface text-ink-primary px-2.5 text-xs focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                    >
                      {mediosPago.map((m) => (
                        <option key={m.id} value={m.id}>
                          {m.nombre}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="sm:col-span-4">
                    <NumericInput
                      id="pos-importe-pago"
                      label="Importe"
                      prefix="$"
                      value={importePagoInput}
                      onValueChange={setImportePagoInput}
                      placeholder="0"
                    />
                  </div>

                  <div className="sm:col-span-3">
                    <Button
                      type="button"
                      size="md"
                      variant="secondary"
                      className="w-full"
                      onClick={handleAgregarPago}
                    >
                      + Sumar
                    </Button>
                  </div>
                </div>

                <InputField
                  label="Nro. de cupón o referencia (opcional)"
                  value={referenciaPagoInput}
                  onChange={(e) => setReferenciaPagoInput(e.target.value)}
                  placeholder="Ej. MP-99482716"
                />

                {pagos.length > 0 && (
                  <div className="space-y-1.5 pt-1">
                    {pagos.map((p, idx) => {
                      const medioObj = mediosPago.find(
                        (m) => m.id === p.medioPagoId
                      );
                      const isCash = isCashPaymentMethod(
                        p.medioPagoNombre || medioObj?.nombre || '',
                        medioObj?.esEfectivoFisico
                      );
                      return (
                        <div
                          key={`${p.medioPagoId}-${idx}`}
                          className={`flex items-center justify-between px-3 py-2 rounded-lg border text-xs ${
                            isCash
                              ? SEMANTIC_COLORS.cash.surfaceClass
                              : SEMANTIC_COLORS.digital.surfaceClass
                          }`}
                        >
                          <div className="flex items-center gap-2 min-w-0">
                            <span
                              className={`h-2 w-2 rounded-full shrink-0 ${
                                isCash
                                  ? SEMANTIC_COLORS.cash.dotClass
                                  : SEMANTIC_COLORS.digital.dotClass
                              }`}
                              aria-hidden="true"
                            />
                            <span className="truncate text-ink-primary">
                              <strong className="font-medium">
                                {p.medioPagoNombre}
                              </strong>
                              {p.referenciaOperacion
                                ? ` · Ref: ${p.referenciaOperacion}`
                                : ''}
                            </span>
                          </div>
                          <div className="flex items-center gap-2 shrink-0">
                            <span className="font-mono-tabular font-semibold text-ink-primary">
                              {formatCurrency(p.importe, moneda)}
                            </span>
                            <button
                              type="button"
                              onClick={() => handleQuitarPago(idx)}
                              aria-label="Quitar pago"
                              className="inline-flex h-6 w-6 items-center justify-center rounded-md text-ink-secondary hover:text-red-600 dark:hover:text-rose-400"
                            >
                              <X className="h-3.5 w-3.5" aria-hidden="true" />
                            </button>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}

                <div className="flex justify-between items-center text-xs font-medium pt-1">
                  <span className="text-ink-secondary">Restante por cubrir</span>
                  <span
                    className={`font-mono-tabular font-semibold ${
                      saldoFaltanteCubrir === 0
                        ? SEMANTIC_COLORS.cash.textClass
                        : SEMANTIC_COLORS.warning.textClass
                    }`}
                  >
                    {formatCurrency(saldoFaltanteCubrir, moneda)}
                  </span>
                </div>
              </div>

              {ultimoErrorRed && (
                <div
                  className={`p-3 rounded-lg border text-xs text-ink-primary ${SEMANTIC_COLORS.warning.surfaceClass}`}
                >
                  Se detectó un problema de conexión. Podés presionar{' '}
                  <strong>Reintentar cobro</strong> con seguridad: el sistema
                  evita duplicaciones automáticamente.
                </div>
              )}

              <Button
                type="button"
                variant="primary"
                size="lg"
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
                  ? 'Reintentar cobro'
                  : `Confirmar y emitir ticket (${formatCurrency(
                      saldoNetoACobrar,
                      moneda
                    )})`}
              </Button>
            </section>
          </div>
        </div>
      </div>

      {/* Modal de Anulación de Venta */}
      <Modal
        isOpen={Boolean(ventaAAnular)}
        onClose={() => setVentaAAnular(null)}
        title={`Anular venta #${ventaAAnular?.id ?? ''}`}
        subtitle="Revertirá automáticamente el ingreso en caja, las comisiones y el stock."
      >
        <form onSubmit={handleConfirmarAnulacion} className="space-y-4">
          <InputField
            label="Motivo de anulación *"
            required
            value={motivoAnulacion}
            onChange={(e) => setMotivoAnulacion(e.target.value)}
          />
          <div className="flex justify-end gap-2 pt-1">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setVentaAAnular(null)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="destructive"
              destructiveTone="solid"
              isLoading={anulando}
            >
              Confirmar anulación
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
        <div className="p-6 text-sm text-ink-secondary">
          Cargando Punto de Venta...
        </div>
      }
    >
      <PuntoDeVentaPosContent />
    </Suspense>
  );
}
