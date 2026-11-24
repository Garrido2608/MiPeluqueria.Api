import { apiClient } from '@/lib/axios';
import {
  extractPagedItems,
  toNormalizedPagedResult,
} from '@/lib/apiHelpers';
import {
  ActualizarFichaTecnicaDto,
  ApiResponse,
  ClienteDto,
  ClienteRequestDto,
  CrearFichaTecnicaDto,
  FichaTecnicaDto,
  PagedResult,
  PaginationFilterDto,
} from '@/types/api.types';

function normalizeCliente(raw: ClienteDto): ClienteDto {
  const full = (raw.nombreCompleto || '').trim();
  let nombre = raw.nombre || '';
  let apellido = raw.apellido || '';

  if ((!nombre || !apellido) && full) {
    const parts = full.split(/\s+/);
    nombre = nombre || parts[0] || '';
    apellido = apellido || parts.slice(1).join(' ') || '';
  }

  const nombreCompleto =
    full || `${nombre} ${apellido}`.trim() || 'Cliente Registrado';

  return {
    ...raw,
    nombre,
    apellido,
    nombreCompleto,
    fechaRegistro:
      raw.fechaRegistro || raw.fechaNacimiento || new Date().toISOString(),
    activo: raw.activo ?? true,
  };
}

function normalizeFichaTecnica(raw: FichaTecnicaDto): FichaTecnicaDto {
  const fecha = raw.fecha || raw.fechaRegistro || new Date().toISOString();
  const formulaAplicada =
    raw.formulaAplicada || raw.detalleFormulaTintura || '';
  const detalleFormulaTintura =
    raw.detalleFormulaTintura || raw.formulaAplicada || '';
  const clienteNombreCompleto =
    raw.clienteNombreCompleto || raw.nombreCliente || '';
  const peluqueroNombreCompleto =
    raw.peluqueroNombreCompleto || raw.nombrePeluquero || '';

  return {
    ...raw,
    fecha,
    fechaRegistro: raw.fechaRegistro || fecha,
    formulaAplicada,
    detalleFormulaTintura,
    nombreCliente: raw.nombreCliente || clienteNombreCompleto,
    clienteNombreCompleto,
    nombrePeluquero: raw.nombrePeluquero || peluqueroNombreCompleto,
    peluqueroNombreCompleto,
  };
}

// 11. ClientesController (/api/clientes)
export const clientesService = {
  async getPaged(
    filter: PaginationFilterDto = {}
  ): Promise<ApiResponse<PagedResult<ClienteDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/clientes',
      { params: filter }
    );

    return {
      ...data,
      data: toNormalizedPagedResult<ClienteDto>(
        data.data,
        normalizeCliente,
        {
          pageNumber: filter.pageNumber,
          pageSize: filter.pageSize,
        }
      ),
    };
  },

  async getAll(
    pageNumber: number = 1,
    pageSize: number = 15,
    search?: string
  ): Promise<ApiResponse<PagedResult<ClienteDto>>> {
    return this.getPaged({ pageNumber, pageSize, search });
  },

  async getById(id: number): Promise<ApiResponse<ClienteDto>> {
    const { data } = await apiClient.get<ApiResponse<ClienteDto>>(
      `/clientes/${id}`
    );
    if (data.data) {
      data.data = normalizeCliente(data.data);
    }
    return data;
  },

  async create(dto: ClienteRequestDto): Promise<ApiResponse<ClienteDto>> {
    const { data } = await apiClient.post<ApiResponse<ClienteDto>>(
      '/clientes',
      dto
    );
    if (data.data) {
      data.data = normalizeCliente(data.data);
    }
    return data;
  },

  async update(
    id: number,
    dto: ClienteRequestDto
  ): Promise<ApiResponse<ClienteDto>> {
    const { data } = await apiClient.put<ApiResponse<ClienteDto>>(
      `/clientes/${id}`,
      dto
    );
    if (data.data) {
      data.data = normalizeCliente(data.data);
    }
    return data;
  },

  async delete(id: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/clientes/${id}`
    );
    return data;
  },
};

// 12. FichasTecnicasController (/api/fichastecnicas & /api/fichas-tecnicas)
export const fichasTecnicasService = {
  async getById(id: number): Promise<ApiResponse<FichaTecnicaDto>> {
    const { data } = await apiClient.get<ApiResponse<FichaTecnicaDto>>(
      `/fichastecnicas/${id}`
    );
    if (data.data) {
      data.data = normalizeFichaTecnica(data.data);
    }
    return data;
  },

  async getPorCliente(
    clienteId: number
  ): Promise<ApiResponse<FichaTecnicaDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      `/fichastecnicas/cliente/${clienteId}`
    );
    return {
      ...data,
      data: extractPagedItems<FichaTecnicaDto>(data.data).map(
        normalizeFichaTecnica
      ),
    };
  },

  async crear(
    dto: CrearFichaTecnicaDto
  ): Promise<ApiResponse<FichaTecnicaDto>> {
    const formula = dto.formulaAplicada || dto.detalleFormulaTintura || '';
    const payload = {
      clienteId: dto.clienteId,
      peluqueroId: dto.peluqueroId,
      turnoId: dto.turnoId ?? null,
      formulaAplicada: formula,
      observaciones: dto.observaciones ?? dto.notasSensibilidadAlergias,
      notasSensibilidadAlergias: dto.notasSensibilidadAlergias,
      productosUtilizados: dto.productosUtilizados,
      fotoResultadoUrl: dto.fotoResultadoUrl,
    };
    const { data } = await apiClient.post<ApiResponse<FichaTecnicaDto>>(
      '/fichastecnicas',
      payload
    );
    if (data.data) {
      data.data = normalizeFichaTecnica(data.data);
    }
    return data;
  },

  async actualizar(
    id: number,
    dto: ActualizarFichaTecnicaDto | CrearFichaTecnicaDto
  ): Promise<ApiResponse<FichaTecnicaDto>> {
    const formula = dto.formulaAplicada || dto.detalleFormulaTintura || '';
    const payload: ActualizarFichaTecnicaDto = {
      peluqueroId: dto.peluqueroId,
      turnoId: dto.turnoId ?? null,
      formulaAplicada: formula,
      observaciones: dto.observaciones ?? dto.notasSensibilidadAlergias,
      notasSensibilidadAlergias: dto.notasSensibilidadAlergias,
      productosUtilizados: dto.productosUtilizados,
      fotoResultadoUrl: dto.fotoResultadoUrl,
    };
    const { data } = await apiClient.put<ApiResponse<FichaTecnicaDto>>(
      `/fichastecnicas/${id}`,
      payload
    );
    if (data.data) {
      data.data = normalizeFichaTecnica(data.data);
    }
    return data;
  },

  async eliminar(id: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/fichastecnicas/${id}`
    );
    return data;
  },
};
