// filepath: src/app/(public)/reservar/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import {
  PeluqueroDto,
  ReservaPublicaResponseDto,
  ServicioDto,
} from '@/types/api.types';
import { portalPublicoService } from '@/services/api.services';
import { useCatalogos } from '@/context/AppProviders';
import { extractErrorMessage, extractPagedItems } from '@/lib/apiHelpers';
import { formatCurrency } from '@/lib/formatters';
import { AsyncStateGuard, Button, InputField } from '@/components/ui';

export default function ReservaPublicaWizardPage() {
  const { configSalon, catalogos } = useCatalogos();
  const moneda = configSalon?.moneda || 'ARS';

  const [step, setStep] = useState<1 | 2 | 3 | 4>(1);
  const [servicios, setServicios] = useState<ServicioDto[]>([]);
  const [peluqueros, setPeluqueros] = useState<PeluqueroDto[]>([]);
  const [slotsDisponibles, setSlotsDisponibles] = useState<string[]>([]);
  const [mapaSlotPeluquero, setMapaSlotPeluquero] = useState<
    Record<string, number>
  >({});

  // Selecciones del usuario en el Wizard
  const [busquedaServicio, setBusquedaServicio] = useState<string>('');
  const [categoriaFiltroId, setCategoriaFiltroId] = useState<number>(0);
  const [serviciosSeleccionadosIds, setServiciosSeleccionadosIds] = useState<
    number[]
  >([]);
  // 0 = "Cualquier profesional disponible"
  const [peluqueroSeleccionadoId, setPeluqueroSeleccionadoId] = useState<
    number | null
  >(0);
  const [fechaSeleccionada, setFechaSeleccionada] = useState<string>(() => {
    const hoy = new Date();
    return hoy.toISOString().split('T')[0];
  });
  const [horaSeleccionada, setHoraSeleccionada] = useState<string>('');

  // Datos del Paso 4
  const [nombreCliente, setNombreCliente] = useState<string>('');
  const [apellidoCliente, setApellidoCliente] = useState<string>('');
  const [telefonoCliente, setTelefonoCliente] = useState<string>('');
  const [emailCliente, setEmailCliente] = useState<string>('');
  const [observaciones, setObservaciones] = useState<string>('');
  const [pasarelaPreferida, setPasarelaPreferida] = useState<
    'MercadoPago' | 'Stripe'
  >('MercadoPago');
  const [aceptaLegales, setAceptaLegales] = useState<boolean>(false);

  const [cargandoInicial, setCargandoInicial] = useState<boolean>(true);
  const [cargandoSlots, setCargandoSlots] = useState<boolean>(false);
  const [enviandoReserva, setEnviandoReserva] = useState<boolean>(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [reservaConfirmada, setReservaConfirmada] =
    useState<ReservaPublicaResponseDto | null>(null);

  const cargarCatalogosPublicos = useCallback(async () => {
    setCargandoInicial(true);
    setErrorMsg(null);
    try {
      const [servRes, pelRes] = await Promise.all([
        portalPublicoService.getCatalogoServicios(),
        portalPublicoService.getPeluqueros(),
      ]);
      if (servRes.success) {
        setServicios(
          extractPagedItems<ServicioDto>(servRes.data).filter((s) => s.activo)
        );
      }
      if (pelRes.success) {
        setPeluqueros(
          extractPagedItems<PeluqueroDto>(pelRes.data).filter((p) => p.activo)
        );
      }
    } catch (err: unknown) {
      setErrorMsg(extractErrorMessage(err, 'No se pudo cargar el catálogo.'));
    } finally {
      setCargandoInicial(false);
    }
  }, []);

  useEffect(() => {
    void cargarCatalogosPublicos();
  }, [cargarCatalogosPublicos]);

  useEffect(() => {
    if (
      step === 3 &&
      peluqueroSeleccionadoId !== null &&
      fechaSeleccionada &&
      serviciosSeleccionadosIds.length > 0
    ) {
      setCargandoSlots(true);
      setHoraSeleccionada('');

      if (peluqueroSeleccionadoId > 0) {
        portalPublicoService
          .getDisponibilidadCompleta(
            peluqueroSeleccionadoId,
            fechaSeleccionada,
            serviciosSeleccionadosIds
          )
          .then((res) => {
            if (res.success && res.data) {
              const slotsActivos = res.data.slots?.length
                ? res.data.slots
                    .filter((s) => s.disponible)
                    .map((s) => s.horaInicioTexto)
                : res.data.horariosDisponibles || [];
              setSlotsDisponibles(slotsActivos);
            } else {
              setSlotsDisponibles([]);
            }
          })
          .catch(() => setSlotsDisponibles([]))
          .finally(() => setCargandoSlots(false));
      } else {
        Promise.all(
          peluqueros.map(async (p) => {
            try {
              const r = await portalPublicoService.getDisponibilidadCompleta(
                p.id,
                fechaSeleccionada,
                serviciosSeleccionadosIds
              );
              const slotsActivos = r.data?.slots?.length
                ? r.data.slots
                    .filter((s) => s.disponible)
                    .map((s) => s.horaInicioTexto)
                : r.data?.horariosDisponibles || [];
              return { peluqueroId: p.id, slots: slotsActivos };
            } catch {
              return { peluqueroId: p.id, slots: [] as string[] };
            }
          })
        )
          .then((resultados) => {
            const mapa: Record<string, number> = {};
            resultados.forEach((item) => {
              item.slots.forEach((slot) => {
                if (!mapa[slot]) {
                  mapa[slot] = item.peluqueroId;
                }
              });
            });
            setMapaSlotPeluquero(mapa);
            setSlotsDisponibles(Object.keys(mapa).sort());
          })
          .finally(() => setCargandoSlots(false));
      }
    }
  }, [
    step,
    peluqueroSeleccionadoId,
    fechaSeleccionada,
    serviciosSeleccionadosIds,
    peluqueros,
  ]);

  const serviciosElegidos = useMemo(
    () =>
      servicios.filter((s) => serviciosSeleccionadosIds.includes(s.id)),
    [servicios, serviciosSeleccionadosIds]
  );

  const montoTotalEstimado = useMemo(
    () => serviciosElegidos.reduce((acc, item) => acc + item.precio, 0),
    [serviciosElegidos]
  );

  const duracionTotalMinutos = useMemo(
    () =>
      serviciosElegidos.reduce((acc, item) => acc + item.duracionMinutos, 0),
    [serviciosElegidos]
  );

  const toggleServicio = (id: number) => {
    setServiciosSeleccionadosIds((prev) =>
      prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
    );
  };

  const handleConfirmarReserva = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);

    const peluqueroFinalId =
      peluqueroSeleccionadoId === 0
        ? mapaSlotPeluquero[horaSeleccionada] || peluqueros[0]?.id
        : peluqueroSeleccionadoId;

    if (
      !peluqueroFinalId ||
      !horaSeleccionada ||
      serviciosSeleccionadosIds.length === 0
    ) {
      setErrorMsg(
        'Por favor completá los servicios, el profesional y el horario.'
      );
      return;
    }

    if (!aceptaLegales) {
      setErrorMsg(
        'Debés aceptar los Términos del Servicio y la Política de Privacidad.'
      );
      return;
    }

    setEnviandoReserva(true);
    try {
      const fechaHoraIso = `${fechaSeleccionada}T${horaSeleccionada}:00`;
      const res = await portalPublicoService.reservar({
        nombreCliente: nombreCliente.trim(),
        apellidoCliente: apellidoCliente.trim(),
        telefonoCliente: telefonoCliente.trim(),
        emailCliente: emailCliente.trim() || undefined,
        peluqueroId: peluqueroFinalId,
        fechaHoraInicio: fechaHoraIso,
        serviciosIds: serviciosSeleccionadosIds,
        observaciones: observaciones.trim() || undefined,
        pasarelaPreferida,
      });

      if (res.success && res.data) {
        setReservaConfirmada(res.data);
        if (res.data.requiereSena && res.data.checkoutPagoUrl) {
          window.location.href = res.data.checkoutPagoUrl;
        }
      } else {
        setErrorMsg(res.message || 'No fue posible registrar la reserva.');
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setErrorMsg(
        customErr?.message || 'Error al procesar la reserva online.'
      );
    } finally {
      setEnviandoReserva(false);
    }
  };

  if (reservaConfirmada) {
    return (
      <div className="min-h-screen bg-[#FAF8F5] text-stone-900 py-12 px-4">
        <div className="max-w-lg mx-auto bg-white border border-stone-200 rounded-lg p-8 shadow-sm">
          <span className="text-xs font-bold uppercase tracking-widest text-emerald-800 bg-emerald-50 px-3 py-1 rounded">
            Turno Registrado #{reservaConfirmada.turnoId}
          </span>
          <h1 className="text-2xl font-bold mt-4 text-stone-900">
            ¡Te esperamos en {configSalon?.nombreComercial || 'el salón'}!
          </h1>
          <p className="text-sm text-stone-600 mt-2">
            Estado actual: <strong>{reservaConfirmada.estadoTurno}</strong>.
            Recibirás un recordatorio automático por WhatsApp 24 hs y 2 hs antes
            de tu turno.
          </p>

          <div className="mt-6 border-t border-stone-200 pt-4 space-y-2 text-sm">
            <div className="flex justify-between">
              <span className="text-stone-500">Fecha y Hora:</span>
              <span className="font-semibold">
                {fechaSeleccionada} a las {horaSeleccionada} hs
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-stone-500">Duración estimada:</span>
              <span className="font-semibold">{duracionTotalMinutos} min</span>
            </div>
            <div className="flex justify-between">
              <span className="text-stone-500">Total estimado:</span>
              <span className="font-mono font-bold">
                {formatCurrency(reservaConfirmada.montoTotalEstimado, moneda)}
              </span>
            </div>
            {reservaConfirmada.requiereSena && reservaConfirmada.montoSena && (
              <div className="flex justify-between text-amber-900 font-semibold pt-2 border-t border-stone-100">
                <span>Seña requerida:</span>
                <span className="font-mono">
                  {formatCurrency(reservaConfirmada.montoSena, moneda)}
                </span>
              </div>
            )}
          </div>

          {reservaConfirmada.requiereSena &&
            reservaConfirmada.checkoutPagoUrl && (
              <a
                href={reservaConfirmada.checkoutPagoUrl}
                className="mt-6 block w-full text-center bg-amber-900 text-white py-3 rounded-md font-semibold text-sm hover:bg-amber-950"
              >
                Ir a Pagar Seña Online ({pasarelaPreferida})
              </a>
            )}

          <div className="mt-6 flex justify-between items-center text-xs text-stone-500">
            <Link
              href="/mi-cuenta/turnos"
              className="underline hover:text-stone-800"
            >
              Ver en Mis Turnos
            </Link>
            <button
              type="button"
              onClick={() => {
                setReservaConfirmada(null);
                setServiciosSeleccionadosIds([]);
                setStep(1);
              }}
              className="underline hover:text-stone-800"
            >
              Realizar otra reserva
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#FAF8F5] text-stone-900 pb-28">
      <header className="border-b border-stone-200 bg-white px-4 py-5">
        <div className="max-w-3xl mx-auto flex items-center justify-between">
          <div>
            <p className="text-[11px] uppercase tracking-widest text-stone-500 font-semibold">
              Reserva Online Directa •{' '}
              {configSalon?.direccion || 'Córdoba, Argentina'}
            </p>
            <h1 className="text-xl font-bold text-stone-900 mt-0.5">
              {configSalon?.nombreComercial || 'Estudio Capilar & Barbería'}
            </h1>
          </div>
          <div className="flex items-center gap-3 text-xs font-medium">
            <Link
              href="/mi-cuenta/turnos"
              className="text-stone-700 hover:underline"
            >
              Mis Turnos
            </Link>
            <Link
              href="/login"
              className="px-3 py-1.5 rounded border border-stone-300 hover:bg-stone-100"
            >
              Ingresar
            </Link>
          </div>
        </div>
      </header>

      <div className="max-w-3xl mx-auto px-4 pt-6">
        <div className="grid grid-cols-4 gap-2 text-xs font-semibold">
          {[
            { num: 1, label: '1. Servicios' },
            { num: 2, label: '2. Profesional' },
            { num: 3, label: '3. Fecha y Hora' },
            { num: 4, label: '4. Confirmación' },
          ].map((item) => (
            <button
              key={item.num}
              type="button"
              disabled={item.num > step}
              onClick={() => setStep(item.num as 1 | 2 | 3 | 4)}
              className={`py-2 px-3 rounded text-left border transition-colors ${
                step === item.num
                  ? 'bg-stone-900 text-white border-stone-900'
                  : step > item.num
                  ? 'bg-stone-200 text-stone-800 border-stone-300'
                  : 'bg-white text-stone-400 border-stone-200'
              }`}
            >
              {item.label}
            </button>
          ))}
        </div>

        {errorMsg && (
          <div className="mt-4 p-3.5 rounded bg-red-50 border border-red-200 text-xs text-red-700 font-medium">
            {errorMsg}
          </div>
        )}

        {step === 1 && (
          <section className="mt-6 space-y-4">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
              <h2 className="text-base font-bold">
                Elegí los servicios para tu visita
              </h2>
              <input
                type="search"
                placeholder="Buscar corte, colorimetría, barba..."
                value={busquedaServicio}
                onChange={(e) => setBusquedaServicio(e.target.value)}
                className="rounded border border-stone-300 bg-white px-3 py-1.5 text-sm w-full sm:w-64"
              />
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                type="button"
                onClick={() => setCategoriaFiltroId(0)}
                className={`px-3 py-1 rounded-full text-xs font-semibold border ${
                  categoriaFiltroId === 0
                    ? 'bg-stone-900 text-white border-stone-900'
                    : 'bg-white text-stone-700 border-stone-300'
                }`}
              >
                Todas las categorías
              </button>
              {(catalogos?.categoriasServicio || []).map((cat) => (
                <button
                  key={cat.id}
                  type="button"
                  onClick={() => setCategoriaFiltroId(cat.id)}
                  className={`px-3 py-1 rounded-full text-xs font-semibold border ${
                    categoriaFiltroId === cat.id
                      ? 'bg-stone-900 text-white border-stone-900'
                      : 'bg-white text-stone-700 border-stone-300'
                  }`}
                >
                  {cat.nombre}
                </button>
              ))}
            </div>

            <AsyncStateGuard
              isLoading={cargandoInicial}
              isError={Boolean(errorMsg && (servicios ?? []).length === 0)}
              errorMessage={errorMsg || undefined}
              isEmpty={
                (servicios ?? []).filter(
                  (s) =>
                    (categoriaFiltroId === 0 ||
                      s.categoriaId === categoriaFiltroId) &&
                    s.nombre
                      .toLowerCase()
                      .includes(busquedaServicio.toLowerCase())
                ).length === 0
              }
              loadingText="Cargando catálogo de servicios..."
              emptyTitle="Sin servicios disponibles"
              emptyDescription="No se encontraron servicios activos con el filtro actual."
              onRetry={cargarCatalogosPublicos}
            >
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {(servicios ?? [])
                  .filter(
                    (s) =>
                      (categoriaFiltroId === 0 ||
                        s.categoriaId === categoriaFiltroId) &&
                      s.nombre
                        .toLowerCase()
                        .includes(busquedaServicio.toLowerCase())
                  )
                  .map((servicio) => {
                    const seleccionado = serviciosSeleccionadosIds.includes(
                      servicio.id
                    );
                    return (
                      <div
                        key={servicio.id}
                        onClick={() => toggleServicio(servicio.id)}
                        className={`cursor-pointer rounded-lg border p-4 transition-all ${
                          seleccionado
                            ? 'border-amber-900 bg-amber-50/40 ring-1 ring-amber-900'
                            : 'border-stone-200 bg-white hover:border-stone-400'
                        }`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <div>
                            <h3 className="font-bold text-sm text-stone-900">
                              {servicio.nombre}
                            </h3>
                            {servicio.descripcion && (
                              <p className="text-xs text-stone-500 mt-1">
                                {servicio.descripcion}
                              </p>
                            )}
                          </div>
                          <span className="font-mono text-sm font-bold text-stone-900 shrink-0">
                            {formatCurrency(servicio.precio, moneda)}
                          </span>
                        </div>
                        <div className="mt-3 flex items-center justify-between text-xs text-stone-500">
                          <span>Duración: {servicio.duracionMinutos} min</span>
                          <span className="font-semibold text-amber-900">
                            {seleccionado ? '✓ Seleccionado' : '+ Agregar'}
                          </span>
                        </div>
                      </div>
                    );
                  })}
              </div>
            </AsyncStateGuard>
          </section>
        )}

        {step === 2 && (
          <section className="mt-6 space-y-4">
            <h2 className="text-base font-bold">
              Seleccioná tu estilista o barbero
            </h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div
                onClick={() => setPeluqueroSeleccionadoId(0)}
                className={`cursor-pointer rounded-lg border p-4 flex items-center gap-3.5 ${
                  peluqueroSeleccionadoId === 0
                    ? 'border-amber-900 bg-amber-50/50 ring-1 ring-amber-900'
                    : 'border-stone-200 bg-white hover:border-stone-400'
                }`}
              >
                <div className="h-11 w-11 rounded-full flex items-center justify-center bg-amber-900 text-white font-bold text-xs shrink-0">
                  ★
                </div>
                <div className="flex-1">
                  <h3 className="text-sm font-bold text-stone-900">
                    Cualquier profesional disponible
                  </h3>
                  <p className="text-xs text-stone-500">
                    Unifica todos los horarios libres y asigna automáticamente
                    al estilista disponible.
                  </p>
                </div>
              </div>

              {peluqueros.map((pel) => {
                const activo = peluqueroSeleccionadoId === pel.id;
                return (
                  <div
                    key={pel.id}
                    onClick={() => setPeluqueroSeleccionadoId(pel.id)}
                    className={`cursor-pointer rounded-lg border p-4 flex items-center gap-3.5 ${
                      activo
                        ? 'border-stone-900 bg-stone-100 ring-1 ring-stone-900'
                        : 'border-stone-200 bg-white hover:border-stone-400'
                    }`}
                  >
                    <div
                      className="h-11 w-11 rounded-full flex items-center justify-center text-white font-bold text-sm shrink-0"
                      style={{
                        backgroundColor: pel.colorAgendaHex || '#18181B',
                      }}
                    >
                      {pel.nombre.charAt(0)}
                      {pel.apellido.charAt(0)}
                    </div>
                    <div className="flex-1">
                      <h3 className="text-sm font-bold text-stone-900">
                        {pel.nombre} {pel.apellido}
                      </h3>
                      <p className="text-xs text-stone-500">
                        {pel.especialidadesNombres?.join(' • ') ||
                          'Estilista Profesional'}
                      </p>
                    </div>
                  </div>
                );
              })}
            </div>
            <div className="flex justify-between pt-4">
              <Button variant="secondary" onClick={() => setStep(1)}>
                ← Volver a Servicios
              </Button>
              <Button
                variant="editorial"
                disabled={peluqueroSeleccionadoId === null}
                onClick={() => setStep(3)}
              >
                Ver Horarios Disponibles →
              </Button>
            </div>
          </section>
        )}

        {step === 3 && (
          <section className="mt-6 space-y-4">
            <h2 className="text-base font-bold">
              Elegí fecha y horario ({duracionTotalMinutos} min requeridos)
            </h2>
            <div className="max-w-xs">
              <InputField
                label="Fecha del turno"
                type="date"
                min={new Date().toISOString().split('T')[0]}
                value={fechaSeleccionada}
                onChange={(e) => setFechaSeleccionada(e.target.value)}
              />
            </div>

            <AsyncStateGuard
              isLoading={cargandoSlots}
              isEmpty={(slotsDisponibles ?? []).length === 0}
              loadingText="Calculando disponibilidad real en agenda..."
              emptyTitle="Sin horarios libres en esta fecha"
              emptyDescription={`No hay bloques continuos de ${duracionTotalMinutos} minutos libres en esta fecha. Probá con otra fecha o estilista.`}
            >
              <div className="grid grid-cols-3 sm:grid-cols-6 gap-2.5 pt-2">
                {(slotsDisponibles ?? []).map((slot) => (
                  <button
                    key={slot}
                    type="button"
                    onClick={() => setHoraSeleccionada(slot)}
                    className={`py-2.5 px-3 rounded border font-mono text-xs font-bold transition-colors ${
                      horaSeleccionada === slot
                        ? 'bg-stone-900 text-white border-stone-900'
                        : 'bg-white text-stone-800 border-stone-300 hover:bg-stone-100'
                    }`}
                  >
                    {slot} hs
                  </button>
                ))}
              </div>
            </AsyncStateGuard>

            <div className="flex justify-between pt-4">
              <Button variant="secondary" onClick={() => setStep(2)}>
                ← Cambiar Profesional
              </Button>
              <Button
                variant="editorial"
                disabled={!horaSeleccionada}
                onClick={() => setStep(4)}
              >
                Continuar a Mis Datos →
              </Button>
            </div>
          </section>
        )}

        {step === 4 && (
          <form
            onSubmit={handleConfirmarReserva}
            className="mt-6 space-y-4 bg-white p-6 rounded-lg border border-stone-200"
          >
            <h2 className="text-base font-bold">Tus datos de contacto</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <InputField
                label="Nombre *"
                required
                value={nombreCliente}
                onChange={(e) => setNombreCliente(e.target.value)}
              />
              <InputField
                label="Apellido *"
                required
                value={apellidoCliente}
                onChange={(e) => setApellidoCliente(e.target.value)}
              />
              <InputField
                label="WhatsApp / Teléfono *"
                required
                value={telefonoCliente}
                onChange={(e) => setTelefonoCliente(e.target.value)}
                helperText="Para enviarte el recordatorio 24h y 2h antes."
              />
              <InputField
                label="Correo Electrónico"
                type="email"
                value={emailCliente}
                onChange={(e) => setEmailCliente(e.target.value)}
              />
            </div>

            <InputField
              label="Notas u observaciones para tu estilista (opcional)"
              value={observaciones}
              onChange={(e) => setObservaciones(e.target.value)}
            />

            {(configSalon?.porcentajeSenaReserva ?? 0) > 0 && (
              <div className="p-3.5 rounded bg-amber-50 border border-amber-200 text-xs text-amber-950 space-y-2">
                <p className="font-semibold">
                  Este salón solicita una seña del{' '}
                  {configSalon?.porcentajeSenaReserva}% (
                  {formatCurrency(
                    (montoTotalEstimado *
                      (configSalon?.porcentajeSenaReserva || 0)) /
                      100,
                    moneda
                  )}
                  ) para confirmar el turno.
                </p>
                <div className="flex items-center gap-4">
                  <label className="inline-flex items-center gap-1.5 font-medium">
                    <input
                      type="radio"
                      name="pasarela"
                      checked={pasarelaPreferida === 'MercadoPago'}
                      onChange={() => setPasarelaPreferida('MercadoPago')}
                    />
                    Mercado Pago
                  </label>
                  <label className="inline-flex items-center gap-1.5 font-medium">
                    <input
                      type="radio"
                      name="pasarela"
                      checked={pasarelaPreferida === 'Stripe'}
                      onChange={() => setPasarelaPreferida('Stripe')}
                    />
                    Tarjeta Internacional (Stripe)
                  </label>
                </div>
              </div>
            )}

            <label className="flex items-start gap-2.5 text-xs text-stone-600 pt-2">
              <input
                type="checkbox"
                checked={aceptaLegales}
                onChange={(e) => setAceptaLegales(e.target.checked)}
                className="mt-0.5"
              />
              <span>
                Acepto los{' '}
                <Link
                  href="/legal/terminos"
                  className="underline font-semibold text-stone-900"
                >
                  Términos de Servicio
                </Link>{' '}
                (incluyendo la política de cancelación con{' '}
                {configSalon?.horasAnticipacionCancelacion ?? 24} hs de
                anticipación) y la{' '}
                <Link
                  href="/legal/privacidad"
                  className="underline font-semibold text-stone-900"
                >
                  Política de Privacidad
                </Link>
                .
              </span>
            </label>

            <div className="flex justify-between pt-4 border-t border-stone-200">
              <Button
                type="button"
                variant="secondary"
                onClick={() => setStep(3)}
              >
                ← Cambiar Horario
              </Button>
              <Button
                type="submit"
                variant="editorial"
                isLoading={enviandoReserva}
              >
                Confirmar Reserva ({formatCurrency(montoTotalEstimado, moneda)})
              </Button>
            </div>
          </form>
        )}
      </div>

      {serviciosSeleccionadosIds.length > 0 && (
        <div className="fixed bottom-0 inset-x-0 bg-stone-900 text-white px-4 py-3.5 shadow-xl z-40">
          <div className="max-w-3xl mx-auto flex items-center justify-between">
            <div>
              <p className="text-xs text-stone-300">
                {serviciosSeleccionadosIds.length} servicio(s) •{' '}
                {duracionTotalMinutos} min totales
              </p>
              <p className="text-base font-mono font-bold">
                {formatCurrency(montoTotalEstimado, moneda)}
              </p>
            </div>
            {step === 1 && (
              <Button variant="editorial" onClick={() => setStep(2)}>
                Elegir Profesional →
              </Button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}