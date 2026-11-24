import {
  EstadoLiquidacionEnum,
  TipoItemVentaEnum,
} from './common.types';

// ==============================================================================
// TESORERÍA (CAJA), VENTAS POS, COMISIONES, LIQUIDACIONES Y DASHBOARD BI
// ==============================================================================

export interface MovimientoCajaDto {
  id: number;
  sesionCajaId: number;
  tipo: string;
  monto: number;
  concepto: string;
  fecha: string;
  usuarioId?: number | null;
}

export type MovimientoCajaResponseDto = MovimientoCajaDto;

export interface SesionCajaDto {
  id: number;
  usuarioAperturaId: number;
  usuarioAperturaNombre?: string | null;
  fechaApertura: string;
  montoInicial: number;
  usuarioCierreId?: number | null;
  usuarioCierreNombre?: string | null;
  fechaCierre?: string | null;
  totalVentasEfectivo: number;
  totalVentasDigital: number;
  totalVentasOtrosMedios?: number;
  totalIngresosManuales: number;
  totalEgresosManuales: number;
  montoFinalEsperado: number;
  montoFinalReal?: number | null;
  diferencia?: number | null;
  observacionesCierre?: string | null;
  estadoAbierta: boolean;
  movimientos: MovimientoCajaDto[];
  movimientosCaja?: MovimientoCajaDto[];
}

export type SesionCajaResponseDto = SesionCajaDto;

export interface AperturaCajaDto {
  montoInicial: number;
  observaciones?: string;
}

export interface RegistrarMovimientoCajaDto {
  sesionCajaId: number;
  monto: number;
  concepto: string;
  tipo?: string;
}

export interface CierreCajaDto {
  sesionCajaId?: number;
  montoFinalReal: number;
  observacionesCierre?: string;
  observaciones?: string;
}

export interface CrearDetalleVentaItemDto {
  tipoItem: TipoItemVentaEnum | string;
  itemId: number;
  nombreItem?: string;
  cantidad: number;
  precioUnitario: number;
  peluqueroId?: number | null;
}

export type DetalleVentaRequestDto = CrearDetalleVentaItemDto;

export interface CrearPagoVentaItemDto {
  medioPagoId: number;
  medioPagoNombre?: string;
  importe: number;
  referenciaOperacion?: string | null;
}

export type PagoRequestDto = CrearPagoVentaItemDto;

export interface CrearVentaDto {
  sesionCajaId: number;
  clienteId?: number | null;
  turnoId?: number | null;
  idempotencyKey?: string | null;
  detalles: CrearDetalleVentaItemDto[];
  pagos: CrearPagoVentaItemDto[];
}

export interface VentaDetalleItemResponseDto {
  id: number;
  tipoItem: TipoItemVentaEnum | string;
  itemId: number;
  conceptoNombre: string;
  itemNombre: string;
  cantidad: number;
  precioUnitario: number;
  subtotal: number;
  peluqueroId?: number | null;
  peluqueroNombre?: string | null;
}

export interface VentaPagoItemResponseDto {
  id: number;
  medioPagoId: number;
  medioPagoNombre: string;
  importe: number;
  referenciaOperacion?: string | null;
}

export interface VentaResponseDto {
  id: number;
  sesionCajaId: number;
  clienteId?: number | null;
  clienteNombre: string;
  clienteNombreCompleto?: string | null;
  turnoId?: number | null;
  usuarioId: number;
  fecha: string;
  subtotal: number;
  descuentoSenaAplicado: number;
  total: number;
  idempotencyKey?: string | null;
  anulada?: boolean;
  detalles: VentaDetalleItemResponseDto[];
  pagos: VentaPagoItemResponseDto[];
}

export interface AnularVentaDto {
  motivo: string;
}

export interface ComisionConfigDto {
  id: number;
  peluqueroId: number;
  peluqueroNombre?: string;
  peluqueroNombreCompleto?: string;
  servicioId?: number | null;
  servicioNombre?: string | null;
  porcentajeServicio: number;
  porcentajeProducto?: number;
  porcentajeVentaProducto: number;
}

export interface GuardarComisionConfigDto {
  peluqueroId: number;
  servicioId?: number | null;
  porcentajeServicio: number;
  porcentajeProducto?: number;
  porcentajeVentaProducto: number;
}

export interface DetalleLiquidacionDto {
  id: number;
  liquidacionId?: number;
  detalleVentaId: number;
  turnoId?: number | null;
  tipoItem: TipoItemVentaEnum | string;
  itemId?: number;
  conceptoNombre: string;
  conceptoDescripcion?: string;
  subtotalVenta: number;
  importeBaseVenta?: number;
  porcentajeAplicado?: number;
  montoComisionCalculado: number;
  fechaVenta?: string | null;
  fechaItem?: string;
}

export type DetalleLiquidacionItemDto = DetalleLiquidacionDto;

export interface LiquidacionDto {
  id: number;
  peluqueroId: number;
  peluqueroNombre: string;
  peluqueroNombreCompleto: string;
  fechaEmision: string;
  fechaDesde: string;
  fechaHasta: string;
  fechaVencimiento?: string | null;
  fechaPago?: string | null;
  totalServicios: number;
  totalProductos: number;
  bonificaciones: number;
  descuentos: number;
  totalPagar: number;
  cantidadItemsLiquidados?: number;
  estado: EstadoLiquidacionEnum;
  estadoNombre: string;
  observaciones?: string | null;
  detalles: DetalleLiquidacionDto[];
  detallesLiquidacion?: DetalleLiquidacionDto[];
}

export type LiquidacionResponseDto = LiquidacionDto;

export interface GenerarLiquidacionDto {
  peluqueroId: number;
  fechaDesde: string;
  fechaHasta: string;
  fechaVencimiento?: string;
  bonificaciones?: number;
  descuentos?: number;
  observaciones?: string;
}

export interface IngresoPorMedioPagoDto {
  medioPago: string;
  medioPagoNombre: string;
  totalRecaudado: number;
  montoTotal: number;
  cantidadOperaciones: number;
  porcentaje: number;
}

export interface RankingPeluqueroDto {
  peluqueroId: number;
  nombrePeluquero: string;
  peluqueroNombre: string;
  serviciosRealizados: number;
  cantidadServicios: number;
  totalFacturado: number;
  facturacionTotal: number;
  cantidadProductosStockCritico: number;
}

export interface DashboardKpisDto {
  fechaDesde?: string;
  fechaHasta?: string;
  facturacionTotalMes?: number;
  ingresosTotalesPeriodo?: number;
  facturacionTotal: number;
  cantidadVentasMes?: number;
  totalVentasPeriodo?: number;
  cantidadVentas: number;
  ticketPromedio: number;
  totalTurnosAtendidosMes?: number;
  turnosCompletadosPeriodo?: number;
  turnosCompletados: number;
  turnosCanceladosPeriodo?: number;
  turnosCancelados: number;
  tasaCancelacionPorcentaje: number;
  turnosPendientesHoy: number;
  clientesNuevosPeriodo?: number;
  productosEnStockCritico?: number;
  productosBajoStockCritico?: number;
  servicioMasVendido: string;
  peluqueroEstrella: string;
  peluqueroTopPeriodo?: string;
  ingresosPorMedioPago: IngresoPorMedioPagoDto[];
  recaudacionPorMedioPago: IngresoPorMedioPagoDto[];
  rankingPeluqueros: RankingPeluqueroDto[];
}

export type DashboardKpiResponseDto = DashboardKpisDto;
