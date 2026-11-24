import { apiClient } from '@/lib/axios';
import { extractPagedItems } from '@/lib/apiHelpers';
import {
  ApiResponse,
  CatalogoPublicoUnificadoDto,
  DisponibilidadHorarioResponseDto,
  PeluqueroDto,
  ReservaPublicaRequestDto,
  ReservaPublicaResponseDto,
  ServicioDto,
  WebhookPagoNotificacionDto,
} from '@/types/api.types';
import { normalizePeluquero } from './turnos.service';

// 5. PortalReservasController (/api/public) & WebhooksController (/api/webhooks/pagos)
export const portalPublicoService = {
  async getCatalogoUnificado(): Promise<
    ApiResponse<CatalogoPublicoUnificadoDto>
  > {
    const { data } = await apiClient.get<
      ApiResponse<CatalogoPublicoUnificadoDto>
    >('/public/catalogo');
    if (data.data) {
      data.data = {
        ...data.data,
        servicios: extractPagedItems<ServicioDto>(data.data.servicios),
        peluqueros: extractPagedItems<PeluqueroDto>(data.data.peluqueros).map(
          normalizePeluquero
        ),
      };
    }
    return data;
  },

  async getCatalogoServicios(): Promise<ApiResponse<ServicioDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/public/catalogo-servicios'
    );
    return {
      ...data,
      data: extractPagedItems<ServicioDto>(data.data),
    };
  },

  async getPeluqueros(): Promise<ApiResponse<PeluqueroDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/public/peluqueros'
    );
    return {
      ...data,
      data: extractPagedItems<PeluqueroDto>(data.data).map(normalizePeluquero),
    };
  },

  async getDisponibilidadCompleta(
    peluqueroId: number,
    fecha: string,
    serviciosIds: number[] = []
  ): Promise<ApiResponse<DisponibilidadHorarioResponseDto>> {
    const params = new URLSearchParams();
    params.append('peluqueroId', String(peluqueroId));
    params.append('fecha', fecha);
    serviciosIds.forEach((id) => params.append('serviciosIds', String(id)));

    const { data } = await apiClient.get<
      ApiResponse<DisponibilidadHorarioResponseDto | string[]>
    >(`/public/disponibilidad?${params.toString()}`);

    if (Array.isArray(data.data)) {
      return {
        ...data,
        data: {
          peluqueroId,
          peluqueroNombre: '',
          fecha,
          intervaloMinutos: 30,
          duracionTotalMinutos: 30,
          horariosDisponibles: data.data,
          slots: data.data.map((hora) => ({
            fechaHoraInicio: `${fecha}T${hora}:00`,
            fechaHoraFin: `${fecha}T${hora}:00`,
            horaInicioTexto: hora,
            horaFinTexto: hora,
            disponible: true,
          })),
        },
      };
    }

    if (data.data) {
      return {
        ...data,
        data: {
          ...data.data,
          horariosDisponibles: extractPagedItems<string>(
            data.data.horariosDisponibles
          ),
          slots: extractPagedItems(data.data.slots),
        },
      };
    }

    return {
      ...data,
      data: {
        peluqueroId,
        peluqueroNombre: '',
        fecha,
        intervaloMinutos: 30,
        duracionTotalMinutos: 30,
        horariosDisponibles: [],
        slots: [],
      },
    };
  },

  async getDisponibilidad(
    peluqueroId: number,
    fecha: string,
    serviciosIds: number[] = []
  ): Promise<ApiResponse<string[]>> {
    const full = await this.getDisponibilidadCompleta(
      peluqueroId,
      fecha,
      serviciosIds
    );
    return {
      ...full,
      data: extractPagedItems<string>(full.data?.horariosDisponibles),
    };
  },

  async reservar(
    dto: ReservaPublicaRequestDto
  ): Promise<ApiResponse<ReservaPublicaResponseDto>> {
    const { data } = await apiClient.post<
      ApiResponse<ReservaPublicaResponseDto>
    >('/public/reservar', dto);
    return data;
  },

  async simularWebhookPago(
    proveedor: 'MercadoPago' | 'Stripe',
    payload: WebhookPagoNotificacionDto,
    signature: string = 'firma-hmac-valida'
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      `/webhooks/pagos/${proveedor}`,
      payload,
      {
        headers: { 'x-signature': signature },
      }
    );
    return data;
  },
};
