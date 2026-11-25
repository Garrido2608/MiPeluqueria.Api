import { apiClient } from '@/lib/axios';
import {
  extractPagedItems,
  toNormalizedPagedResult,
} from '@/lib/apiHelpers';
import {
  ActualizarPeluqueroDto,
  ActualizarServicioDto,
  ApiResponse,
  BloqueoHorarioDto,
  CambiarEstadoTurnoDto,
  ConfigurarRecetaServicioDto,
  CrearPeluqueroDto,
  CrearServicioDto,
  CrearTurnoDto,
  DisponibilidadHorarioResponseDto,
  GuardarHorariosPeluqueroDto,
  HistorialEstadoTurnoDto,
  HorarioLaboralDto,
  PagedResult,
  PaginationFilterDto,
  PeluqueroDto,
  RecetaInsumoItemDto,
  RecetaServicioResponseDto,
  ReprogramarTurnoDto,
  ServicioDto,
  TurnoDto,
} from '@/types/api.types';

export function normalizeTurno(raw: TurnoDto): TurnoDto {
  const clienteNombreCompleto =
    raw.clienteNombreCompleto || raw.clienteNombre || 'Cliente';
  const peluqueroNombreCompleto =
    raw.peluqueroNombreCompleto || raw.peluqueroNombre || 'Profesional';

  const rawServicios = raw.serviciosDetalle || raw.servicios || [];
  const serviciosDetalle = Array.isArray(rawServicios)
    ? rawServicios.filter(
        (item): item is NonNullable<typeof item> =>
          typeof item === 'object' && item !== null && 'servicioId' in item
      )
    : [];

  return {
    ...raw,
    clienteNombre: raw.clienteNombre || clienteNombreCompleto,
    clienteNombreCompleto,
    peluqueroNombre: raw.peluqueroNombre || peluqueroNombreCompleto,
    peluqueroNombreCompleto,
    serviciosDetalle,
    servicios: serviciosDetalle,
  };
}

export function normalizePeluquero(raw: PeluqueroDto): PeluqueroDto {
  const full = (raw.nombreCompleto || '').trim();
  let nombre = raw.nombre || '';
  let apellido = raw.apellido || '';

  if ((!nombre || !apellido) && full) {
    const parts = full.split(/\s+/);
    nombre = nombre || parts[0] || '';
    apellido = apellido || parts.slice(1).join(' ') || '';
  }

  return {
    ...raw,
    nombre,
    apellido,
    nombreCompleto: full || `${nombre} ${apellido}`.trim(),
    especialidadesNombres:
      raw.especialidadesNombres || raw.especialidades || [],
  };
}

function normalizeRecetaItem(raw: RecetaInsumoItemDto): RecetaInsumoItemDto {
  const costo = raw.costoEstimadoItem ?? raw.costoEstimadoInsumo ?? 0;
  return {
    ...raw,
    costoEstimadoInsumo: costo,
    costoEstimadoItem: costo,
    descuentoAutomaticoAlCompletarTurno:
      raw.descuentoAutomaticoAlCompletarTurno ?? true,
  };
}

// 6. TurnosController (/api/turnos)
export const turnosService = {
  async getPaged(
    filter: PaginationFilterDto = {},
    fecha?: string,
    peluqueroId?: number,
    estadoId?: number,
    clienteId?: number
  ): Promise<ApiResponse<PagedResult<TurnoDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>('/turnos', {
      params: {
        ...filter,
        fecha,
        peluqueroId,
        estadoId,
        clienteId,
      },
    });

    return {
      ...data,
      data: toNormalizedPagedResult<TurnoDto>(data.data, normalizeTurno, {
        pageNumber: filter.pageNumber,
        pageSize: filter.pageSize,
      }),
    };
  },

  async getAll(
    fecha?: string,
    peluqueroId?: number,
    estadoId?: number,
    clienteId?: number
  ): Promise<ApiResponse<TurnoDto[]>> {
    const paged = await this.getPaged(
      { pageNumber: 1, pageSize: 100 },
      fecha,
      peluqueroId,
      estadoId,
      clienteId
    );
    return {
      ...paged,
      data: extractPagedItems<TurnoDto>(paged.data),
    };
  },

  async getByRange(
    fechaDesde: string,
    fechaHasta: string,
    peluqueroId?: number,
    estadoId?: number,
    clienteId?: number
  ): Promise<ApiResponse<TurnoDto[]>> {
    const paged = await this.getPaged(
      {
        pageNumber: 1,
        pageSize: 100,
        fechaDesde,
        fechaHasta,
      },
      undefined,
      peluqueroId,
      estadoId,
      clienteId
    );
    return {
      ...paged,
      data: extractPagedItems<TurnoDto>(paged.data),
    };
  },

  async getById(id: number): Promise<ApiResponse<TurnoDto>> {
    const { data } = await apiClient.get<ApiResponse<TurnoDto>>(
      `/turnos/${id}`
    );
    if (data.data) {
      data.data = normalizeTurno(data.data);
    }
    return data;
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
    >(`/turnos/disponibilidad?${params.toString()}`);

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

  async getPorCliente(clienteId: number): Promise<ApiResponse<TurnoDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      `/turnos/cliente/${clienteId}`
    );
    return {
      ...data,
      data: extractPagedItems<TurnoDto>(data.data).map(normalizeTurno),
    };
  },

  async getHistorialEstados(
    id: number
  ): Promise<ApiResponse<HistorialEstadoTurnoDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      `/turnos/${id}/historial`
    );
    return {
      ...data,
      data: extractPagedItems<HistorialEstadoTurnoDto>(data.data),
    };
  },

  async create(dto: CrearTurnoDto): Promise<ApiResponse<TurnoDto>> {
    const { data } = await apiClient.post<ApiResponse<TurnoDto>>(
      '/turnos',
      dto
    );
    if (data.data) {
      data.data = normalizeTurno(data.data);
    }
    return data;
  },

  async reprogramar(
    id: number,
    dto: ReprogramarTurnoDto
  ): Promise<ApiResponse<TurnoDto>> {
    const { data } = await apiClient.put<ApiResponse<TurnoDto>>(
      `/turnos/${id}/reprogramar`,
      dto
    );
    if (data.data) {
      data.data = normalizeTurno(data.data);
    }
    return data;
  },

  async cambiarEstado(
    id: number,
    dto: CambiarEstadoTurnoDto
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.patch<ApiResponse<boolean>>(
      `/turnos/${id}/estado`,
      dto
    );
    return data;
  },
};

// 7. PeluquerosController (/api/peluqueros)
export const peluquerosService = {
  async getAll(soloActivos: boolean = false): Promise<ApiResponse<PeluqueroDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/peluqueros',
      { params: { soloActivos } }
    );
    return {
      ...data,
      data: extractPagedItems<PeluqueroDto>(data.data).map(normalizePeluquero),
    };
  },

  async getById(id: number): Promise<ApiResponse<PeluqueroDto>> {
    const { data } = await apiClient.get<ApiResponse<PeluqueroDto>>(
      `/peluqueros/${id}`
    );
    if (data.data) {
      data.data = normalizePeluquero(data.data);
    }
    return data;
  },

  async create(dto: CrearPeluqueroDto): Promise<ApiResponse<PeluqueroDto>> {
    const { data } = await apiClient.post<ApiResponse<PeluqueroDto>>(
      '/peluqueros',
      dto
    );
    if (data.data) {
      data.data = normalizePeluquero(data.data);
    }
    return data;
  },

  async update(
    id: number,
    dto: ActualizarPeluqueroDto | CrearPeluqueroDto
  ): Promise<ApiResponse<PeluqueroDto>> {
    const { data } = await apiClient.put<ApiResponse<PeluqueroDto>>(
      `/peluqueros/${id}`,
      dto
    );
    if (data.data) {
      data.data = normalizePeluquero(data.data);
    }
    return data;
  },

  async delete(id: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/peluqueros/${id}`
    );
    return data;
  },

  async getHorarios(id: number): Promise<ApiResponse<HorarioLaboralDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      `/peluqueros/${id}/horarios`
    );
    return {
      ...data,
      data: extractPagedItems<HorarioLaboralDto>(data.data),
    };
  },

  async configurarHorarios(
    id: number,
    dto: GuardarHorariosPeluqueroDto
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.put<ApiResponse<boolean>>(
      `/peluqueros/${id}/horarios`,
      dto
    );
    return data;
  },
};

// 8. BloqueosController (/api/bloqueos)
export const bloqueosService = {
  async getPorPeluquero(
    peluqueroId: number
  ): Promise<ApiResponse<BloqueoHorarioDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      `/bloqueos/peluquero/${peluqueroId}`
    );
    return {
      ...data,
      data: extractPagedItems<BloqueoHorarioDto>(data.data),
    };
  },

  async crear(
    dto: BloqueoHorarioDto
  ): Promise<ApiResponse<BloqueoHorarioDto>> {
    const { data } = await apiClient.post<ApiResponse<BloqueoHorarioDto>>(
      '/bloqueos',
      dto
    );
    return data;
  },

  async eliminar(id: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/bloqueos/${id}`
    );
    return data;
  },
};

// 9. ServiciosController (/api/servicios)
export const serviciosService = {
  async getAll(soloActivos: boolean = false): Promise<ApiResponse<ServicioDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/servicios',
      { params: { soloActivos } }
    );
    return {
      ...data,
      data: extractPagedItems<ServicioDto>(data.data),
    };
  },

  async getById(id: number): Promise<ApiResponse<ServicioDto>> {
    const { data } = await apiClient.get<ApiResponse<ServicioDto>>(
      `/servicios/${id}`
    );
    return data;
  },

  async create(dto: CrearServicioDto): Promise<ApiResponse<ServicioDto>> {
    const { data } = await apiClient.post<ApiResponse<ServicioDto>>(
      '/servicios',
      dto
    );
    return data;
  },

  async update(
    id: number,
    dto: ActualizarServicioDto | CrearServicioDto
  ): Promise<ApiResponse<ServicioDto>> {
    const { data } = await apiClient.put<ApiResponse<ServicioDto>>(
      `/servicios/${id}`,
      dto
    );
    return data;
  },

  async delete(id: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/servicios/${id}`
    );
    return data;
  },
};

// 10. RecetasServicioController (/api/recetas-servicio)
export const recetasServicioService = {
  async getPorServicio(
    servicioId: number
  ): Promise<ApiResponse<RecetaServicioResponseDto>> {
    const { data } = await apiClient.get<
      ApiResponse<RecetaServicioResponseDto | RecetaInsumoItemDto[]>
    >(`/recetas-servicio/servicio/${servicioId}`);

    if (Array.isArray(data.data)) {
      const items = data.data.map(normalizeRecetaItem);
      const costoTotal = items.reduce(
        (acc, it) => acc + (it.costoEstimadoItem || 0),
        0
      );
      return {
        ...data,
        data: {
          servicioId,
          servicioNombre: items[0]?.servicioNombre || '',
          precioVentaServicio: 0,
          costoTotalEstimadoInsumos: costoTotal,
          margenNetoEstimado: 0,
          items,
          insumos: items,
        },
      };
    }

    if (data.data) {
      const items = extractPagedItems<RecetaInsumoItemDto>(
        data.data.items ?? data.data.insumos
      ).map(normalizeRecetaItem);
      return {
        ...data,
        data: {
          ...data.data,
          items,
          insumos: items,
        },
      };
    }

    return {
      ...data,
      data: {
        servicioId,
        servicioNombre: '',
        precioVentaServicio: 0,
        costoTotalEstimadoInsumos: 0,
        margenNetoEstimado: 0,
        items: [],
        insumos: [],
      },
    };
  },

  async guardarReceta(
    dto: ConfigurarRecetaServicioDto
  ): Promise<ApiResponse<RecetaServicioResponseDto>> {
    const insumos = dto.insumos || dto.items || [];
    const payload = {
      servicioId: dto.servicioId,
      insumos,
    };

    const { data } = await apiClient.put<ApiResponse<boolean>>(
      '/recetas-servicio',
      payload
    );

    if (data.success) {
      const refreshed = await this.getPorServicio(dto.servicioId);
      return {
        ...refreshed,
        message: data.message || refreshed.message,
        warnings: data.warnings || refreshed.warnings,
      };
    }

    return {
      success: false,
      message: data.message,
      data: null,
      errors: data.errors,
      fieldErrors: data.fieldErrors,
      warnings: data.warnings,
    };
  },

  async consumirPorTurno(turnoId: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<number | boolean>>(
      `/recetas-servicio/turno/${turnoId}/consumir-gabinete`
    );
    return {
      ...data,
      data: data.success,
    };
  },
};
