import { EstadoTurnoEnum } from './common.types';

// ==============================================================================
// TURNOS, PELUQUEROS, SERVICIOS, RECETAS Y BLOQUEOS
// ==============================================================================

export interface ServicioDto {
  id: number;
  categoriaId: number;
  categoriaNombre?: string;
  nombre: string;
  descripcion?: string | null;
  precio: number;
  duracionMinutos: number;
  activo: boolean;
}

export interface CrearServicioDto {
  categoriaId: number;
  nombre: string;
  descripcion?: string;
  precio: number;
  duracionMinutos: number;
  activo: boolean;
}

export interface ActualizarServicioDto {
  categoriaId: number;
  nombre: string;
  descripcion?: string;
  precio: number;
  duracionMinutos: number;
  activo: boolean;
}

export interface ItemRecetaInputDto {
  productoId: number;
  cantidadUnidadesDescontar: number;
  cantidadMedidaEstandar: number;
  unidadMedida: string;
  descuentoAutomaticoAlCompletarTurno: boolean;
}

export interface RecetaInsumoItemDto {
  id?: number;
  servicioId?: number;
  servicioNombre?: string;
  productoId: number;
  productoNombre?: string;
  stockActualProducto?: number;
  precioCostoUnitario?: number;
  cantidadUnidadesDescontar: number;
  cantidadMedidaEstandar: number;
  unidadMedida: 'gr' | 'ml' | 'Unid' | 'Ampolla' | string;
  descuentoAutomaticoAlCompletarTurno: boolean;
  costoEstimadoInsumo?: number;
  costoEstimadoItem?: number;
}

export type RecetaItemResponseDto = RecetaInsumoItemDto;

export interface ConfigurarRecetaServicioDto {
  servicioId: number;
  insumos?: RecetaInsumoItemDto[];
  items?: RecetaInsumoItemDto[];
}

export interface RecetaServicioResponseDto {
  servicioId: number;
  servicioNombre: string;
  precioVentaServicio: number;
  costoTotalEstimadoInsumos: number;
  margenNetoEstimado: number;
  items: RecetaInsumoItemDto[];
  insumos?: RecetaInsumoItemDto[];
}

export interface HorarioLaboralDto {
  id?: number;
  peluqueroId?: number;
  diaSemana: number; // 0 = Domingo, 6 = Sábado
  horaInicio: string; // "09:00:00" o "09:00"
  horaFin: string;
  horaInicioDescanso?: string | null;
  horaFinDescanso?: string | null;
  activo?: boolean;
}

export interface GuardarHorariosPeluqueroDto {
  horarios: HorarioLaboralDto[];
}

export interface PeluqueroDto {
  id: number;
  nombre: string;
  apellido: string;
  nombreCompleto?: string;
  telefono?: string | null;
  email: string;
  colorAgendaHex: string;
  activo: boolean;
  especialidadesIds?: number[];
  especialidadesNombres?: string[];
  especialidades?: string[];
}

export interface CrearPeluqueroDto {
  nombre: string;
  apellido: string;
  telefono?: string;
  email: string;
  colorAgendaHex: string;
  activo: boolean;
  especialidadesIds: number[];
}

export interface ActualizarPeluqueroDto {
  nombre: string;
  apellido: string;
  telefono?: string;
  email: string;
  colorAgendaHex: string;
  activo: boolean;
  especialidadesIds: number[];
}

export interface BloqueoHorarioDto {
  id?: number;
  peluqueroId: number;
  peluqueroNombre?: string;
  fechaInicio: string;
  fechaFin: string;
  motivo: string;
}

export interface BloqueoHorarioResponseDto {
  id: number;
  peluqueroId: number;
  peluqueroNombre: string;
  fechaInicio: string;
  fechaFin: string;
  motivo: string;
}

export interface TurnoServicioDetalleDto {
  turnoId?: number;
  servicioId: number;
  servicioNombre: string;
  precioHistorico: number;
  duracionMinutosHistorica: number;
}

export interface TurnoDto {
  id: number;
  clienteId: number;
  clienteNombre?: string;
  clienteNombreCompleto: string;
  clienteTelefono?: string | null;
  tieneAlertaAlergia?: boolean;
  notasAlergiaCliente?: string | null;
  peluqueroId: number;
  peluqueroNombre?: string;
  peluqueroNombreCompleto: string;
  colorAgendaHex?: string;
  fechaHoraInicio: string;
  fechaHoraFin: string;
  duracionTotalMinutos?: number;
  estadoId: EstadoTurnoEnum;
  estadoNombre: string;
  motivoCancelacionId?: number | null;
  motivoCancelacionDescripcion?: string | null;
  montoTotalEstimado: number;
  requiereSena?: boolean;
  montoSena?: number;
  senaPagada?: boolean;
  fechaPagoSena?: string | null;
  referenciaPagoExterno?: string | null;
  saldoPendienteCobro?: number;
  observaciones?: string | null;
  serviciosIds?: number[];
  serviciosNombres?: string[];
  serviciosDetalle?: TurnoServicioDetalleDto[];
  servicios: TurnoServicioDetalleDto[];
}

export type TurnoResponseDto = TurnoDto;

export interface CrearTurnoDto {
  clienteId: number;
  peluqueroId: number;
  fechaHoraInicio: string;
  serviciosIds: number[];
  observaciones?: string;
}

export interface ReprogramarTurnoDto {
  peluqueroId: number;
  nuevaFechaHoraInicio: string;
  observaciones?: string;
}

export interface CambiarEstadoTurnoDto {
  nuevoEstadoId: EstadoTurnoEnum;
  motivoCancelacionId?: number | null;
  observaciones?: string;
}

export interface HistorialEstadoTurnoDto {
  id: number;
  turnoId: number;
  estadoAnteriorId?: number | null;
  estadoAnteriorNombre?: string | null;
  estadoNuevoId: number;
  estadoNuevoNombre: string;
  fechaCambio: string;
  usuarioId?: number | null;
  observaciones?: string | null;
}

export type HistorialEstadoTurnoResponseDto = HistorialEstadoTurnoDto;

export interface SlotHorarioDisponibleDto {
  fechaHoraInicio: string;
  fechaHoraFin: string;
  horaInicioTexto: string;
  horaFinTexto: string;
  disponible: boolean;
}

export interface DisponibilidadHorarioResponseDto {
  peluqueroId: number;
  peluqueroNombre: string;
  fecha: string;
  intervaloMinutos: number;
  duracionTotalMinutos: number;
  horariosDisponibles: string[];
  slots: SlotHorarioDisponibleDto[];
}
