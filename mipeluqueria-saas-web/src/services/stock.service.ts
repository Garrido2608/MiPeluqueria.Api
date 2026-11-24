import { apiClient } from '@/lib/axios';
import {
  extractPagedItems,
  toNormalizedPagedResult,
} from '@/lib/apiHelpers';
import {
  ActualizarProductoDto,
  ActualizarProveedorDto,
  ApiResponse,
  CrearOrdenCompraDto,
  CrearProductoDto,
  CrearProveedorDto,
  EstadoOrdenCompraEnum,
  KardexItemResponseDto,
  MovimientoStockDto,
  OrdenCompraDto,
  PagedResult,
  PaginationFilterDto,
  ProductoDto,
  ProveedorDto,
  RecepcionarOrdenCompraDto,
} from '@/types/api.types';

function normalizeOrdenCompra(raw: OrdenCompraDto): OrdenCompraDto {
  return {
    ...raw,
    detalles: extractPagedItems(raw.detalles),
  };
}

function normalizeProducto(raw: ProductoDto): ProductoDto {
  const codigoBarras = raw.codigoBarras ?? raw.codigoBarra ?? null;
  const proveedorRazonSocial =
    raw.proveedorRazonSocial ?? raw.proveedorNombre ?? null;
  const stockMinimoAlerta = raw.stockMinimoAlerta ?? raw.stockMinimo ?? 5;

  return {
    ...raw,
    codigoBarra: codigoBarras,
    codigoBarras,
    proveedorNombre: proveedorRazonSocial,
    proveedorRazonSocial,
    stockMinimo: stockMinimoAlerta,
    stockMinimoAlerta,
  };
}

function normalizeKardexItem(raw: KardexItemResponseDto): KardexItemResponseDto {
  const desc =
    raw.tipoMovimientoDescripcion || raw.tipoMovimientoNombre || '';
  return {
    ...raw,
    tipoMovimientoDescripcion: desc,
    tipoMovimientoNombre: desc,
    motivo: raw.motivo || '',
  };
}

// 13. ProductosController (/api/productos)
export const productosService = {
  async getPaged(
    filter: PaginationFilterDto = {},
    soloBajoStock: boolean = false,
    categoriaId?: number,
    proveedorId?: number
  ): Promise<ApiResponse<PagedResult<ProductoDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>('/productos', {
      params: {
        ...filter,
        soloBajoStock,
        categoriaId,
        proveedorId,
      },
    });

    return {
      ...data,
      data: toNormalizedPagedResult<ProductoDto>(
        data.data,
        normalizeProducto,
        {
          pageNumber: filter.pageNumber,
          pageSize: filter.pageSize,
        }
      ),
    };
  },

  async getAll(
    soloBajoStock: boolean = false
  ): Promise<ApiResponse<ProductoDto[]>> {
    const paged = await this.getPaged(
      { pageNumber: 1, pageSize: 100 },
      soloBajoStock
    );
    return {
      ...paged,
      data: extractPagedItems<ProductoDto>(paged.data),
    };
  },

  async getStockCritico(): Promise<ApiResponse<ProductoDto[]>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/productos/stock-critico'
    );
    return {
      ...data,
      data: extractPagedItems<ProductoDto>(data.data).map(normalizeProducto),
    };
  },

  async getById(id: number): Promise<ApiResponse<ProductoDto>> {
    const { data } = await apiClient.get<ApiResponse<ProductoDto>>(
      `/productos/${id}`
    );
    if (data.data) {
      data.data = normalizeProducto(data.data);
    }
    return data;
  },

  async create(dto: CrearProductoDto): Promise<ApiResponse<ProductoDto>> {
    const payload = {
      ...dto,
      codigoBarras: dto.codigoBarras ?? dto.codigoBarra ?? null,
    };
    const { data } = await apiClient.post<ApiResponse<ProductoDto>>(
      '/productos',
      payload
    );
    if (data.data) {
      data.data = normalizeProducto(data.data);
    }
    return data;
  },

  async update(
    id: number,
    dto: ActualizarProductoDto | CrearProductoDto
  ): Promise<ApiResponse<ProductoDto>> {
    const payload: ActualizarProductoDto = {
      categoriaId: dto.categoriaId,
      proveedorId: dto.proveedorId ?? null,
      codigoBarras: dto.codigoBarras ?? dto.codigoBarra ?? null,
      nombre: dto.nombre,
      precioCosto: dto.precioCosto,
      precioVenta: dto.precioVenta,
      stockMinimoAlerta: dto.stockMinimoAlerta,
      esParaVenta: dto.esParaVenta,
    };
    const { data } = await apiClient.put<ApiResponse<ProductoDto>>(
      `/productos/${id}`,
      payload
    );
    if (data.data) {
      data.data = normalizeProducto(data.data);
    }
    return data;
  },

  async delete(id: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/productos/${id}`
    );
    return data;
  },

  async registrarMovimiento(
    dto: MovimientoStockDto
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      '/productos/movimiento-stock',
      dto
    );
    return data;
  },

  async getKardexPaged(
    id: number,
    filter: PaginationFilterDto = {}
  ): Promise<ApiResponse<PagedResult<KardexItemResponseDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      `/productos/${id}/kardex`,
      {
        params: filter,
      }
    );

    return {
      ...data,
      data: toNormalizedPagedResult<KardexItemResponseDto>(
        data.data,
        normalizeKardexItem,
        {
          pageNumber: filter.pageNumber,
          pageSize: filter.pageSize,
        }
      ),
    };
  },

  async getKardex(
    id: number
  ): Promise<ApiResponse<KardexItemResponseDto[]>> {
    const paged = await this.getKardexPaged(id, {
      pageNumber: 1,
      pageSize: 100,
    });
    return {
      ...paged,
      data: extractPagedItems<KardexItemResponseDto>(paged.data),
    };
  },
};

// 14. ProveedoresController (/api/proveedores)
export const proveedoresService = {
  async getPaged(
    filter: PaginationFilterDto = {}
  ): Promise<ApiResponse<PagedResult<ProveedorDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>('/proveedores', {
      params: filter,
    });

    return {
      ...data,
      data: toNormalizedPagedResult<ProveedorDto>(data.data, undefined, {
        pageNumber: filter.pageNumber,
        pageSize: filter.pageSize,
      }),
    };
  },

  async getAll(): Promise<ApiResponse<ProveedorDto[]>> {
    const paged = await this.getPaged({ pageNumber: 1, pageSize: 100 });
    return {
      ...paged,
      data: extractPagedItems<ProveedorDto>(paged.data),
    };
  },

  async getById(id: number): Promise<ApiResponse<ProveedorDto>> {
    const { data } = await apiClient.get<ApiResponse<ProveedorDto>>(
      `/proveedores/${id}`
    );
    return data;
  },

  async create(dto: CrearProveedorDto): Promise<ApiResponse<ProveedorDto>> {
    const { data } = await apiClient.post<ApiResponse<ProveedorDto>>(
      '/proveedores',
      dto
    );
    return data;
  },

  async update(
    id: number,
    dto: ActualizarProveedorDto | CrearProveedorDto
  ): Promise<ApiResponse<ProveedorDto>> {
    const { data } = await apiClient.put<ApiResponse<ProveedorDto>>(
      `/proveedores/${id}`,
      dto
    );
    return data;
  },

  async delete(id: number): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.delete<ApiResponse<boolean>>(
      `/proveedores/${id}`
    );
    return data;
  },
};

// 15. OrdenesCompraController (/api/ordenes-compra)
export const ordenesCompraService = {
  async getPaged(
    filter: PaginationFilterDto = {},
    proveedorId?: number,
    estado?: EstadoOrdenCompraEnum
  ): Promise<ApiResponse<PagedResult<OrdenCompraDto>>> {
    const { data } = await apiClient.get<ApiResponse<unknown>>(
      '/ordenes-compra',
      {
        params: {
          ...filter,
          proveedorId,
          estado,
        },
      }
    );

    return {
      ...data,
      data: toNormalizedPagedResult<OrdenCompraDto>(
        data.data,
        normalizeOrdenCompra,
        {
          pageNumber: filter.pageNumber,
          pageSize: filter.pageSize,
        }
      ),
    };
  },

  async getAll(
    proveedorId?: number,
    estado?: EstadoOrdenCompraEnum
  ): Promise<ApiResponse<OrdenCompraDto[]>> {
    const paged = await this.getPaged(
      { pageNumber: 1, pageSize: 100 },
      proveedorId,
      estado
    );
    return {
      ...paged,
      data: extractPagedItems<OrdenCompraDto>(paged.data),
    };
  },

  async getById(id: number): Promise<ApiResponse<OrdenCompraDto>> {
    const { data } = await apiClient.get<ApiResponse<OrdenCompraDto>>(
      `/ordenes-compra/${id}`
    );
    if (data.data) {
      data.data = normalizeOrdenCompra(data.data);
    }
    return data;
  },

  async crear(dto: CrearOrdenCompraDto): Promise<ApiResponse<OrdenCompraDto>> {
    const { data } = await apiClient.post<ApiResponse<OrdenCompraDto>>(
      '/ordenes-compra',
      dto
    );
    return data;
  },

  async recepcionar(
    id: number,
    dto: RecepcionarOrdenCompraDto
  ): Promise<ApiResponse<OrdenCompraDto>> {
    const payload = {
      numeroComprobanteProveedor: dto.numeroComprobanteProveedor,
      actualizarCostoProductos:
        dto.actualizarCostoProductos ?? dto.actualizarPreciosCosto ?? true,
      imputarEgresoEnCajaAbierta:
        dto.imputarEgresoEnCajaAbierta ?? dto.imputarPagoEnCaja ?? false,
      observacionesRecepcion: dto.observacionesRecepcion,
      cantidadesRecibidas: dto.cantidadesRecibidas || dto.items || [],
    };
    const { data } = await apiClient.post<ApiResponse<OrdenCompraDto>>(
      `/ordenes-compra/${id}/recibir`,
      payload
    );
    return data;
  },

  async cancelar(
    id: number,
    motivo: string = 'Cancelación administrativa'
  ): Promise<ApiResponse<boolean>> {
    const { data } = await apiClient.post<ApiResponse<boolean>>(
      `/ordenes-compra/${id}/cancelar`,
      null,
      { params: { motivo } }
    );
    return data;
  },
};
