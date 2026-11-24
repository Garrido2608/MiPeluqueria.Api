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
