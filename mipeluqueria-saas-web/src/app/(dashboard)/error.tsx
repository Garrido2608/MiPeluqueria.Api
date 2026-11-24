// filepath: src/app/(dashboard)/error.tsx
'use client';

import React, { useEffect } from 'react';
import Link from 'next/link';
import { ErrorFallbackCard, Button } from '@/components/ui';
import { getLastCorrelationId, reportClientError } from '@/lib/errorLogger';

export default function DashboardErrorBoundary({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    reportClientError({
      message:
        error.message || 'Error inesperado en panel operativo (dashboard)',
      stack: error.stack,
      componentStack: error.digest ? `NextDigest: ${error.digest}` : undefined,
      severity: 'error',
    });
  }, [error]);

  return (
    <div className="p-6 sm:p-10 max-w-2xl mx-auto">
      <ErrorFallbackCard
        title="Ocurrió un problema en este módulo del panel"
        description="Tu sesión y el resto de los módulos del salón siguen activos. Reintentá cargar el módulo o navegá a otra sección desde el menú lateral."
        errorMessage={error.message}
        correlationId={getLastCorrelationId()}
        onRetry={reset}
        secondaryAction={
          <Link href="/recepcion/calendario">
            <Button variant="secondary" size="sm">
              Volver a Agenda
            </Button>
          </Link>
        }
      />
    </div>
  );
}
