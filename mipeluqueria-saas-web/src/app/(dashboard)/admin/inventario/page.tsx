// filepath: src/app/(dashboard)/admin/inventario/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
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
import {
  AsyncStateGuard,
  Button,
  InputField,
  Modal,
} from '@/components/ui';

export default function AdminInventarioComprasPage() {
  const { configSalon, catalogos } = useCatalogos();
  const { sesionCajaActual } = useCajaPos();
  const moneda = configSalon?.moneda || 'ARS';

  const [subTab, setSubTab] = useState<'productos' | 'proveedores' | 'ordenes'>(
    'productos'
  );
  const [soloBajoStock, setSoloBajoStock] = useState<boolean>(false);
  const [filtroOcProveedorId, setFiltroOcProveedorId] = useState<
    number | undefined
  >(undefined);
  const [filtroOcEstado, setFiltroOcEstado] = useState<
    EstadoOrdenCompraEnum | undefined
  >(undefined);

  const [productos, setProductos] = useState<ProductoDto[]>([]);
  const [proveedores, setProveedores] = useState<ProveedorDto[]>([]);
  const [ordenes, setOrdenes] = useState<OrdenCompraDto[]>([]);
  const [cargando, setCargando] = useState<boolean>(true);
  const [errorCarga, setErrorCarga] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);
  const [feedback, setFeedback] = useState<string | null>(null);

  // Modal Producto
  const [modalProdOpen, setModalProdOpen] = useState<boolean>(false);
  const [prodEditando, setProdEditando] = useState<ProductoDto | null>(null);
  const [categoriaId, setCategoriaId] = useState<number>(1);
  const [proveedorProdId, setProveedorProdId] = useState<number | null>(null);
  const [nombreProd, setNombreProd] = useState('');
  const [codigoBarras, setCodigoBarras] = useState('');
  const [precioCosto, setPrecioCosto] = useState(0);
  const [precioVenta, setPrecioVenta] = useState(0);
  const [stockActual, setStockActual] = useState(0);
  const [stockMinimo, setStockMinimo] = useState(5);
  const [esParaVenta, setEsParaVenta] = useState(true);

  // Kardex y Ajuste Manual
  const [kardexProducto, setKardexProducto] = useState<{
    producto: ProductoDto;
    movimientos: KardexItemResponseDto[];
  } | null>(null);
  const [modalAjusteOpen, setModalAjusteOpen] = useState<ProductoDto | null>(
    null
  );
  const [tipoMov, setTipoMov] = useState<TipoMovimientoStockEnum>(
    TipoMovimientoStockEnum.AjusteRoturaPerdida
  );
  const [cantidadMov, setCantidadMov] = useState<number>(1);
  const [motivoMov, setMotivoMov] = useState<string>('');

  // Modal Proveedor
  const [modalProvOpen, setModalProvOpen] = useState<boolean>(false);
  const [provEditando, setProvEditando] = useState<ProveedorDto | null>(null);
  const [razonSocial, setRazonSocial] = useState('');
  const [cuitProv, setCuitProv] = useState('');
  const [contactoProv, setContactoProv] = useState('');
  const [telefonoProv, setTelefonoProv] = useState('');
  const [emailProv, setEmailProv] = useState('');

  // Modal Emitir y Recepcionar OC (con edición renglón por renglón para recepción parcial)
  const [modalNuevaOcOpen, setModalNuevaOcOpen] = useState<boolean>(false);
  const [ocProveedorId, setOcProveedorId] = useState<number>(0);
  const [ocComprobante, setOcComprobante] = useState<string>('');
  const [ocObservaciones, setOcObservaciones] = useState<string>('');
  const [ocRenglones, setOcRenglones] = useState<
    {
      productoId: number;
      cantidadPedida: number;
      precioUnitarioCosto: number;
    }[]
  >([]);

  const [ordenARecepcionar, setOrdenARecepcionar] =
    useState<OrdenCompraDto | null>(null);
  const [comprobanteRecepcion, setComprobanteRecepcion] = useState<string>('');
  const [actualizarPreciosCosto, setActualizarPreciosCosto] =
    useState<boolean>(true);
  const [imputarPagoEnCaja, setImputarPagoEnCaja] = useState<boolean>(false);
  const [itemsRecepcion, setItemsRecepcion] = useState<
    {
      detalleOrdenCompraId?: number;
      productoId: number;
      productoNombre: string;
      cantidadPedida: number;
      cantidadRecibida: number;
      precioUnitarioCostoReal: number;
    }[]
  >([]);

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
          'Error al obtener inventario, proveedores y órdenes de compra.'
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
    cargarTodo();
  }, [cargarTodo]);

  const abrirModalRecepcion = async (ocId: number) => {
    try {
      const res = await ordenesCompraService.getById(ocId);
      if (res.success && res.data) {
        const oc = res.data;
        setOrdenARecepcionar(oc);
        setComprobanteRecepcion(oc.numeroComprobanteProveedor || '');
        setItemsRecepcion(
          extractPagedItems(oc.detalles).map((d) => ({
            detalleOrdenCompraId: d.id,
            productoId: d.productoId,
            productoNombre: d.productoNombre || `Producto #${d.productoId}`,
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
    try {
      const res = await ordenesCompraService.recepcionar(ordenARecepcionar.id, {
        numeroComprobanteProveedor: comprobanteRecepcion || undefined,
        actualizarPreciosCosto,
        imputarPagoEnCaja,
        sesionCajaId: imputarPagoEnCaja ? sesionCajaActual?.id : null,
        items: itemsRecepcion.map((item) => ({
          detalleOrdenCompraId: item.detalleOrdenCompraId,
          productoId: item.productoId,
          cantidadRecibida: item.cantidadRecibida,
          precioUnitarioCostoReal: item.precioUnitarioCostoReal,
        })),
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          'Mercadería recepcionada y acreditada en Kardex.'
        );
        setOrdenARecepcionar(null);
        await cargarTodo();
        setFeedback(
          'Mercadería recepcionada: stock actualizado en Kardex e HistorialPreciosProducto.'
        );
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo recepcionar la orden de compra.',
      });
    }
  };

  const handleGuardarProducto = async (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      categoriaId,
      proveedorId: proveedorProdId,
      codigoBarras: codigoBarras || null,
      nombre: nombreProd,
      precioCosto,
      precioVenta,
      stockActual,
      stockMinimoAlerta: stockMinimo,
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
        setFeedback('Producto guardado correctamente.');
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el producto.',
      });
    }
  };

  const handleEliminarProducto = async (prod: ProductoDto) => {
    try {
      const res = await productosService.delete(prod.id);
      if (res.success) {
        notifyApiSuccess(res, `Producto "${prod.nombre}" eliminado.`);
        setFeedback(`Producto "${prod.nombre}" eliminado.`);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar el producto.',
      });
    }
  };

  const abrirKardex = async (prod: ProductoDto) => {
    try {
      const res = await productosService.getKardex(prod.id);
      if (res.success) {
        setKardexProducto({
          producto: prod,
          movimientos: extractPagedItems<KardexItemResponseDto>(res.data),
        });
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo consultar el Kardex del producto.',
      });
    }
  };

  const handleRegistrarMovimientoStock = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!modalAjusteOpen) return;
    try {
      const res = await productosService.registrarMovimiento({
        productoId: modalAjusteOpen.id,
        tipoMovimiento: tipoMov,
        cantidad: cantidadMov,
        motivo: motivoMov,
      });
      if (res.success) {
        notifyApiSuccess(res, 'Movimiento asentado en Kardex.');
        setModalAjusteOpen(null);
        setMotivoMov('');
        await cargarTodo();
        setFeedback('Movimiento asentado en Kardex.');
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo registrar el movimiento en Kardex.',
      });
    }
  };

  const handleGuardarProveedor = async (e: React.FormEvent) => {
    e.preventDefault();
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
        setFeedback('Proveedor guardado correctamente.');
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el proveedor.',
      });
    }
  };

  const handleEliminarProveedor = async (prov: ProveedorDto) => {
    if ((prov.cantidadProductosAsociados ?? 0) > 0) {
      setFeedback(
        `No se puede dar de baja al proveedor "${prov.razonSocial}" porque posee ${prov.cantidadProductosAsociados} producto(s) activos vinculados.`
      );
      return;
    }
    try {
      const res = await proveedoresService.delete(prov.id);
      if (res.success) {
        notifyApiSuccess(res, `Proveedor "${prov.razonSocial}" eliminado.`);
        setFeedback(`Proveedor "${prov.razonSocial}" eliminado.`);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar el proveedor.',
      });
    }
  };

  const handleCrearOrdenCompra = async (e: React.FormEvent) => {
    e.preventDefault();
    const targetProveedorId = ocProveedorId || proveedores[0]?.id || 0;
    if (ocRenglones.length === 0 || !targetProveedorId) return;
    try {
      const res = await ordenesCompraService.crear({
        proveedorId: targetProveedorId,
        numeroComprobanteProveedor: ocComprobante || undefined,
        observaciones: ocObservaciones || undefined,
        detalles: ocRenglones,
      });
      if (res.success) {
        notifyApiSuccess(res, `Orden de Compra #${res.data?.id} emitida.`);
        setModalNuevaOcOpen(false);
        setOcRenglones([]);
        setFeedback(`Orden de Compra #${res.data?.id} emitida.`);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo emitir la orden de compra.',
      });
    }
  };

  const handleCancelarOrden = async (ocId: number) => {
    try {
      const res = await ordenesCompraService.cancelar(
        ocId,
        'Cancelación administrativa en panel de compras'
      );
      if (res.success) {
        notifyApiSuccess(res, `Orden de Compra #${ocId} cancelada.`);
        setFeedback(`Orden de Compra #${ocId} cancelada.`);
        await cargarTodo();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo cancelar la orden de compra.',
      });
    }
  };

  return (
    <div className="p-6 space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-4 bg-white border border-slate-200 rounded-lg p-5">
        <div>
          <h1 className="text-xl font-bold text-slate-900">
            Inventario, Kardex, Proveedores y Órdenes de Compra
          </h1>
          <p className="text-xs text-slate-500">
            Control integral con StockActual blindado y recepción parcial/total
            de mercadería.
          </p>
        </div>

        <div className="flex gap-2">
          <Button
            variant={subTab === 'productos' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('productos')}
          >
            1. Productos y Kardex ({productos.length})
          </Button>
          <Button
            variant={subTab === 'proveedores' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('proveedores')}
          >
            2. Proveedores ({proveedores.length})
          </Button>
          <Button
            variant={subTab === 'ordenes' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('ordenes')}
          >
            3. Órdenes de Compra ({ordenes.length})
          </Button>
        </div>
      </div>

      {feedback && (
        <div className="p-3 rounded bg-slate-900 text-white text-xs flex justify-between">
          <span>{feedback}</span>
          <button type="button" onClick={() => setFeedback(null)}>
            ✕
          </button>
        </div>
      )}

      {subTab === 'productos' && (
        <div className="space-y-4">
          <div className="flex justify-between items-center">
            <label className="inline-flex items-center gap-2 text-xs font-semibold">
              <input
                type="checkbox"
                checked={soloBajoStock}
                onChange={(e) => setSoloBajoStock(e.target.checked)}
              />
              Filtrar solo artículos en Stock Crítico
            </label>
            <Button
              variant="editorial"
              size="sm"
              onClick={() => {
                setProdEditando(null);
                setNombreProd('');
                setCodigoBarras('');
                setPrecioCosto(0);
                setPrecioVenta(0);
                setStockActual(0);
                setModalProdOpen(true);
              }}
            >
              + Nuevo Producto
            </Button>
          </div>

          <AsyncStateGuard
            isLoading={cargando}
            isError={Boolean(errorCarga)}
            errorMessage={errorCarga?.message}
            correlationId={errorCarga?.correlationId}
            isEmpty={(productos ?? []).length === 0}
            loadingText="Cargando catálogo de productos..."
            emptyTitle="Sin productos para mostrar"
            emptyDescription="No hay artículos registrados con el filtro actual."
            onRetry={cargarTodo}
          >
            <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-100 text-slate-700 uppercase">
                  <tr>
                    <th className="p-3">Código</th>
                    <th className="p-3">Artículo</th>
                    <th className="p-3">Uso</th>
                    <th className="p-3">Costo / Venta</th>
                    <th className="p-3">Stock Actual</th>
                    <th className="p-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {(productos ?? []).map((p) => (
                    <tr key={p.id}>
                      <td className="p-3 font-mono">{p.codigoBarras || '—'}</td>
                      <td className="p-3 font-bold">{p.nombre}</td>
                      <td className="p-3">
                        {p.esParaVenta ? 'Reventa Mostrador' : 'Insumo Gabinete'}
                      </td>
                      <td className="p-3 font-mono">
                        {formatCurrency(p.precioCosto, moneda)} /{' '}
                        {formatCurrency(p.precioVenta, moneda)}
                      </td>
                      <td
                        className={`p-3 font-mono font-bold ${
                          p.stockActual <= p.stockMinimoAlerta
                            ? 'text-red-700'
                            : 'text-slate-900'
                        }`}
                      >
                        {p.stockActual} u. (Mín: {p.stockMinimoAlerta})
                      </td>
                      <td className="p-3 text-right space-x-1">
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => {
                            setProdEditando(p);
                            setCategoriaId(p.categoriaId || 1);
                            setProveedorProdId(p.proveedorId || null);
                            setNombreProd(p.nombre);
                            setCodigoBarras(p.codigoBarras || '');
                            setPrecioCosto(p.precioCosto);
                            setPrecioVenta(p.precioVenta);
                            setStockActual(p.stockActual);
                            setStockMinimo(p.stockMinimoAlerta);
                            setEsParaVenta(p.esParaVenta);
                            setModalProdOpen(true);
                          }}
                        >
                          Editar
                        </Button>
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => setModalAjusteOpen(p)}
                        >
                          Movimiento
                        </Button>
                        <Button
                          variant="primary"
                          size="sm"
                          onClick={() => abrirKardex(p)}
                        >
                          Kardex
                        </Button>
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => handleEliminarProducto(p)}
                        >
                          ✕
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

      {subTab === 'proveedores' && (
        <div className="space-y-4">
          <div className="flex justify-end">
            <Button
              variant="editorial"
              size="sm"
              onClick={() => {
                setProvEditando(null);
                setRazonSocial('');
                setCuitProv('');
                setContactoProv('');
                setTelefonoProv('');
                setEmailProv('');
                setModalProvOpen(true);
              }}
            >
              + Nuevo Proveedor
            </Button>
          </div>
          <AsyncStateGuard
            isLoading={cargando}
            isError={Boolean(errorCarga)}
            errorMessage={errorCarga?.message}
            correlationId={errorCarga?.correlationId}
            isEmpty={(proveedores ?? []).length === 0}
            loadingText="Cargando proveedores registrados..."
            emptyTitle="Sin proveedores activos"
            emptyDescription="Registrá tu primer proveedor con '+ Nuevo Proveedor'."
            onRetry={cargarTodo}
          >
            <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-100 text-slate-700 uppercase">
                  <tr>
                    <th className="p-3">ID</th>
                    <th className="p-3">Razón Social / CUIT</th>
                    <th className="p-3">Contacto / Teléfono</th>
                    <th className="p-3">Productos Vinculados</th>
                    <th className="p-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {(proveedores ?? []).map((prov) => (
                    <tr key={prov.id}>
                      <td className="p-3 font-mono">#{prov.id}</td>
                      <td className="p-3">
                        <span className="font-bold block">
                          {prov.razonSocial}
                        </span>
                        <span className="text-[11px] font-mono text-slate-500">
                          CUIT: {prov.cuit || 'S/D'}
                        </span>
                      </td>
                      <td className="p-3">
                        {prov.contacto || '—'} ({prov.telefono || '—'})
                      </td>
                      <td className="p-3 font-mono font-bold">
                        {prov.cantidadProductosAsociados ?? 0} artículo(s)
                      </td>
                      <td className="p-3 text-right space-x-1.5">
                        <Button
                          variant="secondary"
                          size="sm"
                          onClick={() => {
                            setProvEditando(prov);
                            setRazonSocial(prov.razonSocial);
                            setCuitProv(prov.cuit || '');
                            setContactoProv(prov.contacto || '');
                            setTelefonoProv(prov.telefono || '');
                            setEmailProv(prov.email || '');
                            setModalProvOpen(true);
                          }}
                        >
                          Editar
                        </Button>
                        <Button
                          variant="danger"
                          size="sm"
                          onClick={() => handleEliminarProveedor(prov)}
                        >
                          Eliminar
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

      {subTab === 'ordenes' && (
        <div className="space-y-4">
          <div className="flex flex-wrap justify-between items-center gap-3">
            <div className="flex items-center gap-2 text-xs">
              <select
                value={filtroOcProveedorId || ''}
                onChange={(e) =>
                  setFiltroOcProveedorId(
                    e.target.value ? Number(e.target.value) : undefined
                  )
                }
                className="rounded border border-slate-300 px-2.5 py-1.5"
              >
                <option value="">Todos los Proveedores</option>
                {(proveedores ?? []).map((pr) => (
                  <option key={pr.id} value={pr.id}>
                    {pr.razonSocial}
                  </option>
                ))}
              </select>
              <select
                value={filtroOcEstado || ''}
                onChange={(e) =>
                  setFiltroOcEstado(
                    e.target.value
                      ? (Number(e.target.value) as EstadoOrdenCompraEnum)
                      : undefined
                  )
                }
                className="rounded border border-slate-300 px-2.5 py-1.5"
              >
                <option value="">Todos los Estados</option>
                <option value={EstadoOrdenCompraEnum.Borrador}>
                  1 - Borrador
                </option>
                <option value={EstadoOrdenCompraEnum.Emitida}>
                  2 - Emitida
                </option>
                <option value={EstadoOrdenCompraEnum.Recibida}>
                  3 - Recibida
                </option>
                <option value={EstadoOrdenCompraEnum.Cancelada}>
                  4 - Cancelada
                </option>
              </select>
            </div>
            <Button
              variant="editorial"
              size="sm"
              onClick={() => {
                if (proveedores[0] && !ocProveedorId) {
                  setOcProveedorId(proveedores[0].id);
                }
                setOcRenglones([
                  {
                    productoId: productos[0]?.id || 1,
                    cantidadPedida: 10,
                    precioUnitarioCosto: productos[0]?.precioCosto || 5000,
                  },
                ]);
                setModalNuevaOcOpen(true);
              }}
            >
              + Emitir Nueva Orden de Compra
            </Button>
          </div>

          <AsyncStateGuard
            isLoading={cargando}
            isError={Boolean(errorCarga)}
            errorMessage={errorCarga?.message}
            correlationId={errorCarga?.correlationId}
            isEmpty={(ordenes ?? []).length === 0}
            loadingText="Cargando órdenes de compra..."
            emptyTitle="Sin órdenes de compra"
            emptyDescription="No existen órdenes de compra registradas para los filtros seleccionados."
            onRetry={cargarTodo}
          >
            <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-left text-xs">
                <thead className="bg-slate-100 text-slate-700 uppercase">
                  <tr>
                    <th className="p-3">OC #</th>
                    <th className="p-3">Proveedor</th>
                    <th className="p-3">Emisión</th>
                    <th className="p-3">Total</th>
                    <th className="p-3">Estado</th>
                    <th className="p-3 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {(ordenes ?? []).map((oc) => (
                    <tr key={oc.id}>
                      <td className="p-3 font-mono font-bold">#{oc.id}</td>
                      <td className="p-3 font-semibold">
                        {oc.proveedorRazonSocial}
                      </td>
                      <td className="p-3">{formatDateTime(oc.fechaEmision)}</td>
                      <td className="p-3 font-mono font-bold">
                        {formatCurrency(oc.total, moneda)}
                      </td>
                      <td className="p-3">{oc.estadoNombre || oc.estado}</td>
                      <td className="p-3 text-right space-x-1.5">
                        {oc.estado !== EstadoOrdenCompraEnum.Recibida &&
                          oc.estado !== EstadoOrdenCompraEnum.Cancelada && (
                            <>
                              <Button
                                variant="editorial"
                                size="sm"
                                onClick={() => abrirModalRecepcion(oc.id)}
                              >
                                Recepcionar (Total/Parcial)
                              </Button>
                              <Button
                                variant="danger"
                                size="sm"
                                onClick={() => handleCancelarOrden(oc.id)}
                              >
                                Cancelar OC
                              </Button>
                            </>
                          )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </AsyncStateGuard>
        </div>
      )}

      {/* Modal Recepcionar Orden de Compra con Edición de Cantidades y Costos por Renglón */}
      <Modal
        isOpen={!!ordenARecepcionar}
        onClose={() => setOrdenARecepcionar(null)}
        title={`Recepcionar Orden de Compra #${ordenARecepcionar?.id}`}
        maxWidthClass="max-w-2xl"
      >
        <form
          onSubmit={confirmarRecepcionMercaderia}
          className="space-y-4 text-xs"
        >
          <InputField
            label="N° de Remito / Factura del Proveedor"
            value={comprobanteRecepcion}
            onChange={(e) => setComprobanteRecepcion(e.target.value)}
            placeholder="Ej. FAC-A-0004-0019283"
          />
          <div className="border border-slate-200 rounded overflow-hidden">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-600">
                <tr>
                  <th className="p-2">Producto</th>
                  <th className="p-2">Pedido</th>
                  <th className="p-2">Cant. Recibida *</th>
                  <th className="p-2">Costo Real Unit. *</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {itemsRecepcion.map((item, idx) => (
                  <tr key={idx}>
                    <td className="p-2 font-semibold">{item.productoNombre}</td>
                    <td className="p-2 font-mono">{item.cantidadPedida} u.</td>
                    <td className="p-2">
                      <input
                        type="number"
                        min={0}
                        value={item.cantidadRecibida}
                        onChange={(e) => {
                          const next = [...itemsRecepcion];
                          next[idx].cantidadRecibida = Number(e.target.value);
                          setItemsRecepcion(next);
                        }}
                        className="w-20 rounded border border-slate-300 px-2 py-1 font-mono"
                      />
                    </td>
                    <td className="p-2">
                      <input
                        type="number"
                        step="0.01"
                        value={item.precioUnitarioCostoReal}
                        onChange={(e) => {
                          const next = [...itemsRecepcion];
                          next[idx].precioUnitarioCostoReal = Number(
                            e.target.value
                          );
                          setItemsRecepcion(next);
                        }}
                        className="w-28 rounded border border-slate-300 px-2 py-1 font-mono"
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <label className="flex items-center gap-2 font-semibold">
            <input
              type="checkbox"
              checked={actualizarPreciosCosto}
              onChange={(e) => setActualizarPreciosCosto(e.target.checked)}
            />
            Actualizar automáticamente PrecioCosto e HistorialPreciosProducto
          </label>
          <label className="flex items-center gap-2 font-semibold">
            <input
              type="checkbox"
              checked={imputarPagoEnCaja}
              onChange={(e) => setImputarPagoEnCaja(e.target.checked)}
            />
            Imputar egreso (PagoProveedor) en la Sesión de Caja Abierta #
            {sesionCajaActual?.id || '—'}
          </label>
          <Button type="submit" variant="editorial" className="w-full">
            Confirmar Recepción Física e Impactar Stock
          </Button>
        </form>
      </Modal>

      <Modal
        isOpen={modalProdOpen}
        onClose={() => setModalProdOpen(false)}
        title={
          prodEditando
            ? `Editar Producto #${prodEditando.id}`
            : 'Nuevo Producto'
        }
      >
        <form onSubmit={handleGuardarProducto} className="space-y-3">
          <InputField
            label="Nombre del Artículo *"
            required
            value={nombreProd}
            onChange={(e) => setNombreProd(e.target.value)}
          />
          <div className="grid grid-cols-2 gap-3">
            <InputField
              label="Código de Barras"
              value={codigoBarras}
              onChange={(e) => setCodigoBarras(e.target.value)}
            />
            <div className="flex flex-col gap-1">
              <label className="text-xs font-semibold uppercase text-slate-700">
                Categoría *
              </label>
              <select
                value={categoriaId}
                onChange={(e) => setCategoriaId(Number(e.target.value))}
                className="rounded border border-slate-300 px-3 py-2 text-xs"
              >
                {(
                  catalogos?.categoriasProducto || [
                    { id: 1, nombre: 'General' },
                  ]
                ).map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.nombre}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <InputField
              label="Precio Costo *"
              type="number"
              step="0.01"
              required
              value={precioCosto}
              onChange={(e) => setPrecioCosto(Number(e.target.value))}
            />
            <InputField
              label="Precio Venta *"
              type="number"
              step="0.01"
              required
              value={precioVenta}
              onChange={(e) => setPrecioVenta(Number(e.target.value))}
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <InputField
              label="Stock Actual"
              type="number"
              readOnly={!!prodEditando}
              value={stockActual}
              onChange={(e) => setStockActual(Number(e.target.value))}
            />
            <InputField
              label="Stock Mínimo Alerta *"
              type="number"
              required
              value={stockMinimo}
              onChange={(e) => setStockMinimo(Number(e.target.value))}
            />
          </div>
          <label className="flex items-center gap-2 text-xs font-semibold">
            <input
              type="checkbox"
              checked={esParaVenta}
              onChange={(e) => setEsParaVenta(e.target.checked)}
            />
            Para Reventa en Mostrador
          </label>
          <Button type="submit" variant="primary" className="w-full">
            Guardar Producto
          </Button>
        </form>
      </Modal>

      <Modal
        isOpen={modalProvOpen}
        onClose={() => setModalProvOpen(false)}
        title={provEditando ? 'Editar Proveedor' : 'Nuevo Proveedor'}
      >
        <form onSubmit={handleGuardarProveedor} className="space-y-3">
          <InputField
            label="Razón Social Única *"
            required
            value={razonSocial}
            onChange={(e) => setRazonSocial(e.target.value)}
          />
          <InputField
            label="CUIT Fiscal"
            value={cuitProv}
            onChange={(e) => setCuitProv(e.target.value)}
            placeholder="Ej. 30-71234567-9"
          />
          <InputField
            label="Persona de Contacto"
            value={contactoProv}
            onChange={(e) => setContactoProv(e.target.value)}
          />
          <InputField
            label="Teléfono"
            value={telefonoProv}
            onChange={(e) => setTelefonoProv(e.target.value)}
          />
          <InputField
            label="Email"
            type="email"
            value={emailProv}
            onChange={(e) => setEmailProv(e.target.value)}
          />
          <Button type="submit" variant="primary" className="w-full">
            Guardar Proveedor
          </Button>
        </form>
      </Modal>

      <Modal
        isOpen={modalNuevaOcOpen}
        onClose={() => setModalNuevaOcOpen(false)}
        title="Emitir Nueva Orden de Compra a Proveedor"
        maxWidthClass="max-w-2xl"
      >
        <form onSubmit={handleCrearOrdenCompra} className="space-y-4 text-xs">
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1">
              <label className="font-semibold uppercase text-slate-700">
                Proveedor *
              </label>
              <select
                value={ocProveedorId}
                onChange={(e) => setOcProveedorId(Number(e.target.value))}
                className="rounded border border-slate-300 px-3 py-2 text-sm"
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
            />
          </div>
          <div className="space-y-2">
            {ocRenglones.map((r, idx) => (
              <div key={idx} className="grid grid-cols-12 gap-2 items-center">
                <select
                  value={r.productoId}
                  onChange={(e) => {
                    const next = [...ocRenglones];
                    next[idx].productoId = Number(e.target.value);
                    setOcRenglones(next);
                  }}
                  className="col-span-6 rounded border border-slate-300 px-2 py-1.5"
                >
                  {productos.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nombre}
                    </option>
                  ))}
                </select>
                <input
                  type="number"
                  min={1}
                  value={r.cantidadPedida}
                  onChange={(e) => {
                    const next = [...ocRenglones];
                    next[idx].cantidadPedida = Number(e.target.value);
                    setOcRenglones(next);
                  }}
                  className="col-span-3 rounded border border-slate-300 px-2 py-1.5 font-mono"
                />
                <input
                  type="number"
                  step="0.01"
                  value={r.precioUnitarioCosto}
                  onChange={(e) => {
                    const next = [...ocRenglones];
                    next[idx].precioUnitarioCosto = Number(e.target.value);
                    setOcRenglones(next);
                  }}
                  className="col-span-3 rounded border border-slate-300 px-2 py-1.5 font-mono"
                />
              </div>
            ))}
          </div>
          <InputField
            label="Observaciones"
            value={ocObservaciones}
            onChange={(e) => setOcObservaciones(e.target.value)}
          />
          <Button type="submit" variant="primary" className="w-full">
            Emitir Orden de Compra
          </Button>
        </form>
      </Modal>

      <Modal
        isOpen={!!modalAjusteOpen}
        onClose={() => setModalAjusteOpen(null)}
        title={`Movimiento en Kardex — ${modalAjusteOpen?.nombre}`}
      >
        <form
          onSubmit={handleRegistrarMovimientoStock}
          className="space-y-3"
        >
          <select
            value={tipoMov}
            onChange={(e) =>
              setTipoMov(Number(e.target.value) as TipoMovimientoStockEnum)
            }
            className="w-full rounded border border-slate-300 px-3 py-2 text-sm"
          >
            <option value={TipoMovimientoStockEnum.IngresoCompra}>
              1 - Ingreso por Compra
            </option>
            <option value={TipoMovimientoStockEnum.ConsumoInternoSalon}>
              3 - Consumo Interno de Salón
            </option>
            <option value={TipoMovimientoStockEnum.AjusteRoturaPerdida}>
              4 - Ajuste por Rotura / Pérdida
            </option>
          </select>
          <InputField
            label="Cantidad *"
            type="number"
            min={1}
            required
            value={cantidadMov}
            onChange={(e) => setCantidadMov(Number(e.target.value))}
          />
          <InputField
            label="Motivo Auditable *"
            required
            value={motivoMov}
            onChange={(e) => setMotivoMov(e.target.value)}
          />
          <Button type="submit" variant="editorial" className="w-full">
            Asentar en Kardex
          </Button>
        </form>
      </Modal>

      <Modal
        isOpen={!!kardexProducto}
        onClose={() => setKardexProducto(null)}
        title={`Libro Mayor Kardex — ${kardexProducto?.producto.nombre}`}
        maxWidthClass="max-w-2xl"
      >
        <div className="space-y-2 text-xs">
          {(kardexProducto?.movimientos || []).map((m) => (
            <div
              key={m.id}
              className="flex items-center justify-between border-b border-slate-100 py-2.5 gap-3"
            >
              <div>
                <p className="font-bold">
                  {m.tipoMovimientoNombre || `Tipo #${m.tipoMovimiento}`} (
                  {m.cantidad} u.)
                </p>
                <p className="text-slate-500">{m.motivo}</p>
              </div>
              <div className="text-right font-mono">
                {m.saldoResultante != null && (
                  <p className="font-bold text-slate-900">
                    Saldo Resultante: {m.saldoResultante} u.
                  </p>
                )}
                <span className="text-slate-500 text-[11px]">
                  {formatDateTime(m.fecha)}
                </span>
              </div>
            </div>
          ))}
        </div>
      </Modal>
    </div>
  );
}