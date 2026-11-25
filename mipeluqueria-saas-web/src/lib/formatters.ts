export function formatCurrency(
  amount: number | null | undefined,
  currencyIso: string = 'ARS'
): string {
  const safeAmount =
    typeof amount === 'number' && Number.isFinite(amount) ? amount : 0;
  const safeCurrency =
    currencyIso && currencyIso.trim().length === 3
      ? currencyIso.trim().toUpperCase()
      : 'ARS';

  try {
    return new Intl.NumberFormat('es-AR', {
      style: 'currency',
      currency: safeCurrency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(safeAmount);
  } catch {
    return `$ ${safeAmount.toFixed(2)}`;
  }
}

export function formatDateTime(isoString?: string | null): string {
  if (!isoString) return '-';
  const date = new Date(isoString);
  if (Number.isNaN(date.getTime())) return '-';

  return new Intl.DateTimeFormat('es-AR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

export function formatDateOnly(isoString?: string | null): string {
  if (!isoString) return '-';
  const date = new Date(isoString);
  if (Number.isNaN(date.getTime())) return '-';

  return new Intl.DateTimeFormat('es-AR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(date);
}

export function formatTimeOnly(isoStringOrTimeSpan?: string | null): string {
  if (!isoStringOrTimeSpan) return '-';
  const trimmed = isoStringOrTimeSpan.trim();

  // Soporte directo para TimeSpan de .NET ("HH:mm:ss" o "HH:mm")
  if (/^\d{2}:\d{2}(:\d{2})?$/.test(trimmed)) {
    return trimmed.slice(0, 5);
  }

  const date = new Date(trimmed);
  if (Number.isNaN(date.getTime())) return '-';

  return new Intl.DateTimeFormat('es-AR', {
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

export function formatPercentage(
  value?: number | null,
  fractionDigits: number = 1
): string {
  const safeValue =
    typeof value === 'number' && Number.isFinite(value) ? value : 0;
  return `${safeValue.toFixed(fractionDigits)}%`;
}

export function calcularHorasHastaTurno(fechaHoraInicioIso: string): number {
  const inicioMs = new Date(fechaHoraInicioIso).getTime();
  if (Number.isNaN(inicioMs)) return 0;
  const ahoraMs = Date.now();
  return (inicioMs - ahoraMs) / (1000 * 60 * 60);
}

/**
 * Resume un header User-Agent técnico en una etiqueta legible de navegador y sistema operativo
 * (ej. "Chrome · Windows", "Safari · iOS", "Cliente Node").
 *
 * @param userAgent Cadena User-Agent cruda registrada en el historial de sesiones.
 * @returns Etiqueta corta y legible para humanos.
 */
export function formatUserAgentSummary(userAgent?: string | null): string {
  const raw = (userAgent || '').trim();
  if (!raw || raw === '-') {
    return 'Desconocido';
  }

  const ua = raw.toLowerCase();

  // Clientes CLI / automatización / pruebas
  if (ua.includes('playwright')) {
    return 'Playwright · Automatizado';
  }
  if (ua.includes('postman')) {
    return 'Postman';
  }
  if (ua.includes('insomnia')) {
    return 'Insomnia';
  }
  if (ua.includes('curl/')) {
    return 'cURL';
  }
  if (
    ua.includes('node-fetch') ||
    ua.includes('undici') ||
    ua.includes('axios/') ||
    /\bnode\b/.test(ua)
  ) {
    return 'Cliente Node';
  }

  // Detección de navegador (orden estricto por tokens compartidos)
  let browser: string | null = null;
  if (ua.includes('edg/') || ua.includes('edga/') || ua.includes('edgios/')) {
    browser = 'Edge';
  } else if (ua.includes('opr/') || ua.includes('opera')) {
    browser = 'Opera';
  } else if (ua.includes('firefox/') || ua.includes('fxios/')) {
    browser = 'Firefox';
  } else if (ua.includes('headlesschrome')) {
    browser = 'Chrome Headless';
  } else if (ua.includes('chrome/') || ua.includes('crios/')) {
    browser = 'Chrome';
  } else if (ua.includes('safari/')) {
    browser = 'Safari';
  }

  // Detección de sistema operativo
  let os: string | null = null;
  if (
    ua.includes('iphone') ||
    ua.includes('ipad') ||
    ua.includes('ipod') ||
    ua.includes('cpu iphone os')
  ) {
    os = 'iOS';
  } else if (ua.includes('android')) {
    os = 'Android';
  } else if (ua.includes('windows')) {
    os = 'Windows';
  } else if (ua.includes('mac os x') || ua.includes('macintosh')) {
    os = 'macOS';
  } else if (ua.includes('linux')) {
    os = 'Linux';
  }

  if (browser && os) {
    return `${browser} · ${os}`;
  }
  if (browser) {
    return browser;
  }
  if (os) {
    return os;
  }

  return raw.length > 36 ? `${raw.slice(0, 35)}…` : raw;
}
