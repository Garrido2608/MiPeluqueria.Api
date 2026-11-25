'use client';

import React, { useEffect, useRef, useState } from 'react';

export interface NumericInputProps
  extends Omit<
    React.InputHTMLAttributes<HTMLInputElement>,
    'type' | 'value' | 'onChange' | 'min' | 'max' | 'prefix'
  > {
  /** Valor numérico controlado actual (null o undefined muestra el campo vacío con placeholder). */
  value: number | null | undefined;
  /** Callback disparado al cambiar el valor numérico (retorna null cuando el usuario borra el campo). */
  onValueChange: (value: number | null) => void;
  /** Etiqueta visible opcional. */
  label?: string;
  /** Texto auxiliar opcional debajo del campo. */
  helperText?: string;
  /** Mensaje de validación de error. */
  errorText?: string;
  /** Prefijo visual dentro del input (ej. "$"). */
  prefix?: string;
  /** Sufijo visual dentro del input (ej. "%", "min"). */
  suffix?: string;
  /** Permite dígitos decimales (por defecto true). */
  allowDecimals?: boolean;
  /** Cantidad máxima de decimales permitidos cuando allowDecimals es true (por defecto 2). */
  decimalPlaces?: number;
  /** Límite mínimo permitido (por defecto 0, bloqueando el signo negativo). */
  min?: number;
  /** Límite máximo opcional. */
  max?: number;
  /** Valor retornado al perder foco si el campo quedó vacío (opcional; si se omite mantiene null). */
  emptyValueOnBlur?: number | null;
  /** Selecciona automáticamente el contenido al recibir foco (por defecto true). */
  selectOnFocus?: boolean;
}

function formatNumberForDisplay(value: number | null | undefined): string {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return '';
  }
  return String(value);
}

/**
 * Input numérico controlado que erradica el "0 pegajoso" en montos, porcentajes y cantidades.
 * Permite vaciar completamente el campo mientras se edita, selecciona el texto al enfocar,
 * utiliza JetBrains Mono con `tabular-nums` y bloquea caracteres inválidos o negativos cuando `min >= 0`.
 *
 * @param props Propiedades del input numérico.
 * @example
 * <NumericInput
 *   label="Importe"
 *   prefix="$"
 *   value={amount}
 *   onValueChange={(val) => setAmount(val ?? 0)}
 *   placeholder="0"
 * />
 */
export function NumericInput({
  value,
  onValueChange,
  label,
  helperText,
  errorText,
  prefix,
  suffix,
  allowDecimals = true,
  decimalPlaces = 2,
  min = 0,
  max,
  emptyValueOnBlur,
  selectOnFocus = true,
  placeholder = '0',
  disabled = false,
  readOnly = false,
  className = '',
  id,
  onFocus,
  onBlur,
  onKeyDown,
  ...rest
}: NumericInputProps) {
  const [displayValue, setDisplayValue] = useState<string>(() =>
    formatNumberForDisplay(value)
  );
  const isFocusedRef = useRef<boolean>(false);

  // Sincroniza cambios externos del valor cuando el usuario no está escribiendo activamente
  useEffect(() => {
    if (!isFocusedRef.current) {
      setDisplayValue(formatNumberForDisplay(value));
    }
  }, [value]);

  const allowNegative = min < 0;

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (
      event.key === 'e' ||
      event.key === 'E' ||
      event.key === '+' ||
      (!allowNegative && event.key === '-') ||
      (!allowDecimals && (event.key === '.' || event.key === ','))
    ) {
      event.preventDefault();
      return;
    }
    onKeyDown?.(event);
  };

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const raw = event.target.value.replace(',', '.');

    if (raw === '') {
      setDisplayValue('');
      onValueChange(null);
      return;
    }

    if (allowNegative && raw === '-') {
      setDisplayValue('-');
      onValueChange(null);
      return;
    }

    const pattern = allowDecimals
      ? allowNegative
        ? new RegExp(`^-?\\d*(?:\\.\\d{0,${decimalPlaces}})?$`)
        : new RegExp(`^\\d*(?:\\.\\d{0,${decimalPlaces}})?$`)
      : allowNegative
      ? /^-?\d*$/
      : /^\d*$/;

    if (!pattern.test(raw)) {
      return;
    }

    setDisplayValue(raw);

    if (raw === '.' || raw === '-.') {
      return;
    }

    const parsed = Number(raw);
    if (Number.isNaN(parsed)) {
      return;
    }

    if (max !== undefined && parsed > max) {
      setDisplayValue(String(max));
      onValueChange(max);
      return;
    }

    onValueChange(parsed);
  };

  const handleFocus = (event: React.FocusEvent<HTMLInputElement>) => {
    isFocusedRef.current = true;
    if (selectOnFocus && !readOnly) {
      const target = event.currentTarget;
      requestAnimationFrame(() => {
        target.select();
      });
    }
    onFocus?.(event);
  };

  const handleBlur = (event: React.FocusEvent<HTMLInputElement>) => {
    isFocusedRef.current = false;

    if (displayValue === '' || displayValue === '-' || displayValue === '.' || displayValue === '-.') {
      if (emptyValueOnBlur !== undefined) {
        setDisplayValue(formatNumberForDisplay(emptyValueOnBlur));
        onValueChange(emptyValueOnBlur);
      } else {
        setDisplayValue('');
        onValueChange(null);
      }
      onBlur?.(event);
      return;
    }

    let parsed = Number(displayValue);
    if (Number.isNaN(parsed)) {
      setDisplayValue('');
      onValueChange(null);
    } else {
      if (parsed < min) parsed = min;
      if (max !== undefined && parsed > max) parsed = max;
      setDisplayValue(String(parsed));
      onValueChange(parsed);
    }

    onBlur?.(event);
  };

  return (
    <div className="flex flex-col gap-1.5">
      {label && (
        <label htmlFor={id} className="text-xs font-medium text-ink-secondary">
          {label}
        </label>
      )}
      <div className="relative flex items-center">
        {prefix && (
          <span
            aria-hidden="true"
            className="pointer-events-none absolute left-3 text-xs font-mono font-medium text-ink-secondary select-none"
          >
            {prefix}
          </span>
        )}
        <input
          id={id}
          type="text"
          inputMode={allowDecimals ? 'decimal' : 'numeric'}
          autoComplete="off"
          value={displayValue}
          onChange={handleChange}
          onFocus={handleFocus}
          onBlur={handleBlur}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          disabled={disabled}
          readOnly={readOnly}
          className={`h-10 w-full rounded-lg border font-mono tabular-nums text-sm transition-colors placeholder:text-ink-secondary/60 ${
            prefix ? 'pl-8' : 'px-3'
          } ${suffix ? 'pr-10' : 'pr-3'} ${
            readOnly || disabled
              ? 'bg-elevated text-ink-secondary border-border-subtle cursor-not-allowed'
              : 'bg-surface text-ink-primary border-border-strong focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25'
          } ${errorText ? 'border-red-600 dark:border-rose-500' : ''} ${className}`}
          {...rest}
        />
        {suffix && (
          <span
            aria-hidden="true"
            className="pointer-events-none absolute right-3 text-xs font-mono font-medium text-ink-secondary select-none"
          >
            {suffix}
          </span>
        )}
      </div>
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
