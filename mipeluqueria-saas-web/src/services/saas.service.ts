import { apiClient } from '@/lib/axios';
import { extractPagedItems } from '@/lib/apiHelpers';
import {
  ApiResponse,
  CambiarPlanSuscripcionDto,
  CrearSucursalDto,
  PlanSuscripcionDto,
  SucursalResponseDto,
  SuscripcionEstadoResponseDto,
} from '@/types/api.types';

// 21. SucursalesController (/api/sucursales)
export const sucursalesService = {
  async getAll(): Promise<ApiResponse<SucursalResponseDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>('/sucursales');
    return {
      ...data,
      data: extractPagedItems<SucursalResponseDto>(data.data),
    };
  },

  async crear(dto: CrearSucursalDto): Promise<ApiResponse<number>> {
    const { data } = await apiClient.post<
      ApiResponse<SucursalResponseDto | number>
    >('/sucursales', dto);

    const sucursalId =
      typeof data.data === 'number'
        ? data.data
        : data.data?.id ?? 0;

    return {
      ...data,
      data: sucursalId,
    };
  },

  async crearSucursalCompleta(
    dto: CrearSucursalDto
  ): Promise<ApiResponse<SucursalResponseDto>> {
    const { data } = await apiClient.post<ApiResponse<SucursalResponseDto>>(
      '/sucursales',
      dto
    );
    return data;
  },
};

// 22. SuscripcionesController (/api/suscripciones)
export const suscripcionesService = {
  async getPlanes(): Promise<ApiResponse<PlanSuscripcionDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/suscripciones/planes'
    );
    return {
      ...data,
      data: extractPagedItems<PlanSuscripcionDto>(data.data),
    };
  },

  async getMiSuscripcion(): Promise<
    ApiResponse<SuscripcionEstadoResponseDto>
  > {
    const { data } = await apiClient.get<
      ApiResponse<SuscripcionEstadoResponseDto>
    >('/suscripciones/mi-suscripcion');
    return data;
  },

  async cambiarPlan(
    dto: CambiarPlanSuscripcionDto
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      '/suscripciones/cambiar-plan',
      dto
    );
    return data;
  },
};
