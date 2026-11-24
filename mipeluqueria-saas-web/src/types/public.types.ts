import { PeluqueroDto, ServicioDto } from './turnos.types';

// ==============================================================================
// PORTAL PÚBLICO DE RESERVAS Y WEBHOOKS DE PAGOS (/api/public & /api/webhooks/pagos)
// ==============================================================================

export interface ReservaPublicaRequestDto {
  nombreCliente: string;
  apellidoCliente: string;
  telefonoCliente: string;
  emailCliente?: string;
  peluqueroId: number;
  fechaHoraInicio: string;
  serviciosIds: number[];
  observaciones?: string;
  pasarelaPreferida?: 'MercadoPago' | 'Stripe';
}

export interface ReservaPublicaResponseDto {
  turnoId: number;
  clienteId: number;
  estadoTurno: string;
  montoTotalEstimado: number;
  requiereSena: boolean;
  montoSena?: number | null;
  checkoutPagoUrl?: string | null;
  preferenciaPagoId?: string | null;
}

export interface WebhookPagoNotificacionDto {
  id?: string;
  topic?: string;
  type?: string;
  externalReference?: string;
  status?: string;
  paymentId?: string;
  transactionAmount?: number;
}

export interface CatalogoPublicoUnificadoDto {
  nombreSalon: string;
  direccion: string;
  telefono: string;
  monedaIso: string;
  porcentajeSenaReserva: number;
  horasAnticipacionCancelacion: number;
  servicios: ServicioDto[];
  peluqueros: PeluqueroDto[];
}
