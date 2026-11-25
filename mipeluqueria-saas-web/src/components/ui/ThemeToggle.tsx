'use client';

import React from 'react';
import { Moon, Sun } from 'lucide-react';
import { useTheme } from '@/context/ThemeProvider';

export interface ThemeToggleProps {
  /** Estilo visual: 'icon' (botón cuadrado compacto) o 'pill' (con etiqueta de texto). */
  variant?: 'icon' | 'pill';
  /** Clases Tailwind adicionales. */
  className?: string;
}

/**
 * Conmutador compacto Claro (Studio Clean) / Oscuro (Obsidian & Indigo) conectado a `ThemeProvider`.
 * Espera a la hidratación del cliente (`isMounted`) para evitar advertencias SSR.
 *
 * @param props Propiedades del conmutador.
 * @example
 * <ThemeToggle variant="pill" />
 */
export function ThemeToggle({
  variant = 'icon',
  className = '',
}: ThemeToggleProps) {
  const { theme, isMounted, toggleTheme } = useTheme();

  const isDark = isMounted && theme === 'dark';
  const label = isDark ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro';

  if (variant === 'pill') {
    return (
      <button
        type="button"
        onClick={toggleTheme}
        aria-label={label}
        title={label}
        className={`inline-flex h-9 items-center gap-2 rounded-lg border border-border-strong bg-surface px-3 text-xs font-medium text-ink-primary hover:bg-elevated transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-accent ${className}`}
      >
        {isDark ? (
          <>
            <Sun className="h-4 w-4 text-amber-400 shrink-0" aria-hidden="true" />
            <span>Claro</span>
          </>
        ) : (
          <>
            <Moon className="h-4 w-4 text-indigo-600 shrink-0" aria-hidden="true" />
            <span>Oscuro</span>
          </>
        )}
      </button>
    );
  }

  return (
    <button
      type="button"
      onClick={toggleTheme}
      aria-label={label}
      title={label}
      className={`inline-flex h-9 w-9 items-center justify-center rounded-lg border border-border-strong bg-surface text-ink-primary hover:bg-elevated transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-accent ${className}`}
    >
      {isDark ? (
        <Sun className="h-4 w-4 text-amber-400" aria-hidden="true" />
      ) : (
        <Moon className="h-4 w-4 text-indigo-600" aria-hidden="true" />
      )}
    </button>
  );
}
