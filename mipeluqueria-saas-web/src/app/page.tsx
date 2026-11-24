// filepath: src/app/page.tsx
'use client';

import React from 'react';
import Link from 'next/link';
import { useAuth, useCatalogos } from '@/context/AppProviders';

export default function HomePage() {
  const { user, isAuthenticated } = useAuth();
  const { configSalon } = useCatalogos();

  const getDashboardHref = () => {
    if (!user) return '/login';
    switch (user.rol) {
      case 'SuperAdminSaaS':
        return '/saas-admin';
      case 'Administrador':
        return '/admin/dashboard';
      case 'Recepcionista':
      case 'Cajero':
        return '/recepcion/calendario';
      case 'Peluquero':
        return '/peluquero';
      default:
        return '/mi-cuenta/turnos';
    }
  };

  return (
    <main className="min-h-screen bg-[#FAF8F5] text-stone-900 flex flex-col justify-between">
      <header className="border-b border-stone-200 bg-white px-6 py-4">
        <div className="max-w-6xl mx-auto flex items-center justify-between">
          <div>
            <span className="text-[11px] font-bold uppercase tracking-widest text-amber-900">
              Plataforma SaaS Multi-Tenant
            </span>
            <h1 className="text-lg font-bold text-stone-900">
              {configSalon?.nombreComercial || 'Mi Peluquería & Barbería SaaS'}
            </h1>
          </div>
          <div className="flex items-center gap-3 text-xs font-semibold">
            <Link
              href="/reservar"
              className="px-4 py-2 rounded-md bg-amber-900 text-white hover:bg-amber-950"
            >
              Reservar Turno Online 24/7
            </Link>
            {isAuthenticated ? (
              <Link
                href={getDashboardHref()}
                className="px-4 py-2 rounded-md bg-stone-900 text-white hover:bg-stone-800"
              >
                Ir a Mi Panel ({user?.rol})
              </Link>
            ) : (
              <Link
                href="/login"
                className="px-4 py-2 rounded-md border border-stone-300 bg-white text-stone-800 hover:bg-stone-100"
              >
                Acceso Personal / Clientes
              </Link>
            )}
          </div>
        </div>
      </header>

      <section className="max-w-6xl mx-auto px-6 py-16 grid grid-cols-1 lg:grid-cols-12 gap-10 items-center">
        <div className="lg:col-span-7 space-y-5">
          <span className="inline-block px-3 py-1 rounded bg-stone-200 text-stone-800 text-xs font-bold uppercase tracking-wider">
            {configSalon?.direccion || 'Av. Colón 1250, Córdoba, Argentina'}
          </span>
          <h2 className="text-3xl sm:text-4xl font-extrabold text-stone-900 leading-tight">
            Agenda en tiempo real, fichas técnicas capilares y gestión integral del salón.
          </h2>
          <p className="text-sm text-stone-600 leading-relaxed max-w-xl">
            Reservá tu turno en 4 pasos eligiendo a tu profesional preferido con confirmación
            inmediata y recordatorios automáticos por WhatsApp 24 hs y 2 hs antes de tu visita.
          </p>
          <div className="flex flex-wrap gap-3 pt-2">
            <Link
              href="/reservar"
              className="px-6 py-3 rounded-md bg-amber-900 text-white text-sm font-bold hover:bg-amber-950"
            >
              Agendar Turno Ahora →
            </Link>
            <Link
              href="/mi-cuenta/turnos"
              className="px-6 py-3 rounded-md border border-stone-300 bg-white text-stone-800 text-sm font-semibold hover:bg-stone-100"
            >
              Consultar o Cancelar Mis Turnos
            </Link>
          </div>
        </div>

        <div className="lg:col-span-5 bg-white border border-stone-200 rounded-lg p-6 space-y-4 shadow-sm">
          <h3 className="text-sm font-bold uppercase tracking-wider text-stone-700 border-b border-stone-200 pb-2">
            Accesos Directos por Portal RBAC
          </h3>
          <div className="space-y-2.5 text-xs">
            <Link
              href="/reservar"
              className="block p-3 rounded border border-stone-200 hover:border-amber-900 transition-colors"
            >
              <strong className="text-stone-900 block">A. Portal del Cliente Final</strong>
              <span className="text-stone-500">
                Wizard de Reserva 4 pasos, Mis Turnos y Perfil con PIN OTP.
              </span>
            </Link>
            <Link
              href="/peluquero"
              className="block p-3 rounded border border-stone-200 hover:border-amber-900 transition-colors"
            >
              <strong className="text-stone-900 block">B. Portal del Peluquero / Profesional</strong>
              <span className="text-stone-500">
                Agenda táctil 1-clic, Fichas Técnicas Capilares, Comisiones y Bloqueos.
              </span>
            </Link>
            <Link
              href="/recepcion/calendario"
              className="block p-3 rounded border border-stone-200 hover:border-amber-900 transition-colors"
            >
              <strong className="text-stone-900 block">C. Portal de Recepción y Caja POS</strong>
              <span className="text-stone-500">
                Calendario Master, Facturador POS Multipago, Arqueo de Caja y CRM 360°.
              </span>
            </Link>
            <Link
              href="/admin/dashboard"
              className="block p-3 rounded border border-stone-200 hover:border-amber-900 transition-colors"
            >
              <strong className="text-stone-900 block">D. Suite del Administrador del Salón</strong>
              <span className="text-stone-500">
                Dashboard BI, Inventario/Kardex, Recetas de Gabinete, Sueldos y Seguridad.
              </span>
            </Link>
            <Link
              href="/saas-admin"
              className="block p-3 rounded border border-stone-200 hover:border-amber-900 transition-colors"
            >
              <strong className="text-stone-900 block">E. Consola Global Super-Admin SaaS</strong>
              <span className="text-stone-500">
                Gobierno de Tenants, Sucursales, Planes MRR y Auditoría Forense JSON Diff.
              </span>
            </Link>
          </div>
        </div>
      </section>

      <footer className="border-t border-stone-200 bg-white px-6 py-4 text-xs text-stone-500">
        <div className="max-w-6xl mx-auto flex flex-wrap items-center justify-between gap-4">
          <span>
            © {new Date().getFullYear()} {configSalon?.razonSocial || 'Estudio Capilar S.R.L.'} • CUIT{' '}
            {configSalon?.cuit || '30-71548963-9'}
          </span>
          <div className="flex gap-4">
            <Link href="/legal/terminos" className="underline hover:text-stone-900">
              Términos de Servicio (ToS)
            </Link>
            <Link href="/legal/privacidad" className="underline hover:text-stone-900">
              Política de Privacidad
            </Link>
          </div>
        </div>
      </footer>
    </main>
  );
}