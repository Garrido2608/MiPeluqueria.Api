'use client';

import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Check, ChevronDown, Search, X } from 'lucide-react';

export interface SearchComboboxOption<T extends string | number = string | number> {
  /** Valor único de la opción. */
  value: T;
  /** Texto principal visible y buscable. */
  label: string;
  /** Texto secundario opcional (ej. teléfono, fecha o código). */
  subtitle?: string;
  /** Palabras clave adicionales para búsqueda sin afectar la presentación. */
  keywords?: string;
  /** Deshabilita la selección de esta opción puntual. */
  disabled?: boolean;
}

export interface SearchComboboxProps<T extends string | number = string | number> {
  /** Lista de opciones disponibles para filtrar. */
  options: ReadonlyArray<SearchComboboxOption<T>>;
  /** Valor seleccionado actualmente. */
  value: T | null | undefined;
  /** Callback disparado al elegir o limpiar una opción. */
  onChange: (value: T | null, option: SearchComboboxOption<T> | null) => void;
  /** Etiqueta visible opcional del campo. */
  label?: string;
  /** Placeholder mostrado cuando no hay selección ni búsqueda. */
  placeholder?: string;
  /** Mensaje mostrado cuando el filtro no arroja resultados. */
  emptyMessage?: string;
  /** Permite limpiar la selección con el botón X (por defecto true). */
  clearable?: boolean;
  /** Deshabilita la interacción del selector. */
  disabled?: boolean;
  /** Mensaje de validación de error. */
  errorText?: string;
  /** Acción opcional al pie del menú desplegable (ej. "Crear nuevo registro"). */
  footerAction?: React.ReactNode;
  /** Clases Tailwind adicionales para el contenedor raíz. */
  className?: string;
  /** Identificador HTML opcional para accesibilidad. */
  id?: string;
}

/**
 * Selector filtrable por texto con navegación por teclado, soporte genérico `<T>`
 * y compatibilidad total con Modo Claro y Modo Oscuro.
 *
 * @param props Propiedades del combobox.
 * @example
 * <SearchCombobox
 *   label="Seleccionar registro"
 *   options={[{ value: 1, label: 'Ana Gómez', subtitle: '351-555-0101' }]}
 *   value={selectedId}
 *   onChange={(val) => setSelectedId(val)}
 * />
 */
export function SearchCombobox<T extends string | number = string | number>({
  options,
  value,
  onChange,
  label,
  placeholder = 'Buscar y seleccionar...',
  emptyMessage = 'Sin resultados para tu búsqueda.',
  clearable = true,
  disabled = false,
  errorText,
  footerAction,
  className = '',
  id,
}: SearchComboboxProps<T>) {
  const [isOpen, setIsOpen] = useState<boolean>(false);
  const [query, setQuery] = useState<string>('');
  const [highlightedIndex, setHighlightedIndex] = useState<number>(0);

  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const selectedOption = useMemo(
    () => options.find((opt) => opt.value === value) ?? null,
    [options, value]
  );

  const filteredOptions = useMemo(() => {
    const trimmed = query.trim().toLowerCase();
    if (!trimmed) return options;
    return options.filter((opt) => {
      const haystack = `${opt.label} ${opt.subtitle ?? ''} ${opt.keywords ?? ''}`.toLowerCase();
      return haystack.includes(trimmed);
    });
  }, [options, query]);

  useEffect(() => {
    setHighlightedIndex(0);
  }, [query, isOpen]);

  useEffect(() => {
    const handleOutsideClick = (event: MouseEvent) => {
      if (
        containerRef.current &&
        !containerRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
        setQuery('');
      }
    };
    document.addEventListener('mousedown', handleOutsideClick);
    return () => document.removeEventListener('mousedown', handleOutsideClick);
  }, []);

  const handleSelect = (option: SearchComboboxOption<T>) => {
    if (option.disabled) return;
    onChange(option.value, option);
    setQuery('');
    setIsOpen(false);
  };

  const handleClear = (event: React.MouseEvent<HTMLButtonElement>) => {
    event.stopPropagation();
    onChange(null, null);
    setQuery('');
    inputRef.current?.focus();
  };

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (disabled) return;

    if (!isOpen && (event.key === 'ArrowDown' || event.key === 'Enter')) {
      setIsOpen(true);
      return;
    }

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      setHighlightedIndex((prev) =>
        filteredOptions.length === 0 ? 0 : (prev + 1) % filteredOptions.length
      );
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      setHighlightedIndex((prev) =>
        filteredOptions.length === 0
          ? 0
          : (prev - 1 + filteredOptions.length) % filteredOptions.length
      );
    } else if (event.key === 'Enter' && isOpen) {
      event.preventDefault();
      const target = filteredOptions[highlightedIndex];
      if (target && !target.disabled) {
        handleSelect(target);
      }
    } else if (event.key === 'Escape') {
      setIsOpen(false);
      setQuery('');
    }
  };

  const displayedInputValue = isOpen
    ? query
    : selectedOption
    ? selectedOption.label
    : '';

  return (
    <div ref={containerRef} className={`relative flex flex-col gap-1.5 ${className}`}>
      {label && (
        <label htmlFor={id} className="text-xs font-medium text-ink-secondary">
          {label}
        </label>
      )}

      <div className="relative flex items-center">
        <Search
          className="pointer-events-none absolute left-3 h-4 w-4 text-ink-secondary"
          aria-hidden="true"
        />
        <input
          ref={inputRef}
          id={id}
          type="text"
          role="combobox"
          aria-expanded={isOpen}
          aria-autocomplete="list"
          disabled={disabled}
          value={displayedInputValue}
          placeholder={selectedOption ? selectedOption.label : placeholder}
          onFocus={() => {
            if (!disabled) {
              setIsOpen(true);
            }
          }}
          onChange={(event) => {
            setQuery(event.target.value);
            if (!isOpen) setIsOpen(true);
          }}
          onKeyDown={handleKeyDown}
          className={`h-10 w-full rounded-lg border pl-9 pr-14 text-sm transition-colors placeholder:text-ink-secondary/70 ${
            disabled
              ? 'bg-elevated text-ink-secondary border-border-subtle cursor-not-allowed'
              : 'bg-surface text-ink-primary border-border-strong focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25'
          } ${errorText ? 'border-red-600 dark:border-rose-500' : ''}`}
        />

        <div className="absolute right-2 flex items-center gap-1">
          {clearable && selectedOption && !disabled && (
            <button
              type="button"
              onClick={handleClear}
              aria-label="Limpiar selección"
              className="inline-flex h-6 w-6 items-center justify-center rounded-md text-ink-secondary hover:text-ink-primary hover:bg-elevated transition-colors"
            >
              <X className="h-3.5 w-3.5" aria-hidden="true" />
            </button>
          )}
          <button
            type="button"
            tabIndex={-1}
            disabled={disabled}
            onClick={() => {
              if (disabled) return;
              setIsOpen((prev) => !prev);
              if (!isOpen) inputRef.current?.focus();
            }}
            aria-label="Desplegar opciones"
            className="inline-flex h-6 w-6 items-center justify-center rounded-md text-ink-secondary hover:text-ink-primary transition-colors"
          >
            <ChevronDown
              className={`h-4 w-4 transition-transform ${isOpen ? 'rotate-180' : ''}`}
              aria-hidden="true"
            />
          </button>
        </div>
      </div>

      {isOpen && !disabled && (
        <div
          role="listbox"
          className="absolute left-0 right-0 top-full z-40 mt-1.5 rounded-xl border border-border-subtle bg-surface shadow-xl overflow-hidden"
        >
          <ul className="max-h-60 overflow-y-auto py-1">
            {filteredOptions.length === 0 ? (
              <li className="px-3.5 py-3 text-xs text-ink-secondary text-center">
                {emptyMessage}
              </li>
            ) : (
              filteredOptions.map((option, index) => {
                const isSelected = option.value === value;
                const isHighlighted = index === highlightedIndex;

                return (
                  <li key={String(option.value)} role="option" aria-selected={isSelected}>
                    <button
                      type="button"
                      disabled={option.disabled}
                      onMouseEnter={() => setHighlightedIndex(index)}
                      onClick={() => handleSelect(option)}
                      className={`w-full px-3.5 py-2 text-left text-sm flex items-center justify-between gap-2 transition-colors ${
                        option.disabled
                          ? 'opacity-45 cursor-not-allowed'
                          : isHighlighted
                          ? 'bg-elevated text-ink-primary'
                          : 'text-ink-primary hover:bg-elevated/70'
                      }`}
                    >
                      <div className="min-w-0 flex flex-col">
                        <span className="font-medium truncate">{option.label}</span>
                        {option.subtitle && (
                          <span className="text-xs text-ink-secondary truncate">
                            {option.subtitle}
                          </span>
                        )}
                      </div>
                      {isSelected && (
                        <Check
                          className="h-4 w-4 text-brand-primary shrink-0"
                          aria-hidden="true"
                        />
                      )}
                    </button>
                  </li>
                );
              })
            )}
          </ul>
          {footerAction && (
            <div className="border-t border-border-subtle bg-elevated/50 px-3 py-2">
              {footerAction}
            </div>
          )}
        </div>
      )}

      {errorText && (
        <span className="text-xs text-red-600 dark:text-rose-400 font-medium">
          {errorText}
        </span>
      )}
    </div>
  );
}
