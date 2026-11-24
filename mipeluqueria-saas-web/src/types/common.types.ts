// ==============================================================================
// 1. WRAPPERS TRANSVERSALES, PAGINACIÓN, ENUMS Y CATÁLOGOS BASE (CAPA 4 SDD)
// ==============================================================================

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors?: string[] | null;
  fieldErrors?: Record<string, string[]> | null;
  warnings?: string[] | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface PaginationFilterDto {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDescending?: boolean;
  fechaDesde?: string;
  fechaHasta?: string;
}

export interface AuditableEntityDto {
  id: number;
  createdAt: string;
  createdByUserId?: number | null;
  updatedAt?: string | null;
  updatedByUserId?: number | null;
  isDeleted: boolean;
  deletedAt?: string | null;
  deletedByUserId?: number | null;
}

// ==============================================================================
// 2. ENUMS EXACTOS DEL DOMINIO C# (.NET 8)
// ==============================================================================

export enum EstadoSuscripcionEnum {
  Trial = 1,
  Activa = 2,
  PagoPendiente = 3,
  Suspendida = 4,
  Cancelada = 5,
}

export enum MotivoFalloSesionEnum {
  Ninguno = 0,
  CredencialesIncorrectas = 1,
  CuentaBloqueada = 2,
  CuentaInactiva = 3,
  EmailNoVerificado = 4,
  FaltaAceptarTerminos = 5,
  BotDetectado = 6,
  CodigoOTPInvalido = 7,
  IPBloqueada = 8,
}

export enum EstadoOrdenCompraEnum {
  Borrador = 1,
  Emitida = 2,
  Recibida = 3,
  Cancelada = 4,
}

export enum TipoMovimientoStockEnum {
  IngresoCompra = 1,
  EgresoVentaMostrador = 2,
  ConsumoInternoSalon = 3,
  AjusteRoturaPerdida = 4,
}

export enum EstadoTurnoEnum {
  Pendiente = 1,
  Confirmado = 2,
  EnProceso = 3,
  Completado = 4,
  Cancelado = 5,
  NoAsistio = 6,
}

export enum EstadoLiquidacionEnum {
  Pendiente = 1,
  Pagado = 2,
  Atrasado = 3,
  Anulada = 4,
}

export enum TipoItemVentaEnum {
  Servicio = 1,
  Producto = 2,
}

export type RolNombreType =
  | 'Administrador'
  | 'Recepcionista'
  | 'Cajero'
  | 'Peluquero'
  | 'Cliente'
  | 'SuperAdminSaaS';

// ==============================================================================
// 3. CONFIGURACIÓN DEL SALÓN Y 7 CATÁLOGOS CONFIGURABLES (Common)
// ==============================================================================

export interface ConfiguracionSalonDto {
  id?: number;
  nombreSalon?: string;
  nombreComercial: string;
  razonSocial: string;
  cuit: string;
  direccion: string;
  telefono: string;
  emailContacto: string;
  monedaIso?: string;
  moneda: string;
  timeZoneId?: string;
  horarioAtencionTexto?: string;
  intervaloTurnosMinutos: number;
  minutosToleranciaEspera?: number;
  horasAnticipacionCancelacion?: number;
  porcentajeSenaReserva?: number;
  notificacionesWhatsAppActivas?: boolean;
  notificacionesEmailActivas?: boolean;
}

export type ActualizarConfiguracionSalonDto = ConfiguracionSalonDto;

export interface CatalogoItemDto {
  id: number;
  nombre: string;
  descripcion?: string | null;
  esEfectivoFisico?: boolean | null;
  colorHex?: string | null;
  activo?: boolean;
}

export interface CrearCatalogoItemDto {
  nombre: string;
  descripcion?: string | null;
  esEfectivoFisico?: boolean | null;
}

export interface ActualizarCatalogoItemDto {
  nombre: string;
  descripcion?: string | null;
  esEfectivoFisico?: boolean | null;
}

export type TipoCatalogoSalon =
  | 'tipos-cabello'
  | 'tipos-rostro'
  | 'categorias-servicio'
  | 'especialidades'
  | 'motivos-cancelacion'
  | 'categorias-producto'
  | 'medios-pago';

export interface CatalogosMaestrosDto {
  tiposCabello: CatalogoItemDto[];
  tiposRostro: CatalogoItemDto[];
  categoriasServicio: CatalogoItemDto[];
  especialidades: CatalogoItemDto[];
  estadosTurno: CatalogoItemDto[];
  motivosCancelacion: CatalogoItemDto[];
  categoriasProducto: CatalogoItemDto[];
  mediosPago: CatalogoItemDto[];
  roles: CatalogoItemDto[];
}
