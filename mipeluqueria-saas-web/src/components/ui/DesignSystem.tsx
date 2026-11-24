// filepath: src/components/ui/DesignSystem.tsx
'use client';

import React, { useEffect } from 'react';
import Link from 'next/link';
import { useCookieConsent } from '@/context/AppProviders';

// =====================================
// 1. BOTÓN DEL SISTEMA DE DISEÑO
// =====================================
interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'editorial' | 'secondary' | 'danger' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

export function Button({
  children,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  disabled,
  className = '',
  ...props
}: ButtonProps) {
  const base =
    'inline-flex items-center justify-center font-medium transition-colors rounded-md focus:outline-none focus:ring-2 focus:ring-offset-1 disabled:opacity-50 disabled:cursor-not-allowed';

  const variants: Record<NonNullable<ButtonProps['variant']>, string> = {
    primary: 'bg-slate-900 text-white hover:bg-slate-800 focus:ring-slate-900',
    editorial: 'bg-amber-900 text-stone-50 hover:bg-amber-950 focus:ring-amber-900',
    secondary:
      'bg-white text-slate-800 border border-slate-300 hover:bg-slate-50 focus:ring-slate-400',
    danger: 'bg-red-700 text-white hover:bg-red-800 focus:ring-red-700',
    ghost: 'bg-transparent text-slate-700 hover:bg-slate-100 focus:ring-slate-300',
  };

  const sizes: Record<NonNullable<ButtonProps['size']>, string> = {
    sm: 'px-3 py-1.5 text-xs',
    md: 'px-4 py-2 text-sm',
    lg: 'px-6 py-3 text-base',
  };

  return (
    <button
      disabled={disabled || isLoading}
      className={`${base} ${variants[variant]} ${sizes[size]} ${className}`}
      {...props}
    >
      {isLoading ? (
        <span className="inline-flex items-center gap-2">
          <span className="h-4 w-4 border-2 border-current border-t-transparent rounded-full animate-spin" />
          Procesando...
        </span>
      ) : (
        children
      )}
    </button>
  );
}

// =====================================
// 2. INPUT CON BLOQUEO VISUAL PARA CAMPOS RESTRINGIDOS (Ej. StockActual)
// =====================================
interface InputFieldProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
  helperText?: string;
  errorText?: string;
}

export function InputField({
  label,
  helperText,
  errorText,
  readOnly,
  className = '',
  ...props
}: InputFieldProps) {
  return (
    <div className="flex flex-col gap-1.5">
      <label className="text-xs font-semibold uppercase tracking-wider text-slate-700 flex items-center justify-between">
        <span>{label}</span>
        {readOnly && (
          <span className="text-[11px] font-normal text-amber-800 bg-amber-50 px-2 py-0.5 rounded border border-amber-200">
            Campo protegido por Kardex
          </span>
        )}
      </label>
      <input
        readOnly={readOnly}
        className={`w-full rounded-md border px-3.5 py-2 text-sm transition-colors ${
          readOnly
            ? 'bg-slate-100 text-slate-500 border-slate-200 cursor-not-allowed select-none'
            : 'bg-white text-slate-900 border-slate-300 focus:border-slate-900 focus:outline-none'
        } ${errorText ? 'border-red-600' : ''} ${className}`}
        {...props}
      />
      {helperText && !errorText && (
        <span className="text-xs text-slate-500">{helperText}</span>
      )}
      {errorText && (
        <span className="text-xs text-red-600 font-medium">{errorText}</span>
      )}
    </div>
  );
}

// =====================================
// 3. MODAL ACCESIBLE REUTILIZABLE
// =====================================
interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  maxWidthClass?: string;
}

export function Modal({
  isOpen,
  onClose,
  title,
  subtitle,
  children,
  maxWidthClass = 'max-w-xl',
}: ModalProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 p-4 backdrop-blur-[1px]">
      <div
        className={`w-full ${maxWidthClass} rounded-lg bg-white border border-slate-200 shadow-xl overflow-hidden flex flex-col max-h-[90vh]`}
      >
        <div className="flex items-start justify-between border-b border-slate-200 px-6 py-4 bg-slate-50">
          <div>
            <h3 className="text-base font-bold text-slate-900">{title}</h3>
            {subtitle && <p className="text-xs text-slate-600 mt-0.5">{subtitle}</p>}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-slate-400 hover:text-slate-700 text-sm font-bold px-2 py-1"
          >
            X
          </button>
        </div>
        <div className="p-6 overflow-y-auto">{children}</div>
      </div>
    </div>
  );
}

// =====================================
// 4. BANNER DE CONSENTIMIENTO DE COOKIES Y ANALYTICS (GDPR CAPTURA 2)
// =====================================
export function CookieConsentBanner() {
  const { consentGiven, acceptCookies, rejectCookies } = useCookieConsent();

  // Solo inyecta Google Analytics si el usuario aceptó explícitamente
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
    <div className="fixed bottom-0 inset-x-0 z-50 bg-slate-900 text-stone-100 border-t border-slate-700 px-6 py-4 shadow-2xl">
      <div className="max-w-6xl mx-auto flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="text-xs leading-relaxed text-slate-300">
          <strong className="text-white font-semibold">Privacidad y Cookies:</strong>{' '}
          Utilizamos cookies estrictamente necesarias para tu sesión y reserva de turnos.
          No cargamos Google Analytics ni telemetría sin tu consentimiento previo. Consultá
          nuestra{' '}
          <Link href="/legal/privacidad" className="underline text-amber-300 hover:text-amber-200">
            Política de Privacidad
          </Link>{' '}
          y{' '}
          <Link href="/legal/terminos" className="underline text-amber-300 hover:text-amber-200">
            Términos del Servicio
          </Link>
          .
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <Button variant="secondary" size="sm" onClick={rejectCookies}>
            Solo esenciales
          </Button>
          <Button variant="editorial" size="sm" onClick={acceptCookies}>
            Aceptar analíticas
          </Button>
        </div>
      </div>
    </div>
  );
}

// =====================================
// 5. BADGE SEMÁNTICO DE ESTADOS Y ETIQUETAS
// =====================================
interface BadgeProps {
  children: React.ReactNode;
  variant?: 'neutral' | 'success' | 'warning' | 'danger' | 'info';
  className?: string;
}

export function Badge({
  children,
  variant = 'neutral',
  className = '',
}: BadgeProps) {
  const styles: Record<NonNullable<BadgeProps['variant']>, string> = {
    neutral: 'bg-slate-100 text-slate-700 border-slate-200',
    success: 'bg-emerald-50 text-emerald-800 border-emerald-200',
    warning: 'bg-amber-50 text-amber-800 border-amber-200',
    danger: 'bg-red-50 text-red-800 border-red-200',
    info: 'bg-sky-50 text-sky-800 border-sky-200',
  };

  return (
    <span
      className={`inline-flex items-center px-2 py-0.5 rounded text-[11px] font-semibold border ${styles[variant]} ${className}`}
    >
      {children}
    </span>
  );
}

// =====================================
// 6. CABECERA DE PÁGINA CORPORATIVA
// =====================================
interface PageHeaderProps {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
}

export function PageHeader({
  eyebrow,
  title,
  subtitle,
  actions,
}: PageHeaderProps) {
  return (
    <header className="bg-white border-b border-slate-200 px-6 py-5">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          {eyebrow && (
            <p className="text-[11px] uppercase tracking-widest text-amber-900 font-bold">
              {eyebrow}
            </p>
          )}
          <h1 className="text-xl font-bold text-slate-900">{title}</h1>
          {subtitle && (
            <p className="text-xs text-slate-600 mt-0.5">{subtitle}</p>
          )}
        </div>
        {actions && <div className="flex items-center gap-2">{actions}</div>}
      </div>
    </header>
  );
}
