// filepath: src/app/(dashboard)/admin/diagnostico-api/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
import { Activity, Play, RefreshCw, Server, ShieldAlert } from 'lucide-react';
import { apiClient } from '@/lib/axios';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import { Badge, Button } from '@/components/ui';

interface EndpointCheckResult {
  controlador: string;
  endpoint: string;
  status: 'OK' | 'WARN' | 'ERROR';
  httpCode: number;
  latenciaMs: number;
  formaPayload: 'PagedResult' | 'Array' | 'Object' | '-';
  detalle: string;
}

interface HealthProbeState {
  status: 'IDLE' | 'CHECKING' | 'HEALTHY' | 'UNHEALTHY';
  httpCode: number | null;
  latenciaMs: number | null;
  texto: string;
  correlationId?: string;
}

const BATCH_SIZE = 4;

const ENDPOINTS_A_VERIFICAR = [
  {
    controlador: '1. ConfiguracionSalonController',
    endpoint: '/configuracion',
  },
  { controlador: '2. CatalogosController', endpoint: '/catalogos/todos' },
  {
    controlador: '3. PortalReservasController (Servicios)',
    endpoint: '/public/catalogo-servicios',
  },
  {
    controlador: '4. PortalReservasController (Peluqueros)',
    endpoint: '/public/peluqueros',
  },
  { controlador: '5. ServiciosController', endpoint: '/servicios' },
  { controlador: '6. PeluquerosController', endpoint: '/peluqueros' },
  { controlador: '7. BloqueosController', endpoint: '/bloqueos/peluquero/1' },
  { controlador: '8. TurnosController', endpoint: '/turnos' },
  {
    controlador: '9. ClientesController',
    endpoint: '/clientes?pageNumber=1&pageSize=5',
  },
  {
    controlador: '10. FichasTecnicasController',
    endpoint: '/fichastecnicas/cliente/1',
  },
  {
    controlador: '11. RecetasServicioController',
    endpoint: '/recetas-servicio/servicio/1',
  },
  { controlador: '12. ProductosController', endpoint: '/productos' },
  {
    controlador: '13. ProductosController (Stock Crítico)',
    endpoint: '/productos/stock-critico',
  },
  { controlador: '14. ProveedoresController', endpoint: '/proveedores' },
  { controlador: '15. OrdenesCompraController', endpoint: '/ordenes-compra' },
  { controlador: '16. CajaController', endpoint: '/caja/estado-actual' },
  { controlador: '17. VentasController', endpoint: '/ventas/sesion/1' },
  { controlador: '18. ComisionesController', endpoint: '/comisiones' },
  { controlador: '19. LiquidacionesController', endpoint: '/liquidaciones' },
  { controlador: '20. DashboardController', endpoint: '/dashboard/kpis' },
  { controlador: '21. UsuariosController', endpoint: '/usuarios' },
  {
    controlador: '22. Sucursales & Suscripciones SaaS',
    endpoint: '/suscripciones/planes',
  },
] as const;

function resolveServerRootUrl(): string {
  const apiUrl = (
    process.env.NEXT_PUBLIC_API_URL || 'https://localhost:7132/api'
  ).replace(/\/+$/, '');
  return apiUrl.replace(/\/api$/i, '');
}

function classifyPayloadShape(
  apiResponseBody: unknown
): 'PagedResult' | 'Array' | 'Object' {
  if (
    !apiResponseBody ||
    typeof apiResponseBody !== 'object' ||
    Array.isArray(apiResponseBody)
  ) {
    return 'Object';
  }

  const inner = (apiResponseBody as Record<string, unknown>).data;
  if (Array.isArray(inner)) {
    return 'Array';
  }

  if (inner && typeof inner === 'object') {
    const paged = inner as Record<string, unknown>;
    if (
      (Array.isArray(paged.data) || Array.isArray(paged.items)) &&
      (typeof paged.totalRecords === 'number' ||
        typeof paged.totalCount === 'number')
    ) {
      return 'PagedResult';
    }
  }

  return 'Object';
}

const INITIAL_HEALTH_STATE: HealthProbeState = {
  status: 'IDLE',
  httpCode: null,
  latenciaMs: null,
  texto: 'Pendiente de verificación',
};

export default function ConsolaDiagnosticoApiPage() {
  const [resultados, setResultados] = useState<EndpointCheckResult[]>([]);
  const [ejecutando, setEjecutando] = useState<boolean>(false);
  const [progresoLote, setProgresoLote] = useState<{
    actual: number;
    total: number;
  } | null>(null);

  const [liveHealth, setLiveHealth] =
    useState<HealthProbeState>(INITIAL_HEALTH_STATE);
  const [readyHealth, setReadyHealth] =
    useState<HealthProbeState>(INITIAL_HEALTH_STATE);
  const [verificandoHealth, setVerificandoHealth] = useState<boolean>(false);

  const apiBaseUrl =
    process.env.NEXT_PUBLIC_API_URL || 'https://localhost:7132/api';
  const serverRootUrl = resolveServerRootUrl();

  const consultarHealthEndpoint = useCallback(
    async (
      path: '/health/live' | '/health/ready'
    ): Promise<HealthProbeState> => {
      const inicio = performance.now();
      try {
        const res = await fetch(`${serverRootUrl}${path}`, {
          method: 'GET',
          cache: 'no-store',
        });
        const latenciaMs = Math.round(performance.now() - inicio);
        const bodyText = (await res.text()).trim();
        const correlationId =
          res.headers.get('x-correlation-id') ||
          res.headers.get('X-Correlation-Id') ||
          undefined;

        const isHealthy =
          res.ok && bodyText.toLowerCase().includes('healthy');

        return {
          status: isHealthy ? 'HEALTHY' : 'UNHEALTHY',
          httpCode: res.status,
          latenciaMs,
          texto: bodyText || (res.ok ? 'Healthy' : `HTTP ${res.status}`),
          correlationId,
        };
      } catch {
        const latenciaMs = Math.round(performance.now() - inicio);
        return {
          status: 'UNHEALTHY',
          httpCode: 0,
          latenciaMs,
          texto: 'Sin respuesta del host .NET',
        };
      }
    },
    [serverRootUrl]
  );

  const verificarHealthChecks = useCallback(async () => {
    setVerificandoHealth(true);
    setLiveHealth((prev) => ({ ...prev, status: 'CHECKING' }));
    setReadyHealth((prev) => ({ ...prev, status: 'CHECKING' }));

    const [liveRes, readyRes] = await Promise.all([
      consultarHealthEndpoint('/health/live'),
      consultarHealthEndpoint('/health/ready'),
    ]);

    setLiveHealth(liveRes);
    setReadyHealth(readyRes);
    setVerificandoHealth(false);
  }, [consultarHealthEndpoint]);

  useEffect(() => {
    void verificarHealthChecks();
  }, [verificarHealthChecks]);

  const verificarEndpointIndividual = async (item: {
    controlador: string;
    endpoint: string;
  }): Promise<EndpointCheckResult> => {
    const inicio = performance.now();
    try {
      const res = await apiClient.get<unknown>(item.endpoint);
      const latenciaMs = Math.round(performance.now() - inicio);
      const formaPayload = classifyPayloadShape(res.data);

      return {
        controlador: item.controlador,
        endpoint: `GET ${item.endpoint}`,
        status: 'OK',
        httpCode: res.status,
        latenciaMs,
        formaPayload,
        detalle: `Respuesta JSON válida (${formaPayload})`,
      };
    } catch (err: unknown) {
      const latenciaMs = Math.round(performance.now() - inicio);
      const customErr = err as { statusCode?: number; message?: string };
      const code = customErr?.statusCode || 500;

      return {
        controlador: item.controlador,
        endpoint: `GET ${item.endpoint}`,
        status: code === 401 || code === 403 || code === 404 ? 'WARN' : 'ERROR',
        httpCode: code,
        latenciaMs,
        formaPayload: '-',
        detalle:
          code === 401
            ? 'Endpoint protegido — requiere token JWT activo'
            : customErr?.message ||
              'Verificar que el backend .NET esté en ejecución',
      };
    }
  };

  const ejecutarDiagnosticoEnLotes = async () => {
    setEjecutando(true);
    setResultados([]);

    await verificarHealthChecks();

    const totalLotes = Math.ceil(ENDPOINTS_A_VERIFICAR.length / BATCH_SIZE);
    const acumulados: EndpointCheckResult[] = [];

    for (let i = 0; i < ENDPOINTS_A_VERIFICAR.length; i += BATCH_SIZE) {
      const loteNumero = Math.floor(i / BATCH_SIZE) + 1;
      setProgresoLote({ actual: loteNumero, total: totalLotes });

      const lote = ENDPOINTS_A_VERIFICAR.slice(i, i + BATCH_SIZE);
      const resultadosLote = await Promise.all(
        lote.map((endpointItem) => verificarEndpointIndividual(endpointItem))
      );

      acumulados.push(...resultadosLote);
      setResultados([...acumulados]);
    }

    setProgresoLote(null);
    setEjecutando(false);
  };

  const totalOk = resultados.filter((r) => r.status === 'OK').length;
  const totalWarn = resultados.filter((r) => r.status === 'WARN').length;
  const totalErr = resultados.filter((r) => r.status === 'ERROR').length;
  const latenciaPromedio =
    resultados.length > 0
      ? Math.round(
          resultados.reduce((acc, r) => acc + r.latenciaMs, 0) /
            resultados.length
        )
      : 0;

  return (
    <div className="max-w-[1440px] mx-auto space-y-5 min-w-0">
      <header className="bg-surface border border-border-subtle rounded-xl p-5 flex flex-wrap items-center justify-between gap-4 shadow-sm">
        <div className="min-w-0 space-y-1">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant="courtesy">
              <ShieldAlert className="h-3.5 w-3.5" aria-hidden="true" />
              SuperAdminSaaS
            </Badge>
            <span className="text-xs text-ink-secondary">
              Sonda controlada en lotes de {BATCH_SIZE} requests paralelos
            </span>
          </div>
          <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary">
            Diagnóstico API en Vivo
          </h1>
          <p className="text-xs text-ink-secondary break-all">
            API:{' '}
            <code className="font-mono-tabular text-ink-primary">
              {apiBaseUrl}
            </code>{' '}
            • Health:{' '}
            <code className="font-mono-tabular text-ink-primary">
              {serverRootUrl}
            </code>
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2 shrink-0">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={verificarHealthChecks}
            isLoading={verificandoHealth}
            disabled={ejecutando}
            leftIcon={<RefreshCw className="h-4 w-4" aria-hidden="true" />}
          >
            Actualizar Health Checks
          </Button>
          <Button
            type="button"
            variant="primary"
            onClick={ejecutarDiagnosticoEnLotes}
            isLoading={ejecutando}
            leftIcon={<Play className="h-4 w-4" aria-hidden="true" />}
          >
            Probar los 22 controladores (Lotes x{BATCH_SIZE})
          </Button>
        </div>
      </header>

      {/* Tarjetas de Estado en Vivo: /health/live y /health/ready */}
      <section className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {[
          {
            titulo: 'Liveness Probe (Proceso Kestrel)',
            ruta: `${serverRootUrl}/health/live`,
            estado: liveHealth,
            icon: <Activity className="h-4 w-4 text-brand-primary" aria-hidden="true" />,
          },
          {
            titulo: 'Readiness Probe (SQL Server DbContext)',
            ruta: `${serverRootUrl}/health/ready`,
            estado: readyHealth,
            icon: <Server className="h-4 w-4 text-brand-primary" aria-hidden="true" />,
          },
        ].map((probe) => (
          <div
            key={probe.ruta}
            className="bg-surface border border-border-subtle rounded-xl p-4 flex items-center justify-between gap-4 shadow-sm min-w-0"
          >
            <div className="space-y-1 min-w-0">
              <div className="flex items-center gap-2">
                {probe.icon}
                <span className="text-xs font-semibold text-ink-secondary">
                  {probe.titulo}
                </span>
              </div>
              <code className="text-xs font-mono-tabular text-ink-primary block truncate">
                GET {probe.ruta}
              </code>
              <p className="text-xs text-ink-secondary">
                Estado:{' '}
                <strong className="text-ink-primary">
                  {probe.estado.texto}
                </strong>
                {probe.estado.latenciaMs != null && (
                  <span className="font-mono-tabular">
                    {' '}
                    • {probe.estado.latenciaMs} ms
                  </span>
                )}
                {probe.estado.correlationId && (
                  <span className="ml-2 font-mono-tabular text-[11px] text-ink-secondary">
                    CorrId: {probe.estado.correlationId.slice(0, 12)}…
                  </span>
                )}
              </p>
            </div>

            <div className="shrink-0">
              {probe.estado.status === 'HEALTHY' ? (
                <Badge variant="success" className="font-mono-tabular">
                  200 HEALTHY
                </Badge>
              ) : probe.estado.status === 'CHECKING' ? (
                <Badge variant="info">VERIFICANDO...</Badge>
              ) : probe.estado.status === 'UNHEALTHY' ? (
                <Badge variant="danger" className="font-mono-tabular">
                  {probe.estado.httpCode
                    ? `${probe.estado.httpCode} UNHEALTHY`
                    : 'OFFLINE'}
                </Badge>
              ) : (
                <Badge variant="neutral">PENDIENTE</Badge>
              )}
            </div>
          </div>
        ))}
      </section>

      {progresoLote && (
        <div
          className={`rounded-xl border px-4 py-3 text-xs flex flex-wrap items-center justify-between gap-2 ${SEMANTIC_COLORS.warning.surfaceClass}`}
        >
          <span className="text-ink-primary">
            Ejecutando sonda controlada en lotes de {BATCH_SIZE} requests
            paralelos —{' '}
            <strong>
              Lote {progresoLote.actual} de {progresoLote.total}
            </strong>
            ...
          </span>
          <span className="font-mono-tabular font-semibold text-ink-primary">
            {resultados.length} / {ENDPOINTS_A_VERIFICAR.length} completados
          </span>
        </div>
      )}

      {resultados.length > 0 && (
        <div className="space-y-3 min-w-0">
          <div className="flex flex-wrap items-center gap-2.5 text-xs bg-surface border border-border-subtle rounded-xl px-4 py-3 shadow-sm">
            <Badge variant="success" className="font-mono-tabular">
              OK: {totalOk}/22
            </Badge>
            <Badge variant="warning" className="font-mono-tabular">
              WARN: {totalWarn}
            </Badge>
            <Badge variant="danger" className="font-mono-tabular">
              ERROR: {totalErr}
            </Badge>
            <span className="text-ink-secondary font-mono-tabular ml-auto">
              Latencia promedio:{' '}
              <strong className="text-ink-primary">
                {latenciaPromedio} ms
              </strong>
            </span>
          </div>

          <div className="bg-surface border border-border-subtle rounded-xl overflow-hidden shadow-sm">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead className="bg-elevated text-ink-secondary font-medium border-b border-border-subtle">
                  <tr>
                    <th className="px-3.5 py-2.5">Controlador</th>
                    <th className="px-3.5 py-2.5">Endpoint</th>
                    <th className="px-3.5 py-2.5">HTTP</th>
                    <th className="px-3.5 py-2.5">Forma</th>
                    <th className="px-3.5 py-2.5 text-right">Latencia</th>
                    <th className="px-3.5 py-2.5">Diagnóstico</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border-subtle">
                  {resultados.map((r, idx) => (
                    <tr
                      key={idx}
                      className="hover:bg-elevated/40 transition-colors"
                    >
                      <td className="px-3.5 py-2.5 font-medium text-ink-primary whitespace-nowrap">
                        {r.controlador}
                      </td>
                      <td className="px-3.5 py-2.5 font-mono-tabular text-ink-secondary whitespace-nowrap">
                        {r.endpoint}
                      </td>
                      <td className="px-3.5 py-2.5 whitespace-nowrap">
                        <span
                          className={`inline-flex items-center px-2 py-0.5 rounded-md border font-mono-tabular font-semibold ${
                            r.status === 'OK'
                              ? SEMANTIC_COLORS.cash.badgeClass
                              : r.status === 'WARN'
                              ? SEMANTIC_COLORS.warning.badgeClass
                              : SEMANTIC_COLORS.danger.badgeClass
                          }`}
                        >
                          {r.httpCode} ({r.status})
                        </span>
                      </td>
                      <td className="px-3.5 py-2.5 font-mono-tabular text-ink-secondary whitespace-nowrap">
                        {r.formaPayload}
                      </td>
                      <td className="px-3.5 py-2.5 font-mono-tabular text-right text-ink-primary whitespace-nowrap">
                        {r.latenciaMs} ms
                      </td>
                      <td className="px-3.5 py-2.5 text-ink-secondary">
                        {r.detalle}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
