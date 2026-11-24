// filepath: src/components/ui/ConfirmModal.tsx
'use client';

import React from 'react';
import { Button, Modal } from '@/components/ui/DesignSystem';

export interface ConfirmModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  subtitle?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isLoading?: boolean;
  variant?: 'danger' | 'default';
  children?: React.ReactNode;
  className?: string;
}

/**
 * Diálogo modal reutilizable de confirmación para operaciones críticas o destructivas,
 * diseñado por composición (`children`) y sin acoplamiento al dominio.
 *
 * @param {ConfirmModalProps} props Configuración de apertura, variante semántica y callbacks.
 * @example
 * ```tsx
 * <ConfirmModal
 *   isOpen={open}
 *   onClose={() => setOpen(false)}
 *   onConfirm={handleConfirm}
 *   title="Confirmar baja"
 *   variant="danger"
 *   isLoading={isPending}
 * >
 *   <p>¿Estás seguro de continuar con esta acción?</p>
 * </ConfirmModal>
 * ```
 */
export function ConfirmModal({
  isOpen,
  onClose,
  onConfirm,
  title,
  subtitle,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  isLoading = false,
  variant = 'default',
  children,
  className = '',
}: ConfirmModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      onClose={isLoading ? () => undefined : onClose}
      title={title}
      subtitle={subtitle}
      maxWidthClass="max-w-md"
    >
      <div className={`space-y-4 ${className}`}>
        {children && (
          <div className="text-xs sm:text-sm text-slate-700 leading-relaxed">
            {children}
          </div>
        )}

        <div className="flex items-center justify-end gap-2 pt-2 border-t border-slate-100">
          <Button
            type="button"
            variant="secondary"
            size="sm"
            disabled={isLoading}
            onClick={onClose}
          >
            {cancelLabel}
          </Button>
          <Button
            type="button"
            variant={variant === 'danger' ? 'danger' : 'primary'}
            size="sm"
            isLoading={isLoading}
            onClick={onConfirm}
          >
            {confirmLabel}
          </Button>
        </div>
      </div>
    </Modal>
  );
}
