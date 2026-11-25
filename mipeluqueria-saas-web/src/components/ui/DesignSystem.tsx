'use client';

import React, { useEffect } from 'react';
import Link from 'next/link';
import { Loader2, X } from 'lucide-react';
import { useCookieConsent } from '@/context/AppProviders';

// ============================================================================
// 1. BOTÓN DEL SISTEMA DE DISEÑO (4 VARIANTES ESTRICTAS + COMPATIBILIDAD)
// ============================================================================

/** Variantes canónicas del Design System v1.1.0 (máx. 1 primary por pantalla/modal). */
export type CanonicalButtonVariant = 'primary' | 'secondary' | 'destructive' | 'ghost';

/** Incluye alias legacy ('editorial' -> primary, 'danger' -> destructive) para migración sin regresiones. */
export type ButtonVariant = CanonicalButtonVariant | 'editorial' | 'danger';

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  /** Variante jerárquica del botón. Por defecto 'primary'. */
  variant?: ButtonVariant;
  /** Tono aplicado cuando variant es 'destructive': 'soft' para filas de tabla, 'solid' para confirmaciones. */
  destructiveTone?: 'solid' | 'soft';
  /** Altura estandarizada: sm (h-9 toolbars/tablas), md (h-10 CTAs/modales), lg (h-11). */
  size?: 'sm' | 'md' | 'lg';
  /** Muestra spinner automático y bloquea interacción durante mutaciones. */
  isLoading?: boolean;
  /** Ícono opcional alineado a la izquierda (recomendado 16px de lucide-react). */
  leftIcon?: React.ReactNode;
  /** Ícono opcional alineado a la derecha. */
  rightIcon?: React.ReactNode;
}

/**
 * Botón base del Design System Dual (Studio Clean + Obsidian & Indigo) con contraste WCAG 2.2 AA.
 *
 * @param props Propiedades del botón.
 * @example
 * <Button variant="primary" size="md" isLoading={saving}>Guardar cambios</Button>
 */
export function Button({
  children,
  variant = 'primary',
  destructiveTone = 'solid',
  size = 'md',
  isLoading = false,
  leftIcon,
  rightIcon,
  disabled,
  className = '',
  ...props
}: ButtonProps) {
  const normalizedVariant: CanonicalButtonVariant =
    variant === 'editorial'
      ? 'primary'
      : variant === 'danger'
      ? 'destructive'
      : variant;

  const base =
    'inline-flex items-center justify-center gap-2 font-medium transition-colors rounded-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-offset-canvas disabled:opacity-50 disabled:cursor-not-allowed select-none whitespace-nowrap';

  const destructiveStyles =
    destructiveTone === 'soft'
      ? 'bg-red-50 text-red-700 border border-red-200 hover:bg-red-100 focus-visible:ring-red-600 dark:bg-rose-950/50 dark:text-rose-300 dark:border-rose-800/80 dark:hover:bg-rose-900/60'
      : 'bg-red-600 text-white hover:bg-red-700 focus-visible:ring-red-600 dark:bg-rose-600 dark:hover:bg-rose-500';

  const variants: Record<CanonicalButtonVariant, string> = {
    primary:
      'bg-brand-primary text-white hover:bg-brand-primary-hover shadow-sm focus-visible:ring-brand-primary',
    secondary:
      'bg-surface text-ink-primary border border-border-strong hover:bg-elevated focus-visible:ring-brand-accent',
    destructive: destructiveStyles,
    ghost:
      'bg-transparent text-ink-secondary hover:text-ink-primary hover:bg-elevated focus-visible:ring-brand-accent',
  };

  const sizes: Record<NonNullable<ButtonProps['size']>, string> = {
    sm: 'h-9 px-3 text-xs',
    md: 'h-10 px-4 text-sm',
    lg: 'h-11 px-5 text-sm',
  };

  return (
    <button
      disabled={disabled || isLoading}
      className={`${base} ${variants[normalizedVariant]} ${sizes[size]} ${className}`}
      {...props}
    >
      {isLoading ? (
        <span className="inline-flex items-center gap-2">
          <Loader2 className="h-4 w-4 animate-spin shrink-0" aria-hidden="true" />
          <span>Procesando...</span>
        </span>
      ) : (
        <>
          {leftIcon && <span className="shrink-0 inline-flex">{leftIcon}</span>}
          {children}
          {rightIcon && <span className="shrink-0 inline-flex">{rightIcon}</span>}
        </>
      )}
    </button>
  );
}

// ============================================================================
// 2. INPUT DE TEXTO CON SOPORTE DUAL CLARO/OSCURO Y MODO LECTURA
// ============================================================================

export interface InputFieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
  /** Etiqueta visible asociada al campo. */
  label: string;
  /** Texto auxiliar descriptivo debajo del input. */
  helperText?: string;
  /** Mensaje de validación de error. */
  errorText?: string;
  /** Etiqueta opcional mostrada cuando el campo está en modo solo lectura. */
  readOnlyBadgeText?: string;
}

/**
 * Campo de entrada estándar con soporte de tema Claro/Oscuro y estados de error/lectura.
 *
 * @param props Propiedades del campo.
 * @example
 * <InputField label="Nombre" placeholder="Ej. Ana García" value={name} onChange={...} />
 */
export function InputField({
  label,
  helperText,
  errorText,
  readOnly,
  readOnlyBadgeText = 'Solo lectura',
  className = '',
  id,
  ...props
}: InputFieldProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <label
        htmlFor={id}
        className="text-xs font-medium text-ink-secondary flex items-center justify-between gap-2"
      >
        <span>{label}</span>
        {readOnly && (
          <span className="text-[11px] font-medium text-amber-800 bg-amber-50 px-2 py-0.5 rounded-md border border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800/70">
            {readOnlyBadgeText}
          </span>
        )}
      </label>
      <input
        id={id}
        readOnly={readOnly}
        className={`h-10 w-full rounded-lg border px-3 text-sm transition-colors placeholder:text-ink-secondary/70 ${
          readOnly
            ? 'bg-elevated text-ink-secondary border-border-subtle cursor-not-allowed select-none'
            : 'bg-surface text-ink-primary border-border-strong focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25'
        } ${errorText ? 'border-red-600 dark:border-rose-500' : ''} ${className}`}
        {...props}
      />
      {helperText && !errorText && (
        <span className="text-xs text-ink-secondary">{helperText}</span>
      )}
      {errorText && (
        <span className="text-xs text-red-600 dark:text-rose-400 font-medium">
          {errorText}
        </span>
      )}
    </div>
  );
}

// ============================================================================
// 3. MODAL ACCESIBLE REUTILIZABLE
// ============================================================================

export interface ModalProps {
  /** Controla la visibilidad del modal. */
  isOpen: boolean;
  /** Callback de cierre al presionar el botón X o la tecla Escape. */
  onClose: () => void;
  /** Título principal del diálogo. */
  title: string;
  /** Subtítulo opcional descriptivo. */
  subtitle?: string;
  /** Contenido interno del modal. */
  children: React.ReactNode;
  /** Clase Tailwind de ancho máximo (por defecto 'max-w-xl'). */
  maxWidthClass?: string;
}

/**
 * Diálogo modal accesible con soporte de tema Claro/Oscuro y cierre con Escape.
 *
 * @param props Propiedades del modal.
 * @example
 * <Modal isOpen={open} onClose={() => setOpen(false)} title="Nuevo registro">
 *   <form>...</form>
 * </Modal>
 */
export function Modal({
  isOpen,
  onClose,
  title,
  subtitle,
  children,
  maxWidthClass = 'max-w-xl',
}: ModalProps) {
  useEffect(() => {
    if (!isOpen) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label={title}
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 p-4 backdrop-blur-[2px]"
    >
      <div
        className={`w-full ${maxWidthClass} rounded-xl bg-surface text-ink-primary border border-border-subtle shadow-2xl overflow-hidden flex flex-col max-h-[90vh]`}
      >
        <div className="flex items-start justify-between gap-4 border-b border-border-subtle px-5 py-4 bg-elevated/60">
          <div className="min-w-0">
            <h3 className="text-base font-semibold text-ink-primary truncate">
              {title}
            </h3>
            {subtitle && (
              <p className="text-xs text-ink-secondary mt-0.5">{subtitle}</p>
            )}
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Cerrar ventana"
            className="inline-flex h-8 w-8 items-center justify-center rounded-lg text-ink-secondary hover:text-ink-primary hover:bg-elevated transition-colors shrink-0"
          >
            <X className="h-4 w-4" aria-hidden="true" />
          </button>
        </div>
        <div className="p-5 overflow-y-auto">{children}</div>
      </div>
    </div>
  );
}

// ============================================================================
// 4. BANNER DE CONSENTIMIENTO DE COOKIES Y ANALYTICS (GDPR)
// ============================================================================

/**
 * Banner de consentimiento de cookies y analíticas opcionales.
 *
 * @example
 * <CookieConsentBanner />
 */
export function CookieConsentBanner() {
  const { consentGiven, acceptCookies, rejectCookies } = useCookieConsent();

  useEffect(() => {
    const gaId = process.env.NEXT_PUBLIC_GA_MEASUREMENT_ID;
    if (consentGiven === 'accepted' && gaId && typeof window !== 'undefined') {
      const existingScript = document.getElementById('ga-consent-script');
      if (!existingScript) {
        const script = document.createElement('script');
        script.id = 'ga-consent-script';
        script.async = true;
        script.src = `https://www.googletagmanager.com/gtag/js?id=${gaId}`;
        document.head.appendChild(script);
      }
    }
  }, [consentGiven]);

  if (consentGiven !== 'pending') return null;

  return (
    <div className="no-print fixed bottom-0 inset-x-0 z-50 bg-sidebar text-slate-100 border-t border-slate-800 px-6 py-4 shadow-2xl">
      <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="text-xs leading-relaxed text-slate-300">
          <strong className="text-white font-semibold">Privacidad y Cookies:</strong>{' '}
          Utilizamos cookies estrictamente necesarias para tu sesión y reserva de turnos.
          No cargamos Google Analytics ni telemetría sin tu consentimiento previo. Consultá
          nuestra{' '}
          <Link
            href="/legal/privacidad"
            className="underline text-sky-400 hover:text-sky-300"
          >
            Política de Privacidad
          </Link>{' '}
          y{' '}
          <Link
            href="/legal/terminos"
            className="underline text-sky-400 hover:text-sky-300"
          >
            Términos del Servicio
          </Link>
          .
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <Button variant="secondary" size="sm" onClick={rejectCookies}>
            Solo esenciales
          </Button>
          <Button variant="primary" size="sm" onClick={acceptCookies}>
            Aceptar analíticas
          </Button>
        </div>
      </div>
    </div>
  );
}

// ============================================================================
// 5. BADGE SEMÁNTICO DE ESTADOS Y ETIQUETAS
// ============================================================================

export interface BadgeProps {
  /** Contenido textual o ícono del badge. */
  children: React.ReactNode;
  /** Tono semántico alineado al Design System Dual. */
  variant?: 'neutral' | 'success' | 'warning' | 'danger' | 'info' | 'courtesy';
  /** Clases Tailwind adicionales. */
  className?: string;
}

/**
 * Etiqueta semántica compacta con soporte completo para Modo Claro y Modo Oscuro.
 *
 * @param props Propiedades del badge.
 * @example
 * <Badge variant="success">Caja Abierta</Badge>
 */
export function Badge({
  children,
  variant = 'neutral',
  className = '',
}: BadgeProps) {
  const styles: Record<NonNullable<BadgeProps['variant']>, string> = {
    neutral:
      'bg-slate-100 text-slate-700 border-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700',
    success:
      'bg-emerald-50 text-emerald-800 border-emerald-200 dark:bg-emerald-950/60 dark:text-emerald-300 dark:border-emerald-800/70',
    warning:
      'bg-amber-50 text-amber-800 border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800/70',
    danger:
      'bg-red-50 text-red-800 border-red-200 dark:bg-rose-950/60 dark:text-rose-300 dark:border-rose-800/70',
    info:
      'bg-sky-50 text-sky-800 border-sky-200 dark:bg-sky-950/60 dark:text-sky-300 dark:border-sky-800/70',
    courtesy:
      'bg-violet-50 text-violet-800 border-violet-200 dark:bg-violet-950/60 dark:text-violet-300 dark:border-violet-800/70',
  };

  return (
    <span
      className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-medium border ${styles[variant]} ${className}`}
    >
      {children}
    </span>
  );
}

// ============================================================================
// 6. CABECERA DE PÁGINA CORPORATIVA
// ============================================================================

export interface PageHeaderProps {
  /** Sobretítulo discreto opcional. */
  eyebrow?: string;
  /** Título principal de la vista (h1). */
  title: string;
  /** Descripción breve opcional. */
  subtitle?: string;
  /** Slot derecho para acciones y filtros rápidos. */
  actions?: React.ReactNode;
  /** Clases Tailwind adicionales. */
  className?: string;
}

/**
 * Cabecera estándar de sección con soporte de tema Claro/Oscuro y distribución compacta para 1280px.
 *
 * @param props Propiedades de la cabecera.
 * @example
 * <PageHeader title="Clientes" subtitle="Historial y fichas técnicas" actions={<Button>+ Nuevo</Button>} />
 */
export function PageHeader({
  eyebrow,
  title,
  subtitle,
  actions,
  className = '',
}: PageHeaderProps) {
  return (
    <header
      className={`bg-surface border-b border-border-subtle px-5 py-4 lg:px-6 ${className}`}
    >
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div className="min-w-0">
          {eyebrow && (
            <p className="text-xs font-medium text-brand-primary mb-0.5">
              {eyebrow}
            </p>
          )}
          <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary truncate">
            {title}
          </h1>
          {subtitle && (
            <p className="text-xs sm:text-sm text-ink-secondary mt-0.5">
              {subtitle}
            </p>
          )}
        </div>
        {actions && (
          <div className="flex flex-wrap items-center gap-2 shrink-0">
            {actions}
          </div>
        )}
      </div>
    </header>
  );
}
