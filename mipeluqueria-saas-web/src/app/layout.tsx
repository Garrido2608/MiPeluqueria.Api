import React from 'react';
import type { Metadata } from 'next';
import './globals.css';
import { AppProviders } from '@/context/AppProviders';
import { CookieConsentBanner } from '@/components/ui/DesignSystem';

export const metadata: Metadata = {
  title: 'MiPeluqueria SaaS — Gestión Integral de Salones, Agenda, POS y Comisiones',
  description:
    'ERP Vertical Multi-Tenant, Punto de Venta Multipago, Control de Gabinete y Portal de Reservas Online 24/7.',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="es">
      <body className="min-h-screen bg-slate-50 text-slate-900 antialiased">
        <AppProviders>
          {children}
          <CookieConsentBanner />
        </AppProviders>
      </body>
    </html>
  );
}
