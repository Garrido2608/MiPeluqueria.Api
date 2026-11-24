// filepath: src/app/global-error.tsx
'use client';

import React, { useEffect } from 'react';
import { ErrorFallbackCard } from '@/components/ui/ErrorFallbackCard';
import { getLastCorrelationId, reportClientError } from '@/lib/errorLogger';

export default function GlobalErrorBoundary({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    reportClientError({
      message: error.message || 'Error crítico en RootLayout (global-error)',
      stack: error.stack,
      componentStack: error.digest ? `NextDigest: ${error.digest}` : undefined,
      severity: 'error',
    });
  }, [error]);

  return (
    <html lang="es">
      <body className="min-h-screen bg-slate-100 text-slate-900 flex items-center justify-center p-6 antialiased">
        <div className="w-full max-w-xl">
          <ErrorFallbackCard
            title="Error crítico de inicialización de la aplicación"
            description="El sistema detectó y reportó un fallo en el contenedor principal. Presioná el botón para recargar la aplicación."
            errorMessage={error.message}
            correlationId={getLastCorrelationId()}
            onRetry={reset}
            retryLabel="Recargar aplicación"
          />
        </div>
      </body>
    </html>
  );
}
