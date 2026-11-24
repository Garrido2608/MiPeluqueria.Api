// filepath: src/components/ui/PrintReceiptModal.tsx
'use client';

import React from 'react';
import { formatCurrency, formatDateTime } from '@/lib/formatters';
import { Button, Modal } from '@/components/ui/DesignSystem';

export interface PrintableReceiptData {
  titulo: string;
  numeroComprobante: string;
  fechaIso: string;
  clienteOProfesional: string;
  monedaIso: string;
  renglones: {
    descripcion: string;
    cantidad?: number;
    monto: number;
  }[];
  pagosODesglose?: {
    etiqueta: string;
    monto: number;
  }[];
  totalFinal: number;
  pieDePagina?: string;
}

export function PrintReceiptModal({
  data,
  onClose,
  nombreSalon = 'Mi Peluquería & Barbería SaaS',
  cuitSalon = '30-71548963-9',
}: {
  data: PrintableReceiptData | null;
  onClose: () => void;
  nombreSalon?: string;
  cuitSalon?: string;
}) {
  if (!data) return null;

  const handlePrint = () => {
    if (typeof window !== 'undefined') {
      window.print();
    }
  };

  return (
    <Modal
      isOpen={!!data}
      onClose={onClose}
      title={`Vista Previa de Comprobante — ${data.numeroComprobante}`}
      maxWidthClass="max-w-md"
    >
      <div className="space-y-4">
        <div className="border border-dashed border-slate-400 p-5 bg-white font-mono text-xs space-y-3 text-slate-900">
          <div className="text-center border-b border-dashed border-slate-300 pb-3">
            <p className="font-bold text-sm uppercase">{nombreSalon}</p>
            <p className="text-[11px] text-slate-500">CUIT: {cuitSalon}</p>
            <p className="font-bold mt-1">
              {data.titulo} #{data.numeroComprobante}
            </p>
            <p className="text-[11px]">{formatDateTime(data.fechaIso)}</p>
          </div>

          <div>
            <p>
              <strong>Titular:</strong> {data.clienteOProfesional}
            </p>
          </div>

          <div className="border-t border-b border-dashed border-slate-300 py-2 space-y-1">
            {data.renglones.map((r, i) => (
              <div key={i} className="flex justify-between">
                <span>
                  {r.cantidad ? `${r.cantidad}x ` : ''}
                  {r.descripcion}
                </span>
                <span className="font-bold">
                  {formatCurrency(r.monto, data.monedaIso)}
                </span>
              </div>
            ))}
          </div>

          {data.pagosODesglose && data.pagosODesglose.length > 0 && (
            <div className="space-y-1 text-[11px] text-slate-600">
              {data.pagosODesglose.map((p, i) => (
                <div key={i} className="flex justify-between">
                  <span>{p.etiqueta}:</span>
                  <span>{formatCurrency(p.monto, data.monedaIso)}</span>
                </div>
              ))}
            </div>
          )}

          <div className="flex justify-between text-sm font-bold pt-2 border-t border-slate-900">
            <span>TOTAL NETO:</span>
            <span>{formatCurrency(data.totalFinal, data.monedaIso)}</span>
          </div>

          {data.pieDePagina && (
            <p className="text-[10px] text-center text-slate-500 pt-2">
              {data.pieDePagina}
            </p>
          )}
        </div>

        <div className="no-print flex justify-end gap-2">
          <Button variant="secondary" size="sm" onClick={onClose}>
            Cerrar
          </Button>
          <Button variant="primary" size="sm" onClick={handlePrint}>
            Imprimir / Guardar PDF
          </Button>
        </div>
      </div>
    </Modal>
  );
} 