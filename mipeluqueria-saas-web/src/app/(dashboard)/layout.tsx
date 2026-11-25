// filepath: src/app/(dashboard)/layout.tsx
'use client';

import React, { useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import {
  Activity,
  Building2,
  Calendar,
  CalendarDays,
  ChevronRight,
  KeyRound,
  LayoutDashboard,
  LogOut,
  LucideIcon,
  Menu,
  Package,
  Scissors,
  Settings,
  ShieldCheck,
  ShoppingBag,
  Sparkles,
  UserCog,
  Users,
  Wallet,
  X,
} from 'lucide-react';
import { SucursalResponseDto } from '@/types/api.types';
import { authService, sucursalesService } from '@/services/api.services';
import { useAuth, useCajaPos, useCatalogos } from '@/context/AppProviders';
import { extractPagedItems } from '@/lib/apiHelpers';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import {
  BrandLogo,
  Button,
  InputField,
  Modal,
  ThemeToggle,
} from '@/components/ui';

type NavGroup = 'Mi Espacio' | 'Operación' | 'Gestión' | 'Plataforma';

interface NavItem {
  href: string;
  label: string;
  roles: readonly string[];
  grupo: NavGroup;
  icon: LucideIcon;
}

const NAV_GROUPS_ORDER: readonly NavGroup[] = [
  'Mi Espacio',
  'Operación',
  'Gestión',
  'Plataforma',
];

const NAV_ITEMS: readonly NavItem[] = [
  // Mi Espacio
  {
    href: '/peluquero',
    label: 'Mi Puesto',
    roles: ['Peluquero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Mi Espacio',
    icon: Scissors,
  },
  // Operación
  {
    href: '/recepcion/calendario',
    label: 'Calendario',
    roles: ['Recepcionista', 'Cajero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Operación',
    icon: CalendarDays,
  },
  {
    href: '/recepcion/pos',
    label: 'Punto de Venta',
    roles: ['Recepcionista', 'Cajero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Operación',
    icon: ShoppingBag,
  },
  {
    href: '/recepcion/caja',
    label: 'Caja Diaria',
    roles: ['Recepcionista', 'Cajero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Operación',
    icon: Wallet,
  },
  {
    href: '/recepcion/clientes',
    label: 'Clientes',
    roles: ['Recepcionista', 'Cajero', 'Administrador', 'SuperAdminSaaS'],
    grupo: 'Operación',
    icon: Users,
  },
  // Gestión
  {
    href: '/admin/dashboard',
    label: 'Dashboard BI',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Gestión',
    icon: LayoutDashboard,
  },
  {
    href: '/admin/inventario',
    label: 'Inventario y Compras',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Gestión',
    icon: Package,
  },
  {
    href: '/admin/servicios-recetas',
    label: 'Servicios y Recetas',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Gestión',
    icon: Sparkles,
  },
  {
    href: '/admin/staff-liquidaciones',
    label: 'Staff y Liquidaciones',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Gestión',
    icon: UserCog,
  },
  {
    href: '/admin/configuracion-seguridad',
    label: 'Configuración',
    roles: ['Administrador', 'SuperAdminSaaS'],
    grupo: 'Gestión',
    icon: Settings,
  },
  // Plataforma (exclusivo SuperAdminSaaS)
  {
    href: '/saas-admin',
    label: 'Consola SaaS',
    roles: ['SuperAdminSaaS'],
    grupo: 'Plataforma',
    icon: ShieldCheck,
  },
  {
    href: '/admin/diagnostico-api',
    label: 'Diagnóstico API',
    roles: ['SuperAdminSaaS'],
    grupo: 'Plataforma',
    icon: Activity,
  },
];

function capitalizeFirst(value: string): string {
  const cleaned = value.replace(/\.$/, '').trim();
  if (!cleaned) return '';
  return cleaned.charAt(0).toUpperCase() + cleaned.slice(1).toLowerCase();
}

function formatSalonDate(date: Date, timeZone?: string | null): string {
  const baseOptions: Intl.DateTimeFormatOptions = {
    weekday: 'short',
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  };

  let formatter: Intl.DateTimeFormat;
  try {
    formatter = new Intl.DateTimeFormat(
      'es-AR',
      timeZone ? { ...baseOptions, timeZone } : baseOptions
    );
  } catch {
    formatter = new Intl.DateTimeFormat('es-AR', baseOptions);
  }

  const parts = formatter.formatToParts(date);
  const weekday = capitalizeFirst(
    parts.find((p) => p.type === 'weekday')?.value ?? ''
  );
  const day = parts.find((p) => p.type === 'day')?.value ?? '';
  const month = capitalizeFirst(
    parts.find((p) => p.type === 'month')?.value ?? ''
  );
  const year = parts.find((p) => p.type === 'year')?.value ?? '';

  if (weekday && day && month && year) {
    return `${weekday}, ${day} ${month} ${year}`;
  }
  return formatter.format(date);
}

function formatCajaShortDate(isoString?: string | null): string | null {
  if (!isoString) return null;
  const parsed = new Date(isoString);
  if (Number.isNaN(parsed.getTime())) return null;
  const day = String(parsed.getDate()).padStart(2, '0');
  const month = String(parsed.getMonth() + 1).padStart(2, '0');
  return `${day}/${month}`;
}

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
  const [mobileMenuOpen, setMobileMenuOpen] = useState<boolean>(false);
  const [fechaHoyTexto, setFechaHoyTexto] = useState<string>('');

  const [modalClaveOpen, setModalClaveOpen] = useState<boolean>(false);
  const [passwordActual, setPasswordActual] = useState<string>('');
  const [nuevaPassword, setNuevaPassword] = useState<string>('');
  const [msgClave, setMsgClave] = useState<{
    type: 'success' | 'error';
    text: string;
  } | null>(null);
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
        // Si aún no hay sucursales adicionales, se conserva la sucursal activa actual
      });
  }, []);

  useEffect(() => {
    setFechaHoyTexto(formatSalonDate(new Date(), configSalon?.timeZoneId));
  }, [configSalon?.timeZoneId]);

  useEffect(() => {
    setMobileMenuOpen(false);
  }, [pathname]);

  useEffect(() => {
    if (!mobileMenuOpen) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setMobileMenuOpen(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [mobileMenuOpen]);

  const rolActual = user?.rol || 'Administrador';
  const nombreSalon =
    configSalon?.nombreComercial?.trim() || 'Mi Peluquería Central';

  const navByGroup = useMemo(() => {
    return NAV_GROUPS_ORDER.map((group) => ({
      group,
      items: NAV_ITEMS.filter(
        (item) => item.grupo === group && item.roles.includes(rolActual)
      ),
    })).filter((section) => section.items.length > 0);
  }, [rolActual]);

  const currentNavItem = useMemo(() => {
    return NAV_ITEMS.find(
      (item) => pathname === item.href || pathname.startsWith(`${item.href}/`)
    );
  }, [pathname]);

  const sucursalActiva = useMemo(() => {
    return sucursales.find((s) => s.id === sucursalId) ?? sucursales[0] ?? null;
  }, [sucursales, sucursalId]);

  const fechaAperturaCorta = formatCajaShortDate(
    sesionCajaActual?.fechaApertura
  );
  const textoCaja =
    cajaAbierta && sesionCajaActual
      ? fechaAperturaCorta
        ? `Caja #${sesionCajaActual.id} • ${fechaAperturaCorta}`
        : `Caja #${sesionCajaActual.id}`
      : 'Caja Cerrada';

  const handleClosePasswordModal = () => {
    setModalClaveOpen(false);
    setMsgClave(null);
    setPasswordActual('');
    setNuevaPassword('');
  };

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
        setMsgClave({
          type: 'success',
          text: 'Contraseña actualizada correctamente. Por seguridad, tus demás sesiones fueron cerradas.',
        });
        setPasswordActual('');
        setNuevaPassword('');
      } else {
        setMsgClave({
          type: 'error',
          text: res.message || 'No se pudo actualizar la contraseña.',
        });
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setMsgClave({
        type: 'error',
        text: customErr?.message || 'Error al cambiar la contraseña.',
      });
    } finally {
      setCambiandoClave(false);
    }
  };

  return (
    <div className="min-h-screen w-full flex bg-canvas text-ink-primary">
      {/* Backdrop móvil para el drawer del Sidebar */}
      {mobileMenuOpen && (
        <button
          type="button"
          aria-label="Cerrar menú de navegación"
          onClick={() => setMobileMenuOpen(false)}
          className="no-print fixed inset-0 z-40 bg-slate-950/60 backdrop-blur-[1px] lg:hidden"
        />
      )}

      {/* Sidebar Compacto (224px en desktop + drawer colapsable en móvil) */}
      <aside
        aria-label="Navegación principal"
        className={`no-print fixed inset-y-0 left-0 z-50 w-60 lg:w-56 lg:sticky lg:top-0 lg:h-screen shrink-0 bg-sidebar text-slate-100 border-r border-slate-800/80 flex flex-col justify-between transition-transform duration-200 ease-out ${
          mobileMenuOpen
            ? 'translate-x-0'
            : '-translate-x-full lg:translate-x-0'
        }`}
      >
        {/* Identidad del Salón + Rol */}
        <div className="px-3.5 pt-4 pb-3 border-b border-slate-800/80 shrink-0">
          <div className="flex items-start justify-between gap-2">
            <BrandLogo
              brandName={nombreSalon}
              invertText
              size="sm"
              className="min-w-0 flex-1"
            />
            <button
              type="button"
              onClick={() => setMobileMenuOpen(false)}
              aria-label="Cerrar menú"
              className="lg:hidden inline-flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 hover:bg-white/10 hover:text-white transition-colors shrink-0"
            >
              <X className="h-4 w-4" aria-hidden="true" />
            </button>
          </div>
          <div className="mt-2.5 flex items-center justify-between gap-2">
            <span className="inline-flex items-center rounded-md bg-white/10 px-2 py-0.5 text-[11px] font-medium text-slate-200 ring-1 ring-inset ring-white/10 truncate">
              {rolActual}
            </span>
            {user?.username && (
              <span className="text-[11px] text-slate-400 truncate">
                {user.username}
              </span>
            )}
          </div>
        </div>

        {/* Navegación Agrupada */}
        <nav className="flex-1 overflow-y-auto px-2.5 py-3 space-y-4">
          {navByGroup.map(({ group, items }) => (
            <div key={group} className="space-y-1">
              <p className="px-2.5 pb-0.5 text-[11px] font-medium tracking-wide text-slate-400">
                {group}
              </p>
              <div className="space-y-0.5">
                {items.map((item) => {
                  const Icon = item.icon;
                  const active =
                    pathname === item.href ||
                    pathname.startsWith(`${item.href}/`);
                  return (
                    <Link
                      key={item.href}
                      href={item.href}
                      aria-current={active ? 'page' : undefined}
                      className={`flex items-center gap-2.5 rounded-lg px-2.5 py-2 text-xs font-medium transition-colors ${
                        active
                          ? 'bg-brand-primary text-white shadow-sm'
                          : 'text-slate-300 hover:bg-white/10 hover:text-white'
                      }`}
                    >
                      <Icon
                        className={`h-4 w-4 shrink-0 ${
                          active ? 'text-white' : 'text-slate-400'
                        }`}
                        aria-hidden="true"
                      />
                      <span className="truncate">{item.label}</span>
                    </Link>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        {/* Footer del Sidebar: Estado de Caja con Fecha, Cambio de Clave y Salir */}
        <div className="p-3 border-t border-slate-800/80 space-y-2.5 shrink-0">
          <Link
            href="/recepcion/caja"
            className={`flex items-center justify-between gap-2 rounded-lg px-2.5 py-2 border transition-colors ${
              cajaAbierta
                ? 'bg-emerald-950/60 border-emerald-500/30 hover:bg-emerald-950/80'
                : 'bg-rose-950/50 border-rose-500/30 hover:bg-rose-950/70'
            }`}
          >
            <span className="inline-flex items-center gap-2 min-w-0">
              <span
                className={`h-2 w-2 rounded-full shrink-0 ${
                  cajaAbierta ? 'bg-emerald-400' : 'bg-rose-400'
                }`}
                aria-hidden="true"
              />
              <span
                className={`text-xs font-semibold truncate ${
                  cajaAbierta
                    ? 'font-mono-tabular text-emerald-300'
                    : 'text-rose-300'
                }`}
              >
                {textoCaja}
              </span>
            </span>
          </Link>

          <div className="flex items-center justify-between gap-1 pt-0.5">
            <button
              type="button"
              onClick={() => setModalClaveOpen(true)}
              className="inline-flex items-center gap-1.5 rounded-md px-2 py-1.5 text-xs font-medium text-slate-300 hover:bg-white/10 hover:text-white transition-colors"
            >
              <KeyRound className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
              <span>Cambiar Clave</span>
            </button>
            <button
              type="button"
              onClick={logout}
              className="inline-flex items-center gap-1.5 rounded-md px-2 py-1.5 text-xs font-medium text-rose-300 hover:bg-rose-950/60 hover:text-rose-200 transition-colors"
            >
              <LogOut className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
              <span>Salir</span>
            </button>
          </div>
        </div>
      </aside>

      {/* Columna Derecha: Topbar Fino + Área de Contenido Principal */}
      <div className="min-w-0 flex-1 flex flex-col overflow-x-hidden">
        {/* Topbar Fino (h-14) */}
        <header className="app-topbar no-print sticky top-0 z-30 h-14 shrink-0 bg-surface/95 backdrop-blur border-b border-border-subtle px-4 lg:px-6 flex items-center justify-between gap-3">
          {/* Izquierda: Botón Menú Móvil + Breadcrumb / Sección Actual */}
          <div className="min-w-0 flex items-center gap-2.5">
            <button
              type="button"
              onClick={() => setMobileMenuOpen(true)}
              aria-label="Abrir menú de navegación"
              className="lg:hidden inline-flex h-9 w-9 items-center justify-center rounded-lg border border-border-strong bg-surface text-ink-primary hover:bg-elevated transition-colors shrink-0"
            >
              <Menu className="h-4 w-4" aria-hidden="true" />
            </button>

            <nav
              aria-label="Ubicación actual"
              className="flex items-center gap-1.5 min-w-0 text-sm"
            >
              {currentNavItem ? (
                <>
                  <span className="hidden sm:inline text-xs font-medium text-ink-secondary truncate">
                    {currentNavItem.grupo}
                  </span>
                  <ChevronRight
                    className="hidden sm:inline h-3.5 w-3.5 text-ink-secondary/70 shrink-0"
                    aria-hidden="true"
                  />
                  <span className="font-semibold text-ink-primary truncate">
                    {currentNavItem.label}
                  </span>
                </>
              ) : (
                <span className="font-semibold text-ink-primary truncate">
                  {nombreSalon}
                </span>
              )}
            </nav>
          </div>

          {/* Derecha: Fecha del Salón, Selector Compacto de Sucursal y ThemeToggle */}
          <div className="flex items-center gap-2 sm:gap-2.5 shrink-0">
            {fechaHoyTexto && (
              <div
                className="hidden md:inline-flex h-9 items-center gap-1.5 rounded-lg bg-elevated border border-border-subtle px-2.5 text-xs font-medium text-ink-secondary font-mono-tabular"
                title="Fecha actual del salón"
              >
                <Calendar
                  className="h-3.5 w-3.5 text-ink-secondary shrink-0"
                  aria-hidden="true"
                />
                <span>{fechaHoyTexto}</span>
              </div>
            )}

            {sucursales.length > 1 ? (
              <div className="relative inline-flex items-center">
                <Building2
                  className="pointer-events-none absolute left-2.5 h-3.5 w-3.5 text-ink-secondary"
                  aria-hidden="true"
                />
                <select
                  aria-label="Seleccionar sucursal activa"
                  value={sucursalId}
                  onChange={(e) => switchSucursal(Number(e.target.value))}
                  className="h-9 max-w-[175px] rounded-lg bg-surface border border-border-strong pl-8 pr-6 text-xs font-medium text-ink-primary hover:bg-elevated focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25 transition-colors truncate"
                >
                  {sucursales.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.nombre}
                    </option>
                  ))}
                </select>
              </div>
            ) : sucursalActiva ? (
              <div className="hidden sm:inline-flex h-9 items-center gap-1.5 rounded-lg bg-elevated border border-border-subtle px-2.5 text-xs font-medium text-ink-primary max-w-[170px]">
                <Building2
                  className="h-3.5 w-3.5 text-ink-secondary shrink-0"
                  aria-hidden="true"
                />
                <span className="truncate">{sucursalActiva.nombre}</span>
              </div>
            ) : null}

            <ThemeToggle variant="icon" />
          </div>
        </header>

        {/* Área de Contenido Principal (garantía 0px desborde horizontal en 1280x720 y 1366x768) */}
        <main className="min-w-0 flex-1 overflow-x-hidden px-4 py-4 lg:px-6 lg:py-5">
          {children}
        </main>
      </div>

      {/* Modal de Cambio de Contraseña */}
      <Modal
        isOpen={modalClaveOpen}
        onClose={handleClosePasswordModal}
        title="Cambiar Contraseña"
        subtitle="Al actualizar tu contraseña se cerrarán tus otras sesiones activas por seguridad."
      >
        <form onSubmit={handleCambiarPassword} className="space-y-4">
          {msgClave && (
            <div
              className={`p-3 rounded-lg border text-xs font-medium ${
                msgClave.type === 'success'
                  ? `${SEMANTIC_COLORS.cash.surfaceClass} ${SEMANTIC_COLORS.cash.textClass}`
                  : `${SEMANTIC_COLORS.danger.surfaceClass} ${SEMANTIC_COLORS.danger.textClass}`
              }`}
            >
              {msgClave.text}
            </div>
          )}
          <InputField
            id="layout-password-actual"
            label="Contraseña actual *"
            type="password"
            autoComplete="current-password"
            required
            value={passwordActual}
            onChange={(e) => setPasswordActual(e.target.value)}
          />
          <InputField
            id="layout-password-nueva"
            label="Nueva contraseña *"
            type="password"
            autoComplete="new-password"
            required
            value={nuevaPassword}
            onChange={(e) => setNuevaPassword(e.target.value)}
          />
          <div className="flex justify-end gap-2 pt-1">
            <Button
              type="button"
              variant="secondary"
              onClick={handleClosePasswordModal}
            >
              Cancelar
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
