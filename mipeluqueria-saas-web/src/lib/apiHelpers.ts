// filepath: src/lib/apiHelpers.ts
import { PagedResult } from '@/types/api.types';

export interface PaginationMeta {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/**
 * Extrae de forma universal y null-safe los ítems de una respuesta paginada o plana.
 * Soporta simultáneamente:
 * - `PagedResult<T>` de .NET 8 (`{ data: T[], totalRecords: number }`)
 * - `PagedResult<T>` normalizado en Frontend (`{ items: T[], totalCount: number }`)
 * - `ApiResponse<PagedResult<T> | T[]>` envolvente
 * - Arreglos planos `T[]`, `null` o `undefined`
 *
 * Garantiza retornar SIEMPRE un arreglo válido `T[]` (jamás `undefined` ni `null`).
 */
export function extractPagedItems<T>(
  payload: PagedResult<T> | readonly T[] | T[] | null | undefined
): T[];
export function extractPagedItems<T = unknown>(payload: unknown): T[];
export function extractPagedItems<T>(payload: unknown): T[] {
  if (Array.isArray(payload)) {
    return payload as T[];
  }

  if (!isPlainObject(payload)) {
    return [];
  }

  if (Array.isArray(payload.items)) {
    return payload.items as T[];
  }

  if (Array.isArray(payload.data)) {
    return payload.data as T[];
  }

  if (isPlainObject(payload.data)) {
    const nested = payload.data;
    if (Array.isArray(nested.items)) {
      return nested.items as T[];
    }
    if (Array.isArray(nested.data)) {
      return nested.data as T[];
    }
  }

  return [];
}

/**
 * Extrae metadatos seguros de paginación soportando tanto `totalRecords` (.NET 8)
 * como `totalCount` (Frontend) o arreglos planos sin paginar.
 */
export function extractPaginationMeta(
  payload: unknown,
  defaults?: { pageNumber?: number; pageSize?: number }
): PaginationMeta {
  const fallbackPage = Math.max(1, defaults?.pageNumber ?? 1);
  const items = extractPagedItems(payload);
  const fallbackSize = Math.max(
    1,
    defaults?.pageSize ?? (items.length > 0 ? items.length : 15)
  );

  if (Array.isArray(payload) || !isPlainObject(payload)) {
    const totalCount = items.length;
    const totalPages = Math.max(1, Math.ceil(totalCount / fallbackSize));
    return {
      totalCount,
      pageNumber: fallbackPage,
      pageSize: fallbackSize,
      totalPages,
      hasPreviousPage: fallbackPage > 1,
      hasNextPage: fallbackPage < totalPages,
    };
  }

  const source =
    'success' in payload && isPlainObject(payload.data)
      ? payload.data
      : payload;

  const rawTotalCount =
    typeof source.totalCount === 'number'
      ? source.totalCount
      : typeof source.totalRecords === 'number'
      ? source.totalRecords
      : items.length;

  const totalCount = Math.max(items.length, rawTotalCount);

  const pageNumber =
    typeof source.pageNumber === 'number' && source.pageNumber >= 1
      ? source.pageNumber
      : fallbackPage;

  const pageSize =
    typeof source.pageSize === 'number' && source.pageSize >= 1
      ? source.pageSize
      : fallbackSize;

  const computedPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const totalPages =
    typeof source.totalPages === 'number' && source.totalPages >= 1
      ? source.totalPages
      : computedPages;

  const hasPreviousPage =
    typeof source.hasPreviousPage === 'boolean'
      ? source.hasPreviousPage
      : pageNumber > 1;

  const hasNextPage =
    typeof source.hasNextPage === 'boolean'
      ? source.hasNextPage
      : pageNumber < totalPages;

  return {
    totalCount,
    pageNumber,
    pageSize,
    totalPages,
    hasPreviousPage,
    hasNextPage,
  };
}

/**
 * Construye un `PagedResult<T>` canónico y null-safe a partir de cualquier forma de payload
 * (`{ data, totalRecords }`, `{ items, totalCount }`, `T[]` o `null`), aplicando opcionalmente
 * un normalizador por elemento.
 */
export function toNormalizedPagedResult<T>(
  payload: unknown,
  mapItem?: (item: T) => T,
  defaults?: { pageNumber?: number; pageSize?: number }
): PagedResult<T> {
  const rawItems = extractPagedItems<T>(payload);
  const items = mapItem ? rawItems.map(mapItem) : rawItems;
  const meta = extractPaginationMeta(payload, defaults);

  return {
    items,
    totalCount: meta.totalCount,
    pageNumber: meta.pageNumber,
    pageSize: meta.pageSize,
    totalPages: meta.totalPages,
  };
}

/**
 * Obtiene un mensaje legible desde cualquier objeto de error normalizado o nativo.
 */
export function extractErrorMessage(
  error: unknown,
  fallback = 'No se pudo obtener la información solicitada.'
): string {
  if (!error) return fallback;
  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }
  if (
    isPlainObject(error) &&
    typeof error.message === 'string' &&
    error.message.trim()
  ) {
    return error.message;
  }
  return fallback;
}

/**
 * Extrae el `correlationId` propagado por `axios.ts` desde un error HTTP si está presente.
 */
export function extractErrorCorrelationId(error: unknown): string | undefined {
  if (
    isPlainObject(error) &&
    typeof error.correlationId === 'string' &&
    error.correlationId.trim()
  ) {
    return error.correlationId;
  }
  return undefined;
}
