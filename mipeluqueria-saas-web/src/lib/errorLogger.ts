// filepath: src/lib/errorLogger.ts
/**
 * Telemetría Zero-PII de Errores del Navegador hacia POST /api/telemetry/client-errors.
 * Incluye sanitización de secretos/emails/JWT, truncado <= 3000 chars y cortocircuito anti-bucle.
 */

export interface ClientErrorTelemetryInput {
  message: string;
  stack?: string;
  route?: string;
  componentStack?: string;
  correlationId?: string;
  severity?: 'error' | 'warning';
}

interface OutboundClientErrorDto {
  message: string;
  stack?: string;
  route: string;
  componentStack?: string;
  correlationId?: string;
  userAgent?: string;
  timestampUtc: string;
  severity: 'error' | 'warning';
}

const API_BASE_URL = (
  process.env.NEXT_PUBLIC_API_URL || 'https://localhost:7132/api'
).replace(/\/+$/, '');

const TELEMETRY_URL = `${API_BASE_URL}/telemetry/client-errors`;
const DEDUP_WINDOW_MS = 10_000;
const MAX_MESSAGE_CHARS = 500;
const MAX_STACK_CHARS = 2400; // Garantiza que el JSON total sea siempre <= 4 KB (4096 bytes) y <= 3000 chars
const MAX_ROUTE_CHARS = 300;
const MAX_COMPONENT_STACK_CHARS = 600;

const JWT_REGEX = /eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g;
const EMAIL_REGEX = /[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}/g;
const SECRET_KV_REGEX =
  /(password|passwordhash|token|refresh_token|refreshtoken|jwt|secret|authorization|cookie)\s*([:=])\s*([^\s&;,"'}]+)/gi;

const recentFingerprints = new Map<string, number>();
let isDispatchingTelemetry = false;
let lastCorrelationId: string | undefined;

export function setLastCorrelationId(correlationId?: string): void {
  if (correlationId && correlationId.trim().length > 0) {
    lastCorrelationId = correlationId.trim().slice(0, 64);
  }
}

export function getLastCorrelationId(): string | undefined {
  return lastCorrelationId;
}

export function generateUuidV4(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (ch) => {
    const r = (Math.random() * 16) | 0;
    const v = ch === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

function sanitizeZeroPii(
  raw: string | undefined,
  maxChars: number
): string | undefined {
  if (!raw || raw.trim().length === 0) {
    return undefined;
  }

  let cleaned = raw.trim();
  cleaned = cleaned.replace(JWT_REGEX, '[REDACTED_JWT]');
  cleaned = cleaned.replace(EMAIL_REGEX, '[REDACTED_EMAIL]');
  cleaned = cleaned.replace(SECRET_KV_REGEX, '$1$2[REDACTED]');

  if (cleaned.length > maxChars) {
    cleaned = `${cleaned.slice(0, maxChars)}...[TRUNCATED]`;
  }

  return cleaned;
}

function pruneExpiredFingerprints(now: number): void {
  recentFingerprints.forEach((timestamp, key) => {
    if (now - timestamp > DEDUP_WINDOW_MS) {
      recentFingerprints.delete(key);
    }
  });
}

function readPublicTenantHeaders(): { tenantId: string; sucursalId: string } {
  const fallbackTenant = process.env.NEXT_PUBLIC_DEFAULT_TENANT_ID || '1';
  const fallbackSucursal = process.env.NEXT_PUBLIC_DEFAULT_SUCURSAL_ID || '1';

  if (typeof window === 'undefined') {
    return { tenantId: fallbackTenant, sucursalId: fallbackSucursal };
  }

  try {
    return {
      tenantId: localStorage.getItem('mipeluqueria_tenant_id') || fallbackTenant,
      sucursalId:
        localStorage.getItem('mipeluqueria_sucursal_id') || fallbackSucursal,
    };
  } catch {
    return { tenantId: fallbackTenant, sucursalId: fallbackSucursal };
  }
}

/**
 * Envía de forma asíncrona y no bloqueante un reporte de error inesperado del cliente
 * a `POST /api/telemetry/client-errors` sin incluir tokens ni PII.
 */
export function reportClientError(input: ClientErrorTelemetryInput): void {
  if (typeof window === 'undefined' || isDispatchingTelemetry) {
    return;
  }

  const rawMessage = input.message?.trim() || '';
  if (!rawMessage) {
    return;
  }

  // Cortocircuito anti-bucle: jamás reportar fallos originados en el propio endpoint de telemetría
  if (
    rawMessage.toLowerCase().includes('/telemetry/client-errors') ||
    (input.stack || '').toLowerCase().includes('/telemetry/client-errors')
  ) {
    return;
  }

  const currentRoute =
    input.route ||
    (typeof window !== 'undefined' ? window.location.pathname : '/');

  const now = Date.now();
  pruneExpiredFingerprints(now);

  const fingerprint = `${currentRoute}::${rawMessage.slice(0, 180)}`;
  const previousTimestamp = recentFingerprints.get(fingerprint);
  if (previousTimestamp && now - previousTimestamp < DEDUP_WINDOW_MS) {
    return;
  }
  recentFingerprints.set(fingerprint, now);

  const correlationId =
    input.correlationId || lastCorrelationId || generateUuidV4();
  setLastCorrelationId(correlationId);

  const payload: OutboundClientErrorDto = {
    message: sanitizeZeroPii(rawMessage, MAX_MESSAGE_CHARS) || 'Client error',
    stack: sanitizeZeroPii(input.stack, MAX_STACK_CHARS),
    route: sanitizeZeroPii(currentRoute, MAX_ROUTE_CHARS) || '/',
    componentStack: sanitizeZeroPii(
      input.componentStack,
      MAX_COMPONENT_STACK_CHARS
    ),
    correlationId,
    userAgent:
      typeof navigator !== 'undefined'
        ? navigator.userAgent.slice(0, 200)
        : undefined,
    timestampUtc: new Date().toISOString(),
    severity: input.severity || 'error',
  };

  const jsonBody = JSON.stringify(payload);
  isDispatchingTelemetry = true;

  try {
    let sentViaBeacon = false;
    if (
      typeof navigator !== 'undefined' &&
      typeof navigator.sendBeacon === 'function'
    ) {
      const blob = new Blob([jsonBody], { type: 'application/json' });
      sentViaBeacon = navigator.sendBeacon(TELEMETRY_URL, blob);
    }

    if (!sentViaBeacon && typeof fetch === 'function') {
      const { tenantId, sucursalId } = readPublicTenantHeaders();
      void fetch(TELEMETRY_URL, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Correlation-Id': correlationId,
          'X-Tenant-Id': tenantId,
          'X-Sucursal-Id': sucursalId,
        },
        body: jsonBody,
        keepalive: true,
      }).catch(() => {
        // Silencioso por diseño: nunca reintentar ni provocar bucles de error
      });
    }
  } catch {
    // Silencioso por diseño
  } finally {
    isDispatchingTelemetry = false;
  }
}
