'use client';

import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import type { ThemeMode } from '@/lib/semanticColors';

/** Clave persistente en localStorage para el tema dual Studio Clean / Obsidian & Indigo. */
export const THEME_STORAGE_KEY = 'mipeluqueria_theme';

export interface ThemeContextValue {
  /** Tema activo ('light' por defecto = Studio Clean, 'dark' = Obsidian & Indigo). */
  theme: ThemeMode;
  /** Verdadero cuando el cliente ya hidrató el tema para evitar desajustes SSR. */
  isMounted: boolean;
  /** Establece explícitamente el tema activo y lo persiste en localStorage. */
  setTheme: (nextTheme: ThemeMode) => void;
  /** Alterna entre Modo Claro y Modo Oscuro. */
  toggleTheme: () => void;
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

function applyThemeToDocument(theme: ThemeMode): void {
  if (typeof document === 'undefined') return;
  const root = document.documentElement;
  if (theme === 'dark') {
    root.classList.add('dark');
  } else {
    root.classList.remove('dark');
  }
  root.style.colorScheme = theme;
}

function readStoredTheme(): ThemeMode {
  if (typeof window === 'undefined') return 'light';
  try {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
    return stored === 'dark' ? 'dark' : 'light';
  } catch {
    return 'light';
  }
}

export interface ThemeProviderProps {
  children: React.ReactNode;
}

/**
 * Proveedor reactivo de tema dual (Claro por defecto + Oscuro opcional) con
 * persistencia en localStorage (`mipeluqueria_theme`) y sincronización entre pestañas.
 *
 * @param props Propiedades del proveedor.
 * @example
 * <ThemeProvider>
 *   <App />
 * </ThemeProvider>
 */
export function ThemeProvider({ children }: ThemeProviderProps) {
  const [theme, setThemeState] = useState<ThemeMode>('light');
  const [isMounted, setIsMounted] = useState<boolean>(false);

  useEffect(() => {
    const initial = readStoredTheme();
    setThemeState(initial);
    applyThemeToDocument(initial);
    setIsMounted(true);

    const handleStorage = (event: StorageEvent) => {
      if (event.key === THEME_STORAGE_KEY) {
        const next: ThemeMode = event.newValue === 'dark' ? 'dark' : 'light';
        setThemeState(next);
        applyThemeToDocument(next);
      }
    };

    window.addEventListener('storage', handleStorage);
    return () => {
      window.removeEventListener('storage', handleStorage);
    };
  }, []);

  const setTheme = useCallback((nextTheme: ThemeMode) => {
    setThemeState(nextTheme);
    applyThemeToDocument(nextTheme);
    if (typeof window !== 'undefined') {
      try {
        window.localStorage.setItem(THEME_STORAGE_KEY, nextTheme);
      } catch {
        // Ignorar errores de almacenamiento restringido en navegación privada
      }
    }
  }, []);

  const toggleTheme = useCallback(() => {
    setThemeState((prev) => {
      const next: ThemeMode = prev === 'dark' ? 'light' : 'dark';
      applyThemeToDocument(next);
      if (typeof window !== 'undefined') {
        try {
          window.localStorage.setItem(THEME_STORAGE_KEY, next);
        } catch {
          // Ignorar errores de almacenamiento restringido
        }
      }
      return next;
    });
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({ theme, isMounted, setTheme, toggleTheme }),
    [theme, isMounted, setTheme, toggleTheme]
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

/**
 * Hook de acceso al tema activo (Claro / Oscuro).
 *
 * @returns Estado y acciones de control del tema.
 */
export function useTheme(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) {
    throw new Error('useTheme debe usarse dentro de ThemeProvider');
  }
  return ctx;
}
