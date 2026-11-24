import { QueryClient, useMutation } from '@tanstack/react-query';
import { FieldValues, Path, UseFormSetError } from 'react-hook-form';
import { toast } from 'sonner';
import { ApiErrorPayload } from '@/lib/axios';
import { ApiResponse } from '@/types/common.types';

function toCamelCaseKey(key: string): string {
  if (!key) return key;
  return key.charAt(0).toLowerCase() + key.slice(1);
}

/**
 * Procesa una respuesta exitosa de la API (`ApiResponse<T>`):
 * 1. Muestra `toast.success(res.message)`.
 * 2. Si `res.warnings` contiene alertas operativas no bloqueantes (ej. solapamiento de bloqueo
 *    con turnos activos o insumo de gabinete agotado), dispara un `toast.warning(w, { duration: 8000 })`
 *    persistente por cada advertencia.
 */
export function notifyApiSuccess<T>(
  res: ApiResponse<T>,
  fallbackMessage?: string
): void {
  const mainMessage = res.message || fallbackMessage;
  if (mainMessage) {
    toast.success(mainMessage);
  }
  if (Array.isArray(res.warnings) && res.warnings.length > 0) {
    res.warnings.forEach((warningMsg) => {
      if (warningMsg) {
        toast.warning(warningMsg, { duration: 8000 });
      }
    });
  }
}

/**
 * Inyecta el diccionario `fieldErrors` devuelto por `FluentValidation` (HTTP 400)
 * directamente en los campos de `react-hook-form`.
 */
export function injectServerFieldErrors<TFieldValues extends FieldValues>(
  fieldErrors: Record<string, string[]> | undefined,
  setError?: UseFormSetError<TFieldValues>
): void {
  if (!fieldErrors || !setError) return;
  Object.entries(fieldErrors).forEach(([rawField, messages]) => {
    if (Array.isArray(messages) && messages.length > 0) {
      const camelField = toCamelCaseKey(rawField) as Path<TFieldValues>;
      setError(camelField, {
        type: 'server',
        message: messages[0],
      });
    }
  });
}

/**
 * Procesa de forma centralizada los errores HTTP (400, 403, 409, 429, 500)
 * según la Sección 3 de `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`.
 */
export function notifyApiError<TFieldValues extends FieldValues = FieldValues>(
  err: unknown,
  options?: {
    queryClient?: QueryClient;
    invalidateKeysOnConflict?: ReadonlyArray<readonly unknown[]>;
    setError?: UseFormSetError<TFieldValues>;
    fallbackMessage?: string;
  }
): string {
  const apiErr = err as Partial<ApiErrorPayload> | undefined;
  const statusCode = apiErr?.statusCode ?? 500;
  const message =
    apiErr?.message ||
    options?.fallbackMessage ||
    'Ocurrió un error al procesar la solicitud.';

  if (Array.isArray(apiErr?.warnings) && apiErr.warnings.length > 0) {
    apiErr.warnings.forEach((w) => {
      toast.warning(w, { duration: 8000 });
    });
  }

  if (statusCode === 400 && apiErr?.fieldErrors) {
    injectServerFieldErrors(apiErr.fieldErrors, options?.setError);
    toast.error(message);
    return message;
  }

  if (statusCode === 409) {
    toast.error(
      message ||
        'Conflicto de concurrencia: otro usuario modificó este registro hace instantes. Refrescando datos...',
      { duration: 6000 }
    );
    if (options?.queryClient) {
      if (
        options.invalidateKeysOnConflict &&
        options.invalidateKeysOnConflict.length > 0
      ) {
        options.invalidateKeysOnConflict.forEach((key) => {
          options.queryClient?.invalidateQueries({ queryKey: key });
        });
      } else {
        options.queryClient.invalidateQueries();
      }
    }
    return message;
  }

  if (statusCode === 403) {
    const forbiddenMsg =
      'No tenés permisos suficientes para ejecutar esta acción.';
    toast.error(forbiddenMsg);
    return forbiddenMsg;
  }

  if (statusCode === 429) {
    const rateLimitMsg =
      'Demasiados intentos seguidos. Por favor aguardá 1 minuto.';
    toast.error(rateLimitMsg);
    return rateLimitMsg;
  }

  toast.error(message);
  return message;
}

export interface UseApiMutationConfig<
  TData,
  TVariables,
  TFieldValues extends FieldValues = FieldValues,
> {
  mutationFn: (variables: TVariables) => Promise<ApiResponse<TData>>;
  queryClient: QueryClient;
  invalidateKeys?: ReadonlyArray<readonly unknown[]>;
  setError?: UseFormSetError<TFieldValues>;
  successFallbackMessage?: string;
  errorFallbackMessage?: string;
  onSuccessCallback?: (
    res: ApiResponse<TData>,
    variables: TVariables
  ) => void | Promise<void>;
  onErrorCallback?: (
    error: ApiErrorPayload,
    variables: TVariables
  ) => void | Promise<void>;
}

/**
 * Wrapper de `useMutation` (TanStack Query v5) que aplica automáticamente:
 * - Invalidación cruzada de `QueryKeys` al tener éxito.
 * - Disparo de `toast.success` y `toast.warning` (`duration: 8000`) por cada alerta en `res.warnings`.
 * - Mapeo de `fieldErrors` a `react-hook-form` en `400 Bad Request`.
 * - Invalidación automática de caché y alerta en `409 Conflict` (`xmin` / solapamiento).
 */
export function useApiMutation<
  TData,
  TVariables,
  TFieldValues extends FieldValues = FieldValues,
>(config: UseApiMutationConfig<TData, TVariables, TFieldValues>) {
  const {
    mutationFn,
    queryClient,
    invalidateKeys = [],
    setError,
    successFallbackMessage,
    errorFallbackMessage,
    onSuccessCallback,
    onErrorCallback,
  } = config;

  return useMutation<ApiResponse<TData>, ApiErrorPayload, TVariables>({
    mutationFn,
    onSuccess: async (res, variables) => {
      if (res.success) {
        notifyApiSuccess(res, successFallbackMessage);
        await Promise.all(
          invalidateKeys.map((key) =>
            queryClient.invalidateQueries({ queryKey: key })
          )
        );
      } else {
        toast.error(
          res.message || errorFallbackMessage || 'No se pudo completar la operación.'
        );
      }
      if (onSuccessCallback) {
        await onSuccessCallback(res, variables);
      }
    },
    onError: async (error, variables) => {
      notifyApiError(error, {
        queryClient,
        invalidateKeysOnConflict: invalidateKeys,
        setError,
        fallbackMessage: errorFallbackMessage,
      });
      if (onErrorCallback) {
        await onErrorCallback(error, variables);
      }
    },
  });
}
