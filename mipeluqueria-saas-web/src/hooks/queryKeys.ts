export const QUERY_KEYS = {
  configuracionSalon: () => ['configuracion-salon'] as const,
  catalogosMaestros: () => ['catalogos-maestros'] as const,
  clientes: (params?: { search?: string; page?: number; pageSize?: number }) =>
    ['clientes', params ?? {}] as const,
  fichasTecnicas: (clienteId: number) =>
    ['fichas-tecnicas', clienteId] as const,
  servicios: () => ['servicios'] as const,
  peluqueros: () => ['peluqueros'] as const,
  turnos: (params?: { fecha?: string; peluqueroId?: number }) =>
    ['turnos', params ?? {}] as const,
  bloqueos: (params?: { desde?: string; hasta?: string }) =>
    ['bloqueos', params ?? {}] as const,
  productos: (params?: { search?: string; soloStockBajo?: boolean }) =>
    ['productos', params ?? {}] as const,
  kardex: (productoId: number) => ['kardex', productoId] as const,
  proveedores: () => ['proveedores'] as const,
  ordenesCompra: () => ['ordenes-compra'] as const,
  cajaActual: () => ['caja-actual'] as const,
  cajaHistorial: (params?: { page?: number }) =>
    ['caja-historial', params ?? {}] as const,
  ventasSesion: (sesionId: number) => ['ventas-sesion', sesionId] as const,
  liquidaciones: (params?: { desde?: string; hasta?: string }) =>
    ['liquidaciones', params ?? {}] as const,
  dashboardKpis: (params?: { desde?: string; hasta?: string }) =>
    ['dashboard-kpis', params ?? {}] as const,
  saasResumen: () => ['saas-resumen'] as const,
  sucursales: () => ['sucursales'] as const,
  auditoria: (params?: { page?: number; entidad?: string }) =>
    ['auditoria', params ?? {}] as const,
} as const;
