/**
 * Mapa centralizado e inmutable de colores semánticos globales (Design System v1.1.0).
 *
 * Garantiza coherencia cromática absoluta entre Caja Diaria, POS, Dashboard BI
 * (gráficos Recharts y barras), Inventario, Agenda y Diagnóstico API tanto en
 * Modo Claro (Studio Clean) como en Modo Oscuro (Obsidian & Indigo).
 */

export type SemanticToneKey =
  | 'cash'
  | 'digital'
  | 'warning'
  | 'danger'
  | 'courtesy'
  | 'neutral';

export type ThemeMode = 'light' | 'dark';

export interface SemanticColorToken {
  readonly label: string;
  readonly lightHex: string;
  readonly darkHex: string;
  readonly cssVar: string;
  readonly badgeClass: string;
  readonly surfaceClass: string;
  readonly textClass: string;
  readonly solidButtonClass: string;
  readonly softButtonClass: string;
  readonly dotClass: string;
}

export const SEMANTIC_COLORS: Readonly<Record<SemanticToneKey, SemanticColorToken>> = {
  /** Efectivo Físico / Éxito / Caja Abierta / API 200 OK — Esmeralda */
  cash: {
    label: 'Efectivo / Éxito',
    lightHex: '#059669',
    darkHex: '#10B981',
    cssVar: 'var(--semantic-cash)',
    badgeClass:
      'bg-emerald-50 text-emerald-800 border-emerald-200 dark:bg-emerald-950/60 dark:text-emerald-300 dark:border-emerald-800/70',
    surfaceClass:
      'bg-emerald-50/70 border-emerald-200 dark:bg-emerald-950/30 dark:border-emerald-800/60',
    textClass: 'text-emerald-700 dark:text-emerald-400',
    solidButtonClass:
      'bg-emerald-600 text-white hover:bg-emerald-700 focus-visible:ring-emerald-600 dark:bg-emerald-600 dark:hover:bg-emerald-500',
    softButtonClass:
      'bg-emerald-50 text-emerald-800 border border-emerald-200 hover:bg-emerald-100 dark:bg-emerald-950/50 dark:text-emerald-300 dark:border-emerald-800 dark:hover:bg-emerald-900/60',
    dotClass: 'bg-emerald-600 dark:bg-emerald-400',
  },

  /** Digital / Mercado Pago QR / Transferencia / Tarjetas / Info — Azul Cielo */
  digital: {
    label: 'Digital / Info',
    lightHex: '#0284C7',
    darkHex: '#38BDF8',
    cssVar: 'var(--semantic-digital)',
    badgeClass:
      'bg-sky-50 text-sky-800 border-sky-200 dark:bg-sky-950/60 dark:text-sky-300 dark:border-sky-800/70',
    surfaceClass:
      'bg-sky-50/70 border-sky-200 dark:bg-sky-950/30 dark:border-sky-800/60',
    textClass: 'text-sky-700 dark:text-sky-400',
    solidButtonClass:
      'bg-sky-600 text-white hover:bg-sky-700 focus-visible:ring-sky-600 dark:bg-sky-600 dark:hover:bg-sky-500',
    softButtonClass:
      'bg-sky-50 text-sky-800 border border-sky-200 hover:bg-sky-100 dark:bg-sky-950/50 dark:text-sky-300 dark:border-sky-800 dark:hover:bg-sky-900/60',
    dotClass: 'bg-sky-600 dark:bg-sky-400',
  },

  /** Advertencia / Pendiente / Seña / Stock Bajo — Ámbar */
  warning: {
    label: 'Advertencia / Seña',
    lightHex: '#D97706',
    darkHex: '#F59E0B',
    cssVar: 'var(--semantic-warning)',
    badgeClass:
      'bg-amber-50 text-amber-800 border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800/70',
    surfaceClass:
      'bg-amber-50/70 border-amber-200 dark:bg-amber-950/30 dark:border-amber-800/60',
    textClass: 'text-amber-700 dark:text-amber-400',
    solidButtonClass:
      'bg-amber-600 text-white hover:bg-amber-700 focus-visible:ring-amber-600 dark:bg-amber-600 dark:hover:bg-amber-500',
    softButtonClass:
      'bg-amber-50 text-amber-800 border border-amber-200 hover:bg-amber-100 dark:bg-amber-950/50 dark:text-amber-300 dark:border-amber-800 dark:hover:bg-amber-900/60',
    dotClass: 'bg-amber-600 dark:bg-amber-400',
  },

  /** Destructivo / Error / Anular / Baja / Alergias / Stock 0 — Carmesí */
  danger: {
    label: 'Destructivo / Alerta Crítica',
    lightHex: '#DC2626',
    darkHex: '#F43F5E',
    cssVar: 'var(--semantic-danger)',
    badgeClass:
      'bg-red-50 text-red-800 border-red-200 dark:bg-rose-950/60 dark:text-rose-300 dark:border-rose-800/70',
    surfaceClass:
      'bg-red-50/70 border-red-200 dark:bg-rose-950/30 dark:border-rose-800/60',
    textClass: 'text-red-700 dark:text-rose-400',
    solidButtonClass:
      'bg-red-600 text-white hover:bg-red-700 focus-visible:ring-red-600 dark:bg-rose-600 dark:hover:bg-rose-500',
    softButtonClass:
      'bg-red-50 text-red-700 border border-red-200 hover:bg-red-100 dark:bg-rose-950/50 dark:text-rose-300 dark:border-rose-800/80 dark:hover:bg-rose-900/60',
    dotClass: 'bg-red-600 dark:bg-rose-400',
  },

  /** Canje / Cortesía / Tarjeta Secundaria — Violeta */
  courtesy: {
    label: 'Canje / Cortesía',
    lightHex: '#7C3AED',
    darkHex: '#8B5CF6',
    cssVar: 'var(--semantic-courtesy)',
    badgeClass:
      'bg-violet-50 text-violet-800 border-violet-200 dark:bg-violet-950/60 dark:text-violet-300 dark:border-violet-800/70',
    surfaceClass:
      'bg-violet-50/70 border-violet-200 dark:bg-violet-950/30 dark:border-violet-800/60',
    textClass: 'text-violet-700 dark:text-violet-400',
    solidButtonClass:
      'bg-violet-600 text-white hover:bg-violet-700 focus-visible:ring-violet-600 dark:bg-violet-600 dark:hover:bg-violet-500',
    softButtonClass:
      'bg-violet-50 text-violet-800 border border-violet-200 hover:bg-violet-100 dark:bg-violet-950/50 dark:text-violet-300 dark:border-violet-800 dark:hover:bg-violet-900/60',
    dotClass: 'bg-violet-600 dark:bg-violet-400',
  },

  /** Neutro / Inactivo / Informativo Base — Slate */
  neutral: {
    label: 'Neutro',
    lightHex: '#64748B',
    darkHex: '#94A3B8',
    cssVar: 'var(--semantic-neutral)',
    badgeClass:
      'bg-slate-100 text-slate-700 border-slate-200 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700',
    surfaceClass:
      'bg-slate-50 border-slate-200 dark:bg-slate-800/60 dark:border-slate-700',
    textClass: 'text-slate-600 dark:text-slate-400',
    solidButtonClass:
      'bg-slate-700 text-white hover:bg-slate-800 focus-visible:ring-slate-600 dark:bg-slate-600 dark:hover:bg-slate-500',
    softButtonClass:
      'bg-slate-100 text-slate-700 border border-slate-200 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:border-slate-700 dark:hover:bg-slate-700',
    dotClass: 'bg-slate-500 dark:bg-slate-400',
  },
} as const;

/**
 * Paleta diferenciada para desgloses de medios de pago en gráficos Recharts
 * manteniendo siempre Efectivo en Esmeralda y Digital/QR en Azul Cielo.
 */
export const PAYMENT_CHART_COLORS: Readonly<{
  efectivo: { light: string; dark: string };
  mercadoPago: { light: string; dark: string };
  transferencia: { light: string; dark: string };
  tarjeta: { light: string; dark: string };
  otros: { light: string; dark: string };
}> = {
  efectivo: { light: '#059669', dark: '#10B981' },
  mercadoPago: { light: '#0284C7', dark: '#38BDF8' },
  transferencia: { light: '#4F46E5', dark: '#818CF8' },
  tarjeta: { light: '#7C3AED', dark: '#A78BFA' },
  otros: { light: '#64748B', dark: '#94A3B8' },
};

/**
 * Devuelve el código hexadecimal de un token semántico según el tema activo.
 *
 * @param tone Clave semántica del token.
 * @param mode Tema activo ('light' | 'dark').
 * @returns Código hexadecimal listo para SVG / Recharts.
 */
export function getSemanticHex(tone: SemanticToneKey, mode: ThemeMode = 'light'): string {
  const token = SEMANTIC_COLORS[tone];
  return mode === 'dark' ? token.darkHex : token.lightHex;
}

/**
 * Clasifica un medio de pago por nombre o bandera `esEfectivoFisico` y retorna
 * el color hexadecimal coherente para Donut Charts, barras y badges.
 *
 * @param nombre Nombre visible del medio de pago (ej. "Efectivo", "Mercado Pago QR").
 * @param mode Tema activo ('light' | 'dark').
 * @param esEfectivoFisico Bandera explícita desde el backend si está disponible.
 * @returns Objeto con tono semántico y color hexadecimal correspondiente.
 */
export function getPaymentMethodColor(
  nombre: string,
  mode: ThemeMode = 'light',
  esEfectivoFisico?: boolean
): { tone: SemanticToneKey; hex: string } {
  const normalized = (nombre || '').toLowerCase();

  if (esEfectivoFisico === true || normalized.includes('efectivo') || normalized.includes('cash')) {
    return {
      tone: 'cash',
      hex: mode === 'dark' ? PAYMENT_CHART_COLORS.efectivo.dark : PAYMENT_CHART_COLORS.efectivo.light,
    };
  }

  if (
    normalized.includes('mercado') ||
    normalized.includes('qr') ||
    normalized.includes('mp')
  ) {
    return {
      tone: 'digital',
      hex:
        mode === 'dark'
          ? PAYMENT_CHART_COLORS.mercadoPago.dark
          : PAYMENT_CHART_COLORS.mercadoPago.light,
    };
  }

  if (normalized.includes('transfer')) {
    return {
      tone: 'digital',
      hex:
        mode === 'dark'
          ? PAYMENT_CHART_COLORS.transferencia.dark
          : PAYMENT_CHART_COLORS.transferencia.light,
    };
  }

  if (
    normalized.includes('tarjeta') ||
    normalized.includes('crédito') ||
    normalized.includes('credito') ||
    normalized.includes('débito') ||
    normalized.includes('debito')
  ) {
    return {
      tone: 'courtesy',
      hex: mode === 'dark' ? PAYMENT_CHART_COLORS.tarjeta.dark : PAYMENT_CHART_COLORS.tarjeta.light,
    };
  }

  if (normalized.includes('canje') || normalized.includes('cortes')) {
    return {
      tone: 'courtesy',
      hex: getSemanticHex('courtesy', mode),
    };
  }

  return {
    tone: 'neutral',
    hex: mode === 'dark' ? PAYMENT_CHART_COLORS.otros.dark : PAYMENT_CHART_COLORS.otros.light,
  };
}
