import { apiClient } from '@/lib/axios';
import {
  extractPagedItems,
  toNormalizedPagedResult,
} from '@/lib/apiHelpers';
import {
  ActualizarEstadoUsuarioDto,
  ApiResponse,
  AuthResponseDto,
  CambiarPasswordDto,
  CambiarRolUsuarioDto,
  CrearUsuarioStaffDto,
  HistorialSesionDto,
  LoginRequestDto,
  OlvidePasswordDto,
  PagedResult,
  PaginationFilterDto,
  RefreshTokenRequestDto,
  RegistroUsuarioDto,
  RestablecerPasswordDto,
  UsuarioDto,
  VerificarEmailDto,
} from '@/types/api.types';

function normalizeHistorialSesion(raw: HistorialSesionDto): HistorialSesionDto {
  const fechaIntento = raw.fechaIntento || raw.fechaHora || new Date().toISOString();
  const direccionIP = raw.direccionIP || raw.ipOrigen || '127.0.0.1';
  const motivoFalloTexto =
    raw.motivoFalloTexto ||
    (typeof raw.motivoFallo === 'string' ? raw.motivoFallo : undefined);

  return {
    ...raw,
    fechaHora: raw.fechaHora || fechaIntento,
    fechaIntento,
    ipOrigen: raw.ipOrigen || direccionIP,
    direccionIP,
    motivoFalloTexto,
  };
}

// 1. AuthController (/api/auth)
export const authService = {
  async login(dto: LoginRequestDto): Promise<ApiResponse<AuthResponseDto>> {
    const { data } = await apiClient.post<ApiResponse<AuthResponseDto>>(
      '/auth/login',
      dto
    );
    return data;
  },

  async refreshToken(
    dto: RefreshTokenRequestDto
  ): Promise<ApiResponse<AuthResponseDto>> {
    const { data } = await apiClient.post<ApiResponse<AuthResponseDto>>(
      '/auth/refresh-token',
      dto
    );
    return data;
  },

  async registro(dto: RegistroUsuarioDto): Promise<ApiResponse<boolean>> {
    const payload = {
      ...dto,
      rolId: 4, // RolId = 4 (Cliente) fijo según 04_API_CONTRACTS.md
    };
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      '/auth/registro',
      payload
    );
    return data;
  },

  async verificarEmail(dto: VerificarEmailDto): Promise<ApiResponse<boolean>> {
    const codigo = dto.codigoVerificacion || dto.codigoOtp || '';
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      '/auth/verificar-email',
      {
        email: dto.email,
        codigoVerificacion: codigo,
        codigoOtp: codigo,
      }
    );
    return data;
  },

  async cambiarPassword(
    dto: CambiarPasswordDto
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      '/auth/cambiar-password',
      dto
    );
    return data;
  },

  async olvidePassword(dto: OlvidePasswordDto): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      '/auth/olvide-password',
      dto
    );
    return data;
  },

  async restablecerPassword(
    dto: RestablecerPasswordDto
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      '/auth/restablecer-password',
      dto
    );
    return data;
  },
};

// 2. UsuariosController (/api/usuarios)
export const usuariosService = {
  async getAll(): Promise<ApiResponse<UsuarioDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>('/usuarios');
    return {
      ...data,
      data: extractPagedItems<UsuarioDto>(data.data),
    };
  },

  async getById(id: number): Promise<ApiResponse<UsuarioDto>> {
    const { data } = await apiClient.get<ApiResponse<UsuarioDto>>(
      `/usuarios/${id}`
    );
    return data;
  },

  async crearStaff(
    dto: CrearUsuarioStaffDto
  ): Promise<ApiResponse<UsuarioDto>> {
    const { data } = await apiClient.post<ApiResponse<UsuarioDto>>(
      '/usuarios',
      dto
    );
    return data;
  },

  async actualizarEstado(
    id: number,
    dto: ActualizarEstadoUsuarioDto
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.put<ApiResponse<UsuarioDto | boolean>>(
      `/usuarios/${id}/estado`,
      dto
    );
    return {
      ...data,
      data: data.success,
    };
  },

  async cambiarRol(
    id: number,
    rolIdOrDto: number | CambiarRolUsuarioDto,
    _activo?: boolean
  ): Promise<ApiResponse<boolean>> {
    const rolId =
      typeof rolIdOrDto === 'number'
        ? rolIdOrDto
        : rolIdOrDto.rolId || rolIdOrDto.nuevoRolId || 4;
    const { data } = await apiClient.patch<ApiResponse<UsuarioDto | boolean>>(
      `/usuarios/${id}/rol`,
      { rolId, nuevoRolId: rolId }
    );
    return {
      ...data,
      data: data.success,
    };
  },

  async desbloquear(id: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<UsuarioDto | boolean>>(
      `/usuarios/${id}/desbloquear`
    );
    return {
      ...data,
      data: data.success,
    };
  },

  async getHistorialSesionesPaged(
    filter: PaginationFilterDto = {},
    usuarioId?: number
  ): Promise<ApiResponse<PagedResult<HistorialSesionDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/usuarios/historial-sesiones',
      {
        params: {
          ...filter,
          usuarioId,
        },
      }
    );

    return {
      ...data,
      data: toNormalizedPagedResult<HistorialSesionDto>(
        data.data,
        normalizeHistorialSesion,
        {
          pageNumber: filter.pageNumber,
          pageSize: filter.pageSize,
        }
      ),
    };
  },

  async getHistorialSesiones(
    usuarioId?: number,
    top: number = 100
  ): Promise<ApiResponse<HistorialSesionDto[]>> {
    const pagedRes = await this.getHistorialSesionesPaged(
      { pageNumber: 1, pageSize: Math.min(top, 100) },
      usuarioId
    );
    return {
      ...pagedRes,
      data: extractPagedItems<HistorialSesionDto>(pagedRes.data),
    };
  },
};
