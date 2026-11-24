// filepath: src/app/(dashboard)/layout.tsx
'use client';

import React, { useEffect, useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { SucursalResponseDto } from '@/types/api.types';
import { authService, sucursalesService } from '@/services/api.services';
import { useAuth, useCajaPos, useCatalogos } from '@/context/AppProviders';
import { extractPagedItems } from '@/lib/apiHelpers';
import { Button, InputField, Modal } from '@/components/ui/DesignSystem';

interface NavItem {
  href: string;
  label: string;
  roles: string[];
  grupo: string;
}

const NAV_ITEMS: NavItem[] = [
  // Portal Peluquero
  {
    href: '/peluquero',
    label: 'Mi Puesto Profesional',
    roles: ['Peluquero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Peluquero',
  },
  // Portal Recepción y Caja (Incluido también para Administrador)
  {
    href: '/recepcion/calendario',
    label: '1. Calendario Master',
    roles: ['Recepcionista', 'Cajero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Mostrador & POS',
  },
  {
    href: '/recepcion/pos',
    label: '2. Punto de Venta (POS)',
    roles: ['Recepcionista', 'Cajero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Mostrador & POS',
  },
  {
    href: '/recepcion/caja',
    label: '3. Caja Diaria y Arqueo',
    roles: ['Recepcionista', 'Cajero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Mostrador & POS',
  },
  {
    href: '/recepcion/clientes',
    label: '4. Clientes CRM 360°',
    roles: ['Recepcionista', 'Cajero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Mostrador & POS',
  },
  // Portal Administrador del Salón
  {
    href: '/admin/dashboard',
    label: '5. Dashboard Ejecutivo BI',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Dirección & Finanzas',
  },
  {
    href: '/admin/inventario',
    label: '6. Inventario, Kardex y OC',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Dirección & Finanzas',
  },
  {
    href: '/admin/servicios-recetas',
    label: '7. Servicios y Recetas',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Dirección & Finanzas',
  },
  {
    href: '/admin/staff-liquidaciones',
    label: '8. Staff, Comisiones y Sueldos',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Dirección & Finanzas',
  },
  {
    href: '/admin/configuracion-seguridad',
    label: '9. Configuración y Seguridad',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Dirección & Finanzas',
  },
  {
    href: '/admin/diagnostico-api',
    label: '10. Diagnóstico API (22 Ctrl)',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Dirección & Finanzas',
  },
  // Portal Super-Admin SaaS
  {
    href: '/saas-admin',
    label: 'Consola Global SaaS',
    roles: ['SuperAdminSaaS', 'Administrador'],
    grupo: 'Backoffice SaaS',
  },
];

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const { user, sucursalId, switchSucursal, logout } = useAuth();
  const { configSalon } = useCatalogos();
  const { sesionCajaActual, cajaAbierta } = useCajaPos();

  const [sucursales, setSucursales] = useState<SucursalResponseDto[]>([]);
  const [modalClaveOpen, setModalClaveOpen] = useState<boolean>(false);
  const [passwordActual, setPasswordActual] = useState<string>('');
  const [nuevaPassword, setNuevaPassword] = useState<string>('');
  const [msgClave, setMsgClave] = useState<string | null>(null);
  const [cambiandoClave, setCambiandoClave] = useState<boolean>(false);

  useEffect(() => {
    sucursalesService
      .getAll()
      .then((res) => {
        if (res.success) {
          setSucursales(extractPagedItems<SucursalResponseDto>(res.data));
        }
      })
      .catch(() => {
        // Si aún no hay sucursales creadas usa la #1 por defecto
      });
  }, []);

  const rolActual = user?.rol || 'Administrador';
  const itemsVisibles = NAV_ITEMS.filter((item) => item.roles.includes(rolActual));

  const handleCambiarPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setCambiandoClave(true);
    setMsgClave(null);
    try {
      const res = await authService.cambiarPassword({
        passwordActual,
        nuevaPassword,
      });
      if (res.success) {
        setMsgClave(
          'Contraseña actualizada. Por seguridad se revocaron todos los Refresh Tokens anteriores.'
        );
        setPasswordActual('');
        setNuevaPassword('');
      } else {
        setMsgClave(res.message || 'No se pudo actualizar la contraseña.');
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setMsgClave(customErr?.message || 'Error al cambiar la contraseña.');
    } finally {
      setCambiandoClave(false);
    }
  };

  return (
    <div className="min-h-screen flex flex-col lg:flex-row bg-slate-100 text-slate-900">
      {/* Sidebar Corporativo Multi-Portal */}
      <aside className="no-print w-full lg:w-64 bg-slate-900 text-slate-100 flex flex-col justify-between shrink-0 border-r border-slate-800">
        <div className="p-4 space-y-5">
          <div className="border-b border-slate-800 pb-3">
            <span className="text-[10px] font-mono uppercase tracking-widest text-amber-400 block">
              Tenant #{user?.tenantId || 1} • Rol: {rolActual}
            </span>
            <h2 className="text-sm font-bold text-white truncate mt-0.5">
              {configSalon?.nombreComercial || 'Mi Peluquería SaaS'}
            </h2>
          </div>

          {/* Selector Multi-Sucursal (X-Sucursal-Id) */}
          <div className="space-y-1">
            <label className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">
              Sucursal Activa
            </label>
            <select
              value={sucursalId}
              onChange={(e) => switchSucursal(Number(e.target.value))}
              className="w-full rounded bg-slate-800 border border-slate-700 px-2.5 py-1.5 text-xs text-white"
            >
              {sucursales.length > 0 ? (
                sucursales.map((s) => (
                  <option key={s.id} value={s.id}>
                    #{s.id} — {s.nombre} ({s.monedaIso})
                  </option>
                ))
              ) : (
                <option value={1}>
                  #1 — Casa Matriz ({configSalon?.moneda || 'ARS'})
                </option>
              )}
            </select>
          </div>

          {/* Navegación por Módulos */}
          <nav className="space-y-1">
            {itemsVisibles.map((item) => {
              const active =
                pathname === item.href || pathname.startsWith(`${item.href}/`);
              return (
                <Link
                  key={item.href}
                  href={item.href}
                  className={`block px-3 py-2 rounded-md text-xs font-semibold transition-colors ${
                    active
                      ? 'bg-amber-800 text-white'
                      : 'text-slate-300 hover:bg-slate-800 hover:text-white'
                  }`}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </div>

        {/* Pie del Sidebar: Estado de Caja, Cambio de Clave y Logout */}
        <div className="p-4 border-t border-slate-800 space-y-2.5 text-xs">
          <div className="flex items-center justify-between bg-slate-950 px-3 py-2 rounded border border-slate-800">
            <span className="text-slate-400">Tesorería:</span>
            <span
              className={`font-mono font-bold ${
                cajaAbierta ? 'text-emerald-400' : 'text-red-400'
              }`}
            >
              {cajaAbierta ? `CAJA #${sesionCajaActual?.id}` : 'CERRADA'}
            </span>
          </div>

          <div className="flex items-center justify-between pt-1">
            <button
              type="button"
              onClick={() => setModalClaveOpen(true)}
              className="text-slate-300 hover:text-white underline"
            >
              Cambiar Clave
            </button>
            <button
              type="button"
              onClick={logout}
              className="text-red-400 hover:text-red-300 font-semibold"
            >
              Salir
            </button>
          </div>
        </div>
      </aside>

      {/* Área de Contenido Principal */}
      <div className="flex-1 flex flex-col min-w-0">{children}</div>

      {/* Modal de Cambio de Contraseña (POST /api/auth/cambiar-password) */}
      <Modal
        isOpen={modalClaveOpen}
        onClose={() => setModalClaveOpen(false)}
        title="Cambiar Contraseña de Seguridad"
        subtitle="Al actualizar tu clave se revocarán todas las sesiones y Refresh Tokens previos."
      >
        <form onSubmit={handleCambiarPassword} className="space-y-4">
          {msgClave && (
            <div className="p-3 rounded bg-slate-900 text-white text-xs">
              {msgClave}
            </div>
          )}
          <InputField
            label="Contraseña Actual *"
            type="password"
            required
            value={passwordActual}
            onChange={(e) => setPasswordActual(e.target.value)}
          />
          <InputField
            label="Nueva Contraseña *"
            type="password"
            required
            value={nuevaPassword}
            onChange={(e) => setNuevaPassword(e.target.value)}
          />
          <div className="flex justify-end gap-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setModalClaveOpen(false)}
            >
              Cerrar
            </Button>
            <Button type="submit" variant="primary" isLoading={cambiandoClave}>
              Actualizar Contraseña
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}