import { apiClient, withIdempotencyKey } from '@/lib/axios';
import {
  extractPagedItems,
  toNormalizedPagedResult,
} from '@/lib/apiHelpers';
import {
  AnularVentaDto,
  AperturaCajaDto,
  ApiResponse,
  CierreCajaDto,
  ComisionConfigDto,
  CrearVentaDto,
  DashboardKpisDto,
  DetalleLiquidacionDto,
  EstadoLiquidacionEnum,
  GenerarLiquidacionDto,
  GuardarComisionConfigDto,
  IngresoPorMedioPagoDto,
  LiquidacionDto,
  MovimientoCajaDto,
  PagedResult,
  PaginationFilterDto,
  RankingPeluqueroDto,
  RegistrarMovimientoCajaDto,
  SesionCajaDto,
  VentaResponseDto,
} from '@/types/api.types';

function normalizeSesionCaja(raw: SesionCajaDto): SesionCajaDto {
  const movimientos = extractPagedItems<MovimientoCajaDto>(
    raw.movimientos ?? raw.movimientosCaja
  );
  const totalVentasDigital =
    raw.totalVentasDigital ?? raw.totalVentasOtrosMedios ?? 0;

  return {
    ...raw,
    totalVentasEfectivo: raw.totalVentasEfectivo ?? 0,
    totalVentasDigital,
    totalVentasOtrosMedios: totalVentasDigital,
    totalIngresosManuales: raw.totalIngresosManuales ?? 0,
    totalEgresosManuales: raw.totalEgresosManuales ?? 0,
    montoFinalEsperado: raw.montoFinalEsperado ?? raw.montoInicial ?? 0,
    movimientos,
    movimientosCaja: movimientos,
  };
}

function normalizeVenta(raw: VentaResponseDto): VentaResponseDto {
  const clienteNombre =
    raw.clienteNombre || raw.clienteNombreCompleto || 'Consumidor Final';
  const detalles = extractPagedItems(raw.detalles).map((d) => ({
    ...d,
    conceptoNombre: d.conceptoNombre || d.itemNombre || '',
    itemNombre: d.itemNombre || d.conceptoNombre || '',
  }));

  return {
    ...raw,
    clienteNombre,
    clienteNombreCompleto: raw.clienteNombreCompleto || clienteNombre,
    subtotal: raw.subtotal ?? raw.total,
    descuentoSenaAplicado: raw.descuentoSenaAplicado ?? 0,
    detalles,
    pagos: extractPagedItems(raw.pagos),
  };
}

function normalizeComision(raw: ComisionConfigDto): ComisionConfigDto {
  const peluqueroNombre =
    raw.peluqueroNombre || raw.peluqueroNombreCompleto || '';
  const porcentajeVentaProducto =
    raw.porcentajeVentaProducto ?? raw.porcentajeProducto ?? 0;

  return {
    ...raw,
    peluqueroNombre,
    peluqueroNombreCompleto: peluqueroNombre,
    porcentajeProducto: porcentajeVentaProducto,
    porcentajeVentaProducto,
  };
}

function normalizeDetalleLiquidacion(
  raw: DetalleLiquidacionDto
): DetalleLiquidacionDto {
  const conceptoNombre =
    raw.conceptoNombre || raw.conceptoDescripcion || 'Servicio / Producto';
  const subtotalVenta = raw.subtotalVenta ?? raw.importeBaseVenta ?? 0;
  const fechaVenta = raw.fechaVenta || raw.fechaItem || null;

  return {
    ...raw,
    conceptoNombre,
    conceptoDescripcion: conceptoNombre,
    subtotalVenta,
    importeBaseVenta: subtotalVenta,
    fechaVenta,
    fechaItem: fechaVenta || undefined,
  };
}

function mapEstadoLiquidacion(
  estadoRaw: EstadoLiquidacionEnum | string
): { estado: EstadoLiquidacionEnum; estadoNombre: string } {
  if (typeof estadoRaw === 'number') {
    const nombres: Record<number, string> = {
      1: 'Pendiente',
      2: 'Pagado',
      3: 'Atrasado',
      4: 'Anulada',
    };
    return {
      estado: estadoRaw,
      estadoNombre: nombres[estadoRaw] || 'Pendiente',
    };
  }

  const normalized = String(estadoRaw).toLowerCase();
  if (normalized.includes('pagad')) {
    return { estado: EstadoLiquidacionEnum.Pagado, estadoNombre: 'Pagado' };
  }
  if (normalized.includes('atrasad')) {
    return { estado: EstadoLiquidacionEnum.Atrasado, estadoNombre: 'Atrasado' };
  }
  if (normalized.includes('anulad')) {
    return { estado: EstadoLiquidacionEnum.Anulada, estadoNombre: 'Anulada' };
  }
  return { estado: EstadoLiquidacionEnum.Pendiente, estadoNombre: 'Pendiente' };
}

function normalizeLiquidacion(
  raw: LiquidacionDto & { estado: EstadoLiquidacionEnum | string }
): LiquidacionDto {
  const peluqueroNombre =
    raw.peluqueroNombre || raw.peluqueroNombreCompleto || '';
  const detalles = extractPagedItems<DetalleLiquidacionDto>(
    raw.detalles ?? raw.detallesLiquidacion
  ).map(normalizeDetalleLiquidacion);
  const { estado, estadoNombre } = mapEstadoLiquidacion(raw.estado);

  return {
    ...raw,
    peluqueroNombre,
    peluqueroNombreCompleto: peluqueroNombre,
    fechaEmision: raw.fechaEmision || raw.fechaDesde,
    bonificaciones: raw.bonificaciones ?? 0,
    descuentos: raw.descuentos ?? 0,
    estado,
    estadoNombre: raw.estadoNombre || estadoNombre,
    detalles,
    detallesLiquidacion: detalles,
  };
}

function normalizeDashboardKpis(raw: DashboardKpisDto): DashboardKpisDto {
  const facturacionTotal =
    raw.facturacionTotal ??
    raw.facturacionTotalMes ??
    raw.ingresosTotalesPeriodo ??
    0;
  const cantidadVentas =
    raw.cantidadVentas ?? raw.cantidadVentasMes ?? raw.totalVentasPeriodo ?? 0;
  const turnosCompletados =
    raw.turnosCompletados ??
    raw.totalTurnosAtendidosMes ??
    raw.turnosCompletadosPeriodo ??
    0;
  const turnosCancelados =
    raw.turnosCancelados ?? raw.turnosCanceladosPeriodo ?? 0;

  const rawMedios = extractPagedItems<IngresoPorMedioPagoDto>(
    raw.ingresosPorMedioPago ?? raw.recaudacionPorMedioPago
  );
  const totalMedios = rawMedios.reduce(
    (acc, m) => acc + (m.totalRecaudado ?? m.montoTotal ?? 0),
    0
  );

  const ingresosPorMedioPago: IngresoPorMedioPagoDto[] = rawMedios.map((m) => {
    const monto = m.totalRecaudado ?? m.montoTotal ?? 0;
    const nombre = m.medioPago || m.medioPagoNombre || 'Medio de Pago';
    const pct =
      m.porcentaje ?? (totalMedios > 0 ? (monto / totalMedios) * 100 : 0);
    return {
      medioPago: nombre,
      medioPagoNombre: nombre,
      totalRecaudado: monto,
      montoTotal: monto,
      cantidadOperaciones: m.cantidadOperaciones ?? 0,
      porcentaje: pct,
    };
  });

  const rankingPeluqueros: RankingPeluqueroDto[] =
    extractPagedItems<RankingPeluqueroDto>(raw.rankingPeluqueros).map((r) => {
    const nombre = r.nombrePeluquero || r.peluqueroNombre || 'Profesional';
    const servicios = r.serviciosRealizados ?? r.cantidadServicios ?? 0;
    const facturado = r.totalFacturado ?? r.facturacionTotal ?? 0;
    return {
      peluqueroId: r.peluqueroId,
      nombrePeluquero: nombre,
      peluqueroNombre: nombre,
      serviciosRealizados: servicios,
      cantidadServicios: servicios,
      totalFacturado: facturado,
      facturacionTotal: facturado,
      cantidadProductosStockCritico:
        r.cantidadProductosStockCritico ??
        raw.productosEnStockCritico ??
        raw.productosBajoStockCritico ??
        0,
    };
  });

  return {
    ...raw,
    facturacionTotal,
    facturacionTotalMes: facturacionTotal,
    ingresosTotalesPeriodo: facturacionTotal,
    cantidadVentas,
    cantidadVentasMes: cantidadVentas,
    totalVentasPeriodo: cantidadVentas,
    turnosCompletados,
    totalTurnosAtendidosMes: turnosCompletados,
    turnosCompletadosPeriodo: turnosCompletados,
    turnosCancelados,
    turnosCanceladosPeriodo: turnosCancelados,
    ingresosPorMedioPago,
    recaudacionPorMedioPago: ingresosPorMedioPago,
    rankingPeluqueros,
  };
}

// 16. CajaController (/api/caja)
export const cajaService = {
  async getEstadoActual(): Promise<ApiResponse<SesionCajaDto | null>> {
    const { data } = await apiClient.get<ApiResponse<SesionCajaDto | null>>(
      '/caja/estado-actual'
    );
    if (data.data) {
      data.data = normalizeSesionCaja(data.data);
    }
    return data;
  },

  async getById(id: number): Promise<ApiResponse<SesionCajaDto>> {
    const { data } = await apiClient.get<ApiResponse<SesionCajaDto>>(
      `/caja/${id}`
    );
    if (data.data) {
      data.data = normalizeSesionCaja(data.data);
    }
    return data;
  },

  async getHistorialPaged(
    filter: PaginationFilterDto = {}
  ): Promise<ApiResponse<PagedResult<SesionCajaDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/caja/historial',
      {
        params: filter,
      }
    );

    return {
      ...data,
      data: toNormalizedPagedResult<SesionCajaDto>(
        data.data,
        normalizeSesionCaja,
        {
          pageNumber: filter.pageNumber,
          pageSize: filter.pageSize,
        }
      ),
    };
  },

  async getHistorial(
    fechaDesde?: string,
    fechaHasta?: string
  ): Promise<ApiResponse<SesionCajaDto[]>> {
    const paged = await this.getHistorialPaged({
      pageNumber: 1,
      pageSize: 100,
      fechaDesde: fechaDesde || undefined,
      fechaHasta: fechaHasta || undefined,
    });
    return {
      ...paged,
      data: extractPagedItems<SesionCajaDto>(paged.data),
    };
  },

  async abrir(dto: AperturaCajaDto): Promise<ApiResponse<SesionCajaDto>> {
    const { data } = await apiClient.post<ApiResponse<SesionCajaDto>>(
      '/caja/abrir',
      dto
    );
    if (data.data) {
      data.data = normalizeSesionCaja(data.data);
    }
    return data;
  },

  async registrarEgreso(
    dto: RegistrarMovimientoCajaDto
  ): Promise<ApiResponse<MovimientoCajaDto>> {
    const { data } = await apiClient.post<ApiResponse<MovimientoCajaDto>>(
      '/caja/egreso',
      dto
    );
    return data;
  },

  async registrarIngreso(
    dto: RegistrarMovimientoCajaDto
  ): Promise<ApiResponse<MovimientoCajaDto>> {
    const { data } = await apiClient.post<ApiResponse<MovimientoCajaDto>>(
      '/caja/ingreso',
      dto
    );
    return data;
  },

  async cerrar(dto: CierreCajaDto): Promise<ApiResponse<SesionCajaDto>> {
    const payload: CierreCajaDto = {
      ...dto,
      observacionesCierre: dto.observacionesCierre ?? dto.observaciones,
    };
    const { data } = await apiClient.post<ApiResponse<SesionCajaDto>>(
      '/caja/cerrar',
      payload
    );
    if (data.data) {
      data.data = normalizeSesionCaja(data.data);
    }
    return data;
  },
};

// 17. VentasController (/api/ventas)
export const ventasService = {
  async getById(id: number): Promise<ApiResponse<VentaResponseDto>> {
    const { data } = await apiClient.get<ApiResponse<VentaResponseDto>>(
      `/ventas/${id}`
    );
    if (data.data) {
      data.data = normalizeVenta(data.data);
    }
    return data;
  },

  async getPorSesionPaged(
    sesionCajaId: number,
    filter: PaginationFilterDto = {}
  ): Promise<ApiResponse<PagedResult<VentaResponseDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      `/ventas/sesion/${sesionCajaId}`,
      {
        params: filter,
      }
    );

    return {
      ...data,
      data: toNormalizedPagedResult<VentaResponseDto>(
        data.data,
        normalizeVenta,
        {
          pageNumber: filter.pageNumber,
          pageSize: filter.pageSize,
        }
      ),
    };
  },

  async getPorSesion(
    sesionCajaId: number
  ): Promise<ApiResponse<VentaResponseDto[]>> {
    const paged = await this.getPorSesionPaged(sesionCajaId, {
      pageNumber: 1,
      pageSize: 100,
    });
    return {
      ...paged,
      data: extractPagedItems<VentaResponseDto>(paged.data),
    };
  },

  async registrar(
    dto: CrearVentaDto,
    idempotencyKey?: string
  ): Promise<ApiResponse<VentaResponseDto>> {
    const effectiveKey = idempotencyKey || dto.idempotencyKey || undefined;
    const payload: CrearVentaDto = {
      ...dto,
      idempotencyKey: effectiveKey,
    };

    const config = effectiveKey
      ? withIdempotencyKey(effectiveKey)
      : undefined;

    const { data } = await apiClient.post<ApiResponse<VentaResponseDto>>(
      '/ventas',
      payload,
      config
    );
    if (data.data) {
      data.data = normalizeVenta(data.data);
    }
    return data;
  },

  async anular(
    id: number,
    dto?: AnularVentaDto
  ): Promise<ApiResponse<boolean>> {
    const motivo = dto?.motivo || 'Anulación administrativa';
    const { data } = await apiClient.post<
      ApiResponse<VentaResponseDto | boolean>
    >(`/ventas/${id}/anular`, null, {
      params: { motivo },
    });
    return {
      ...data,
      data: data.success,
    };
  },
};

// 18. ComisionesController (/api/comisiones)
export const comisionesService = {
  async getAll(): Promise<ApiResponse<ComisionConfigDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>('/comisiones');
    return {
      ...data,
      data: extractPagedItems<ComisionConfigDto>(data.data).map(
        normalizeComision
      ),
    };
  },

  async getByPeluquero(
    peluqueroId: number
  ): Promise<ApiResponse<ComisionConfigDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>('/comisiones', {
      params: { peluqueroId },
    });
    return {
      ...data,
      data: extractPagedItems<ComisionConfigDto>(data.data).map(
        normalizeComision
      ),
    };
  },

  async saveConfig(
    dto: GuardarComisionConfigDto
  ): Promise<ApiResponse<ComisionConfigDto>> {
    const payload = {
      peluqueroId: dto.peluqueroId,
      servicioId: dto.servicioId ?? null,
      porcentajeServicio: dto.porcentajeServicio,
      porcentajeProducto:
        dto.porcentajeProducto ?? dto.porcentajeVentaProducto ?? 0,
      porcentajeVentaProducto:
        dto.porcentajeVentaProducto ?? dto.porcentajeProducto ?? 0,
    };
    const { data } = await apiClient.post<ApiResponse<ComisionConfigDto>>(
      '/comisiones',
      payload
    );
    if (data.data) {
      data.data = normalizeComision(data.data);
    }
    return data;
  },

  async updateConfig(
    id: number,
    dto: GuardarComisionConfigDto
  ): Promise<ApiResponse<ComisionConfigDto>> {
    const payload = {
      peluqueroId: dto.peluqueroId,
      servicioId: dto.servicioId ?? null,
      porcentajeServicio: dto.porcentajeServicio,
      porcentajeProducto:
        dto.porcentajeProducto ?? dto.porcentajeVentaProducto ?? 0,
      porcentajeVentaProducto:
        dto.porcentajeVentaProducto ?? dto.porcentajeProducto ?? 0,
    };
    const { data } = await apiClient.put<ApiResponse<ComisionConfigDto>>(
      `/comisiones/${id}`,
      payload
    );
    if (data.data) {
      data.data = normalizeComision(data.data);
    }
    return data;
  },

  async deleteConfig(id: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/comisiones/${id}`
    );
    return data;
  },
};

// 19. LiquidacionesController (/api/liquidaciones)
export const liquidacionesService = {
  async getPaged(
    filter: PaginationFilterDto = {},
    peluqueroId?: number,
    estado?: EstadoLiquidacionEnum
  ): Promise<ApiResponse<PagedResult<LiquidacionDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/liquidaciones',
      {
        params: {
          ...filter,
          peluqueroId,
          estado,
          desde: filter.fechaDesde,
          hasta: filter.fechaHasta,
        },
      }
    );

    return {
      ...data,
      data: toNormalizedPagedResult<LiquidacionDto>(
        data.data,
        normalizeLiquidacion,
        {
          pageNumber: filter.pageNumber,
          pageSize: filter.pageSize,
        }
      ),
    };
  },

  async getAll(
    peluqueroId?: number,
    desde?: string,
    hasta?: string,
    estado?: EstadoLiquidacionEnum
  ): Promise<ApiResponse<LiquidacionDto[]>> {
    const paged = await this.getPaged(
      {
        pageNumber: 1,
        pageSize: 100,
        fechaDesde: desde,
        fechaHasta: hasta,
      },
      peluqueroId,
      estado
    );
    return {
      ...paged,
      data: extractPagedItems<LiquidacionDto>(paged.data),
    };
  },

  async getById(id: number): Promise<ApiResponse<LiquidacionDto>> {
    const { data } = await apiClient.get<ApiResponse<LiquidacionDto>>(
      `/liquidaciones/${id}`
    );
    if (data.data) {
      data.data = normalizeLiquidacion(data.data);
    }
    return data;
  },

  async getPorPeluquero(
    peluqueroId: number
  ): Promise<ApiResponse<LiquidacionDto[]>> {
    return this.getAll(peluqueroId);
  },

  async generar(
    dto: GenerarLiquidacionDto
  ): Promise<ApiResponse<LiquidacionDto>> {
    const { data } = await apiClient.post<ApiResponse<LiquidacionDto>>(
      '/liquidaciones/generar',
      dto
    );
    if (data.data) {
      data.data = normalizeLiquidacion(data.data);
    }
    return data;
  },

  async pagar(
    id: number,
    sesionCajaId?: number
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      `/liquidaciones/${id}/pagar`,
      null,
      { params: { sesionCajaId } }
    );
    return data;
  },

  async anular(
    id: number,
    motivo: string = 'Anulación de liquidación'
  ): Promise<ApiResponse<LiquidacionDto>> {
    const { data } = await apiClient.post<ApiResponse<LiquidacionDto>>(
      `/liquidaciones/${id}/anular`,
      null,
      { params: { motivo } }
    );
    if (data.data) {
      data.data = normalizeLiquidacion(data.data);
    }
    return data;
  },
};

// 20. DashboardController (/api/dashboard)
export const dashboardService = {
  async getKpis(
    fechaDesde?: string,
    fechaHasta?: string
  ): Promise<ApiResponse<DashboardKpisDto>> {
    const { data } = await apiClient.get<ApiResponse<DashboardKpisDto>>(
      '/dashboard/kpis',
      {
        params: { fechaDesde, fechaHasta },
      }
    );
    if (data.data) {
      data.data = normalizeDashboardKpis(data.data);
    }
    return data;
  },

  async getMetricasMes(): Promise<ApiResponse<DashboardKpisDto>> {
    const { data } = await apiClient.get<ApiResponse<DashboardKpisDto>>(
      '/dashboard/metricas-mes'
    );
    if (data.data) {
      data.data = normalizeDashboardKpis(data.data);
    }
    return data;
  },
};
