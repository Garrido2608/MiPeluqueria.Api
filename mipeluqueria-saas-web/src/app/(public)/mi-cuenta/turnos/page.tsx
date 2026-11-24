// filepath: src/app/(public)/mi-cuenta/turnos/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
import Link from 'next/link';
import { EstadoTurnoEnum, TurnoDto } from '@/types/api.types';
import { turnosService } from '@/services/api.services';
import { useAuth, useCatalogos } from '@/context/AppProviders';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import {
  calcularHorasHastaTurno,
  formatCurrency,
  formatDateTime,
} from '@/lib/formatters';
import { AsyncStateGuard, Button, Modal } from '@/components/ui';

function generarLinkGoogleCalendar(
  turno: TurnoDto,
  nombreSalon: string,
  direccionSalon: string
): string {
  const toGCalDate = (iso: string) =>
    new Date(iso).toISOString().replace(/-|:|\.\d\d\d/g, '');
  const inicio = toGCalDate(turno.fechaHoraInicio);
  const fin = turno.fechaHoraFin
    ? toGCalDate(turno.fechaHoraFin)
    : toGCalDate(
        new Date(
          new Date(turno.fechaHoraInicio).getTime() + 45 * 60 * 1000
        ).toISOString()
      );
  const serviciosTexto =
    turno.servicios?.map((s) => s.servicioNombre).join(' + ') ||
    'Turno de Peluquería';
  const params = new URLSearchParams({
    action: 'TEMPLATE',
    text: `${nombreSalon} — ${serviciosTexto}`,
    dates: `${inicio}/${fin}`,
    details: `Profesional: ${turno.peluqueroNombreCompleto} | Turno #${turno.id}`,
    location: direccionSalon,
  });
  return `https://calendar.google.com/calendar/render?${params.toString()}`;
}

export default function MisTurnosClientePage() {
  const { user } = useAuth();
  const { configSalon, catalogos } = useCatalogos();
  const [tab, setTab] = useState<'proximos' | 'historial'>('proximos');
  const [turnos, setTurnos] = useState<TurnoDto[]>([]);
  const [cargando, setCargando] = useState<boolean>(true);
  const [errorTurnos, setErrorTurnos] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);

  const [turnoACancelar, setTurnoACancelar] = useState<TurnoDto | null>(null);
  const [motivoCancelacionId, setMotivoCancelacionId] = useState<number>(1);
  const [observacionesCancelacion, setObservacionesCancelacion] =
    useState<string>('');
  const [procesando, setProcesando] = useState<boolean>(false);
  const [feedback, setFeedback] = useState<string | null>(null);

  const clienteId = user?.clienteId || 1;
  const horasMinimasCancelacion =
    configSalon?.horasAnticipacionCancelacion ?? 24;
  const moneda = configSalon?.moneda || 'ARS';
  const nombreSalon =
    configSalon?.nombreComercial || 'Mi Peluquería & Barbería';
  const direccionSalon =
    configSalon?.direccion || 'Av. Colón 1250, Córdoba, Argentina';

  const cargarMisTurnos = useCallback(async () => {
    setCargando(true);
    setErrorTurnos(null);
    try {
      const res = await turnosService.getPorCliente(clienteId);
      if (res.success) {
        setTurnos(extractPagedItems<TurnoDto>(res.data));
      } else {
        setTurnos([]);
      }
    } catch (err) {
      setTurnos([]);
      setErrorTurnos({
        message: extractErrorMessage(
          err,
          'No se pudieron obtener tus turnos registrados.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
    } finally {
      setCargando(false);
    }
  }, [clienteId]);

  useEffect(() => {
    cargarMisTurnos();
  }, [cargarMisTurnos]);

  const confirmarCancelacion = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!turnoACancelar) return;
    setProcesando(true);
    try {
      const res = await turnosService.cambiarEstado(turnoACancelar.id, {
        nuevoEstadoId: EstadoTurnoEnum.Cancelado,
        motivoCancelacionId,
        observaciones:
          observacionesCancelacion || 'Cancelado por el cliente desde Mi Cuenta',
      });
      if (res.success) {
        setFeedback(
          `El turno #${turnoACancelar.id} fue cancelado correctamente.`
        );
        setTurnoACancelar(null);
        await cargarMisTurnos();
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setFeedback(customErr?.message || 'No se pudo cancelar el turno.');
    } finally {
      setProcesando(false);
    }
  };

  const turnosProximos = turnos.filter((t) =>
    [
      EstadoTurnoEnum.Pendiente,
      EstadoTurnoEnum.Confirmado,
      EstadoTurnoEnum.EnProceso,
    ].includes(t.estadoId)
  );

  const turnosHistorial = turnos.filter((t) =>
    [
      EstadoTurnoEnum.Completado,
      EstadoTurnoEnum.Cancelado,
      EstadoTurnoEnum.NoAsistio,
    ].includes(t.estadoId)
  );

  const listaActiva = tab === 'proximos' ? turnosProximos : turnosHistorial;

  return (
    <div className="min-h-screen bg-[#FAF8F5] text-stone-900 py-8 px-4">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-stone-200 pb-4">
          <div>
            <h1 className="text-xl font-bold">Mis Turnos</h1>
            <p className="text-xs text-stone-500">
              Sucursal: {direccionSalon} • Cancelación sin cargo hasta{' '}
              {horasMinimasCancelacion} hs antes.
            </p>
          </div>
          <div className="flex gap-2">
            <Link
              href="/mi-cuenta/perfil"
              className="px-3 py-1.5 rounded border border-stone-300 bg-white text-xs font-semibold hover:bg-stone-100"
            >
              Mi Perfil y Clave
            </Link>
            <Button
              variant={tab === 'proximos' ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setTab('proximos')}
            >
              Próximos ({turnosProximos.length})
            </Button>
            <Button
              variant={tab === 'historial' ? 'primary' : 'secondary'}
              size="sm"
              onClick={() => setTab('historial')}
            >
              Historial ({turnosHistorial.length})
            </Button>
          </div>
        </div>

        {feedback && (
          <div className="p-3 rounded bg-stone-900 text-white text-xs flex justify-between items-center">
            <span>{feedback}</span>
            <button type="button" onClick={() => setFeedback(null)}>
              ✕
            </button>
          </div>
        )}

        <AsyncStateGuard
          isLoading={cargando}
          isError={Boolean(errorTurnos)}
          errorMessage={errorTurnos?.message}
          correlationId={errorTurnos?.correlationId}
          isEmpty={(listaActiva ?? []).length === 0}
          loadingText="Cargando tus turnos..."
          emptyTitle="Sin turnos en esta sección"
          emptyDescription="Todavía no registrás turnos activos o históricos en este listado."
          onRetry={cargarMisTurnos}
        >
          <div className="space-y-3">
            {(listaActiva ?? []).map((turno) => {
              const horasRestantes = calcularHorasHastaTurno(
                turno.fechaHoraInicio
              );
              const puedeCancelar =
                tab === 'proximos' && horasRestantes >= horasMinimasCancelacion;

              return (
                <div
                  key={turno.id}
                  className="bg-white border border-stone-200 rounded-lg p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4"
                >
                  <div className="space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-bold uppercase px-2.5 py-0.5 rounded bg-stone-100 text-stone-800">
                        {turno.estadoNombre}
                      </span>
                      <span className="text-xs text-stone-500 font-mono">
                        Turno #{turno.id}
                      </span>
                    </div>
                    <h3 className="text-base font-bold text-stone-900">
                      {formatDateTime(turno.fechaHoraInicio)} hs
                    </h3>
                    <p className="text-xs text-stone-600">
                      Profesional:{' '}
                      <strong>{turno.peluqueroNombreCompleto}</strong> •{' '}
                      {direccionSalon}
                    </p>
                    <p className="text-xs text-stone-500">
                      Servicios:{' '}
                      {(turno.servicios ?? [])
                        .map((s) => s.servicioNombre)
                        .join(', ') || 'Servicio general'}
                    </p>
                  </div>

                  <div className="flex flex-col sm:items-end gap-2">
                    <span className="font-mono text-base font-bold">
                      {formatCurrency(turno.montoTotalEstimado, moneda)}
                    </span>
                    {tab === 'proximos' && (
                      <div className="flex flex-wrap gap-2">
                        <a
                          href={generarLinkGoogleCalendar(
                            turno,
                            nombreSalon,
                            direccionSalon
                          )}
                          target="_blank"
                          rel="noreferrer"
                          className="px-3 py-1.5 rounded border border-stone-300 bg-stone-50 text-stone-800 text-xs font-semibold hover:bg-stone-100"
                        >
                          📅 Agregar a Google Calendar
                        </a>
                        {puedeCancelar ? (
                          <Button
                            variant="danger"
                            size="sm"
                            onClick={() => setTurnoACancelar(turno)}
                          >
                            Cancelar Turno
                          </Button>
                        ) : (
                          <span className="text-[11px] text-amber-800 bg-amber-50 px-2.5 py-1 rounded border border-amber-200">
                            Faltan menos de {horasMinimasCancelacion} hs
                          </span>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </AsyncStateGuard>
      </div>

      <Modal
        isOpen={!!turnoACancelar}
        onClose={() => setTurnoACancelar(null)}
        title={`Cancelar Turno #${turnoACancelar?.id}`}
        subtitle="Por favor indicanos el motivo de la cancelación."
      >
        <form onSubmit={confirmarCancelacion} className="space-y-4">
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold uppercase text-slate-700">
              Motivo de cancelación *
            </label>
            <select
              value={motivoCancelacionId}
              onChange={(e) => setMotivoCancelacionId(Number(e.target.value))}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            >
              {(
                catalogos?.motivosCancelacion || [
                  { id: 1, nombre: 'Imprevisto personal' },
                  { id: 2, nombre: 'Cambio de horario laboral' },
                ]
              ).map((m) => (
                <option key={m.id} value={m.id}>
                  {m.nombre || m.descripcion}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold uppercase text-slate-700">
              Observaciones adicionales
            </label>
            <textarea
              rows={3}
              value={observacionesCancelacion}
              onChange={(e) => setObservacionesCancelacion(e.target.value)}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setTurnoACancelar(null)}
            >
              Volver
            </Button>
            <Button type="submit" variant="danger" isLoading={procesando}>
              Confirmar Cancelación
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}