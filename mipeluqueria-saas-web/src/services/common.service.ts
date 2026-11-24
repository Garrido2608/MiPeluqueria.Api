import { apiClient, STORAGE_KEYS } from '@/lib/axios';
import { extractPagedItems } from '@/lib/apiHelpers';
import {
  ActualizarCatalogoItemDto,
  ActualizarConfiguracionSalonDto,
  ApiResponse,
  CatalogoItemDto,
  CatalogosMaestrosDto,
  ConfiguracionSalonDto,
  CrearCatalogoItemDto,
  TipoCatalogoSalon,
} from '@/types/api.types';

function normalizeConfiguracionSalon(
  raw: ConfiguracionSalonDto,
  fallback?: Partial<ConfiguracionSalonDto>
): ConfiguracionSalonDto {
  const nombreComercial =
    raw.nombreComercial ||
    raw.nombreSalon ||
    fallback?.nombreComercial ||
    fallback?.nombreSalon ||
    'Mi Peluquería & Barbería SaaS';
  const moneda =
    raw.moneda || raw.monedaIso || fallback?.moneda || fallback?.monedaIso || 'ARS';

  return {
    ...fallback,
    ...raw,
    nombreComercial,
    nombreSalon: raw.nombreSalon || nombreComercial,
    moneda,
    monedaIso: raw.monedaIso || moneda,
    timeZoneId:
      raw.timeZoneId || fallback?.timeZoneId || 'America/Argentina/Cordoba',
    minutosToleranciaEspera:
      raw.minutosToleranciaEspera ?? fallback?.minutosToleranciaEspera ?? 15,
    horasAnticipacionCancelacion:
      raw.horasAnticipacionCancelacion ??
      fallback?.horasAnticipacionCancelacion ??
      24,
    porcentajeSenaReserva:
      raw.porcentajeSenaReserva ?? fallback?.porcentajeSenaReserva ?? 0,
    notificacionesWhatsAppActivas:
      raw.notificacionesWhatsAppActivas ??
      fallback?.notificacionesWhatsAppActivas ??
      true,
    notificacionesEmailActivas:
      raw.notificacionesEmailActivas ??
      fallback?.notificacionesEmailActivas ??
      true,
  };
}

// 3. ConfiguracionSalonController (/api/configuracion)
export const configuracionSalonService = {
  async obtener(): Promise<ApiResponse<ConfiguracionSalonDto>> {
    let localExtra: Partial<ConfiguracionSalonDto> = {};
    if (typeof window !== 'undefined') {
      try {
        const raw = localStorage.getItem(STORAGE_KEYS.SALON_CONFIG_CACHE);
        if (raw) localExtra = JSON.parse(raw) as Partial<ConfiguracionSalonDto>;
      } catch {
        localExtra = {};
      }
    }

    const { data } = await apiClient.get<ApiResponse<ConfiguracionSalonDto>>(
      '/configuracion'
    );

    if (data.data) {
      data.data = normalizeConfiguracionSalon(data.data, localExtra);
    }

    return data;
  },

  async actualizar(
    dto: ActualizarConfiguracionSalonDto
  ): Promise<ApiResponse<ConfiguracionSalonDto>> {
    const payload: ActualizarConfiguracionSalonDto = {
      ...dto,
      nombreComercial:
        dto.nombreComercial || dto.nombreSalon || 'Mi Peluquería SaaS',
      nombreSalon:
        dto.nombreSalon || dto.nombreComercial || 'Mi Peluquería SaaS',
      moneda: dto.moneda || dto.monedaIso || 'ARS',
      monedaIso: dto.monedaIso || dto.moneda || 'ARS',
    };

    if (typeof window !== 'undefined') {
      localStorage.setItem(
        STORAGE_KEYS.SALON_CONFIG_CACHE,
        JSON.stringify(payload)
      );
    }

    const { data } = await apiClient.put<ApiResponse<ConfiguracionSalonDto>>(
      '/configuracion',
      payload
    );

    if (data.data) {
      data.data = normalizeConfiguracionSalon(data.data, payload);
    }

    return data;
  },
};

// 4. CatalogosController (/api/catalogos) — 7 Catálogos con CRUD completo + EstadosTurno y Roles
export const catalogosService = {
  async obtenerTodos(): Promise<ApiResponse<CatalogosMaestrosDto>> {
    const { data } = await apiClient.get<
      ApiResponse<Partial<CatalogosMaestrosDto>>
    >('/catalogos/todos');
    const raw = data.data ?? {};
    return {
      ...data,
      data: {
        tiposCabello: extractPagedItems<CatalogoItemDto>(raw.tiposCabello),
        tiposRostro: extractPagedItems<CatalogoItemDto>(raw.tiposRostro),
        categoriasServicio: extractPagedItems<CatalogoItemDto>(
          raw.categoriasServicio
        ),
        especialidades: extractPagedItems<CatalogoItemDto>(raw.especialidades),
        estadosTurno: extractPagedItems<CatalogoItemDto>(raw.estadosTurno),
        motivosCancelacion: extractPagedItems<CatalogoItemDto>(
          raw.motivosCancelacion
        ),
        categoriasProducto: extractPagedItems<CatalogoItemDto>(
          raw.categoriasProducto
        ),
        mediosPago: extractPagedItems<CatalogoItemDto>(raw.mediosPago),
        roles: extractPagedItems<CatalogoItemDto>(raw.roles),
      },
    };
  },

  async getEstadosTurno(): Promise<ApiResponse<CatalogoItemDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/catalogos/estados-turno'
    );
    return {
      ...data,
      data: extractPagedItems<CatalogoItemDto>(data.data),
    };
  },

  async getRoles(): Promise<ApiResponse<CatalogoItemDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/catalogos/roles'
    );
    return {
      ...data,
      data: extractPagedItems<CatalogoItemDto>(data.data),
    };
  },

  // Métodos genéricos parametrizados por TipoCatalogoSalon
  async listarPorTipo(
    tipo: TipoCatalogoSalon
  ): Promise<ApiResponse<CatalogoItemDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      `/catalogos/${tipo}`
    );
    return {
      ...data,
      data: extractPagedItems<CatalogoItemDto>(data.data),
    };
  },

  async crearPorTipo(
    tipo: TipoCatalogoSalon,
    dto: CrearCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    const { data } = await apiClient.post<ApiResponse<CatalogoItemDto>>(
      `/catalogos/${tipo}`,
      dto
    );
    return data;
  },

  async actualizarPorTipo(
    tipo: TipoCatalogoSalon,
    id: number,
    dto: ActualizarCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    const { data } = await apiClient.put<ApiResponse<CatalogoItemDto>>(
      `/catalogos/${tipo}/${id}`,
      dto
    );
    return data;
  },

  async eliminarPorTipo(
    tipo: TipoCatalogoSalon,
    id: number
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/catalogos/${tipo}/${id}`
    );
    return data;
  },

  // 4.1 Tipos de Cabello (/api/catalogos/tipos-cabello)
  async getTiposCabello(): Promise<ApiResponse<CatalogoItemDto[]>> {
    return this.listarPorTipo('tipos-cabello');
  },
  async crearTipoCabello(
    dto: CrearCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.crearPorTipo('tipos-cabello', dto);
  },
  async actualizarTipoCabello(
    id: number,
    dto: ActualizarCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.actualizarPorTipo('tipos-cabello', id, dto);
  },
  async eliminarTipoCabello(id: number): Promise<ApiResponse<boolean>> {
    return this.eliminarPorTipo('tipos-cabello', id);
  },

  // 4.2 Tipos de Rostro (/api/catalogos/tipos-rostro)
  async getTiposRostro(): Promise<ApiResponse<CatalogoItemDto[]>> {
    return this.listarPorTipo('tipos-rostro');
  },
  async crearTipoRostro(
    dto: CrearCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.crearPorTipo('tipos-rostro', dto);
  },
  async actualizarTipoRostro(
    id: number,
    dto: ActualizarCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.actualizarPorTipo('tipos-rostro', id, dto);
  },
  async eliminarTipoRostro(id: number): Promise<ApiResponse<boolean>> {
    return this.eliminarPorTipo('tipos-rostro', id);
  },

  // 4.3 Categorías de Servicio (/api/catalogos/categorias-servicio)
  async getCategoriasServicio(): Promise<ApiResponse<CatalogoItemDto[]>> {
    return this.listarPorTipo('categorias-servicio');
  },
  async crearCategoriaServicio(
    dto: CrearCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.crearPorTipo('categorias-servicio', dto);
  },
  async actualizarCategoriaServicio(
    id: number,
    dto: ActualizarCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.actualizarPorTipo('categorias-servicio', id, dto);
  },
  async eliminarCategoriaServicio(id: number): Promise<ApiResponse<boolean>> {
    return this.eliminarPorTipo('categorias-servicio', id);
  },

  // 4.4 Especialidades (/api/catalogos/especialidades)
  async getEspecialidades(): Promise<ApiResponse<CatalogoItemDto[]>> {
    return this.listarPorTipo('especialidades');
  },
  async crearEspecialidad(
    dto: CrearCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.crearPorTipo('especialidades', dto);
  },
  async actualizarEspecialidad(
    id: number,
    dto: ActualizarCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.actualizarPorTipo('especialidades', id, dto);
  },
  async eliminarEspecialidad(id: number): Promise<ApiResponse<boolean>> {
    return this.eliminarPorTipo('especialidades', id);
  },

  // 4.5 Motivos de Cancelación (/api/catalogos/motivos-cancelacion)
  async getMotivosCancelacion(): Promise<ApiResponse<CatalogoItemDto[]>> {
    return this.listarPorTipo('motivos-cancelacion');
  },
  async crearMotivoCancelacion(
    dto: CrearCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.crearPorTipo('motivos-cancelacion', dto);
  },
  async actualizarMotivoCancelacion(
    id: number,
    dto: ActualizarCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.actualizarPorTipo('motivos-cancelacion', id, dto);
  },
  async eliminarMotivoCancelacion(id: number): Promise<ApiResponse<boolean>> {
    return this.eliminarPorTipo('motivos-cancelacion', id);
  },

  // 4.6 Categorías de Producto (/api/catalogos/categorias-producto)
  async getCategoriasProducto(): Promise<ApiResponse<CatalogoItemDto[]>> {
    return this.listarPorTipo('categorias-producto');
  },
  async crearCategoriaProducto(
    dto: CrearCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.crearPorTipo('categorias-producto', dto);
  },
  async actualizarCategoriaProducto(
    id: number,
    dto: ActualizarCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.actualizarPorTipo('categorias-producto', id, dto);
  },
  async eliminarCategoriaProducto(id: number): Promise<ApiResponse<boolean>> {
    return this.eliminarPorTipo('categorias-producto', id);
  },

  // 4.7 Medios de Pago (/api/catalogos/medios-pago)
  async getMediosPago(): Promise<ApiResponse<CatalogoItemDto[]>> {
    return this.listarPorTipo('medios-pago');
  },
  async crearMedioPago(
    dto: CrearCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.crearPorTipo('medios-pago', dto);
  },
  async actualizarMedioPago(
    id: number,
    dto: ActualizarCatalogoItemDto
  ): Promise<ApiResponse<CatalogoItemDto>> {
    return this.actualizarPorTipo('medios-pago', id, dto);
  },
  async eliminarMedioPago(id: number): Promise<ApiResponse<boolean>> {
    return this.eliminarPorTipo('medios-pago', id);
  },
};
