// filepath: src/components/ui/AsyncStateGuard.tsx
'use client';

import React from 'react';
import { Inbox } from 'lucide-react';
import { ErrorFallbackCard } from '@/components/ui/ErrorFallbackCard';

export interface AsyncStateGuardProps {
  isLoading: boolean;
  isError?: boolean;
  errorMessage?: string;
  correlationId?: string;
  isEmpty?: boolean;
  loadingText?: string;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyAction?: React.ReactNode;
  onRetry?: () => void;
  children: React.ReactNode;
  className?: string;
}

/**
 * Contenedor declarativo y agnóstico de dominio que orquesta los estados de carga (`isLoading`),
 * fallo recuperable (`isError`), colección vacía (`isEmpty`) y contenido exitoso (`children`).
 *
 * @param {AsyncStateGuardProps} props Estado asíncrono, callbacks de reintento y contenido hijo.
 * @example
 * ```tsx
 * <AsyncStateGuard
 *   isLoading={isPending}
 *   isError={isError}
 *   isEmpty={items.length === 0}
 *   onRetry={() => refetch()}
 * >
 *   <ListaItems data={items} />
 * </AsyncStateGuard>
 * ```
 */
export function AsyncStateGuard({
  isLoading,
  isError = false,
  errorMessage,
  correlationId,
  isEmpty = false,
  loadingText = 'Cargando información...',
  emptyTitle = 'Sin registros para mostrar',
  emptyDescription = 'Aún no existen elementos que coincidan con la consulta actual.',
  emptyAction,
  onRetry,
  children,
  className = '',
}: AsyncStateGuardProps) {
  if (isLoading) {
    return (
      <div
        role="status"
        aria-live="polite"
        className={`flex flex-col items-center justify-center rounded-lg border border-slate-200 bg-white p-10 text-center ${className}`}
      >
        <span className="h-7 w-7 rounded-full border-2 border-slate-900 border-t-transparent animate-spin mb-3" />
        <p className="text-xs font-medium text-slate-600">{loadingText}</p>
      </div>
    );
  }

  if (isError) {
    return (
      <div className={className}>
        <ErrorFallbackCard
          compact
          title="No se pudo completar la consulta"
          description="Se produjo un error al obtener la información desde el servidor."
          errorMessage={errorMessage}
          correlationId={correlationId}
          onRetry={onRetry}
        />
      </div>
    );
  }

  if (isEmpty) {
    return (
      <div
        className={`flex flex-col items-center justify-center rounded-lg border border-dashed border-slate-300 bg-white p-10 text-center ${className}`}
      >
        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-100 text-slate-500 mb-3">
          <Inbox className="h-5 w-5" aria-hidden="true" />
        </div>
        <h3 className="text-sm font-bold text-slate-800">{emptyTitle}</h3>
        <p className="text-xs text-slate-500 mt-1 max-w-md">
          {emptyDescription}
        </p>
        {emptyAction && <div className="mt-4">{emptyAction}</div>}
      </div>
    );
  }

  return <div className={className}>{children}</div>;
}
