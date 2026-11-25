// filepath: src/components/ui/PrintReceiptModal.tsx
'use client';

import React, { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { Printer } from 'lucide-react';
import { formatCurrency, formatDateTime } from '@/lib/formatters';
import { BrandLogo } from '@/components/ui/BrandLogo';
import { Button, Modal } from '@/components/ui/DesignSystem';

/**
 * Datos estructurados de un comprobante imprimible (ticket POS, acta de arqueo,
 * recibo de liquidación u orden de compra).
 */
export interface PrintableReceiptData {
  /** Título del documento (ej. "Ticket de Venta", "Acta de Arqueo de Caja"). */
  titulo: string;
  /** Número de comprobante formateado. */
  numeroComprobante: string;
  /** Fecha y hora ISO del documento. */
  fechaIso: string;
  /** Nombre del cliente, profesional o responsable. */
  clienteOProfesional: string;
  /** Código ISO de moneda (ej. "ARS"). */
  monedaIso: string;
  /** Renglones principales del detalle. */
  renglones: {
    descripcion: string;
    cantidad?: number;
    monto: number;
  }[];
  /** Desglose adicional opcional (señas, medios de pago, diferencias). */
  pagosODesglose?: {
    etiqueta: string;
    monto: number;
  }[];
  /** Monto total final destacado. */
  totalFinal: number;
  /** Leyenda opcional al pie del documento. */
  pieDePagina?: string;
}

export interface PrintReceiptModalProps {
  /** Datos del comprobante a previsualizar e imprimir. Si es null el modal permanece cerrado. */
  data: PrintableReceiptData | null;
  /** Callback de cierre del modal. */
  onClose: () => void;
  /** Nombre comercial del establecimiento. */
  nombreSalon?: string;
  /** Identificación tributaria del establecimiento. */
  cuitSalon?: string;
}

interface ReceiptSheetContentProps {
  data: PrintableReceiptData;
  nombreSalon: string;
  cuitSalon: string;
}

function ReceiptSheetContent({
  data,
  nombreSalon,
  cuitSalon,
}: ReceiptSheetContentProps) {
  return (
    <div className="bg-white text-slate-900 rounded-lg border border-slate-200 p-5 sm:p-6 space-y-4 print:border-0 print:rounded-none print:p-0">
      {/* Encabezado comercial */}
      <div className="flex items-start justify-between gap-3 border-b border-slate-200 pb-4">
        <BrandLogo
          brandName={nombreSalon}
          subtitle={`CUIT: ${cuitSalon}`}
          size="sm"
          showText
        />
        <div className="text-right shrink-0">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-900">
            {data.titulo}
          </p>
          <p className="font-mono-tabular text-sm font-bold text-slate-900">
            #{data.numeroComprobante}
          </p>
          <p className="font-mono-tabular text-[11px] text-slate-500 mt-0.5">
            {formatDateTime(data.fechaIso)}
          </p>
        </div>
      </div>

      {/* Titular / Responsable */}
      <div className="flex items-center justify-between text-xs bg-slate-50 border border-slate-200/80 rounded-md px-3 py-2 print:bg-transparent print:border-slate-300">
        <span className="text-slate-500 font-medium">Titular / Responsable</span>
        <span className="font-semibold text-slate-900 text-right">
          {data.clienteOProfesional}
        </span>
      </div>

      {/* Tabla de renglones */}
      <div className="border-t border-b border-slate-200 py-2">
        <table className="w-full text-xs border-collapse">
          <thead>
            <tr className="text-[11px] text-slate-500 border-b border-slate-100">
              <th className="text-left font-medium pb-1.5">Detalle</th>
              <th className="text-right font-medium pb-1.5">Importe</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {data.renglones.map((r, i) => (
              <tr key={`${r.descripcion}-${i}`}>
                <td className="py-1.5 pr-3 text-slate-800">
                  {r.cantidad !== undefined && (
                    <span className="font-mono-tabular font-semibold text-slate-600 mr-1.5">
                      {r.cantidad}x
                    </span>
                  )}
                  <span>{r.descripcion}</span>
                </td>
                <td className="py-1.5 text-right font-mono-tabular font-semibold text-slate-900 whitespace-nowrap">
                  {formatCurrency(r.monto, data.monedaIso)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Desglose de señas, pagos o arqueo */}
      {data.pagosODesglose && data.pagosODesglose.length > 0 && (
        <div className="space-y-1.5 text-xs">
          {data.pagosODesglose.map((p, i) => (
            <div
              key={`${p.etiqueta}-${i}`}
              className="flex items-center justify-between gap-2 text-slate-600"
            >
              <span>{p.etiqueta}</span>
              <span className="font-mono-tabular font-medium text-slate-800 whitespace-nowrap">
                {formatCurrency(p.monto, data.monedaIso)}
              </span>
            </div>
          ))}
        </div>
      )}

      {/* Total Final */}
      <div className="flex items-center justify-between pt-3 border-t-2 border-slate-900">
        <span className="text-xs font-bold uppercase tracking-wide text-slate-900">
          Total Neto
        </span>
        <span className="font-mono-tabular text-lg font-bold text-slate-900">
          {formatCurrency(data.totalFinal, data.monedaIso)}
        </span>
      </div>

      {/* Pie de página */}
      {data.pieDePagina && (
        <p className="text-[11px] text-center text-slate-500 pt-2 border-t border-dashed border-slate-200">
          {data.pieDePagina}
        </p>
      )}
    </div>
  );
}

/**
 * Modal agnóstico de vista previa e impresión de comprobantes en 1 sola hoja.
 * Utiliza un portal dedicado en `document.body` junto con reglas `@media print`
 * estrictas para ocultar el backdrop del modal, la cabecera de vista previa,
 * el botón de cierre, el sidebar y cualquier otro elemento de la aplicación.
 *
 * @param props Propiedades del modal de comprobante imprimible.
 * @example
 * <PrintReceiptModal
 *   data={receiptData}
 *   onClose={() => setReceiptData(null)}
 *   nombreSalon="Mi Peluquería Central"
 *   cuitSalon="30-71548963-9"
 * />
 */
export function PrintReceiptModal({
  data,
  onClose,
  nombreSalon = 'Mi Peluquería Central',
  cuitSalon = '30-71548963-9',
}: PrintReceiptModalProps) {
  const [isMounted, setIsMounted] = useState<boolean>(false);

  useEffect(() => {
    setIsMounted(true);
  }, []);

  if (!data) return null;

  const handlePrint = () => {
    if (typeof window !== 'undefined') {
      window.print();
    }
  };

  const printPortal =
    isMounted && typeof document !== 'undefined'
      ? createPortal(
          <div
            id="mipeluqueria-print-portal"
            className="hidden print:block print-only-sheet"
            aria-hidden="true"
          >
            <ReceiptSheetContent
              data={data}
              nombreSalon={nombreSalon}
              cuitSalon={cuitSalon}
            />
          </div>,
          document.body
        )
      : null;

  return (
    <>
      <style>{`
        @media print {
          @page {
            size: auto;
            margin: 10mm;
          }
          body > *:not(#mipeluqueria-print-portal) {
            display: none !important;
          }
          #mipeluqueria-print-portal {
            display: block !important;
            position: static !important;
            width: 100% !important;
            max-width: 170mm !important;
            margin: 0 auto !important;
            padding: 0 !important;
            background: #FFFFFF !important;
            color: #0F172A !important;
            box-shadow: none !important;
            border: none !important;
            page-break-inside: avoid !important;
            break-inside: avoid !important;
          }
        }
      `}</style>

      {printPortal}

      <Modal
        isOpen={Boolean(data)}
        onClose={onClose}
        title={`Vista previa del comprobante #${data.numeroComprobante}`}
        subtitle="Formato optimizado para impresión en una sola hoja o guardado en PDF."
        maxWidthClass="max-w-lg"
      >
        <div className="space-y-4">
          <ReceiptSheetContent
            data={data}
            nombreSalon={nombreSalon}
            cuitSalon={cuitSalon}
          />

          <div className="no-print flex items-center justify-end gap-2 pt-1">
            <Button variant="secondary" size="sm" onClick={onClose}>
              Cerrar
            </Button>
            <Button
              variant="primary"
              size="sm"
              leftIcon={<Printer className="h-4 w-4" aria-hidden="true" />}
              onClick={handlePrint}
            >
              Imprimir / Guardar PDF
            </Button>
          </div>
        </div>
      </Modal>
    </>
  );
}
