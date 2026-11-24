// filepath: src/components/ui/DataTable.tsx
'use client';

import React from 'react';

export interface DataTableColumn<T> {
  key: string;
  header: React.ReactNode;
  render: (row: T, rowIndex: number) => React.ReactNode;
  headerClassName?: string;
  cellClassName?: string;
}

export interface DataTableProps<T> {
  columns: readonly DataTableColumn<T>[];
  data: readonly T[];
  keyExtractor: (row: T, index: number) => string | number;
  isLoading?: boolean;
  loadingMessage?: string;
  emptyMessage?: string;
  onRowClick?: (row: T) => void;
  className?: string;
}

/**
 * Tabla de datos genérica `<T>` y 100% agnóstica de dominio con soporte de columnas tipadas,
 * estado de carga integrado, estado vacío seguro y filas interactivas opcionales.
 *
 * @template T Tipo del modelo de fila a renderizar.
 * @param {DataTableProps<T>} props Definición de columnas, arreglo de filas y extractor de clave única.
 * @example
 * ```tsx
 * <DataTable
 *   data={rows}
 *   keyExtractor={(r) => r.id}
 *   columns={[
 *     { key: 'nombre', header: 'Nombre', render: (r) => r.nombre },
 *   ]}
 * />
 * ```
 */
export function DataTable<T>({
  columns,
  data,
  keyExtractor,
  isLoading = false,
  loadingMessage = 'Cargando registros...',
  emptyMessage = 'No se encontraron registros.',
  onRowClick,
  className = '',
}: DataTableProps<T>) {
  const safeRows = Array.isArray(data) ? data : [];

  return (
    <div
      className={`bg-white border border-slate-200 rounded-lg overflow-hidden ${className}`}
    >
      <div className="overflow-x-auto">
        <table className="w-full text-left border-collapse text-xs">
          <thead>
            <tr className="bg-slate-50 border-b border-slate-200 text-[11px] uppercase text-slate-500">
              {columns.map((col) => (
                <th
                  key={col.key}
                  className={`py-3 px-4 font-semibold ${
                    col.headerClassName || ''
                  }`}
                >
                  {col.header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-200">
            {isLoading ? (
              <tr>
                <td
                  colSpan={Math.max(1, columns.length)}
                  className="py-8 px-4 text-center text-slate-400"
                >
                  {loadingMessage}
                </td>
              </tr>
            ) : safeRows.length === 0 ? (
              <tr>
                <td
                  colSpan={Math.max(1, columns.length)}
                  className="py-8 px-4 text-center text-slate-400"
                >
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              safeRows.map((row, rowIndex) => (
                <tr
                  key={keyExtractor(row, rowIndex)}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  className={`hover:bg-slate-50 transition-colors ${
                    onRowClick ? 'cursor-pointer' : ''
                  }`}
                >
                  {columns.map((col) => (
                    <td
                      key={col.key}
                      className={`py-3 px-4 ${col.cellClassName || ''}`}
                    >
                      {col.render(row, rowIndex)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
