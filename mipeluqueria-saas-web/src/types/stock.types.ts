import {
  EstadoOrdenCompraEnum,
  TipoMovimientoStockEnum,
} from './common.types';

// ==============================================================================
// STOCK, KARDEX, PROVEEDORES Y ÓRDENES DE COMPRA
// ==============================================================================

export interface ProductoDto {
  id: number;
  categoriaId: number;
  categoriaNombre?: string;
  proveedorId?: number | null;
  proveedorNombre?: string | null;
  proveedorRazonSocial?: string | null;
  codigoBarra?: string | null;
  codigoBarras?: string | null;
  nombre: string;
  precioCosto: number;
  precioVenta: number;
  stockActual: number;
  stockMinimo?: number;
  stockMinimoAlerta: number;
  esParaVenta: boolean;
  alertaStockCritico?: boolean;
}

export interface CrearProductoDto {
  categoriaId: number;
  proveedorId?: number | null;
  codigoBarra?: string | null;
  codigoBarras?: string | null;
  nombre: string;
  precioCosto: number;
  precioVenta: number;
  stockActual: number;
  stockMinimoAlerta: number;
  esParaVenta: boolean;
}

export interface ActualizarProductoDto {
  categoriaId: number;
  proveedorId?: number | null;
  codigoBarra?: string | null;
  codigoBarras?: string | null;
  nombre: string;
  precioCosto: number;
  precioVenta: number;
  stockActual?: number;
  stockMinimoAlerta: number;
  esParaVenta: boolean;
}

export interface MovimientoStockDto {
  id?: number;
  productoId: number;
  productoNombre?: string;
  tipoMovimiento: TipoMovimientoStockEnum;
  tipoMovimientoDescripcion?: string;
  tipoMovimientoNombre?: string;
  cantidad: number;
  saldoResultante?: number;
  fecha?: string;
  usuarioId?: number | null;
  motivo: string;
}

export interface KardexItemResponseDto {
  id: number;
  productoId: number;
  productoNombre: string;
  tipoMovimiento: TipoMovimientoStockEnum;
  tipoMovimientoDescripcion: string;
  tipoMovimientoNombre?: string;
  cantidad: number;
  saldoResultante: number;
  fecha: string;
  usuarioId?: number | null;
  motivo: string;
}

export interface ProveedorDto {
  id: number;
  razonSocial: string;
  cuit?: string | null;
  contacto?: string | null;
  telefono?: string | null;
  email?: string | null;
  cantidadProductosAsociados: number;
}

export interface CrearProveedorDto {
  razonSocial: string;
  cuit?: string | null;
  contacto?: string;
  telefono?: string;
  email?: string;
}

export interface ActualizarProveedorDto {
  razonSocial: string;
  cuit?: string | null;
  contacto?: string;
  telefono?: string;
  email?: string;
}

export interface DetalleOrdenCompraDto {
  id?: number;
  ordenCompraId?: number;
  productoId: number;
  productoNombre?: string;
  cantidadPedida: number;
  cantidadRecibida: number;
  precioUnitarioCosto: number;
  subtotal: number;
}

export type DetalleOrdenCompraResponseDto = DetalleOrdenCompraDto;

export interface DetalleOrdenCompraItemDto {
  productoId: number;
  cantidadPedida: number;
  precioUnitarioCosto: number;
}

export interface DetalleRecepcionItemDto {
  detalleOrdenCompraId?: number;
  productoId: number;
  cantidadRecibida: number;
  precioUnitarioCostoReal?: number;
  nuevoPrecioVentaSugerido?: number | null;
}

export interface OrdenCompraDto {
  id: number;
  proveedorId: number;
  proveedorRazonSocial: string;
  numeroComprobanteProveedor?: string | null;
  fechaEmision: string;
  fechaRecepcion?: string | null;
  estado: EstadoOrdenCompraEnum;
  estadoNombre?: string;
  total: number;
  observaciones?: string | null;
  detalles: DetalleOrdenCompraDto[];
}

export type OrdenCompraResponseDto = OrdenCompraDto;

export interface CrearOrdenCompraDto {
  proveedorId: number;
  numeroComprobanteProveedor?: string;
  observaciones?: string;
  detalles: DetalleOrdenCompraItemDto[];
}

export interface RecepcionarOrdenCompraDto {
  numeroComprobanteProveedor?: string;
  actualizarCostoProductos?: boolean;
  actualizarPreciosCosto?: boolean;
  imputarEgresoEnCajaAbierta?: boolean;
  imputarPagoEnCaja?: boolean;
  sesionCajaId?: number | null;
  observacionesRecepcion?: string;
  cantidadesRecibidas?: DetalleRecepcionItemDto[];
  items?: DetalleRecepcionItemDto[];
}
