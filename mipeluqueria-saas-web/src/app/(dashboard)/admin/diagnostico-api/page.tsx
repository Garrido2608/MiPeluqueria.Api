// filepath: src/app/(dashboard)/admin/diagnostico-api/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
import { apiClient } from '@/lib/axios';
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
    async (path: '/health/live' | '/health/ready'): Promise<HealthProbeState> => {
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
        detalle: `Respuesta JSON válida (${formaPayload}) desde MiPeluqueria.Api`,
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
            ? 'Endpoint protegido por [Authorize] — requiere token JWT activo'
            : customErr?.message ||
              'Verificar que el backend .NET esté corriendo',
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
    <div className="p-6 space-y-6">
      <header className="bg-white border border-slate-200 rounded-lg p-5 flex flex-wrap items-center justify-between gap-4">
        <div>
          <span className="text-xs font-bold uppercase tracking-widest text-amber-900">
            Herramienta de Ingeniería • Sonda en Lotes de {BATCH_SIZE} Requests
            Paralelos
          </span>
          <h1 className="text-xl font-bold text-slate-900 mt-0.5">
            Diagnóstico en Vivo de Conectividad con MiPeluqueria.Api (.NET 8)
          </h1>
          <p className="text-xs text-slate-500">
            API Base: <code>{apiBaseUrl}</code> • Health Host:{' '}
            <code>{serverRootUrl}</code>
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="secondary"
            size="sm"
            onClick={verificarHealthChecks}
            isLoading={verificandoHealth}
            disabled={ejecutando}
          >
            ↻ Actualizar Health Checks
          </Button>
          <Button
            variant="editorial"
            onClick={ejecutarDiagnosticoEnLotes}
            isLoading={ejecutando}
          >
            ▶ Probar los 22 Controladores (Lotes x{BATCH_SIZE})
          </Button>
        </div>
      </header>

      {/* Tarjeta Superior de Estado en Vivo: /health/live y /health/ready */}
      <section className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {[
          {
            titulo: 'Liveness Probe (Proceso Kestrel)',
            ruta: `${serverRootUrl}/health/live`,
            estado: liveHealth,
          },
          {
            titulo: 'Readiness Probe (SQL Server DbContextCheck)',
            ruta: `${serverRootUrl}/health/ready`,
            estado: readyHealth,
          },
        ].map((probe) => (
          <div
            key={probe.ruta}
            className="bg-white border border-slate-200 rounded-lg p-4 flex items-center justify-between gap-4 shadow-sm"
          >
            <div className="space-y-1 min-w-0">
              <span className="text-[11px] font-bold uppercase tracking-wider text-slate-500 block">
                {probe.titulo}
              </span>
              <code className="text-xs font-mono text-slate-800 block truncate">
                GET {probe.ruta}
              </code>
              <p className="text-xs text-slate-500">
                Estado: <strong>{probe.estado.texto}</strong>
                {probe.estado.latenciaMs != null &&
                  ` • ${probe.estado.latenciaMs} ms`}
                {probe.estado.correlationId && (
                  <span className="ml-2 font-mono text-[11px] text-slate-400">
                    CorrId: {probe.estado.correlationId.slice(0, 12)}…
                  </span>
                )}
              </p>
            </div>
            <div className="shrink-0">
              {probe.estado.status === 'HEALTHY' ? (
                <Badge variant="success">
                  200 HEALTHY
                </Badge>
              ) : probe.estado.status === 'CHECKING' ? (
                <Badge variant="info">VERIFICANDO...</Badge>
              ) : probe.estado.status === 'UNHEALTHY' ? (
                <Badge variant="danger">
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
        <div className="bg-amber-50 border border-amber-200 rounded-lg px-4 py-3 text-xs text-amber-950 flex items-center justify-between">
          <span>
            Ejecutando sonda controlada en lotes de {BATCH_SIZE} requests
            paralelos —{' '}
            <strong>
              Lote {progresoLote.actual} de {progresoLote.total}
            </strong>
            ...
          </span>
          <span className="font-mono font-bold">
            {resultados.length} / {ENDPOINTS_A_VERIFICAR.length} completados
          </span>
        </div>
      )}

      {resultados.length > 0 && (
        <div className="space-y-3">
          <div className="flex flex-wrap items-center gap-3 text-xs bg-white border border-slate-200 rounded-lg px-4 py-3">
            <Badge variant="success">OK: {totalOk}/22</Badge>
            <Badge variant="warning">WARN: {totalWarn}</Badge>
            <Badge variant="danger">ERROR: {totalErr}</Badge>
            <span className="text-slate-600 font-mono ml-auto">
              Latencia promedio: <strong>{latenciaPromedio} ms</strong>
            </span>
          </div>

          <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-100 text-slate-700 uppercase">
                <tr>
                  <th className="p-3">Controlador C#</th>
                  <th className="p-3">Endpoint</th>
                  <th className="p-3">HTTP</th>
                  <th className="p-3">Forma</th>
                  <th className="p-3">Latencia</th>
                  <th className="p-3">Diagnóstico</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {resultados.map((r, idx) => (
                  <tr key={idx}>
                    <td className="p-3 font-bold text-slate-900">
                      {r.controlador}
                    </td>
                    <td className="p-3 font-mono">{r.endpoint}</td>
                    <td className="p-3 font-mono font-bold">
                      <span
                        className={`px-2 py-0.5 rounded ${
                          r.status === 'OK'
                            ? 'bg-emerald-100 text-emerald-900'
                            : r.status === 'WARN'
                            ? 'bg-amber-100 text-amber-900'
                            : 'bg-red-100 text-red-900'
                        }`}
                      >
                        {r.httpCode} ({r.status})
                      </span>
                    </td>
                    <td className="p-3 font-mono text-slate-700">
                      {r.formaPayload}
                    </td>
                    <td className="p-3 font-mono">{r.latenciaMs} ms</td>
                    <td className="p-3 text-slate-600">{r.detalle}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
