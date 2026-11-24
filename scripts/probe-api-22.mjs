// filepath: scripts/probe-api-22.mjs
/**
 * Sonda Determinística Secuencial — 22 Controladores de MiPeluqueria.Api (.NET 8)
 * Etapa 7 (Tarea 7.2) — Harness Engineering Sensor
 *
 * Reglas:
 * 1. Acepta certificado dev autofirmado ÚNICAMENTE para https://localhost:7132.
 * 2. Autentica vía POST /api/auth/login (admin@tesis.com / Admin123!, Tenant=1, Sucursal=1).
 * 3. Invoca de forma ESTRICTAMENTE SECUENCIAL los 22 endpoints GET de diagnóstico.
 * 4. Falla con exit code 1 si cualquier endpoint responde HTTP != 200 o latencia > 2000 ms.
 */

const BASE_URL = (process.env.API_BASE_URL || 'https://localhost:7132/api').replace(/\/+$/, '');
const TENANT_ID = process.env.TENANT_ID || '1';
const SUCURSAL_ID = process.env.SUCURSAL_ID || '1';
const MAX_LATENCY_MS = 2000;
const WARM_TARGET_MS = 500;
const REQUEST_TIMEOUT_MS = 25000;

if (BASE_URL.startsWith('https://localhost:7132')) {
  process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';
}

const ENDPOINTS_22 = [
  { id: 1, controller: 'ConfiguracionSalonController', path: 'configuracion' },
  { id: 2, controller: 'CatalogosController', path: 'catalogos/todos' },
  { id: 3, controller: 'PortalReservasController (Servicios)', path: 'public/catalogo-servicios' },
  { id: 4, controller: 'PortalReservasController (Peluqueros)', path: 'public/peluqueros' },
  { id: 5, controller: 'ServiciosController', path: 'servicios' },
  { id: 6, controller: 'PeluquerosController', path: 'peluqueros' },
  { id: 7, controller: 'BloqueosController', path: 'bloqueos/peluquero/1' },
  { id: 8, controller: 'TurnosController', path: 'turnos' },
  { id: 9, controller: 'ClientesController', path: 'clientes?pageNumber=1&pageSize=5' },
  { id: 10, controller: 'FichasTecnicasController', path: 'fichastecnicas/cliente/1' },
  { id: 11, controller: 'RecetasServicioController', path: 'recetas-servicio/servicio/1' },
  { id: 12, controller: 'ProductosController', path: 'productos' },
  { id: 13, controller: 'ProductosController (Stock Crítico)', path: 'productos/stock-critico' },
  { id: 14, controller: 'ProveedoresController', path: 'proveedores' },
  { id: 15, controller: 'OrdenesCompraController', path: 'ordenes-compra' },
  { id: 16, controller: 'CajaController', path: 'caja/estado-actual' },
  { id: 17, controller: 'VentasController', path: 'ventas/sesion/1' },
  { id: 18, controller: 'ComisionesController', path: 'comisiones' },
  { id: 19, controller: 'LiquidacionesController', path: 'liquidaciones' },
  { id: 20, controller: 'DashboardController', path: 'dashboard/kpis' },
  { id: 21, controller: 'UsuariosController', path: 'usuarios' },
  { id: 22, controller: 'SuscripcionesController', path: 'suscripciones/planes' },
];

function classifyPayloadShape(apiResponseBody) {
  if (!apiResponseBody || typeof apiResponseBody !== 'object') {
    return 'Object';
  }

  const inner = apiResponseBody.data;
  if (Array.isArray(inner)) {
    return 'Array';
  }

  if (
    inner !== null &&
    typeof inner === 'object' &&
    (Array.isArray(inner.data) || Array.isArray(inner.items)) &&
    (typeof inner.totalRecords === 'number' || typeof inner.totalCount === 'number')
  ) {
    return 'PagedResult';
  }

  return 'Object';
}

function padRight(str, len) {
  const text = String(str);
  return text.length >= len ? text.slice(0, len) : text + ' '.repeat(len - text.length);
}

function padLeft(str, len) {
  const text = String(str);
  return text.length >= len ? text : ' '.repeat(len - text.length) + text;
}

async function authenticate() {
  const loginUrl = `${BASE_URL}/auth/login`;
  const start = performance.now();

  const res = await fetch(loginUrl, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      'X-Tenant-Id': TENANT_ID,
      'X-Sucursal-Id': SUCURSAL_ID,
    },
    body: JSON.stringify({
      usernameOrEmail: 'admin@tesis.com',
      password: 'Admin123!',
    }),
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
  });

  const elapsedMs = Math.round(performance.now() - start);
  const json = await res.json().catch(() => null);

  if (res.status !== 200 || !json?.success || !json?.data?.token) {
    console.error(
      `[FATAL] Falló POST /api/auth/login -> HTTP ${res.status} (${elapsedMs} ms): ${JSON.stringify(json)}`
    );
    process.exit(1);
  }

  console.log(
    `[AUTH OK] POST /api/auth/login -> HTTP ${res.status} (${elapsedMs} ms) | Tenant=${TENANT_ID}, Sucursal=${SUCURSAL_ID}`
  );
  return json.data.token;
}

async function runSequentialProbe() {
  console.log(`=== SONDA RUNTIME SECUENCIAL (22 CONTROLADORES) — ${BASE_URL} ===`);
  const token = await authenticate();

  const results = [];
  const failures = [];

  // Ejecución ESTRICTAMENTE SECUENCIAL (uno tras otro, jamás en paralelo)
  for (const item of ENDPOINTS_22) {
    const url = `${BASE_URL}/${item.path}`;
    const start = performance.now();
    let statusText = 'ERR';
    let httpStatus = 0;
    let shape = '-';
    let errorDetail = '';

    try {
      const response = await fetch(url, {
        method: 'GET',
        headers: {
          Accept: 'application/json',
          Authorization: `Bearer ${token}`,
          'X-Tenant-Id': TENANT_ID,
          'X-Sucursal-Id': SUCURSAL_ID,
        },
        signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
      });

      const latencyMs = Math.round(performance.now() - start);
      httpStatus = response.status;
      const body = await response.json().catch(() => null);
      shape = classifyPayloadShape(body);
      statusText = `${httpStatus} ${response.ok ? 'OK' : 'FAIL'}`;

      const row = {
        id: item.id,
        label: `${item.controller} (/api/${item.path})`,
        httpStatus,
        statusText,
        latencyMs,
        shape,
        apiSuccess: Boolean(body?.success),
      };
      results.push(row);

      if (httpStatus !== 200 || !body?.success || latencyMs > MAX_LATENCY_MS) {
        errorDetail = body?.message || `HTTP ${httpStatus} / Latencia ${latencyMs} ms`;
        failures.push({ ...row, errorDetail });
      }
    } catch (err) {
      const latencyMs = Math.round(performance.now() - start);
      const isTimeout = err?.name === 'TimeoutError' || err?.name === 'AbortError';
      statusText = isTimeout ? 'TIMEOUT' : 'NET_ERR';
      errorDetail = err instanceof Error ? err.message : String(err);

      const row = {
        id: item.id,
        label: `${item.controller} (/api/${item.path})`,
        httpStatus: 0,
        statusText,
        latencyMs,
        shape: '-',
        apiSuccess: false,
      };
      results.push(row);
      failures.push({ ...row, errorDetail });
    }
  }

  const colId = 3;
  const colEndpoint = 70;
  const colStatus = 12;
  const colLatency = 14;
  const colShape = 13;

  const sep = `+-${'-'.repeat(colId)}-+-${'-'.repeat(colEndpoint)}-+-${'-'.repeat(colStatus)}-+-${'-'.repeat(colLatency)}-+-${'-'.repeat(colShape)}-+`;
  console.log(sep);
  console.log(
    `| ${padLeft('#', colId)} | ${padRight('Controlador / Endpoint', colEndpoint)} | ${padRight('Status HTTP', colStatus)} | ${padLeft('Latencia (ms)', colLatency)} | ${padRight('Forma Payload', colShape)} |`
  );
  console.log(sep);

  for (const r of results) {
    console.log(
      `| ${padLeft(r.id, colId)} | ${padRight(r.label, colEndpoint)} | ${padRight(r.statusText, colStatus)} | ${padLeft(r.latencyMs, colLatency)} | ${padRight(r.shape, colShape)} |`
    );
  }
  console.log(sep);

  const ok200Count = results.filter((r) => r.httpStatus === 200 && r.apiSuccess).length;
  const underWarmCount = results.filter((r) => r.httpStatus === 200 && r.latencyMs < WARM_TARGET_MS).length;
  const maxLatency = Math.max(...results.map((r) => r.latencyMs));
  const avgLatency = Math.round(results.reduce((acc, r) => acc + r.latencyMs, 0) / results.length);

  console.log(
    `[RESUMEN] 200 OK: ${ok200Count}/${ENDPOINTS_22.length} | < ${WARM_TARGET_MS} ms (caliente): ${underWarmCount}/${ENDPOINTS_22.length} | Promedio: ${avgLatency} ms | Máx: ${maxLatency} ms`
  );

  if (failures.length > 0) {
    console.error('\n[FALLOS DETECTADOS POR SENSOR DETERMINÍSTICO]:');
    for (const f of failures) {
      console.error(`  - #${f.id} ${f.label} -> ${f.statusText} (${f.latencyMs} ms): ${f.errorDetail}`);
    }
    process.exit(1);
  }

  console.log('[PASS] 22/22 endpoints respondieron 200 OK dentro del umbral (< 2000 ms).');
}

runSequentialProbe().catch((err) => {
  console.error('[FATAL] Error inesperado ejecutando probe-api-22.mjs:', err);
  process.exit(1);
});
