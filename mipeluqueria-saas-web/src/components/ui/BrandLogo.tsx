'use client';

import React, { useState } from 'react';
import { Scissors } from 'lucide-react';

export interface BrandLogoProps {
  /** Ruta de la imagen del logotipo (por defecto '/logo.png', pensado para trazo negro). */
  src?: string;
  /** Nombre comercial visible junto al logotipo. */
  brandName?: string;
  /** Subtítulo opcional (ej. nombre de sucursal o leyenda breve). */
  subtitle?: string;
  /** Tamaño visual del isotipo. */
  size?: 'sm' | 'md' | 'lg';
  /** Muestra el nombre comercial al lado del isotipo. */
  showText?: boolean;
  /** Invierte el color del texto acompañante para fondos oscuros (ej. Sidebar). */
  invertText?: boolean;
  /** Clases Tailwind adicionales para el contenedor raíz. */
  className?: string;
}

/**
 * Componente de identidad visual agnóstico preparado para un logotipo de trazo negro.
 * Monta siempre la imagen sobre una placa clara dedicada (`bg-white`) con anillo sutil
 * para mantener contraste impecable en Sidebar oscuro, Modo Oscuro e impresión.
 * Si `/logo.png` aún no existe en `public/`, muestra un monograma minimalista con ícono.
 *
 * @param props Propiedades del logotipo.
 * @example
 * <BrandLogo brandName="Mi Peluquería Central" invertText size="md" />
 */
export function BrandLogo({
  src = '/logo.png',
  brandName = 'Mi Peluquería',
  subtitle,
  size = 'md',
  showText = true,
  invertText = false,
  className = '',
}: BrandLogoProps) {
  const [imageFailed, setImageFailed] = useState<boolean>(false);

  const boxSizes: Record<NonNullable<BrandLogoProps['size']>, string> = {
    sm: 'h-8 w-8 p-1 rounded-lg',
    md: 'h-10 w-10 p-1.5 rounded-xl',
    lg: 'h-12 w-12 p-2 rounded-xl',
  };

  const iconSizes: Record<NonNullable<BrandLogoProps['size']>, string> = {
    sm: 'h-4 w-4',
    md: 'h-5 w-5',
    lg: 'h-6 w-6',
  };

  const titleClass = invertText
    ? 'text-white'
    : 'text-ink-primary';

  const subtitleClass = invertText
    ? 'text-slate-400'
    : 'text-ink-secondary';

  return (
    <div className={`inline-flex items-center gap-2.5 min-w-0 ${className}`}>
      {/* Placa clara obligatoria para garantizar visibilidad del logotipo de trazo negro */}
      <div
        className={`${boxSizes[size]} bg-white shadow-sm ring-1 ring-slate-200/80 flex items-center justify-center shrink-0 overflow-hidden`}
      >
        {!imageFailed ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={src}
            alt={brandName}
            className="h-full w-full object-contain"
            onError={() => setImageFailed(true)}
          />
        ) : (
          <Scissors
            className={`${iconSizes[size]} text-slate-900`}
            aria-hidden="true"
          />
        )}
      </div>

      {showText && (
        <div className="min-w-0 flex flex-col">
          <span
            className={`text-sm font-semibold tracking-tight truncate ${titleClass}`}
          >
            {brandName}
          </span>
          {subtitle && (
            <span className={`text-[11px] truncate ${subtitleClass}`}>
              {subtitle}
            </span>
          )}
        </div>
      )}
    </div>
  );
}
