import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  extractPagedItems,
  toNormalizedPagedResult,
} from '@/lib/apiHelpers';
import {
  bloqueosService,
  cajaService,
  catalogosService,
  clientesService,
  configuracionSalonService,
  dashboardService,
  fichasTecnicasService,
  liquidacionesService,
  ordenesCompraService,
  peluquerosService,
  productosService,
  proveedoresService,
  serviciosService,
  sucursalesService,
  suscripcionesService,
  turnosService,
  ventasService,
} from '@/services/api.services';
import {
  ActualizarFichaTecnicaDto,
  ActualizarProductoDto,
  AperturaCajaDto,
  BloqueoHorarioDto,
  CambiarEstadoTurnoDto,
  CierreCajaDto,
  ClienteDto,
  ClienteRequestDto,
  ConfiguracionSalonDto,
  CrearFichaTecnicaDto,
  CrearOrdenCompraDto,
  CrearProductoDto,
  CrearTurnoDto,
  CrearVentaDto,
  FichaTecnicaDto,
  GenerarLiquidacionDto,
  KardexItemResponseDto,
  LiquidacionDto,
  MovimientoStockDto,
  OrdenCompraDto,
  PeluqueroDto,
  PlanSuscripcionDto,
  ProductoDto,
  ProveedorDto,
  RecepcionarOrdenCompraDto,
  RegistrarMovimientoCajaDto,
  ReprogramarTurnoDto,
  ServicioDto,
  SesionCajaDto,
  SucursalResponseDto,
  TurnoDto,
  VentaResponseDto,
} from '@/types/api.types';
import { QUERY_KEYS } from './queryKeys';
import { useApiMutation } from './useApiFeedback';

// ============================================================================
// 1. CONFIGURACIÓN DEL SALÓN Y CATÁLOGOS MAESTROS
// ============================================================================
export function useConfiguracionSalonQuery() {
  return useQuery({
    queryKey: QUERY_KEYS.configuracionSalon(),
    queryFn: async () => {
      const res = await configuracionSalonService.obtener();
      return res.data;
    },
    staleTime: 10 * 60 * 1000,
  });
}

export function useActualizarConfiguracionSalonMutation() {
  const queryClient = useQueryClient();
  return useApiMutation<ConfiguracionSalonDto, ConfiguracionSalonDto>({
    queryClient,
    mutationFn: (dto) => configuracionSalonService.actualizar(dto),
    invalidateKeys: [QUERY_KEYS.configuracionSalon()],
    successFallbackMessage: 'Configuración del salón actualizada.',
  });
}

export function useCatalogosMaestrosQuery() {
  return useQuery({
    queryKey: QUERY_KEYS.catalogosMaestros(),
    queryFn: async () => {
      const res = await catalogosService.obtenerTodos();
      return res.data;
    },
    staleTime: 15 * 60 * 1000,
  });
}

// ============================================================================
// 2. CLIENTES CRM & FICHAS TÉCNICAS CAPILARES
// ============================================================================
export function useClientesQuery(params?: {
  search?: string;
  page?: number;
  pageSize?: number;
}) {
  const page = params?.page ?? 1;
  const pageSize = params?.pageSize ?? 20;
  const search = params?.search;

  return useQuery({
    queryKey: QUERY_KEYS.clientes({ search, page, pageSize }),
    queryFn: async () => {
      const res = await clientesService.getAll(page, pageSize, search);
      return toNormalizedPagedResult<ClienteDto>(res.data, undefined, {
        pageNumber: page,
        pageSize,
      });
    },
    staleTime: 60 * 1000,
  });
}

export function useCrearClienteMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: ClienteRequestDto) => clientesService.create(dto),
    invalidateKeys: [['clientes']],
    successFallbackMessage: 'Cliente registrado exitosamente.',
  });
}

export function useActualizarClienteMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({ id, dto }: { id: number; dto: ClienteRequestDto }) =>
      clientesService.update(id, dto),
    invalidateKeys: [['clientes']],
    successFallbackMessage: 'Datos del cliente actualizados.',
  });
}

export function useEliminarClienteMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (id: number) => clientesService.delete(id),
    invalidateKeys: [['clientes']],
    successFallbackMessage: 'Cliente dado de baja correctamente.',
  });
}

export function useFichasTecnicasClienteQuery(clienteId: number | null) {
  return useQuery({
    queryKey: QUERY_KEYS.fichasTecnicas(clienteId ?? 0),
    queryFn: async () => {
      if (!clienteId) return [];
      const res = await fichasTecnicasService.getPorCliente(clienteId);
      return extractPagedItems<FichaTecnicaDto>(res.data);
    },
    enabled: typeof clienteId === 'number' && clienteId > 0,
    staleTime: 2 * 60 * 1000,
  });
}

export function useCrearFichaTecnicaMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: CrearFichaTecnicaDto) => fichasTecnicasService.crear(dto),
    invalidateKeys: [['fichas-tecnicas']],
    successFallbackMessage: 'Ficha técnica capilar registrada.',
  });
}

export function useActualizarFichaTecnicaMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({
      id,
      dto,
    }: {
      id: number;
      dto: ActualizarFichaTecnicaDto;
    }) => fichasTecnicasService.actualizar(id, dto),
    invalidateKeys: [['fichas-tecnicas']],
    successFallbackMessage: 'Ficha técnica capilar actualizada.',
  });
}

export function useEliminarFichaTecnicaMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (id: number) => fichasTecnicasService.eliminar(id),
    invalidateKeys: [['fichas-tecnicas']],
    successFallbackMessage: 'Ficha técnica eliminada.',
  });
}

// ============================================================================
// 3. SERVICIOS, PELUQUEROS, TURNOS Y BLOQUEOS DE AGENDA
// ============================================================================
export function useServiciosQuery() {
  return useQuery({
    queryKey: QUERY_KEYS.servicios(),
    queryFn: async () => {
      const res = await serviciosService.getAll();
      return extractPagedItems<ServicioDto>(res.data);
    },
    staleTime: 5 * 60 * 1000,
  });
}

export function usePeluquerosQuery() {
  return useQuery({
    queryKey: QUERY_KEYS.peluqueros(),
    queryFn: async () => {
      const res = await peluquerosService.getAll();
      return extractPagedItems<PeluqueroDto>(res.data);
    },
    staleTime: 5 * 60 * 1000,
  });
}

export function useTurnosAgendaQuery(params: {
  fecha: string;
  peluqueroId?: number;
}) {
  return useQuery({
    queryKey: QUERY_KEYS.turnos(params),
    queryFn: async () => {
      const res = await turnosService.getAll(
        params.fecha,
        params.peluqueroId
      );
      return extractPagedItems<TurnoDto>(res.data);
    },
    enabled: Boolean(params.fecha),
    staleTime: 15 * 1000,
  });
}

export function useCrearTurnoMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: CrearTurnoDto) => turnosService.create(dto),
    invalidateKeys: [['turnos'], ['dashboard-kpis']],
    successFallbackMessage: 'Turno agendado correctamente.',
  });
}

export function useReprogramarTurnoMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({ id, dto }: { id: number; dto: ReprogramarTurnoDto }) =>
      turnosService.reprogramar(id, dto),
    invalidateKeys: [['turnos'], ['dashboard-kpis']],
    successFallbackMessage: 'Turno reprogramado correctamente.',
  });
}

export function useCambiarEstadoTurnoMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({ id, dto }: { id: number; dto: CambiarEstadoTurnoDto }) =>
      turnosService.cambiarEstado(id, dto),
    invalidateKeys: [
      ['turnos'],
      ['productos'],
      ['kardex'],
      ['dashboard-kpis'],
    ],
    successFallbackMessage: 'Estado del turno actualizado.',
  });
}

export function useBloqueosPeluqueroQuery(peluqueroId: number | null) {
  return useQuery({
    queryKey: QUERY_KEYS.bloqueos({ desde: String(peluqueroId ?? 0) }),
    queryFn: async () => {
      if (!peluqueroId) return [];
      const res = await bloqueosService.getPorPeluquero(peluqueroId);
      return extractPagedItems<BloqueoHorarioDto>(res.data);
    },
    enabled: typeof peluqueroId === 'number' && peluqueroId > 0,
    staleTime: 60 * 1000,
  });
}

export function useCrearBloqueoMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: BloqueoHorarioDto) => bloqueosService.crear(dto),
    invalidateKeys: [['bloqueos'], ['turnos']],
    successFallbackMessage: 'Bloqueo de agenda registrado.',
  });
}

export function useEliminarBloqueoMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (id: number) => bloqueosService.eliminar(id),
    invalidateKeys: [['bloqueos'], ['turnos']],
    successFallbackMessage: 'Bloqueo de agenda eliminado.',
  });
}

// ============================================================================
// 4. PRODUCTOS, KARDEX, PROVEEDORES Y ÓRDENES DE COMPRA
// ============================================================================
export function useProductosQuery(params?: {
  search?: string;
  soloStockBajo?: boolean;
}) {
  return useQuery({
    queryKey: QUERY_KEYS.productos(params),
    queryFn: async () => {
      if (params?.soloStockBajo) {
        const res = await productosService.getStockCritico();
        return extractPagedItems<ProductoDto>(res.data);
      }
      const res = await productosService.getPaged(
        { pageNumber: 1, pageSize: 200, search: params?.search },
        false
      );
      return extractPagedItems<ProductoDto>(res.data);
    },
    staleTime: 60 * 1000,
  });
}

export function useKardexProductoQuery(productoId: number | null) {
  return useQuery({
    queryKey: QUERY_KEYS.kardex(productoId ?? 0),
    queryFn: async () => {
      if (!productoId) return [];
      const res = await productosService.getKardex(productoId);
      return extractPagedItems<KardexItemResponseDto>(res.data);
    },
    enabled: typeof productoId === 'number' && productoId > 0,
    staleTime: 30 * 1000,
  });
}

export function useCrearProductoMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: CrearProductoDto) => productosService.create(dto),
    invalidateKeys: [['productos'], ['kardex']],
    successFallbackMessage: 'Producto creado exitosamente.',
  });
}

export function useActualizarProductoMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({ id, dto }: { id: number; dto: ActualizarProductoDto }) =>
      productosService.update(id, dto),
    invalidateKeys: [['productos'], ['kardex']],
    successFallbackMessage: 'Producto actualizado correctamente.',
  });
}

export function useAjusteStockMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: MovimientoStockDto) =>
      productosService.registrarMovimiento(dto),
    invalidateKeys: [['productos'], ['kardex']],
    successFallbackMessage: 'Ajuste de stock registrado en el Kardex.',
  });
}

export function useProveedoresQuery() {
  return useQuery({
    queryKey: QUERY_KEYS.proveedores(),
    queryFn: async () => {
      const res = await proveedoresService.getAll();
      return extractPagedItems<ProveedorDto>(res.data);
    },
    staleTime: 2 * 60 * 1000,
  });
}

export function useOrdenesCompraQuery() {
  return useQuery({
    queryKey: QUERY_KEYS.ordenesCompra(),
    queryFn: async () => {
      const res = await ordenesCompraService.getAll();
      return extractPagedItems<OrdenCompraDto>(res.data);
    },
    staleTime: 60 * 1000,
  });
}

export function useCrearOrdenCompraMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: CrearOrdenCompraDto) => ordenesCompraService.crear(dto),
    invalidateKeys: [QUERY_KEYS.ordenesCompra()],
    successFallbackMessage: 'Orden de compra emitida.',
  });
}

export function useRecibirOrdenCompraMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({ id, dto }: { id: number; dto: RecepcionarOrdenCompraDto }) =>
      ordenesCompraService.recepcionar(id, dto),
    invalidateKeys: [QUERY_KEYS.ordenesCompra(), ['productos'], ['kardex']],
    successFallbackMessage: 'Mercadería recibida y acreditada en Kardex.',
  });
}

// ============================================================================
// 5. CAJA DIARIA, POS IDEMPOTENTE, LIQUIDACIONES Y DASHBOARD BI
// ============================================================================
export function useCajaActualQuery() {
  return useQuery({
    queryKey: QUERY_KEYS.cajaActual(),
    queryFn: async () => {
      const res = await cajaService.getEstadoActual();
      return res.data;
    },
    staleTime: 10 * 1000,
  });
}

export function useCajaHistorialQuery(page = 1, pageSize = 20) {
  return useQuery({
    queryKey: QUERY_KEYS.cajaHistorial({ page }),
    queryFn: async () => {
      const res = await cajaService.getHistorialPaged({
        pageNumber: page,
        pageSize,
      });
      return toNormalizedPagedResult<SesionCajaDto>(res.data, undefined, {
        pageNumber: page,
        pageSize,
      });
    },
    staleTime: 2 * 60 * 1000,
  });
}

export function useAbrirCajaMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: AperturaCajaDto) => cajaService.abrir(dto),
    invalidateKeys: [QUERY_KEYS.cajaActual(), ['caja-historial'], ['dashboard-kpis']],
    successFallbackMessage: 'Sesión de caja abierta.',
  });
}

export function useCerrarCajaMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: CierreCajaDto) => cajaService.cerrar(dto),
    invalidateKeys: [QUERY_KEYS.cajaActual(), ['caja-historial'], ['dashboard-kpis']],
    successFallbackMessage: 'Arqueo de cierre de caja completado.',
  });
}

export function useRegistrarMovimientoCajaMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({
      tipo,
      dto,
    }: {
      tipo: 'ingreso' | 'egreso';
      dto: RegistrarMovimientoCajaDto;
    }) =>
      tipo === 'ingreso'
        ? cajaService.registrarIngreso(dto)
        : cajaService.registrarEgreso(dto),
    invalidateKeys: [QUERY_KEYS.cajaActual(), ['dashboard-kpis']],
    successFallbackMessage: 'Movimiento manual imputado en caja.',
  });
}

export function useVentasPorSesionQuery(sesionId: number | null) {
  return useQuery({
    queryKey: QUERY_KEYS.ventasSesion(sesionId ?? 0),
    queryFn: async () => {
      if (!sesionId) return [];
      const res = await ventasService.getPorSesion(sesionId);
      return extractPagedItems<VentaResponseDto>(res.data);
    },
    enabled: typeof sesionId === 'number' && sesionId > 0,
    staleTime: 30 * 1000,
  });
}

export function useFacturarVentaPosMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({
      dto,
      idempotencyKey,
    }: {
      dto: CrearVentaDto;
      idempotencyKey: string;
    }) => ventasService.registrar(dto, idempotencyKey),
    invalidateKeys: [
      QUERY_KEYS.cajaActual(),
      ['ventas-sesion'],
      ['turnos'],
      ['productos'],
      ['kardex'],
      ['dashboard-kpis'],
    ],
    successFallbackMessage: 'Venta facturada exitosamente.',
  });
}

export function useAnularVentaMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({ ventaId, motivo }: { ventaId: number; motivo?: string }) =>
      ventasService.anular(ventaId, motivo ? { motivo } : undefined),
    invalidateKeys: [
      QUERY_KEYS.cajaActual(),
      ['ventas-sesion'],
      ['productos'],
      ['kardex'],
      ['dashboard-kpis'],
    ],
    successFallbackMessage: 'Venta anulada y stock revertido.',
  });
}

export function useLiquidacionesQuery(params?: {
  peluqueroId?: number;
  desde?: string;
  hasta?: string;
}) {
  return useQuery({
    queryKey: QUERY_KEYS.liquidaciones({
      desde: params?.desde,
      hasta: params?.hasta,
    }),
    queryFn: async () => {
      const res = await liquidacionesService.getAll(
        params?.peluqueroId,
        params?.desde,
        params?.hasta
      );
      return extractPagedItems<LiquidacionDto>(res.data);
    },
    staleTime: 60 * 1000,
  });
}

export function useGenerarLiquidacionMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: (dto: GenerarLiquidacionDto) =>
      liquidacionesService.generar(dto),
    invalidateKeys: [['liquidaciones']],
    successFallbackMessage: 'Liquidación de haberes calculada.',
  });
}

export function useAbonarLiquidacionMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({ id, sesionCajaId }: { id: number; sesionCajaId?: number }) =>
      liquidacionesService.pagar(id, sesionCajaId),
    invalidateKeys: [['liquidaciones'], QUERY_KEYS.cajaActual(), ['dashboard-kpis']],
    successFallbackMessage: 'Liquidación abonada.',
  });
}

export function useAnularLiquidacionMutation() {
  const queryClient = useQueryClient();
  return useApiMutation({
    queryClient,
    mutationFn: ({ id, motivo }: { id: number; motivo?: string }) =>
      liquidacionesService.anular(id, motivo),
    invalidateKeys: [['liquidaciones']],
    successFallbackMessage: 'Liquidación anulada y comisiones liberadas.',
  });
}

export function useDashboardKpisQuery(params: {
  desde: string;
  hasta: string;
}) {
  return useQuery({
    queryKey: QUERY_KEYS.dashboardKpis(params),
    queryFn: async () => {
      const res = await dashboardService.getKpis(params.desde, params.hasta);
      return res.data;
    },
    enabled: Boolean(params.desde && params.hasta),
    staleTime: 60 * 1000,
  });
}

export function useSucursalesQuery() {
  return useQuery({
    queryKey: QUERY_KEYS.sucursales(),
    queryFn: async () => {
      const res = await sucursalesService.getAll();
      return extractPagedItems<SucursalResponseDto>(res.data);
    },
    staleTime: 5 * 60 * 1000,
  });
}

export function usePlanesSuscripcionQuery() {
  return useQuery({
    queryKey: QUERY_KEYS.saasResumen(),
    queryFn: async () => {
      const res = await suscripcionesService.getPlanes();
      return extractPagedItems<PlanSuscripcionDto>(res.data);
    },
    staleTime: 5 * 60 * 1000,
  });
}
