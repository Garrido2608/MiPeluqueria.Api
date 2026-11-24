// filepath: src/app/error.tsx
'use client';

import React, { useEffect } from 'react';
import Link from 'next/link';
import { ErrorFallbackCard, Button } from '@/components/ui';
import { getLastCorrelationId, reportClientError } from '@/lib/errorLogger';

export default function RootErrorBoundary({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    reportClientError({
      message: error.message || 'Error inesperado en segmento raíz App Router',
      stack: error.stack,
      componentStack: error.digest ? `NextDigest: ${error.digest}` : undefined,
      severity: 'error',
    });
  }, [error]);

  return (
    <main className="min-h-[70vh] flex items-center justify-center p-6 bg-slate-50">
      <div className="w-full max-w-xl">
        <ErrorFallbackCard
          title="Se interrumpió la visualización de esta pantalla"
          description="Registramos el incidente de forma automática. Podés volver a intentar cargar la vista o regresar al inicio."
          errorMessage={error.message}
          correlationId={getLastCorrelationId()}
          onRetry={reset}
          secondaryAction={
            <Link href="/">
              <Button variant="secondary" size="sm">
                Ir al inicio
              </Button>
            </Link>
          }
        />
      </div>
    </main>
  );
}
