// filepath: src/app/(dashboard)/admin/inventario/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  ArrowLeftRight,
  Building2,
  ClipboardList,
  History,
  MessageCircle,
  Package,
  PackageCheck,
  PackagePlus,
  Pencil,
  Plus,
  Printer,
  Search,
  Sparkles,
  Trash2,
  X,
} from 'lucide-react';
import {
  EstadoOrdenCompraEnum,
  KardexItemResponseDto,
  OrdenCompraDto,
  ProductoDto,
  ProveedorDto,
  TipoMovimientoStockEnum,
} from '@/types/api.types';
import {
  ordenesCompraService,
  productosService,
  proveedoresService,
} from '@/services/api.services';
import { useCajaPos, useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import { formatCurrency, formatDateTime } from '@/lib/formatters';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import {
  AsyncStateGuard,
  Badge,
  Button,
  ConfirmModal,
  InputField,
  Modal,
  NumericInput,
  PaginationBar,
  PrintableReceiptData,
  PrintReceiptModal,
} from '@/components/ui';

type InventarioTab = 'productos' | 'proveedores' | 'ordenes';

interface OcRenglonForm {
  productoId: number;
  cantidadPedida: number;
  precioUnitarioCosto: number;
}

interface RecepcionItemForm {
  detalleOrdenCompraId?: number;
  productoId: number;
  productoNombre: string;
  cantidadPedida: number;
  cantidadRecibida: number;
  precioUnitarioCostoReal: number;
}

interface FaltanteProveedorGrupo {
  proveedorId: number | null;
  proveedorNombre: string;
  items: {
    producto: ProductoDto;
    cantidadSugerida: number;
  }[];
}

const PAGE_SIZE = 10;

function getEstadoOcBadge(
  estado: EstadoOrdenCompraEnum,
  estadoNombre?: string
): { label: string; variant: 'neutral' | 'info' | 'success' | 'danger' } {
  switch (estado) {
    case EstadoOrdenCompraEnum.Borrador:
      return { label: estadoNombre || 'Borrador', variant: 'neutral' };
    case EstadoOrdenCompraEnum.Emitida:
      return { label: estadoNombre || 'Emitida', variant: 'info' };
    case EstadoOrdenCompraEnum.Recibida:
      return { label: estadoNombre || 'Recibida', variant: 'success' };
    case EstadoOrdenCompraEnum.Cancelada:
      return { label: estadoNombre || 'Cancelada', variant: 'danger' };
    default:
      return { label: estadoNombre || 'Desconocido', variant: 'neutral' };
  }
}

function getMovimientoBadgeVariant(
  tipo: TipoMovimientoStockEnum
): 'success' | 'info' | 'warning' | 'danger' {
  switch (tipo) {
    case TipoMovimientoStockEnum.IngresoCompra:
      return 'success';
    case TipoMovimientoStockEnum.EgresoVentaMostrador:
      return 'info';
    case TipoMovimientoStockEnum.ConsumoInternoSalon:
      return 'warning';
    case TipoMovimientoStockEnum.AjusteRoturaPerdida:
      return 'danger';
    default:
      return 'warning';
  }
}

function sanitizeWhatsAppPhone(rawPhone?: string | null): string {
  if (!rawPhone) return '';
  return rawPhone.replace(/[^\d]/g, '');
}

export default function AdminInventarioComprasPage() {
  const { configSalon, catalogos } = useCatalogos();
  const { sesionCajaActual } = useCajaPos();
  const moneda = configSalon?.moneda || configSalon?.monedaIso || 'ARS';

  const [subTab, setSubTab] = useState<InventarioTab>('productos');

  // Filtros Productos
  const [busquedaProd, setBusquedaProd] = useState<string>('');
  const [filtroCategoriaId, setFiltroCategoriaId] = useState<number | ''>('');
  const [soloBajoStock, setSoloBajoStock] = useState<boolean>(false);
  const [pageProd, setPageProd] = useState<number>(1);

  // Filtros Proveedores
  const [busquedaProv, setBusquedaProv] = useState<string>('');
  const [pageProv, setPageProv] = useState<number>(1);

  // Filtros Órdenes de Compra
  const [filtroOcProveedorId, setFiltroOcProveedorId] = useState<
    number | undefined
  >(undefined);
  const [filtroOcEstado, setFiltroOcEstado] = useState<
    EstadoOrdenCompraEnum | undefined
  >(undefined);
  const [pageOc, setPageOc] = useState<number>(1);

  // Datos principales
  const [productos, setProductos] = useState<ProductoDto[]>([]);
  const [proveedores, setProveedores] = useState<ProveedorDto[]>([]);
  const [ordenes, setOrdenes] = useState<OrdenCompraDto[]>([]);
  const [cargando, setCargando] = useState<boolean>(true);
  const [errorCarga, setErrorCarga] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);

  // Modal Producto
  const [modalProdOpen, setModalProdOpen] = useState<boolean>(false);
  const [prodEditando, setProdEditando] = useState<ProductoDto | null>(null);
  const [categoriaId, setCategoriaId] = useState<number>(1);
  const [proveedorProdId, setProveedorProdId] = useState<number | null>(null);
  const [nombreProd, setNombreProd] = useState<string>('');
  const [codigoBarras, setCodigoBarras] = useState<string>('');
  const [precioCosto, setPrecioCosto] = useState<number | null>(null);
  const [precioVenta, setPrecioVenta] = useState<number | null>(null);
  const [stockActual, setStockActual] = useState<number | null>(null);
  const [stockMinimo, setStockMinimo] = useState<number | null>(5);
  const [esParaVenta, setEsParaVenta] = useState<boolean>(true);
  const [guardandoProd, setGuardandoProd] = useState<boolean>(false);

  // Historial de Stock y Ajuste Manual
  const [historialProducto, setHistorialProducto] = useState<{
    producto: ProductoDto;
    movimientos: KardexItemResponseDto[];
  } | null>(null);
  const [modalAjusteOpen, setModalAjusteOpen] = useState<ProductoDto | null>(
    null
  );
  const [tipoMov, setTipoMov] = useState<TipoMovimientoStockEnum>(
    TipoMovimientoStockEnum.AjusteRoturaPerdida
  );
  const [cantidadMov, setCantidadMov] = useState<number | null>(1);
  const [motivoMov, setMotivoMov] = useState<string>('');
  const [guardandoMov, setGuardandoMov] = useState<boolean>(false);

  // Modal Proveedor y Productos Vinculados
  const [modalProvOpen, setModalProvOpen] = useState<boolean>(false);
  const [provEditando, setProvEditando] = useState<ProveedorDto | null>(null);
  const [razonSocial, setRazonSocial] = useState<string>('');
  const [cuitProv, setCuitProv] = useState<string>('');
  const [contactoProv, setContactoProv] = useState<string>('');
  const [telefonoProv, setTelefonoProv] = useState<string>('');
  const [emailProv, setEmailProv] = useState<string>('');
  const [guardandoProv, setGuardandoProv] = useState<boolean>(false);

  const [provProductosModal, setProvProductosModal] = useState<{
    proveedor: ProveedorDto;
    productos: ProductoDto[];
    cargando: boolean;
  } | null>(null);

  // Modal Nueva OC + Auto-OC con Faltantes
  const [modalNuevaOcOpen, setModalNuevaOcOpen] = useState<boolean>(false);
  const [ocProveedorId, setOcProveedorId] = useState<number>(0);
  const [ocComprobante, setOcComprobante] = useState<string>('');
  const [ocObservaciones, setOcObservaciones] = useState<string>('');
  const [ocRenglones, setOcRenglones] = useState<OcRenglonForm[]>([]);
  const [guardandoOc, setGuardandoOc] = useState<boolean>(false);

  const [cargandoFaltantes, setCargandoFaltantes] = useState<boolean>(false);
  const [modalFaltantesOpen, setModalFaltantesOpen] = useState<boolean>(false);
  const [gruposFaltantes, setGruposFaltantes] = useState<
    FaltanteProveedorGrupo[]
  >([]);

  // Modal Recepcionar OC
  const [ordenARecepcionar, setOrdenARecepcionar] =
    useState<OrdenCompraDto | null>(null);
  const [comprobanteRecepcion, setComprobanteRecepcion] = useState<string>('');
  const [actualizarPreciosCosto, setActualizarPreciosCosto] =
    useState<boolean>(true);
  const [imputarPagoEnCaja, setImputarPagoEnCaja] = useState<boolean>(false);
  const [itemsRecepcion, setItemsRecepcion] = useState<RecepcionItemForm[]>([]);
  const [recepcionandoOc, setRecepcionandoOc] = useState<boolean>(false);

  // Impresión PDF de OC
  const [ocImprimible, setOcImprimible] = useState<PrintableReceiptData | null>(
    null
  );

  // Confirmaciones destructivas
  const [prodAEliminar, setProdAEliminar] = useState<ProductoDto | null>(null);
  const [provAEliminar, setProvAEliminar] = useState<ProveedorDto | null>(null);
  const [ocACancelar, setOcACancelar] = useState<OrdenCompraDto | null>(null);
  const [ejecutandoBaja, setEjecutandoBaja] = useState<boolean>(false);

  const cargarTodo = useCallback(async () => {
    setCargando(true);
    setErrorCarga(null);
    try {
      const [pRes, provRes, ordRes] = await Promise.all([
        productosService.getAll(soloBajoStock),
        proveedoresService.getAll(),
        ordenesCompraService.getAll(filtroOcProveedorId, filtroOcEstado),
      ]);

      if (pRes.success) {
        setProductos(extractPagedItems<ProductoDto>(pRes.data));
      }
      if (provRes.success) {
        const provItems = extractPagedItems<ProveedorDto>(provRes.data);
        setProveedores(provItems);
        if (provItems.length > 0) {
          setOcProveedorId((prev) => (prev > 0 ? prev : provItems[0].id));
        }
      }
      if (ordRes.success) {
        setOrdenes(extractPagedItems<OrdenCompraDto>(ordRes.data));
      }
    } catch (err) {
      setErrorCarga({
        message: extractErrorMessage(
          err,
          'No se pudieron obtener los datos de inventario, proveedores y órdenes de compra.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
      notifyApiError(err, {
        fallbackMessage: 'Error al cargar los datos de inventario.',
      });
    } finally {
      setCargando(false);
    }
  }, [soloBajoStock, filtroOcProveedorId, filtroOcEstado]);

  useEffect(() => {
    void cargarTodo();
  }, [cargarTodo]);

  // Reset de paginación al cambiar filtros
  useEffect(() => {
    setPageProd(1);
  }, [busquedaProd, filtroCategoriaId, soloBajoStock]);

  useEffect(() => {
    setPageProv(1);
  }, [busquedaProv]);

  useEffect(() => {
    setPageOc(1);
  }, [filtroOcProveedorId, filtroOcEstado]);

  const categoriasProducto = useMemo(
    () =>
      catalogos?.categoriasProducto && catalogos.categoriasProducto.length > 0
        ? catalogos.categoriasProducto
        : [{ id: 1, nombre: 'General' }],
    [catalogos]
  );

  // Listas filtradas y paginadas
  const productosFiltrados = useMemo(() => {
    const q = busquedaProd.trim().toLowerCase();
    return productos.filter((p) => {
      if (filtroCategoriaId !== '' && p.categoriaId !== filtroCategoriaId) {
        return false;
      }
      if (!q) return true;
      const nombre = (p.nombre || '').toLowerCase();
      const codigo = (p.codigoBarras || p.codigoBarra || '').toLowerCase();
      const categoria = (p.categoriaNombre || '').toLowerCase();
      return nombre.includes(q) || codigo.includes(q) || categoria.includes(q);
    });
  }, [productos, busquedaProd, filtroCategoriaId]);

  const totalPagesProd = Math.max(
    1,
    Math.ceil(productosFiltrados.length / PAGE_SIZE)
  );
  const productosPagina = useMemo(() => {
    const start = (pageProd - 1) * PAGE_SIZE;
    return productosFiltrados.slice(start, start + PAGE_SIZE);
  }, [productosFiltrados, pageProd]);

  const proveedoresFiltrados = useMemo(() => {
    const q = busquedaProv.trim().toLowerCase();
    if (!q) return proveedores;
    return proveedores.filter((prov) => {
      const rs = (prov.razonSocial || '').toLowerCase();
      const cuit = (prov.cuit || '').toLowerCase();
      const contacto = (prov.contacto || '').toLowerCase();
      const tel = (prov.telefono || '').toLowerCase();
      return (
        rs.includes(q) ||
        cuit.includes(q) ||
        contacto.includes(q) ||
        tel.includes(q)
      );
    });
  }, [proveedores, busquedaProv]);

  const totalPagesProv = Math.max(
    1,
    Math.ceil(proveedoresFiltrados.length / PAGE_SIZE)
  );
  const proveedoresPagina = useMemo(() => {
    const start = (pageProv - 1) * PAGE_SIZE;
    return proveedoresFiltrados.slice(start, start + PAGE_SIZE);
  }, [proveedoresFiltrados, pageProv]);

  const totalPagesOc = Math.max(1, Math.ceil(ordenes.length / PAGE_SIZE));
  const ordenesPagina = useMemo(() => {
    const start = (pageOc - 1) * PAGE_SIZE;
    return ordenes.slice(start, start + PAGE_SIZE);
  }, [ordenes, pageOc]);

  const totalNuevaOc = useMemo(
    () =>
      ocRenglones.reduce(
        (acc, r) => acc + (r.cantidadPedida || 0) * (r.precioUnitarioCosto || 0),
        0
      ),
    [ocRenglones]
  );

  // Handlers de Productos
  const abrirNuevoProducto = () => {
    setProdEditando(null);
    setCategoriaId(categoriasProducto[0]?.id || 1);
    setProveedorProdId(null);
    setNombreProd('');
    setCodigoBarras('');
    setPrecioCosto(null);
    setPrecioVenta(null);
    setStockActual(null);
    setStockMinimo(5);
    setEsParaVenta(true);
    setModalProdOpen(true);
  };

  const abrirEditarProducto = (p: ProductoDto) => {
    setProdEditando(p);
    setCategoriaId(p.categoriaId || categoriasProducto[0]?.id || 1);
    setProveedorProdId(p.proveedorId ?? null);
    setNombreProd(p.nombre);
    setCodigoBarras(p.codigoBarras || p.codigoBarra || '');
    setPrecioCosto(p.precioCosto);
    setPrecioVenta(p.precioVenta);
    setStockActual(p.stockActual);
    setStockMinimo(p.stockMinimoAlerta);
    setEsParaVenta(p.esParaVenta);
    setModalProdOpen(true);
  };

  const handleGuardarProducto = async (e: React.FormEvent) => {
    e.preventDefault();
    setGuardandoProd(true);
    const payload = {
      categoriaId,
      proveedorId: proveedorProdId,
      codigoBarras: codigoBarras.trim() || null,
      nombre: nombreProd.trim(),
      precioCosto: precioCosto ?? 0,
      precioVenta: precioVenta ?? 0,
      stockActual: stockActual ?? 0,
      stockMinimoAlerta: stockMinimo ?? 0,
      esParaVenta,
    };

    try {
      const res = prodEditando
        ? await productosService.update(prodEditando.id, payload)
        : await productosService.create(payload);

      if (res.success) {
        notifyApiSuccess(res, 'Producto guardado correctamente.');
        setModalProdOpen(false);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el producto.',
      });
    } finally {
      setGuardandoProd(false);
    }
  };

  const confirmarEliminarProducto = async () => {
    if (!prodAEliminar) return;
    setEjecutandoBaja(true);
    try {
      const res = await productosService.delete(prodAEliminar.id);
      if (res.success) {
        notifyApiSuccess(res, `Producto "${prodAEliminar.nombre}" eliminado.`);
        setProdAEliminar(null);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar el producto.',
      });
    } finally {
      setEjecutandoBaja(false);
    }
  };

  const abrirHistorialStock = async (prod: ProductoDto) => {
    try {
      const res = await productosService.getKardex(prod.id);
      if (res.success) {
        setHistorialProducto({
          producto: prod,
          movimientos: extractPagedItems<KardexItemResponseDto>(res.data),
        });
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo consultar el historial de stock.',
      });
    }
  };

  const handleRegistrarMovimientoStock = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!modalAjusteOpen) return;
    setGuardandoMov(true);
    try {
      const res = await productosService.registrarMovimiento({
        productoId: modalAjusteOpen.id,
        tipoMovimiento: tipoMov,
        cantidad: Math.max(1, cantidadMov ?? 1),
        motivo: motivoMov.trim(),
      });
      if (res.success) {
        notifyApiSuccess(res, 'Movimiento de stock registrado correctamente.');
        setModalAjusteOpen(null);
        setCantidadMov(1);
        setMotivoMov('');
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo registrar el movimiento de stock.',
      });
    } finally {
      setGuardandoMov(false);
    }
  };

  // Handlers de Proveedores
  const abrirNuevoProveedor = () => {
    setProvEditando(null);
    setRazonSocial('');
    setCuitProv('');
    setContactoProv('');
    setTelefonoProv('');
    setEmailProv('');
    setModalProvOpen(true);
  };

  const abrirEditarProveedor = (prov: ProveedorDto) => {
    setProvEditando(prov);
    setRazonSocial(prov.razonSocial);
    setCuitProv(prov.cuit || '');
    setContactoProv(prov.contacto || '');
    setTelefonoProv(prov.telefono || '');
    setEmailProv(prov.email || '');
    setModalProvOpen(true);
  };

  const handleGuardarProveedor = async (e: React.FormEvent) => {
    e.preventDefault();
    setGuardandoProv(true);
    const payload = {
      razonSocial: razonSocial.trim(),
      cuit: cuitProv.trim() || null,
      contacto: contactoProv.trim() || undefined,
      telefono: telefonoProv.trim() || undefined,
      email: emailProv.trim() || undefined,
    };
    try {
      const res = provEditando
        ? await proveedoresService.update(provEditando.id, payload)
        : await proveedoresService.create(payload);
      if (res.success) {
        notifyApiSuccess(res, 'Proveedor guardado correctamente.');
        setModalProvOpen(false);
        setProvEditando(null);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el proveedor.',
      });
    } finally {
      setGuardandoProv(false);
    }
  };

  const confirmarEliminarProveedor = async () => {
    if (!provAEliminar) return;
    if ((provAEliminar.cantidadProductosAsociados ?? 0) > 0) {
      notifyApiError(
        new Error(
          `No se puede dar de baja al proveedor "${provAEliminar.razonSocial}" porque posee ${provAEliminar.cantidadProductosAsociados} artículo(s) vinculado(s).`
        )
      );
      setProvAEliminar(null);
      return;
    }
    setEjecutandoBaja(true);
    try {
      const res = await proveedoresService.delete(provAEliminar.id);
      if (res.success) {
        notifyApiSuccess(
          res,
          `Proveedor "${provAEliminar.razonSocial}" eliminado.`
        );
        setProvAEliminar(null);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar el proveedor.',
      });
    } finally {
      setEjecutandoBaja(false);
    }
  };

  const abrirProductosVinculadosProveedor = async (prov: ProveedorDto) => {
    setProvProductosModal({
      proveedor: prov,
      productos: [],
      cargando: true,
    });
    try {
      const res = await productosService.getPaged(
        { pageNumber: 1, pageSize: 100 },
        false,
        undefined,
        prov.id
      );
      if (res.success) {
        setProvProductosModal({
          proveedor: prov,
          productos: extractPagedItems<ProductoDto>(res.data),
          cargando: false,
        });
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage:
          'No se pudieron obtener los productos vinculados al proveedor.',
      });
      setProvProductosModal(null);
    }
  };

  // Handlers de Órdenes de Compra
  const abrirNuevaOrdenCompra = () => {
    const defaultProvId = ocProveedorId || proveedores[0]?.id || 0;
    setOcProveedorId(defaultProvId);
    setOcComprobante('');
    setOcObservaciones('');
    setOcRenglones(
      productos.length > 0
        ? [
            {
              productoId: productos[0].id,
              cantidadPedida: 10,
              precioUnitarioCosto: productos[0].precioCosto || 0,
            },
          ]
        : []
    );
    setModalNuevaOcOpen(true);
  };

  const agregarRenglonOc = () => {
    const usados = new Set(ocRenglones.map((r) => r.productoId));
    const siguienteProd =
      productos.find((p) => !usados.has(p.id)) || productos[0];
    if (!siguienteProd) return;
    setOcRenglones((prev) => [
      ...prev,
      {
        productoId: siguienteProd.id,
        cantidadPedida: 1,
        precioUnitarioCosto: siguienteProd.precioCosto || 0,
      },
    ]);
  };

  const quitarRenglonOc = (idx: number) => {
    setOcRenglones((prev) => prev.filter((_, i) => i !== idx));
  };

  const precargarOrdenDesdeGrupo = (grupo: FaltanteProveedorGrupo) => {
    const targetProvId = grupo.proveedorId || proveedores[0]?.id || 0;
    setOcProveedorId(targetProvId);
    setOcComprobante('');
    setOcObservaciones('Reposición sugerida automáticamente por stock crítico');
    setOcRenglones(
      grupo.items.map((it) => ({
        productoId: it.producto.id,
        cantidadPedida: it.cantidadSugerida,
        precioUnitarioCosto: it.producto.precioCosto || 0,
      }))
    );
    setModalFaltantesOpen(false);
    setModalNuevaOcOpen(true);
  };

  const handleGenerarOcConFaltantes = async () => {
    setCargandoFaltantes(true);
    try {
      const res = await productosService.getStockCritico();
      const criticos = res.success
        ? extractPagedItems<ProductoDto>(res.data)
        : [];
      if (criticos.length === 0) {
        notifyApiSuccess(
          { success: true, message: 'No hay artículos con stock crítico para reponer.', data: null },
          'No hay artículos con stock crítico para reponer.'
        );
        return;
      }

      const mapGrupos = new Map<string, FaltanteProveedorGrupo>();
      criticos.forEach((prod) => {
        const provId = prod.proveedorId ?? null;
        const key = provId ? String(provId) : 'sin-proveedor';
        const provNombre =
          prod.proveedorRazonSocial ||
          prod.proveedorNombre ||
          proveedores.find((pr) => pr.id === provId)?.razonSocial ||
          'Sin proveedor asignado';
        const sugerida = Math.max(
          1,
          prod.stockMinimoAlerta * 2 - prod.stockActual
        );

        const existing = mapGrupos.get(key);
        if (existing) {
          existing.items.push({ producto: prod, cantidadSugerida: sugerida });
        } else {
          mapGrupos.set(key, {
            proveedorId: provId,
            proveedorNombre: provNombre,
            items: [{ producto: prod, cantidadSugerida: sugerida }],
          });
        }
      });

      const listaGrupos = Array.from(mapGrupos.values());
      if (listaGrupos.length === 1) {
        precargarOrdenDesdeGrupo(listaGrupos[0]);
      } else {
        setGruposFaltantes(listaGrupos);
        setModalFaltantesOpen(true);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo consultar el stock crítico para generar la OC.',
      });
    } finally {
      setCargandoFaltantes(false);
    }
  };

  const handleCrearOrdenCompra = async (e: React.FormEvent) => {
    e.preventDefault();
    const targetProveedorId = ocProveedorId || proveedores[0]?.id || 0;
    if (ocRenglones.length === 0 || !targetProveedorId) return;
    setGuardandoOc(true);
    try {
      const res = await ordenesCompraService.crear({
        proveedorId: targetProveedorId,
        numeroComprobanteProveedor: ocComprobante.trim() || undefined,
        observaciones: ocObservaciones.trim() || undefined,
        detalles: ocRenglones.map((r) => ({
          productoId: r.productoId,
          cantidadPedida: Math.max(1, r.cantidadPedida),
          precioUnitarioCosto: Math.max(0, r.precioUnitarioCosto),
        })),
      });
      if (res.success) {
        notifyApiSuccess(res, `Orden de Compra #${res.data?.id} emitida.`);
        setModalNuevaOcOpen(false);
        setOcRenglones([]);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo emitir la orden de compra.',
      });
    } finally {
      setGuardandoOc(false);
    }
  };

  const abrirModalRecepcion = async (ocId: number) => {
    try {
      const res = await ordenesCompraService.getById(ocId);
      if (res.success && res.data) {
        const oc = res.data;
        setOrdenARecepcionar(oc);
        setComprobanteRecepcion(oc.numeroComprobanteProveedor || '');
        setImputarPagoEnCaja(false);
        setItemsRecepcion(
          extractPagedItems(oc.detalles).map((d) => ({
            detalleOrdenCompraId: d.id,
            productoId: d.productoId,
            productoNombre: d.productoNombre || `Artículo #${d.productoId}`,
            cantidadPedida: d.cantidadPedida,
            cantidadRecibida: d.cantidadPedida,
            precioUnitarioCostoReal: d.precioUnitarioCosto,
          }))
        );
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo cargar el detalle de la orden de compra.',
      });
    }
  };

  const confirmarRecepcionMercaderia = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!ordenARecepcionar) return;
    setRecepcionandoOc(true);
    try {
      const res = await ordenesCompraService.recepcionar(ordenARecepcionar.id, {
        numeroComprobanteProveedor: comprobanteRecepcion.trim() || undefined,
        actualizarPreciosCosto,
        imputarPagoEnCaja,
        sesionCajaId: imputarPagoEnCaja ? sesionCajaActual?.id : null,
        items: itemsRecepcion.map((item) => ({
          detalleOrdenCompraId: item.detalleOrdenCompraId,
          productoId: item.productoId,
          cantidadRecibida: Math.max(0, item.cantidadRecibida),
          precioUnitarioCostoReal: Math.max(0, item.precioUnitarioCostoReal),
        })),
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          'Mercadería recepcionada y stock actualizado correctamente.'
        );
        setOrdenARecepcionar(null);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo recepcionar la orden de compra.',
      });
    } finally {
      setRecepcionandoOc(false);
    }
  };

  const confirmarCancelarOrden = async () => {
    if (!ocACancelar) return;
    setEjecutandoBaja(true);
    try {
      const res = await ordenesCompraService.cancelar(
        ocACancelar.id,
        'Cancelación administrativa en panel de compras'
      );
      if (res.success) {
        notifyApiSuccess(res, `Orden de Compra #${ocACancelar.id} cancelada.`);
        setOcACancelar(null);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo cancelar la orden de compra.',
      });
    } finally {
      setEjecutandoBaja(false);
    }
  };

  const abrirImpresionOc = async (oc: OrdenCompraDto) => {
    try {
      let fullOc = oc;
      if (!oc.detalles || oc.detalles.length === 0) {
        const res = await ordenesCompraService.getById(oc.id);
        if (res.success && res.data) {
          fullOc = res.data;
        }
      }
      const detalles = extractPagedItems(fullOc.detalles);
      const prov = proveedores.find((p) => p.id === fullOc.proveedorId);

      const receipt: PrintableReceiptData = {
        titulo: 'Orden de Compra',
        numeroComprobante: `OC-${String(fullOc.id).padStart(5, '0')}`,
        fechaIso: fullOc.fechaEmision,
        clienteOProfesional: prov?.cuit
          ? `${fullOc.proveedorRazonSocial} (CUIT: ${prov.cuit})`
          : fullOc.proveedorRazonSocial,
        monedaIso: moneda,
        renglones:
          detalles.length > 0
            ? detalles.map((d) => ({
                descripcion:
                  d.productoNombre || `Artículo #${d.productoId}`,
                cantidad: d.cantidadPedida,
                monto:
                  d.subtotal || d.cantidadPedida * d.precioUnitarioCosto,
              }))
            : [
                {
                  descripcion: 'Pedido consolidado de mercadería',
                  monto: fullOc.total,
                },
              ],
        pagosODesglose: [
          ...(fullOc.numeroComprobanteProveedor
            ? [
                {
                  etiqueta: `Ref. Comprobante: ${fullOc.numeroComprobanteProveedor}`,
                  monto: fullOc.total,
                },
              ]
            : []),
        ],
        totalFinal: fullOc.total,
        pieDePagina:
          fullOc.observaciones ||
          `Estado: ${getEstadoOcBadge(fullOc.estado, fullOc.estadoNombre).label} • Documento comercial para proveedor.`,
      };
      setOcImprimible(receipt);
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo preparar la vista imprimible de la orden.',
      });
    }
  };

  const enviarOcPorWhatsApp = async (oc: OrdenCompraDto) => {
    try {
      let fullOc = oc;
      if (!oc.detalles || oc.detalles.length === 0) {
        const res = await ordenesCompraService.getById(oc.id);
        if (res.success && res.data) {
          fullOc = res.data;
        }
      }
      const detalles = extractPagedItems(fullOc.detalles);
      const prov = proveedores.find((p) => p.id === fullOc.proveedorId);
      const phone = sanitizeWhatsAppPhone(prov?.telefono);
      const nombreSalon =
        configSalon?.nombreComercial ||
        configSalon?.nombreSalon ||
        'Mi Peluquería Central';

      const lineasProductos =
        detalles.length > 0
          ? detalles
              .map(
                (d) =>
                  `- ${d.cantidadPedida}x ${
                    d.productoNombre || `Artículo #${d.productoId}`
                  }`
              )
              .join('\n')
          : '- Ver detalle adjunto de la orden.';

      const mensaje = [
        `Hola ${fullOc.proveedorRazonSocial},`,
        `Te enviamos la Orden de Compra #${fullOc.id} desde *${nombreSalon}*:`,
        '',
        lineasProductos,
        '',
        `Total estimado: ${formatCurrency(fullOc.total, moneda)}`,
        fullOc.observaciones ? `Observaciones: ${fullOc.observaciones}` : '',
        '¡Muchas gracias!',
      ]
        .filter(Boolean)
        .join('\n');

      const encoded = encodeURIComponent(mensaje);
      const url = phone
        ? `https://wa.me/${phone}?text=${encoded}`
        : `https://wa.me/?text=${encoded}`;

      if (typeof window !== 'undefined') {
        window.open(url, '_blank', 'noopener,noreferrer');
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo armar el mensaje de WhatsApp.',
      });
    }
  };

  return (
    <div className="space-y-5 max-w-[1440px] mx-auto">
      {/* Cabecera de Sección + Tabs Limpias */}
      <section className="rounded-xl border border-border-subtle bg-surface p-4 lg:p-5 shadow-sm">
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
          <div className="min-w-0">
            <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary">
              Inventario y Compras
            </h1>
            <p className="text-xs sm:text-sm text-ink-secondary mt-0.5">
              Gestión de stock auditado, proveedores y órdenes de reposición.
            </p>
          </div>

          <div
            role="tablist"
            aria-label="Secciones de inventario"
            className="inline-flex flex-wrap items-center rounded-lg border border-border-subtle bg-elevated p-1 gap-1"
          >
            {(
              [
                {
                  id: 'productos',
                  label: 'Productos',
                  count: productos.length,
                  icon: Package,
                },
                {
                  id: 'proveedores',
                  label: 'Proveedores',
                  count: proveedores.length,
                  icon: Building2,
                },
                {
                  id: 'ordenes',
                  label: 'Órdenes de Compra',
                  count: ordenes.length,
                  icon: ClipboardList,
                },
              ] as const
            ).map((tab) => {
              const Icon = tab.icon;
              const active = subTab === tab.id;
              return (
                <button
                  key={tab.id}
                  role="tab"
                  type="button"
                  aria-selected={active}
                  onClick={() => setSubTab(tab.id)}
                  className={`inline-flex items-center gap-2 h-8 rounded-md px-3 text-xs font-medium transition-colors whitespace-nowrap ${
                    active
                      ? 'bg-surface text-ink-primary shadow-sm ring-1 ring-border-subtle'
                      : 'text-ink-secondary hover:text-ink-primary'
                  }`}
                >
                  <Icon className="h-3.5 w-3.5" aria-hidden="true" />
                  <span>{tab.label}</span>
                  <span className="font-mono-tabular text-[11px] px-1.5 py-0.2 rounded bg-elevated text-ink-secondary">
                    {tab.count}
                  </span>
                </button>
              );
            })}
          </div>
        </div>
      </section>

      {/* ================================================================== */}
      {/* TAB 1: PRODUCTOS                                                   */}
      {/* ================================================================== */}
      {subTab === 'productos' && (
        <section className="space-y-4">
          {/* Toolbar de búsqueda y filtros */}
          <div className="rounded-xl border border-border-subtle bg-surface p-3.5 shadow-sm flex flex-wrap items-center justify-between gap-3">
            <div className="flex flex-wrap items-center gap-2.5 flex-1 min-w-0">
              <div className="relative flex-1 min-w-[200px] max-w-xs">
                <Search
                  className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-secondary"
                  aria-hidden="true"
                />
                <input
                  type="text"
                  value={busquedaProd}
                  onChange={(e) => setBusquedaProd(e.target.value)}
                  placeholder="Buscar por nombre o código..."
                  className="h-9 w-full rounded-lg border border-border-strong bg-surface pl-9 pr-8 text-xs text-ink-primary placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                />
                {busquedaProd && (
                  <button
                    type="button"
                    aria-label="Limpiar búsqueda"
                    onClick={() => setBusquedaProd('')}
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-ink-secondary hover:text-ink-primary"
                  >
                    <X className="h-3.5 w-3.5" aria-hidden="true" />
                  </button>
                )}
              </div>

              <select
                aria-label="Filtrar por categoría"
                value={filtroCategoriaId}
                onChange={(e) =>
                  setFiltroCategoriaId(
                    e.target.value ? Number(e.target.value) : ''
                  )
                }
                className="h-9 rounded-lg border border-border-strong bg-surface px-3 text-xs text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                <option value="">Todas las categorías</option>
                {categoriasProducto.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nombre}
                  </option>
                ))}
              </select>

              <label className="inline-flex items-center gap-2 h-9 px-3 rounded-lg border border-border-subtle bg-elevated/60 text-xs font-medium text-ink-primary cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={soloBajoStock}
                  onChange={(e) => setSoloBajoStock(e.target.checked)}
                  className="rounded border-border-strong text-brand-primary focus:ring-brand-accent"
                />
                <span>Solo stock crítico</span>
              </label>
            </div>

            <Button
              variant="primary"
              size="sm"
              leftIcon={<Plus className="h-4 w-4" aria-hidden="true" />}
              onClick={abrirNuevoProducto}
            >
              Nuevo Producto
            </Button>
          </div>

          <AsyncStateGuard
            isLoading={cargando}
            isError={Boolean(errorCarga)}
            errorMessage={errorCarga?.message}
            correlationId={errorCarga?.correlationId}
            isEmpty={productosFiltrados.length === 0}
            loadingText="Cargando catálogo de productos..."
            emptyTitle="Sin productos para mostrar"
            emptyDescription="No hay artículos que coincidan con los filtros seleccionados."
            onRetry={cargarTodo}
          >
            <div className="rounded-xl border border-border-subtle bg-surface shadow-sm overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="bg-elevated text-ink-secondary border-b border-border-subtle">
                    <tr>
                      <th className="px-4 py-3 font-medium">Código</th>
                      <th className="px-4 py-3 font-medium">Artículo</th>
                      <th className="px-4 py-3 font-medium">Uso</th>
                      <th className="px-4 py-3 font-medium text-right">
                        Costo / Venta
                      </th>
                      <th className="px-4 py-3 font-medium">Stock</th>
                      <th className="px-4 py-3 font-medium text-right">
                        Acciones
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {productosPagina.map((p) => {
                      const esCritico = p.stockActual <= p.stockMinimoAlerta;
                      return (
                        <tr
                          key={p.id}
                          className="hover:bg-elevated/50 transition-colors"
                        >
                          <td className="px-4 py-3 font-mono-tabular text-ink-secondary whitespace-nowrap">
                            {p.codigoBarras || p.codigoBarra || '—'}
                          </td>
                          <td className="px-4 py-3 min-w-[180px]">
                            <p className="font-semibold text-ink-primary">
                              {p.nombre}
                            </p>
                            <p className="text-[11px] text-ink-secondary">
                              {p.categoriaNombre || 'General'}
                              {p.proveedorRazonSocial
                                ? ` · ${p.proveedorRazonSocial}`
                                : ''}
                            </p>
                          </td>
                          <td className="px-4 py-3 whitespace-nowrap">
                            <Badge
                              variant={p.esParaVenta ? 'info' : 'neutral'}
                            >
                              {p.esParaVenta ? 'Reventa' : 'Gabinete'}
                            </Badge>
                          </td>
                          <td className="px-4 py-3 text-right font-mono-tabular whitespace-nowrap">
                            <span className="text-ink-secondary">
                              {formatCurrency(p.precioCosto, moneda)}
                            </span>
                            <span className="mx-1 text-ink-secondary/60">/</span>
                            <span className="font-semibold text-ink-primary">
                              {formatCurrency(p.precioVenta, moneda)}
                            </span>
                          </td>
                          <td className="px-4 py-3 whitespace-nowrap">
                            <Badge
                              variant={esCritico ? 'danger' : 'success'}
                              className="font-mono-tabular"
                            >
                              {p.stockActual} u. (Mín: {p.stockMinimoAlerta})
                            </Badge>
                          </td>
                          <td className="px-4 py-3 text-right whitespace-nowrap">
                            <div className="inline-flex items-center gap-1.5">
                              <Button
                                variant="secondary"
                                size="sm"
                                leftIcon={
                                  <Pencil
                                    className="h-3.5 w-3.5"
                                    aria-hidden="true"
                                  />
                                }
                                onClick={() => abrirEditarProducto(p)}
                              >
                                Editar
                              </Button>
                              <Button
                                variant="secondary"
                                size="sm"
                                leftIcon={
                                  <ArrowLeftRight
                                    className="h-3.5 w-3.5"
                                    aria-hidden="true"
                                  />
                                }
                                onClick={() => {
                                  setCantidadMov(1);
                                  setMotivoMov('');
                                  setModalAjusteOpen(p);
                                }}
                              >
                                Movimiento
                              </Button>
                              <Button
                                variant="secondary"
                                size="sm"
                                title="Movimientos auditados de stock"
                                leftIcon={
                                  <History
                                    className="h-3.5 w-3.5"
                                    aria-hidden="true"
                                  />
                                }
                                onClick={() => abrirHistorialStock(p)}
                              >
                                Historial
                              </Button>
                              <Button
                                variant="destructive"
                                destructiveTone="soft"
                                size="sm"
                                aria-label={`Eliminar ${p.nombre}`}
                                onClick={() => setProdAEliminar(p)}
                              >
                                <Trash2
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
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

            {productosFiltrados.length > PAGE_SIZE && (
              <PaginationBar
                pageNumber={pageProd}
                totalPages={totalPagesProd}
                totalCount={productosFiltrados.length}
                itemLabel="productos"
                onPageChange={setPageProd}
              />
            )}
          </AsyncStateGuard>
        </section>
      )}

      {/* ================================================================== */}
      {/* TAB 2: PROVEEDORES                                                 */}
      {/* ================================================================== */}
      {subTab === 'proveedores' && (
        <section className="space-y-4">
          <div className="rounded-xl border border-border-subtle bg-surface p-3.5 shadow-sm flex flex-wrap items-center justify-between gap-3">
            <div className="relative flex-1 min-w-[220px] max-w-sm">
              <Search
                className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-secondary"
                aria-hidden="true"
              />
              <input
                type="text"
                value={busquedaProv}
                onChange={(e) => setBusquedaProv(e.target.value)}
                placeholder="Buscar por razón social, CUIT o contacto..."
                className="h-9 w-full rounded-lg border border-border-strong bg-surface pl-9 pr-8 text-xs text-ink-primary placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              />
              {busquedaProv && (
                <button
                  type="button"
                  aria-label="Limpiar búsqueda"
                  onClick={() => setBusquedaProv('')}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-ink-secondary hover:text-ink-primary"
                >
                  <X className="h-3.5 w-3.5" aria-hidden="true" />
                </button>
              )}
            </div>

            <Button
              variant="primary"
              size="sm"
              leftIcon={<Plus className="h-4 w-4" aria-hidden="true" />}
              onClick={abrirNuevoProveedor}
            >
              Nuevo Proveedor
            </Button>
          </div>

          <AsyncStateGuard
            isLoading={cargando}
            isError={Boolean(errorCarga)}
            errorMessage={errorCarga?.message}
            correlationId={errorCarga?.correlationId}
            isEmpty={proveedoresFiltrados.length === 0}
            loadingText="Cargando proveedores registrados..."
            emptyTitle="Sin proveedores para mostrar"
            emptyDescription="Registrá un proveedor o ajustá el criterio de búsqueda."
            onRetry={cargarTodo}
          >
            <div className="rounded-xl border border-border-subtle bg-surface shadow-sm overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="bg-elevated text-ink-secondary border-b border-border-subtle">
                    <tr>
                      <th className="px-4 py-3 font-medium">
                        Razón Social / CUIT
                      </th>
                      <th className="px-4 py-3 font-medium">
                        Contacto / Teléfono
                      </th>
                      <th className="px-4 py-3 font-medium">
                        Productos Vinculados
                      </th>
                      <th className="px-4 py-3 font-medium text-right">
                        Acciones
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {proveedoresPagina.map((prov) => (
                      <tr
                        key={prov.id}
                        className="hover:bg-elevated/50 transition-colors"
                      >
                        <td className="px-4 py-3">
                          <p className="font-semibold text-ink-primary">
                            {prov.razonSocial}
                          </p>
                          <p className="text-[11px] font-mono-tabular text-ink-secondary">
                            CUIT: {prov.cuit || 'Sin informar'}
                          </p>
                        </td>
                        <td className="px-4 py-3">
                          <p className="text-ink-primary">
                            {prov.contacto || '—'}
                          </p>
                          <p className="text-[11px] font-mono-tabular text-ink-secondary">
                            {prov.telefono || 'Sin teléfono'}
                            {prov.email ? ` · ${prov.email}` : ''}
                          </p>
                        </td>
                        <td className="px-4 py-3 whitespace-nowrap">
                          <button
                            type="button"
                            onClick={() =>
                              abrirProductosVinculadosProveedor(prov)
                            }
                            className={`inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1 text-xs font-mono-tabular font-medium transition-colors ${SEMANTIC_COLORS.digital.softButtonClass}`}
                            title="Ver artículos vinculados a este proveedor"
                          >
                            <Package
                              className="h-3.5 w-3.5"
                              aria-hidden="true"
                            />
                            <span>
                              {prov.cantidadProductosAsociados ?? 0} artículo(s)
                            </span>
                          </button>
                        </td>
                        <td className="px-4 py-3 text-right whitespace-nowrap">
                          <div className="inline-flex items-center gap-1.5">
                            <Button
                              variant="secondary"
                              size="sm"
                              leftIcon={
                                <Pencil
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                              }
                              onClick={() => abrirEditarProveedor(prov)}
                            >
                              Editar
                            </Button>
                            <Button
                              variant="destructive"
                              destructiveTone="soft"
                              size="sm"
                              onClick={() => setProvAEliminar(prov)}
                            >
                              Eliminar
                            </Button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            {proveedoresFiltrados.length > PAGE_SIZE && (
              <PaginationBar
                pageNumber={pageProv}
                totalPages={totalPagesProv}
                totalCount={proveedoresFiltrados.length}
                itemLabel="proveedores"
                onPageChange={setPageProv}
              />
            )}
          </AsyncStateGuard>
        </section>
      )}

      {/* ================================================================== */}
      {/* TAB 3: ÓRDENES DE COMPRA                                           */}
      {/* ================================================================== */}
      {subTab === 'ordenes' && (
        <section className="space-y-4">
          <div className="rounded-xl border border-border-subtle bg-surface p-3.5 shadow-sm flex flex-wrap items-center justify-between gap-3">
            <div className="flex flex-wrap items-center gap-2">
              <select
                aria-label="Filtrar órdenes por proveedor"
                value={filtroOcProveedorId || ''}
                onChange={(e) =>
                  setFiltroOcProveedorId(
                    e.target.value ? Number(e.target.value) : undefined
                  )
                }
                className="h-9 rounded-lg border border-border-strong bg-surface px-3 text-xs text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                <option value="">Todos los proveedores</option>
                {proveedores.map((pr) => (
                  <option key={pr.id} value={pr.id}>
                    {pr.razonSocial}
                  </option>
                ))}
              </select>

              <select
                aria-label="Filtrar órdenes por estado"
                value={filtroOcEstado || ''}
                onChange={(e) =>
                  setFiltroOcEstado(
                    e.target.value
                      ? (Number(e.target.value) as EstadoOrdenCompraEnum)
                      : undefined
                  )
                }
                className="h-9 rounded-lg border border-border-strong bg-surface px-3 text-xs text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                <option value="">Todos los estados</option>
                <option value={EstadoOrdenCompraEnum.Borrador}>Borrador</option>
                <option value={EstadoOrdenCompraEnum.Emitida}>Emitida</option>
                <option value={EstadoOrdenCompraEnum.Recibida}>Recibida</option>
                <option value={EstadoOrdenCompraEnum.Cancelada}>
                  Cancelada
                </option>
              </select>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <Button
                variant="secondary"
                size="sm"
                isLoading={cargandoFaltantes}
                leftIcon={<Sparkles className="h-4 w-4" aria-hidden="true" />}
                onClick={handleGenerarOcConFaltantes}
              >
                Generar OC con faltantes
              </Button>
              <Button
                variant="primary"
                size="sm"
                leftIcon={<PackagePlus className="h-4 w-4" aria-hidden="true" />}
                onClick={abrirNuevaOrdenCompra}
              >
                Nueva Orden de Compra
              </Button>
            </div>
          </div>

          <AsyncStateGuard
            isLoading={cargando}
            isError={Boolean(errorCarga)}
            errorMessage={errorCarga?.message}
            correlationId={errorCarga?.correlationId}
            isEmpty={ordenes.length === 0}
            loadingText="Cargando órdenes de compra..."
            emptyTitle="Sin órdenes de compra"
            emptyDescription="No existen órdenes registradas para los filtros seleccionados."
            onRetry={cargarTodo}
          >
            <div className="rounded-xl border border-border-subtle bg-surface shadow-sm overflow-hidden">
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="bg-elevated text-ink-secondary border-b border-border-subtle">
                    <tr>
                      <th className="px-4 py-3 font-medium">OC #</th>
                      <th className="px-4 py-3 font-medium">Proveedor</th>
                      <th className="px-4 py-3 font-medium">Emisión</th>
                      <th className="px-4 py-3 font-medium text-right">
                        Total
                      </th>
                      <th className="px-4 py-3 font-medium">Estado</th>
                      <th className="px-4 py-3 font-medium text-right">
                        Acciones
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle">
                    {ordenesPagina.map((oc) => {
                      const badge = getEstadoOcBadge(
                        oc.estado,
                        oc.estadoNombre
                      );
                      const puedeRecepcionar =
                        oc.estado !== EstadoOrdenCompraEnum.Recibida &&
                        oc.estado !== EstadoOrdenCompraEnum.Cancelada;
                      return (
                        <tr
                          key={oc.id}
                          className="hover:bg-elevated/50 transition-colors"
                        >
                          <td className="px-4 py-3 font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                            #{oc.id}
                          </td>
                          <td className="px-4 py-3 font-medium text-ink-primary">
                            {oc.proveedorRazonSocial}
                          </td>
                          <td className="px-4 py-3 font-mono-tabular text-ink-secondary whitespace-nowrap">
                            {formatDateTime(oc.fechaEmision)}
                          </td>
                          <td className="px-4 py-3 text-right font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                            {formatCurrency(oc.total, moneda)}
                          </td>
                          <td className="px-4 py-3 whitespace-nowrap">
                            <Badge variant={badge.variant}>{badge.label}</Badge>
                          </td>
                          <td className="px-4 py-3 text-right whitespace-nowrap">
                            <div className="inline-flex items-center gap-1.5">
                              <Button
                                variant="secondary"
                                size="sm"
                                title="Ver o imprimir Orden de Compra en PDF"
                                leftIcon={
                                  <Printer
                                    className="h-3.5 w-3.5"
                                    aria-hidden="true"
                                  />
                                }
                                onClick={() => abrirImpresionOc(oc)}
                              >
                                PDF
                              </Button>
                              <Button
                                variant="secondary"
                                size="sm"
                                title="Enviar pedido por WhatsApp al proveedor"
                                leftIcon={
                                  <MessageCircle
                                    className="h-3.5 w-3.5 text-emerald-600 dark:text-emerald-400"
                                    aria-hidden="true"
                                  />
                                }
                                onClick={() => enviarOcPorWhatsApp(oc)}
                              >
                                WhatsApp
                              </Button>
                              {puedeRecepcionar && (
                                <>
                                  <Button
                                    variant="secondary"
                                    size="sm"
                                    leftIcon={
                                      <PackageCheck
                                        className="h-3.5 w-3.5"
                                        aria-hidden="true"
                                      />
                                    }
                                    onClick={() => abrirModalRecepcion(oc.id)}
                                  >
                                    Recepcionar
                                  </Button>
                                  <Button
                                    variant="destructive"
                                    destructiveTone="soft"
                                    size="sm"
                                    onClick={() => setOcACancelar(oc)}
                                  >
                                    Cancelar
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
            </div>

            {ordenes.length > PAGE_SIZE && (
              <PaginationBar
                pageNumber={pageOc}
                totalPages={totalPagesOc}
                totalCount={ordenes.length}
                itemLabel="órdenes"
                onPageChange={setPageOc}
              />
            )}
          </AsyncStateGuard>
        </section>
      )}

      {/* ================================================================== */}
      {/* MODALES                                                            */}
      {/* ================================================================== */}

      {/* Modal Crear / Editar Producto */}
      <Modal
        isOpen={modalProdOpen}
        onClose={() => setModalProdOpen(false)}
        title={
          prodEditando
            ? `Editar Producto — ${prodEditando.nombre}`
            : 'Nuevo Producto'
        }
      >
        <form onSubmit={handleGuardarProducto} className="space-y-4">
          <InputField
            label="Nombre del artículo *"
            required
            value={nombreProd}
            onChange={(e) => setNombreProd(e.target.value)}
            placeholder="Ej. Shampoo Nutritivo 500 ml"
          />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <InputField
              label="Código de barras / SKU"
              value={codigoBarras}
              onChange={(e) => setCodigoBarras(e.target.value)}
              placeholder="Opcional"
            />
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-ink-secondary">
                Categoría *
              </label>
              <select
                value={categoriaId}
                onChange={(e) => setCategoriaId(Number(e.target.value))}
                className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                {categoriasProducto.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nombre}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium text-ink-secondary">
              Proveedor habitual
            </label>
            <select
              value={proveedorProdId ?? ''}
              onChange={(e) =>
                setProveedorProdId(
                  e.target.value ? Number(e.target.value) : null
                )
              }
              className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
            >
              <option value="">Sin proveedor asignado</option>
              {proveedores.map((pr) => (
                <option key={pr.id} value={pr.id}>
                  {pr.razonSocial}
                </option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <NumericInput
              label="Precio de costo *"
              prefix="$"
              required
              value={precioCosto}
              onValueChange={setPrecioCosto}
              placeholder="0.00"
            />
            <NumericInput
              label="Precio de venta *"
              prefix="$"
              required
              value={precioVenta}
              onValueChange={setPrecioVenta}
              placeholder="0.00"
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {prodEditando ? (
              <div className="flex flex-col gap-1.5">
                <div className="flex items-center justify-between gap-2">
                  <span className="text-xs font-medium text-ink-secondary">
                    Stock actual
                  </span>
                  <Badge variant="info">Se ajusta desde Movimientos</Badge>
                </div>
                <div className="h-10 rounded-lg border border-border-subtle bg-elevated px-3 flex items-center justify-between gap-2">
                  <span className="font-mono-tabular text-sm font-semibold text-ink-primary">
                    {prodEditando.stockActual} u.
                  </span>
                  <button
                    type="button"
                    onClick={() => {
                      const target = prodEditando;
                      setModalProdOpen(false);
                      setCantidadMov(1);
                      setMotivoMov('');
                      setModalAjusteOpen(target);
                    }}
                    className="text-xs font-medium text-brand-primary hover:underline"
                  >
                    Abrir movimiento
                  </button>
                </div>
              </div>
            ) : (
              <NumericInput
                label="Stock inicial"
                allowDecimals={false}
                min={0}
                value={stockActual}
                onValueChange={setStockActual}
                placeholder="0"
              />
            )}

            <NumericInput
              label="Stock mínimo de alerta *"
              allowDecimals={false}
              min={0}
              required
              value={stockMinimo}
              onValueChange={setStockMinimo}
              placeholder="5"
            />
          </div>

          <label className="flex items-center gap-2 text-xs font-medium text-ink-primary cursor-pointer">
            <input
              type="checkbox"
              checked={esParaVenta}
              onChange={(e) => setEsParaVenta(e.target.checked)}
              className="rounded border-border-strong text-brand-primary focus:ring-brand-accent"
            />
            <span>Disponible para venta en mostrador</span>
          </label>

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border-subtle">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setModalProdOpen(false)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="sm"
              isLoading={guardandoProd}
            >
              Guardar Producto
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Movimiento de Stock */}
      <Modal
        isOpen={Boolean(modalAjusteOpen)}
        onClose={() => setModalAjusteOpen(null)}
        title={`Movimiento de Stock — ${modalAjusteOpen?.nombre || ''}`}
        subtitle={`Stock actual disponible: ${modalAjusteOpen?.stockActual ?? 0} u.`}
      >
        <form onSubmit={handleRegistrarMovimientoStock} className="space-y-4">
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium text-ink-secondary">
              Tipo de movimiento *
            </label>
            <select
              value={tipoMov}
              onChange={(e) =>
                setTipoMov(Number(e.target.value) as TipoMovimientoStockEnum)
              }
              className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
            >
              <option value={TipoMovimientoStockEnum.IngresoCompra}>
                Ingreso de mercadería / reposición
              </option>
              <option value={TipoMovimientoStockEnum.ConsumoInternoSalon}>
                Consumo interno de gabinete
              </option>
              <option value={TipoMovimientoStockEnum.AjusteRoturaPerdida}>
                Ajuste por rotura, vencimiento o merma
              </option>
            </select>
          </div>

          <NumericInput
            label="Cantidad *"
            allowDecimals={false}
            min={1}
            required
            value={cantidadMov}
            onValueChange={setCantidadMov}
            placeholder="1"
          />

          <InputField
            label="Motivo del movimiento *"
            required
            value={motivoMov}
            onChange={(e) => setMotivoMov(e.target.value)}
            placeholder="Ej. Reposición manual o frasco dañado en depósito"
          />

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border-subtle">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setModalAjusteOpen(null)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="sm"
              isLoading={guardandoMov}
            >
              Registrar Movimiento
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Historial de Stock */}
      <Modal
        isOpen={Boolean(historialProducto)}
        onClose={() => setHistorialProducto(null)}
        title={`Historial de Stock — ${historialProducto?.producto.nombre || ''}`}
        subtitle={`Saldo actual: ${historialProducto?.producto.stockActual ?? 0} u.`}
        maxWidthClass="max-w-2xl"
      >
        <div className="space-y-2 text-xs">
          {(historialProducto?.movimientos || []).length === 0 ? (
            <p className="py-8 text-center text-ink-secondary">
              Este artículo todavía no registra movimientos en su historial.
            </p>
          ) : (
            <div className="divide-y divide-border-subtle">
              {(historialProducto?.movimientos || []).map((m) => (
                <div
                  key={m.id}
                  className="flex items-center justify-between py-3 gap-3"
                >
                  <div className="min-w-0 space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <Badge
                        variant={getMovimientoBadgeVariant(m.tipoMovimiento)}
                      >
                        {m.tipoMovimientoNombre ||
                          m.tipoMovimientoDescripcion ||
                          'Movimiento'}
                      </Badge>
                      <span className="font-mono-tabular font-semibold text-ink-primary">
                        {m.cantidad} u.
                      </span>
                    </div>
                    <p className="text-ink-secondary">{m.motivo || '—'}</p>
                  </div>
                  <div className="text-right shrink-0">
                    {m.saldoResultante != null && (
                      <p className="font-mono-tabular font-semibold text-ink-primary">
                        Saldo: {m.saldoResultante} u.
                      </p>
                    )}
                    <span className="font-mono-tabular text-[11px] text-ink-secondary">
                      {formatDateTime(m.fecha)}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </Modal>

      {/* Modal Crear / Editar Proveedor */}
      <Modal
        isOpen={modalProvOpen}
        onClose={() => setModalProvOpen(false)}
        title={provEditando ? 'Editar Proveedor' : 'Nuevo Proveedor'}
      >
        <form onSubmit={handleGuardarProveedor} className="space-y-3.5">
          <InputField
            label="Razón Social *"
            required
            value={razonSocial}
            onChange={(e) => setRazonSocial(e.target.value)}
            placeholder="Ej. Distribuidora Capilar SRL"
          />
          <InputField
            label="CUIT"
            value={cuitProv}
            onChange={(e) => setCuitProv(e.target.value)}
            placeholder="Ej. 30-71234567-9"
          />
          <InputField
            label="Persona de contacto"
            value={contactoProv}
            onChange={(e) => setContactoProv(e.target.value)}
            placeholder="Ej. Martín López"
          />
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <InputField
              label="Teléfono / WhatsApp"
              value={telefonoProv}
              onChange={(e) => setTelefonoProv(e.target.value)}
              placeholder="Ej. +54 9 11 5555-1234"
            />
            <InputField
              label="Email"
              type="email"
              value={emailProv}
              onChange={(e) => setEmailProv(e.target.value)}
              placeholder="ventas@proveedor.com"
            />
          </div>
          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border-subtle">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setModalProvOpen(false)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="sm"
              isLoading={guardandoProv}
            >
              Guardar Proveedor
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Productos Vinculados a Proveedor */}
      <Modal
        isOpen={Boolean(provProductosModal)}
        onClose={() => setProvProductosModal(null)}
        title={`Artículos de ${provProductosModal?.proveedor.razonSocial || ''}`}
        subtitle="Listado de productos actualmente vinculados a este proveedor."
        maxWidthClass="max-w-2xl"
      >
        {provProductosModal?.cargando ? (
          <p className="py-8 text-center text-xs text-ink-secondary">
            Cargando artículos vinculados...
          </p>
        ) : (provProductosModal?.productos || []).length === 0 ? (
          <p className="py-8 text-center text-xs text-ink-secondary">
            Este proveedor aún no tiene productos asociados.
          </p>
        ) : (
          <div className="overflow-x-auto rounded-lg border border-border-subtle">
            <table className="w-full text-left text-xs">
              <thead className="bg-elevated text-ink-secondary border-b border-border-subtle">
                <tr>
                  <th className="px-3 py-2 font-medium">Artículo</th>
                  <th className="px-3 py-2 font-medium">SKU / Código</th>
                  <th className="px-3 py-2 font-medium text-right">Costo</th>
                  <th className="px-3 py-2 font-medium text-right">
                    Stock actual
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-subtle">
                {provProductosModal?.productos.map((item) => (
                  <tr key={item.id}>
                    <td className="px-3 py-2.5 font-medium text-ink-primary">
                      {item.nombre}
                    </td>
                    <td className="px-3 py-2.5 font-mono-tabular text-ink-secondary">
                      {item.codigoBarras || item.codigoBarra || '—'}
                    </td>
                    <td className="px-3 py-2.5 text-right font-mono-tabular text-ink-primary">
                      {formatCurrency(item.precioCosto, moneda)}
                    </td>
                    <td className="px-3 py-2.5 text-right">
                      <Badge
                        variant={
                          item.stockActual <= item.stockMinimoAlerta
                            ? 'danger'
                            : 'success'
                        }
                        className="font-mono-tabular"
                      >
                        {item.stockActual} u.
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Modal>

      {/* Modal Selector de Grupos de Faltantes por Proveedor */}
      <Modal
        isOpen={modalFaltantesOpen}
        onClose={() => setModalFaltantesOpen(false)}
        title="Faltantes agrupados por proveedor"
        subtitle="Seleccioná un proveedor para precargar automáticamente su Orden de Compra sugerida."
        maxWidthClass="max-w-2xl"
      >
        <div className="space-y-3">
          {gruposFaltantes.map((g, idx) => (
            <div
              key={`${g.proveedorId ?? 'sin'}-${idx}`}
              className="rounded-lg border border-border-subtle bg-elevated/40 p-3.5 flex flex-col sm:flex-row sm:items-center justify-between gap-3"
            >
              <div className="min-w-0 space-y-1">
                <p className="text-sm font-semibold text-ink-primary">
                  {g.proveedorNombre}
                </p>
                <p className="text-xs text-ink-secondary">
                  {g.items
                    .map(
                      (i) =>
                        `${i.producto.nombre} (+${i.cantidadSugerida} u.)`
                    )
                    .join(' · ')}
                </p>
              </div>
              <Button
                variant="primary"
                size="sm"
                className="shrink-0"
                onClick={() => precargarOrdenDesdeGrupo(g)}
              >
                Precargar OC ({g.items.length})
              </Button>
            </div>
          ))}
        </div>
      </Modal>

      {/* Modal Emitir Nueva Orden de Compra (Multi-ítem Pro) */}
      <Modal
        isOpen={modalNuevaOcOpen}
        onClose={() => setModalNuevaOcOpen(false)}
        title="Nueva Orden de Compra"
        subtitle="Agregá los artículos a solicitar y verificá el total estimado antes de emitir."
        maxWidthClass="max-w-3xl"
      >
        <form onSubmit={handleCrearOrdenCompra} className="space-y-4 text-xs">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-medium text-ink-secondary">
                Proveedor *
              </label>
              <select
                value={ocProveedorId}
                onChange={(e) => setOcProveedorId(Number(e.target.value))}
                className="h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                {proveedores.map((pr) => (
                  <option key={pr.id} value={pr.id}>
                    {pr.razonSocial}
                  </option>
                ))}
              </select>
            </div>
            <InputField
              label="N° Comprobante / Cotización"
              value={ocComprobante}
              onChange={(e) => setOcComprobante(e.target.value)}
              placeholder="Opcional"
            />
          </div>

          {/* Tabla Multi-ítem con encabezados claros */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-ink-secondary">
                Detalle de artículos solicitados
              </span>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                leftIcon={<Plus className="h-3.5 w-3.5" aria-hidden="true" />}
                onClick={agregarRenglonOc}
              >
                Agregar renglón
              </Button>
            </div>

            <div className="overflow-x-auto rounded-lg border border-border-subtle">
              <table className="w-full text-left text-xs">
                <thead className="bg-elevated text-ink-secondary border-b border-border-subtle">
                  <tr>
                    <th className="px-3 py-2 font-medium">Producto</th>
                    <th className="px-3 py-2 font-medium w-28">Cantidad</th>
                    <th className="px-3 py-2 font-medium w-36">Costo Unit.</th>
                    <th className="px-3 py-2 font-medium text-right w-32">
                      Subtotal
                    </th>
                    <th className="px-2 py-2 w-10" />
                  </tr>
                </thead>
                <tbody className="divide-y divide-border-subtle">
                  {ocRenglones.map((r, idx) => (
                    <tr key={idx}>
                      <td className="p-2">
                        <select
                          value={r.productoId}
                          onChange={(e) => {
                            const nextProdId = Number(e.target.value);
                            const prod = productos.find(
                              (p) => p.id === nextProdId
                            );
                            setOcRenglones((prev) =>
                              prev.map((row, i) =>
                                i === idx
                                  ? {
                                      ...row,
                                      productoId: nextProdId,
                                      precioUnitarioCosto:
                                        prod?.precioCosto ??
                                        row.precioUnitarioCosto,
                                    }
                                  : row
                              )
                            );
                          }}
                          className="h-9 w-full rounded-lg border border-border-strong bg-surface px-2.5 text-xs text-ink-primary focus:border-brand-primary focus:outline-none"
                        >
                          {productos.map((p) => (
                            <option key={p.id} value={p.id}>
                              {p.nombre}
                            </option>
                          ))}
                        </select>
                      </td>
                      <td className="p-2">
                        <NumericInput
                          allowDecimals={false}
                          min={1}
                          value={r.cantidadPedida}
                          onValueChange={(val) => {
                            setOcRenglones((prev) =>
                              prev.map((row, i) =>
                                i === idx
                                  ? { ...row, cantidadPedida: val ?? 0 }
                                  : row
                              )
                            );
                          }}
                          className="!h-9"
                        />
                      </td>
                      <td className="p-2">
                        <NumericInput
                          prefix="$"
                          min={0}
                          value={r.precioUnitarioCosto}
                          onValueChange={(val) => {
                            setOcRenglones((prev) =>
                              prev.map((row, i) =>
                                i === idx
                                  ? { ...row, precioUnitarioCosto: val ?? 0 }
                                  : row
                              )
                            );
                          }}
                          className="!h-9"
                        />
                      </td>
                      <td className="px-3 py-2 text-right font-mono-tabular font-semibold text-ink-primary whitespace-nowrap">
                        {formatCurrency(
                          (r.cantidadPedida || 0) *
                            (r.precioUnitarioCosto || 0),
                          moneda
                        )}
                      </td>
                      <td className="p-2 text-center">
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          disabled={ocRenglones.length <= 1}
                          aria-label="Quitar renglón"
                          onClick={() => quitarRenglonOc(idx)}
                          className="!px-2"
                        >
                          <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
                        </Button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="flex items-center justify-between rounded-lg border border-border-subtle bg-elevated/60 px-4 py-2.5">
              <span className="text-xs font-medium text-ink-secondary">
                Total estimado de la orden ({ocRenglones.length} renglón/es)
              </span>
              <span className="text-base font-bold font-mono-tabular text-ink-primary">
                {formatCurrency(totalNuevaOc, moneda)}
              </span>
            </div>
          </div>

          <InputField
            label="Observaciones"
            value={ocObservaciones}
            onChange={(e) => setOcObservaciones(e.target.value)}
            placeholder="Notas de entrega o condiciones comerciales..."
          />

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border-subtle">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setModalNuevaOcOpen(false)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="sm"
              isLoading={guardandoOc}
              disabled={ocRenglones.length === 0 || proveedores.length === 0}
            >
              Emitir Orden de Compra
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Recepcionar Orden de Compra */}
      <Modal
        isOpen={Boolean(ordenARecepcionar)}
        onClose={() => setOrdenARecepcionar(null)}
        title={`Recepcionar Orden de Compra #${ordenARecepcionar?.id || ''}`}
        subtitle={ordenARecepcionar?.proveedorRazonSocial}
        maxWidthClass="max-w-2xl"
      >
        <form
          onSubmit={confirmarRecepcionMercaderia}
          className="space-y-4 text-xs"
        >
          <InputField
            label="N° de Remito / Factura del proveedor"
            value={comprobanteRecepcion}
            onChange={(e) => setComprobanteRecepcion(e.target.value)}
            placeholder="Ej. FAC-A-0004-0019283"
          />

          <div className="overflow-x-auto rounded-lg border border-border-subtle">
            <table className="w-full text-left text-xs">
              <thead className="bg-elevated text-ink-secondary border-b border-border-subtle">
                <tr>
                  <th className="px-3 py-2 font-medium">Producto</th>
                  <th className="px-3 py-2 font-medium text-right">Pedido</th>
                  <th className="px-3 py-2 font-medium w-28">
                    Cant. Recibida *
                  </th>
                  <th className="px-3 py-2 font-medium w-36">
                    Costo Real Unit. *
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-subtle">
                {itemsRecepcion.map((item, idx) => (
                  <tr key={`${item.productoId}-${idx}`}>
                    <td className="px-3 py-2 font-medium text-ink-primary">
                      {item.productoNombre}
                    </td>
                    <td className="px-3 py-2 text-right font-mono-tabular text-ink-secondary">
                      {item.cantidadPedida} u.
                    </td>
                    <td className="p-2">
                      <NumericInput
                        allowDecimals={false}
                        min={0}
                        value={item.cantidadRecibida}
                        onValueChange={(val) => {
                          setItemsRecepcion((prev) =>
                            prev.map((row, i) =>
                              i === idx
                                ? { ...row, cantidadRecibida: val ?? 0 }
                                : row
                            )
                          );
                        }}
                        className="!h-9"
                      />
                    </td>
                    <td className="p-2">
                      <NumericInput
                        prefix="$"
                        min={0}
                        value={item.precioUnitarioCostoReal}
                        onValueChange={(val) => {
                          setItemsRecepcion((prev) =>
                            prev.map((row, i) =>
                              i === idx
                                ? { ...row, precioUnitarioCostoReal: val ?? 0 }
                                : row
                            )
                          );
                        }}
                        className="!h-9"
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="space-y-2 pt-1">
            <label className="flex items-center gap-2 font-medium text-ink-primary cursor-pointer">
              <input
                type="checkbox"
                checked={actualizarPreciosCosto}
                onChange={(e) => setActualizarPreciosCosto(e.target.checked)}
                className="rounded border-border-strong text-brand-primary focus:ring-brand-accent"
              />
              <span>
                Actualizar automáticamente el precio de costo de los productos
              </span>
            </label>
            <label className="flex items-center gap-2 font-medium text-ink-primary cursor-pointer">
              <input
                type="checkbox"
                disabled={!sesionCajaActual}
                checked={imputarPagoEnCaja}
                onChange={(e) => setImputarPagoEnCaja(e.target.checked)}
                className="rounded border-border-strong text-brand-primary focus:ring-brand-accent disabled:opacity-50"
              />
              <span>
                Registrar egreso en la caja abierta{' '}
                {sesionCajaActual ? `(#${sesionCajaActual.id})` : '(sin caja abierta)'}
              </span>
            </label>
          </div>

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border-subtle">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setOrdenARecepcionar(null)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              size="sm"
              isLoading={recepcionandoOc}
            >
              Confirmar Recepción
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Vista Previa / PDF de Orden de Compra */}
      <PrintReceiptModal
        data={ocImprimible}
        onClose={() => setOcImprimible(null)}
        nombreSalon={
          configSalon?.nombreComercial ||
          configSalon?.nombreSalon ||
          'Mi Peluquería Central'
        }
        cuitSalon={configSalon?.cuit || '30-71548963-9'}
      />

      {/* Confirmaciones Destructivas */}
      <ConfirmModal
        isOpen={Boolean(prodAEliminar)}
        onClose={() => setProdAEliminar(null)}
        onConfirm={confirmarEliminarProducto}
        title="Eliminar producto"
        confirmLabel="Eliminar"
        variant="danger"
        isLoading={ejecutandoBaja}
      >
        <p>
          ¿Confirmás la baja del producto{' '}
          <strong>{prodAEliminar?.nombre}</strong>? Dejará de figurar en el
          catálogo activo.
        </p>
      </ConfirmModal>

      <ConfirmModal
        isOpen={Boolean(provAEliminar)}
        onClose={() => setProvAEliminar(null)}
        onConfirm={confirmarEliminarProveedor}
        title="Eliminar proveedor"
        confirmLabel="Eliminar"
        variant="danger"
        isLoading={ejecutandoBaja}
      >
        <p>
          ¿Confirmás la baja del proveedor{' '}
          <strong>{provAEliminar?.razonSocial}</strong>?
        </p>
      </ConfirmModal>

      <ConfirmModal
        isOpen={Boolean(ocACancelar)}
        onClose={() => setOcACancelar(null)}
        onConfirm={confirmarCancelarOrden}
        title={`Cancelar Orden de Compra #${ocACancelar?.id || ''}`}
        confirmLabel="Cancelar orden"
        variant="danger"
        isLoading={ejecutandoBaja}
      >
        <p>
          ¿Confirmás la cancelación de la orden emitida a{' '}
          <strong>{ocACancelar?.proveedorRazonSocial}</strong>? Esta acción no
          impactará el stock.
        </p>
      </ConfirmModal>
    </div>
  );
}
