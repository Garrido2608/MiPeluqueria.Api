import React from 'react';
import type { Metadata } from 'next';
import { Plus_Jakarta_Sans, JetBrains_Mono } from 'next/font/google';
import './globals.css';
import { AppProviders } from '@/context/AppProviders';
import { CookieConsentBanner } from '@/components/ui/DesignSystem';

const plusJakartaSans = Plus_Jakarta_Sans({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-sans',
  display: 'swap',
});

const jetbrainsMono = JetBrains_Mono({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-mono',
  display: 'swap',
});

/**
 * Script inline minimalista ejecutado antes del primer paint para evitar
 * parpadeo (FOUC) al restaurar el tema guardado en localStorage.
 * Por defecto aplica Studio Clean ('light').
 */
const THEME_INIT_SCRIPT = `(function(){try{var t=localStorage.getItem('mipeluqueria_theme');var d=document.documentElement;if(t==='dark'){d.classList.add('dark');d.style.colorScheme='dark';}else{d.classList.remove('dark');d.style.colorScheme='light';}}catch(e){}})();`;

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
    <html
      lang="es"
      className={`${plusJakartaSans.variable} ${jetbrainsMono.variable}`}
      suppressHydrationWarning
    >
      <head>
        <script dangerouslySetInnerHTML={{ __html: THEME_INIT_SCRIPT }} />
      </head>
      <body className="min-h-screen bg-canvas text-ink-primary font-sans antialiased transition-colors duration-150">
        <AppProviders>
          {children}
          <CookieConsentBanner />
        </AppProviders>
      </body>
    </html>
  );
}
