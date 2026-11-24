// filepath: src/app/(public)/legal/terminos/page.tsx
import React from 'react';
import Link from 'next/link';

export default function TerminosServicioPage() {
  return (
    <main className="min-h-screen bg-[#FAF8F5] text-stone-900 py-12 px-4">
      <article className="max-w-3xl mx-auto bg-white border border-stone-200 rounded-lg p-8 space-y-5 text-sm leading-relaxed">
        <div className="border-b border-stone-200 pb-4">
          <p className="text-xs font-bold uppercase tracking-widest text-amber-900">
            Condiciones Contractuales de Uso y Reservas
          </p>
          <h1 className="text-2xl font-bold mt-1">
            Términos y Condiciones del Servicio (ToS)
          </h1>
        </div>
        <p>
          <strong>1. Reservas, Tolerancia y Cancelaciones:</strong> Las reservas realizadas a
          través del portal público bloquean la agenda del profesional seleccionado. Las
          cancelaciones sin cargo deben efectuarse respetando las horas mínimas de anticipación
          configuradas por el salón (<code>HorasAnticipacionCancelacion</code>). La tolerancia
          máxima de espera en el local está regida por <code>MinutosToleranciaEspera</code>.
        </p>
        <p>
          <strong>2. Señas Online y Pasarelas de Pago:</strong> Cuando el salón requiere seña
          porcentual (<code>PorcentajeSenaReserva &gt; 0</code>), el pago se procesa de forma
          externa y segura a través de Mercado Pago o Stripe y se acredita automáticamente vía
          Webhook criptográfico firmado (<code>x-signature</code>).
        </p>
        <p>
          <strong>3. Declaración de Sensibilidad Capilar:</strong> El cliente se compromete a
          informar a su estilista cualquier antecedente de alergia o sensibilidad cutánea antes de
          la aplicación de productos químicos de gabinete.
        </p>
        <div className="pt-4 border-t border-stone-200">
          <Link href="/reservar" className="text-amber-900 font-semibold underline">
            Volver al Portal de Reservas
          </Link>
        </div>
      </article>
    </main>
  );
}