// filepath: src/components/ui/ErrorFallbackCard.tsx
'use client';

import React from 'react';
import { AlertTriangle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/DesignSystem';

export interface ErrorFallbackCardProps {
  title?: string;
  description?: string;
  errorMessage?: string;
  correlationId?: string;
  onRetry?: () => void;
  retryLabel?: string;
  secondaryAction?: React.ReactNode;
  compact?: boolean;
  className?: string;
}

/**
 * Tarjeta reutilizable y 100% agnóstica de dominio para mostrar estados de error
 * recuperables (tanto en Error Boundaries como en fallos de consultas asíncronas).
 *
 * @param {ErrorFallbackCardProps} props Propiedades de configuración visual y recuperación.
 * @example
 * ```tsx
 * <ErrorFallbackCard
 *   title="No pudimos cargar los datos"
 *   description="Verificá tu conexión e intentá nuevamente."
 *   correlationId="c9b1-42f0"
 *   onRetry={() => refetch()}
 * />
 * ```
 */
export function ErrorFallbackCard({
  title = 'Ocurrió un inconveniente inesperado',
  description = 'El incidente fue registrado automáticamente para diagnóstico técnico. Podés reintentar la operación de forma segura.',
  errorMessage,
  correlationId,
  onRetry,
  retryLabel = 'Reintentar ahora',
  secondaryAction,
  compact = false,
  className = '',
}: ErrorFallbackCardProps) {
  return (
    <div
      role="alert"
      className={`rounded-lg border border-red-200 bg-white shadow-sm ${
        compact ? 'p-4' : 'p-6 sm:p-8'
      } ${className}`}
    >
      <div className="flex items-start gap-4">
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-red-50 border border-red-200 text-red-700">
          <AlertTriangle className="h-5 w-5" aria-hidden="true" />
        </div>

        <div className="flex-1 min-w-0 space-y-2">
          <h2
            className={`font-bold text-slate-900 ${
              compact ? 'text-sm' : 'text-base sm:text-lg'
            }`}
          >
            {title}
          </h2>
          <p className="text-xs sm:text-sm text-slate-600 leading-relaxed">
            {description}
          </p>

          {errorMessage && (
            <p className="rounded-md bg-red-50/70 border border-red-100 px-3 py-2 text-xs font-mono text-red-900 break-words">
              {errorMessage}
            </p>
          )}

          {correlationId && (
            <p className="text-[11px] font-mono text-slate-500">
              Código de trazabilidad (Correlation ID):{' '}
              <span className="font-semibold text-slate-700">
                {correlationId}
              </span>
            </p>
          )}

          {(onRetry || secondaryAction) && (
            <div className="flex flex-wrap items-center gap-2.5 pt-2">
              {onRetry && (
                <Button variant="primary" size="sm" onClick={onRetry}>
                  <RefreshCw className="h-3.5 w-3.5 mr-1.5" aria-hidden="true" />
                  {retryLabel}
                </Button>
              )}
              {secondaryAction}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
