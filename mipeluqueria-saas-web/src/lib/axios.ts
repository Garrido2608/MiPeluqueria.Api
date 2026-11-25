import axios, {
  AxiosError,
  AxiosRequestConfig,
  InternalAxiosRequestConfig,
} from 'axios';
import { ApiResponse, AuthResponseDto } from '@/types/api.types';
import { generateUuidV4, setLastCorrelationId } from '@/lib/errorLogger';

declare module 'axios' {
  export interface AxiosRequestConfig {
    idempotencyKey?: string;
    correlationId?: string;
    _retry?: boolean;
  }
  export interface InternalAxiosRequestConfig {
    idempotencyKey?: string;
    correlationId?: string;
    _retry?: boolean;
  }
}

export interface ApiErrorPayload {
  success: false;
  message: string;
  data: null;
  errors: string[];
  fieldErrors?: Record<string, string[]>;
  warnings?: string[];
  statusCode?: number;
  correlationId?: string;
}

type BackendApiEnvelope<T> = ApiResponse<T> & {
  fieldErrors?: Record<string, string[]>;
  warnings?: string[];
};

interface FailedQueueItem {
  resolve: (newToken: string) => void;
  reject: (error: unknown) => void;
}

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_URL || 'https://localhost:7132/api';

const DEFAULT_TENANT_ID =
  process.env.NEXT_PUBLIC_DEFAULT_TENANT_ID || '1';
const DEFAULT_SUCURSAL_ID =
  process.env.NEXT_PUBLIC_DEFAULT_SUCURSAL_ID || '1';

export const STORAGE_KEYS = {
  ACCESS_TOKEN: 'mipeluqueria_jwt_token',
  REFRESH_TOKEN: 'mipeluqueria_refresh_token',
  USER_SESSION: 'mipeluqueria_user_session',
  TENANT_ID: 'mipeluqueria_tenant_id',
  SUCURSAL_ID: 'mipeluqueria_sucursal_id',
  COOKIE_CONSENT: 'mipeluqueria_cookie_consent',
  SALON_CONFIG_CACHE: 'mipeluqueria_salon_config_cache',
} as const;

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 35000,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
});

let isRefreshing = false;
let failedQueue: FailedQueueItem[] = [];

function processQueue(error: unknown, token: string | null = null): void {
  const queueSnapshot = [...failedQueue];
  failedQueue = [];

  for (const item of queueSnapshot) {
    if (error || !token) {
      item.reject(error);
    } else {
      item.resolve(token);
    }
  }
}

function readStoredSession(): AuthResponseDto | null {
  if (typeof window === 'undefined') return null;
  try {
    const raw = localStorage.getItem(STORAGE_KEYS.USER_SESSION);
    if (!raw) return null;
    return JSON.parse(raw) as AuthResponseDto;
  } catch {
    return null;
  }
}

function resolveTenantAndSucursalHeaders(): {
  tenantId: string;
  sucursalId: string;
} {
  if (typeof window === 'undefined') {
    return {
      tenantId: DEFAULT_TENANT_ID,
      sucursalId: DEFAULT_SUCURSAL_ID,
    };
  }

  const session = readStoredSession();
  const storedTenant = localStorage.getItem(STORAGE_KEYS.TENANT_ID);
  const storedSucursal = localStorage.getItem(STORAGE_KEYS.SUCURSAL_ID);

  const tenantId =
    storedTenant ||
    (session?.tenantId ? String(session.tenantId) : '') ||
    DEFAULT_TENANT_ID;

  const sucursalId =
    storedSucursal ||
    (session?.sucursalId ? String(session.sucursalId) : '') ||
    DEFAULT_SUCURSAL_ID;

  return { tenantId, sucursalId };
}

export function withIdempotencyKey(
  idempotencyKey: string,
  config?: AxiosRequestConfig
): AxiosRequestConfig {
  return {
    ...config,
    idempotencyKey,
    headers: {
      ...(config?.headers ?? {}),
      'X-Idempotency-Key': idempotencyKey,
    },
  };
}

// ============================================================================
// 1. REQUEST INTERCEPTOR: Authorization, X-Tenant-Id, X-Sucursal-Id, X-Correlation-Id, X-Idempotency-Key
// ============================================================================
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const { tenantId, sucursalId } = resolveTenantAndSucursalHeaders();
    const correlationId = config.correlationId || generateUuidV4();
    config.correlationId = correlationId;
    setLastCorrelationId(correlationId);

    if (config.headers) {
      if (typeof window !== 'undefined') {
        const token = localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN);
        if (token && !config.headers.Authorization) {
          config.headers.Authorization = `Bearer ${token}`;
        }
      }

      if (!config.headers['X-Tenant-Id']) {
        config.headers['X-Tenant-Id'] = tenantId;
      }
      if (!config.headers['X-Sucursal-Id']) {
        config.headers['X-Sucursal-Id'] = sucursalId;
      }
      if (!config.headers['X-Correlation-Id']) {
        config.headers['X-Correlation-Id'] = correlationId;
      }
      if (config.idempotencyKey && !config.headers['X-Idempotency-Key']) {
        config.headers['X-Idempotency-Key'] = config.idempotencyKey;
      }
    }

    return config;
  },
  (error: unknown) => Promise.reject(error)
);

function extractResponseCorrelationId(
  errorOrResponseHeaders?: Record<string, unknown>,
  fallbackCorrelationId?: string
): string | undefined {
  if (errorOrResponseHeaders) {
    const headerVal =
      errorOrResponseHeaders['x-correlation-id'] ??
      errorOrResponseHeaders['X-Correlation-Id'];
    if (typeof headerVal === 'string' && headerVal.trim().length > 0) {
      return headerVal.trim();
    }
  }
  return fallbackCorrelationId;
}

// ============================================================================
// 2. RESPONSE INTERCEPTOR: Cola failedQueue + POST /api/auth/refresh-token
// ============================================================================
apiClient.interceptors.response.use(
  (response) => {
    const corrId = extractResponseCorrelationId(
      response.headers as Record<string, unknown>,
      response.config.correlationId
    );
    if (corrId) {
      setLastCorrelationId(corrId);
    }
    return response;
  },
  async (error: AxiosError<BackendApiEnvelope<unknown>>) => {
    const originalRequest = error.config;
    const responseCorrelationId = extractResponseCorrelationId(
      error.response?.headers as Record<string, unknown> | undefined,
      originalRequest?.correlationId
    );
    if (responseCorrelationId) {
      setLastCorrelationId(responseCorrelationId);
    }

    // 1. Control de Rate Limiting (StrictAuthOtpPolicy: 5 req/min por IP)
    if (error.response?.status === 429) {
      const rateLimitPayload: ApiErrorPayload = {
        success: false,
        message:
          error.response?.data?.message ||
          'Has superado el límite de seguridad de 5 intentos por minuto (StrictAuthOtpPolicy). Por favor aguardá 60 segundos antes de reintentar.',
        data: null,
        errors: error.response?.data?.errors || [],
        fieldErrors: error.response?.data?.fieldErrors,
        warnings: error.response?.data?.warnings,
        statusCode: 429,
        correlationId: responseCorrelationId,
      };
      return Promise.reject(rateLimitPayload);
    }

    // 2. Prevención de "Refresh Token Storm" ante 401 Unauthorized
    const reqUrl = (originalRequest?.url || '').toLowerCase();
    const isExcludedAuthEndpoint =
      reqUrl.includes('/auth/login') ||
      reqUrl.includes('/auth/refresh-token') ||
      reqUrl.includes('/auth/registro') ||
      reqUrl.includes('/auth/verificar-email') ||
      reqUrl.includes('/auth/olvide-password') ||
      reqUrl.includes('/auth/restablecer-password') ||
      reqUrl.includes('/webhooks/');

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !isExcludedAuthEndpoint
    ) {
      if (typeof window === 'undefined') {
        return Promise.reject(buildApiErrorPayload(error));
      }

      // Si ya hay una rotación de token en vuelo, encolamos la petición en failedQueue
      if (isRefreshing) {
        return new Promise<string>((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then((newToken) => {
          originalRequest._retry = true;
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
          }
          return apiClient(originalRequest);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const currentToken =
        localStorage.getItem(STORAGE_KEYS.ACCESS_TOKEN) || '';
      const currentRefreshToken =
        localStorage.getItem(STORAGE_KEYS.REFRESH_TOKEN) || '';

      if (!currentRefreshToken) {
        isRefreshing = false;
        const unauthorizedError = buildApiErrorPayload(error);
        processQueue(unauthorizedError, null);
        clearSessionAndRedirect();
        return Promise.reject(unauthorizedError);
      }

      const { tenantId, sucursalId } = resolveTenantAndSucursalHeaders();

      try {
        const res = await axios.post<BackendApiEnvelope<AuthResponseDto>>(
          `${API_BASE_URL}/auth/refresh-token`,
          {
            token: currentToken,
            refreshToken: currentRefreshToken,
          },
          {
            headers: {
              'Content-Type': 'application/json',
              Accept: 'application/json',
              'X-Tenant-Id': tenantId,
              'X-Sucursal-Id': sucursalId,
            },
            timeout: 15000,
          }
        );

        if (res.data?.success && res.data.data?.token) {
          const refreshedAuth = res.data.data;
          const newAccessToken = refreshedAuth.token;
          const newRefreshToken = refreshedAuth.refreshToken;

          persistRefreshedSession(refreshedAuth);
          apiClient.defaults.headers.common.Authorization = `Bearer ${newAccessToken}`;

          processQueue(null, newAccessToken);

          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
          }

          // Garantizamos preservar la referencia de newRefreshToken en almacenamiento
          if (newRefreshToken) {
            localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, newRefreshToken);
          }

          return apiClient(originalRequest);
        }

        const refreshFailedError = buildApiErrorPayload(error);
        processQueue(refreshFailedError, null);
        clearSessionAndRedirect();
        return Promise.reject(refreshFailedError);
      } catch (refreshErr: unknown) {
        const normalizedRefreshError = axios.isAxiosError<
          BackendApiEnvelope<unknown>
        >(refreshErr)
          ? buildApiErrorPayload(refreshErr)
          : buildApiErrorPayload(error);

        processQueue(normalizedRefreshError, null);
        clearSessionAndRedirect();
        return Promise.reject(normalizedRefreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(buildApiErrorPayload(error));
  }
);

function buildApiErrorPayload(
  error: AxiosError<BackendApiEnvelope<unknown>>
): ApiErrorPayload {
  const apiErrorMessage =
    error.response?.data?.message ||
    error.message ||
    'Error de comunicación con el servidor.';

  const correlationId = extractResponseCorrelationId(
    error.response?.headers as Record<string, unknown> | undefined,
    error.config?.correlationId
  );

  return {
    success: false,
    message: apiErrorMessage,
    data: null,
    errors: error.response?.data?.errors || [],
    fieldErrors: error.response?.data?.fieldErrors,
    warnings: error.response?.data?.warnings,
    statusCode: error.response?.status,
    correlationId,
  };
}

function persistRefreshedSession(authData: AuthResponseDto): void {
  if (typeof window === 'undefined') return;

  localStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, authData.token);
  localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, authData.refreshToken);
  localStorage.setItem(STORAGE_KEYS.USER_SESSION, JSON.stringify(authData));

  if (authData.tenantId) {
    localStorage.setItem(STORAGE_KEYS.TENANT_ID, String(authData.tenantId));
  }
  if (authData.sucursalId && !localStorage.getItem(STORAGE_KEYS.SUCURSAL_ID)) {
    localStorage.setItem(
      STORAGE_KEYS.SUCURSAL_ID,
      String(authData.sucursalId)
    );
  }

  document.cookie = `mipeluqueria_token=${authData.token}; path=/; max-age=28800; SameSite=Lax`;
  document.cookie = `mipeluqueria_role=${authData.rol}; path=/; max-age=28800; SameSite=Lax`;
}

function clearSessionAndRedirect(): void {
  if (typeof window === 'undefined') return;

  localStorage.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
  localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
  localStorage.removeItem(STORAGE_KEYS.USER_SESSION);
  document.cookie = 'mipeluqueria_token=; path=/; max-age=0';
  document.cookie = 'mipeluqueria_role=; path=/; max-age=0';

  const pathname = window.location.pathname;
  const isPublicPath =
    pathname === '/' ||
    pathname.startsWith('/login') ||
    pathname.startsWith('/reservar') ||
    pathname.startsWith('/registro') ||
    pathname.startsWith('/recuperar-clave') ||
    pathname.startsWith('/legal');

  if (!isPublicPath) {
    window.location.href = '/login';
  }
}

export default apiClient;
