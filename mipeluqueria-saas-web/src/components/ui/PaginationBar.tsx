// filepath: src/components/ui/PaginationBar.tsx
'use client';

import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/DesignSystem';

export interface PaginationBarProps {
  pageNumber: number;
  totalPages: number;
  totalCount?: number;
  itemLabel?: string;
  isLoading?: boolean;
  onPageChange: (nextPage: number) => void;
  className?: string;
}

/**
 * Barra de paginación reutilizable y agnóstica de dominio con controles accesibles
 * de navegación anterior/siguiente y resumen de conteo total.
 *
 * @param {PaginationBarProps} props Metadatos de página actual, total de páginas y callback de cambio.
 * @example
 * ```tsx
 * <PaginationBar
 *   pageNumber={page}
 *   totalPages={totalPages}
 *   totalCount={totalRecords}
 *   onPageChange={(next) => setPage(next)}
 * />
 * ```
 */
export function PaginationBar({
  pageNumber,
  totalPages,
  totalCount,
  itemLabel = 'registros',
  isLoading = false,
  onPageChange,
  className = '',
}: PaginationBarProps) {
  const safePage = Math.max(1, pageNumber);
  const safeTotalPages = Math.max(1, totalPages);
  const canGoPrev = safePage > 1 && !isLoading;
  const canGoNext = safePage < safeTotalPages && !isLoading;

  return (
    <div
      className={`px-4 py-3 border border-slate-200 rounded-lg bg-slate-50 flex flex-wrap items-center justify-between gap-3 text-xs text-slate-600 ${className}`}
    >
      <Button
        type="button"
        size="sm"
        variant="secondary"
        disabled={!canGoPrev}
        onClick={() => onPageChange(Math.max(1, safePage - 1))}
      >
        <ChevronLeft className="h-3.5 w-3.5 mr-1" aria-hidden="true" />
        Anterior
      </Button>

      <div className="text-center">
        <span>
          Página <strong className="text-slate-900">{safePage}</strong> de{' '}
          <strong className="text-slate-900">{safeTotalPages}</strong>
        </span>
        {typeof totalCount === 'number' && (
          <span className="ml-1.5 text-slate-500">
            ({totalCount} {itemLabel})
          </span>
        )}
      </div>

      <Button
        type="button"
        size="sm"
        variant="secondary"
        disabled={!canGoNext}
        onClick={() => onPageChange(Math.min(safeTotalPages, safePage + 1))}
      >
        Siguiente
        <ChevronRight className="h-3.5 w-3.5 ml-1" aria-hidden="true" />
      </Button>
    </div>
  );
}
