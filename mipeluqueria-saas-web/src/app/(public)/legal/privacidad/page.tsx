// filepath: src/app/(public)/legal/privacidad/page.tsx
import React from 'react';
import Link from 'next/link';

export default function PoliticaPrivacidadPage() {
  return (
    <main className="min-h-screen bg-[#FAF8F5] text-stone-900 py-12 px-4">
      <article className="max-w-3xl mx-auto bg-white border border-stone-200 rounded-lg p-8 space-y-5 text-sm leading-relaxed">
        <div className="border-b border-stone-200 pb-4">
          <p className="text-xs font-bold uppercase tracking-widest text-amber-900">
            Cumplimiento Normativo Ley 25.326 (Argentina) & GDPR
          </p>
          <h1 className="text-2xl font-bold mt-1">
            Política de Privacidad y Protección de Datos
          </h1>
        </div>
        <p>
          <strong>1. Datos Recolectados:</strong> Recolectamos únicamente nombre, apellido,
          teléfono móvil (para el envío de recordatorios automáticos de turnos 24 hs y 2 hs antes
          mediante <code>RecordatorioTurnosBackgroundService</code>), correo electrónico, fecha de
          nacimiento (para beneficios del mes de cumpleaños) y fichas técnicas capilares
          (fórmulas de colorimetría y notas de sensibilidad o alergias en el cuero cabelludo).
        </p>
        <p>
          <strong>2. Aislamiento Multi-Tenant y Seguridad:</strong> Todos los registros se
          encuentran aislados lógicamente por salón (<code>TenantId</code> y{' '}
          <code>SucursalId</code>), las contraseñas se almacenan encriptadas mediante hash BCrypt
          y jamás se comparten con terceros ajenos a la operatoria de tu reserva.
        </p>
        <p>
          <strong>3. Cookies y Telemetría:</strong> No ejecutamos scripts de Google Analytics ni
          cookies publicitarias sin tu consentimiento explícito previo otorgado en el banner de
          privacidad.
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