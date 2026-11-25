'use client';

import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
} from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';
import { ThemeProvider, useTheme } from '@/context/ThemeProvider';
import {
  AuthResponseDto,
  CatalogosMaestrosDto,
  ConfiguracionSalonDto,
  CrearDetalleVentaItemDto,
  SesionCajaDto,
  TipoItemVentaEnum,
  TurnoDto,
} from '@/types/api.types';
import { STORAGE_KEYS } from '@/lib/axios';
import { reportClientError } from '@/lib/errorLogger';
import {
  cajaService,
  catalogosService,
  configuracionSalonService,
} from '@/services/api.services';

// ============================================================================
// 1. AUTH & MULTI-TENANT CONTEXT
// ============================================================================
interface AuthContextValue {
  user: AuthResponseDto | null;
  isAuthenticated: boolean;
  isLoadingAuth: boolean;
  tenantId: number;
  sucursalId: number;
  setSession: (authData: AuthResponseDto) => void;
  switchSucursal: (nuevaSucursalId: number) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

// ============================================================================
// 2. CATÁLOGOS MAESTROS & CONFIGURACIÓN DEL SALÓN CONTEXT
// ============================================================================
interface CatalogosContextValue {
  catalogos: CatalogosMaestrosDto | null;
  configSalon: ConfiguracionSalonDto | null;
  recargarCatalogosYConfig: () => Promise<void>;
}

const CatalogosContext = createContext<CatalogosContextValue | undefined>(
  undefined
);

// ============================================================================
// 3. TESORERÍA (CAJA ABIERTA) Y PUENTE AGENDA -> POS CONTEXT
// ============================================================================
export interface PosPreloadTicket {
  turnoId: number;
  clienteId: number;
  clienteNombre: string;
  renglones: CrearDetalleVentaItemDto[];
}

interface CajaPosContextValue {
  sesionCajaActual: SesionCajaDto | null;
  cajaAbierta: boolean;
  cargandoCaja: boolean;
  posPreload: PosPreloadTicket | null;
  refrescarEstadoCaja: () => Promise<void>;
  enviarTurnoAlPos: (turno: TurnoDto) => void;
  limpiarPreloadPos: () => void;
}

const CajaPosContext = createContext<CajaPosContextValue | undefined>(
  undefined
);

// ============================================================================
// 4. CONSENTIMIENTO DE COOKIES Y ANALYTICS (GDPR)
// ============================================================================
interface CookieConsentContextValue {
  consentGiven: 'accepted' | 'rejected' | 'pending';
  acceptCookies: () => void;
  rejectCookies: () => void;
}

const CookieConsentContext = createContext<
  CookieConsentContextValue | undefined
>(undefined);

const DEFAULT_TENANT_ID = Number(process.env.NEXT_PUBLIC_DEFAULT_TENANT_ID || 1);
const DEFAULT_SUCURSAL_ID = Number(process.env.NEXT_PUBLIC_DEFAULT_SUCURSAL_ID || 1);

export function AppProviders({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000,
            retry: 1,
            refetchOnWindowFocus: false,
          },
          mutations: {
            retry: 0,
          },
        },
      })
  );

  const [user, setUser] = useState<AuthResponseDto | null>(null);
  const [isLoadingAuth, setIsLoadingAuth] = useState<boolean>(true);
  const [tenantId, setTenantId] = useState<number>(DEFAULT_TENANT_ID);
  const [sucursalId, setSucursalId] = useState<number>(DEFAULT_SUCURSAL_ID);

  const [catalogos, setCatalogos] = useState<CatalogosMaestrosDto | null>(null);
  const [configSalon, setConfigSalon] = useState<ConfiguracionSalonDto | null>(
    null
  );

  const [sesionCajaActual, setSesionCajaActual] =
    useState<SesionCajaDto | null>(null);
  const [cargandoCaja, setCargandoCaja] = useState<boolean>(false);
  const [posPreload, setPosPreload] = useState<PosPreloadTicket | null>(null);

  const [consentGiven, setConsentGiven] = useState<
    'accepted' | 'rejected' | 'pending'
  >('pending');

  // Hidratación inicial desde localStorage
  useEffect(() => {
    if (typeof window !== 'undefined') {
      const savedSession = localStorage.getItem(STORAGE_KEYS.USER_SESSION);
      const savedConsent = localStorage.getItem(
        STORAGE_KEYS.COOKIE_CONSENT
      ) as 'accepted' | 'rejected' | null;

      if (savedConsent) {
        setConsentGiven(savedConsent);
      }

      if (savedSession) {
        try {
          const parsed: AuthResponseDto = JSON.parse(savedSession);
          setUser(parsed);
          setTenantId(parsed.tenantId || DEFAULT_TENANT_ID);
          setSucursalId(parsed.sucursalId || DEFAULT_SUCURSAL_ID);
        } catch {
          localStorage.removeItem(STORAGE_KEYS.USER_SESSION);
        }
      }
    }
    setIsLoadingAuth(false);
  }, []);

  // Listeners globales de crashes y promesas no capturadas hacia telemetría Zero-PII
  useEffect(() => {
    if (typeof window === 'undefined') return;

    const handleWindowError = (event: ErrorEvent) => {
      const errObj = event.error instanceof Error ? event.error : undefined;
      reportClientError({
        message:
          errObj?.message ||
          event.message ||
          'Excepción no controlada en ventana del navegador',
        stack: errObj?.stack,
        route: window.location.pathname,
        severity: 'error',
      });
    };

    const handleUnhandledRejection = (event: PromiseRejectionEvent) => {
      const reason: unknown = event.reason;

      // Ignorar rechazos HTTP de negocio esperados (< 500) ya procesados por Axios/UI
      if (
        reason &&
        typeof reason === 'object' &&
        'statusCode' in reason &&
        typeof (reason as { statusCode?: unknown }).statusCode === 'number' &&
        ((reason as { statusCode: number }).statusCode < 500)
      ) {
        return;
      }

      if (reason instanceof Error) {
        reportClientError({
          message: reason.message || 'Promesa rechazada no controlada',
          stack: reason.stack,
          route: window.location.pathname,
          severity: 'error',
        });
        return;
      }

      const fallbackMsg =
        reason &&
        typeof reason === 'object' &&
        'message' in reason &&
        typeof (reason as { message?: unknown }).message === 'string'
          ? (reason as { message: string }).message
          : 'Promesa rechazada sin instancia Error';

      reportClientError({
        message: fallbackMsg,
        route: window.location.pathname,
        severity: 'warning',
      });
    };

    window.addEventListener('error', handleWindowError);
    window.addEventListener('unhandledrejection', handleUnhandledRejection);

    return () => {
      window.removeEventListener('error', handleWindowError);
      window.removeEventListener(
        'unhandledrejection',
        handleUnhandledRejection
      );
    };
  }, []);

  const recargarCatalogosYConfig = useCallback(async () => {
    try {
      const [configRes, catRes] = await Promise.allSettled([
        configuracionSalonService.obtener(),
        catalogosService.obtenerTodos(),
      ]);

      if (configRes.status === 'fulfilled' && configRes.value.success) {
        setConfigSalon(configRes.value.data);
      }
      if (catRes.status === 'fulfilled' && catRes.value.success) {
        setCatalogos(catRes.value.data);
      }
    } catch {
      // Silencioso en modo público si algún subcatálogo requiere sesión
    }
  }, []);

  const refrescarEstadoCaja = useCallback(async () => {
    setCargandoCaja(true);
    try {
      const res = await cajaService.getEstadoActual();
      if (res.success && res.data && res.data.estadoAbierta) {
        setSesionCajaActual(res.data);
      } else {
        setSesionCajaActual(null);
      }
    } catch {
      setSesionCajaActual(null);
    } finally {
      setCargandoCaja(false);
    }
  }, []);

  const setSession = useCallback((authData: AuthResponseDto) => {
    setUser(authData);
    setTenantId(authData.tenantId || DEFAULT_TENANT_ID);
    setSucursalId(authData.sucursalId || DEFAULT_SUCURSAL_ID);
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEYS.ACCESS_TOKEN, authData.token);
      localStorage.setItem(STORAGE_KEYS.REFRESH_TOKEN, authData.refreshToken);
      localStorage.setItem(
        STORAGE_KEYS.USER_SESSION,
        JSON.stringify(authData)
      );
      localStorage.setItem(
        STORAGE_KEYS.TENANT_ID,
        String(authData.tenantId || DEFAULT_TENANT_ID)
      );
      localStorage.setItem(
        STORAGE_KEYS.SUCURSAL_ID,
        String(authData.sucursalId || DEFAULT_SUCURSAL_ID)
      );
      document.cookie = `mipeluqueria_token=${authData.token}; path=/; max-age=28800; SameSite=Lax`;
      document.cookie = `mipeluqueria_role=${authData.rol}; path=/; max-age=28800; SameSite=Lax`;
    }
  }, []);

  // Sincronización Multi-Sucursal Reactiva con invalidación determinista de TanStack Query
  const switchSucursal = useCallback(
    async (nuevaSucursalId: number) => {
      setSucursalId(nuevaSucursalId);
      if (typeof window !== 'undefined') {
        localStorage.setItem(
          STORAGE_KEYS.SUCURSAL_ID,
          String(nuevaSucursalId)
        );
      }
      await Promise.allSettled([
        recargarCatalogosYConfig(),
        refrescarEstadoCaja(),
        queryClient.invalidateQueries(),
      ]);
    },
    [queryClient, recargarCatalogosYConfig, refrescarEstadoCaja]
  );

  const logout = useCallback(() => {
    setUser(null);
    queryClient.clear();
    if (typeof window !== 'undefined') {
      localStorage.removeItem(STORAGE_KEYS.ACCESS_TOKEN);
      localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
      localStorage.removeItem(STORAGE_KEYS.USER_SESSION);
      document.cookie = 'mipeluqueria_token=; path=/; max-age=0';
      document.cookie = 'mipeluqueria_role=; path=/; max-age=0';
      window.location.href = '/login';
    }
  }, [queryClient]);

  useEffect(() => {
    recargarCatalogosYConfig();
    if (
      user &&
      ['Administrador', 'Recepcionista', 'Cajero', 'SuperAdminSaaS'].includes(
        user.rol
      )
    ) {
      refrescarEstadoCaja();
    }
  }, [user, recargarCatalogosYConfig, refrescarEstadoCaja]);

  const enviarTurnoAlPos = useCallback((turno: TurnoDto) => {
    const renglones: CrearDetalleVentaItemDto[] = (turno.servicios || []).map(
      (s) => ({
        tipoItem: TipoItemVentaEnum.Servicio,
        itemId: s.servicioId,
        nombreItem: s.servicioNombre,
        cantidad: 1,
        precioUnitario: s.precioHistorico,
        peluqueroId: turno.peluqueroId,
      })
    );

    setPosPreload({
      turnoId: turno.id,
      clienteId: turno.clienteId,
      clienteNombre: turno.clienteNombreCompleto,
      renglones,
    });
  }, []);

  const limpiarPreloadPos = useCallback(() => {
    setPosPreload(null);
  }, []);

  const acceptCookies = useCallback(() => {
    setConsentGiven('accepted');
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEYS.COOKIE_CONSENT, 'accepted');
    }
  }, []);

  const rejectCookies = useCallback(() => {
    setConsentGiven('rejected');
    if (typeof window !== 'undefined') {
      localStorage.setItem(STORAGE_KEYS.COOKIE_CONSENT, 'rejected');
    }
  }, []);

  return (
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <CookieConsentContext.Provider
          value={{ consentGiven, acceptCookies, rejectCookies }}
        >
          <AuthContext.Provider
            value={{
              user,
              isAuthenticated: !!user,
              isLoadingAuth,
              tenantId,
              sucursalId,
              setSession,
              switchSucursal,
              logout,
            }}
          >
            <CatalogosContext.Provider
              value={{ catalogos, configSalon, recargarCatalogosYConfig }}
            >
              <CajaPosContext.Provider
                value={{
                  sesionCajaActual,
                  cajaAbierta:
                    !!sesionCajaActual && sesionCajaActual.estadoAbierta,
                  cargandoCaja,
                  posPreload,
                  refrescarEstadoCaja,
                  enviarTurnoAlPos,
                  limpiarPreloadPos,
                }}
              >
                {children}
                <ThemedToaster />
              </CajaPosContext.Provider>
            </CatalogosContext.Provider>
          </AuthContext.Provider>
        </CookieConsentContext.Provider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}

function ThemedToaster() {
  const { theme } = useTheme();
  return (
    <Toaster
      theme={theme}
      position="top-right"
      richColors
      closeButton
      expand
    />
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth debe usarse dentro de AppProviders');
  return ctx;
}

export function useCatalogos() {
  const ctx = useContext(CatalogosContext);
  if (!ctx) throw new Error('useCatalogos debe usarse dentro de AppProviders');
  return ctx;
}

export function useCajaPos() {
  const ctx = useContext(CajaPosContext);
  if (!ctx) throw new Error('useCajaPos debe usarse dentro de AppProviders');
  return ctx;
}

export function useCookieConsent() {
  const ctx = useContext(CookieConsentContext);
  if (!ctx)
    throw new Error('useCookieConsent debe usarse dentro de AppProviders');
  return ctx;
}
