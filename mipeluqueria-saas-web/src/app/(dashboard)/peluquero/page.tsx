// filepath: src/app/(dashboard)/peluquero/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
import {
  BloqueoHorarioDto,
  ClienteDto,
  ComisionConfigDto,
  EstadoTurnoEnum,
  FichaTecnicaDto,
  HorarioLaboralDto,
  LiquidacionDto,
  TurnoDto,
} from '@/types/api.types';
import {
  bloqueosService,
  clientesService,
  comisionesService,
  fichasTecnicasService,
  liquidacionesService,
  peluquerosService,
  recetasServicioService,
  turnosService,
} from '@/services/api.services';
import { useAuth, useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import { uploadImageToCloudinary } from '@/lib/cloudinary';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
} from '@/lib/apiHelpers';
import {
  formatCurrency,
  formatDateOnly,
  formatDateTime,
} from '@/lib/formatters';
import {
  AsyncStateGuard,
  Button,
  InputField,
  Modal,
} from '@/components/ui';
import {
  PrintableReceiptData,
  PrintReceiptModal,
} from '@/components/ui/PrintReceiptModal';

const DIAS_SEMANA = [
  'Domingo',
  'Lunes',
  'Martes',
  'Miércoles',
  'Jueves',
  'Viernes',
  'Sábado',
];

export default function PortalPeluqueroPage() {
  const { user } = useAuth();
  const { configSalon } = useCatalogos();
  const moneda = configSalon?.moneda || 'ARS';
  const miPeluqueroId = user?.peluqueroId || 1;

  const [activeTab, setActiveTab] = useState<
    'agenda' | 'fichas' | 'comisiones' | 'horarios'
  >('agenda');
  const [feedback, setFeedback] = useState<string | null>(null);

  // 1. Estado Mi Agenda de Hoy
  const [fechaAgenda, setFechaAgenda] = useState<string>(
    () => new Date().toISOString().split('T')[0]
  );
  const [misTurnos, setMisTurnos] = useState<TurnoDto[]>([]);
  const [cargandoAgenda, setCargandoAgenda] = useState<boolean>(false);
  const [errorAgenda, setErrorAgenda] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);

  // 2. Estado Fichas Técnicas Capilares
  const [busquedaCliente, setBusquedaCliente] = useState<string>('');
  const [clientesEncontrados, setClientesEncontrados] = useState<ClienteDto[]>(
    []
  );
  const [clienteSeleccionado, setClienteSeleccionado] =
    useState<ClienteDto | null>(null);
  const [fichasCliente, setFichasCliente] = useState<FichaTecnicaDto[]>([]);
  const [modalFichaOpen, setModalFichaOpen] = useState<boolean>(false);
  const [fichaEditando, setFichaEditando] = useState<FichaTecnicaDto | null>(
    null
  );
  const [turnoAsociadoId, setTurnoAsociadoId] = useState<number | null>(null);
  const [formulaTintura, setFormulaTintura] = useState<string>('');
  const [notasAlergias, setNotasAlergias] = useState<string>('');
  const [productosUsados, setProductosUsados] = useState<string>('');
  const [fotoUrl, setFotoUrl] = useState<string>('');
  const [subiendoFoto, setSubiendoFoto] = useState<boolean>(false);

  // 3. Estado Mis Comisiones y Liquidaciones + Impresión PDF/80mm
  const [misComisionesConfig, setMisComisionesConfig] = useState<
    ComisionConfigDto[]
  >([]);
  const [misLiquidaciones, setMisLiquidaciones] = useState<LiquidacionDto[]>(
    []
  );
  const [reciboDetalle, setReciboDetalle] = useState<LiquidacionDto | null>(
    null
  );
  const [reciboImprimir, setReciboImprimir] =
    useState<PrintableReceiptData | null>(null);

  // 4. Estado Mis Horarios y Bloqueos
  const [misHorarios, setMisHorarios] = useState<HorarioLaboralDto[]>([]);
  const [misBloqueos, setMisBloqueos] = useState<BloqueoHorarioDto[]>([]);
  const [bloqueoInicio, setBloqueoInicio] = useState<string>('');
  const [bloqueoFin, setBloqueoFin] = useState<string>('');
  const [bloqueoMotivo, setBloqueoMotivo] = useState<string>('');

  const cargarAgendaHoy = useCallback(async () => {
    setCargandoAgenda(true);
    setErrorAgenda(null);
    try {
      const res = await turnosService.getAll(fechaAgenda, miPeluqueroId);
      if (res.success) {
        setMisTurnos(extractPagedItems<TurnoDto>(res.data));
      } else {
        setMisTurnos([]);
      }
    } catch (err) {
      setMisTurnos([]);
      setErrorAgenda({
        message: extractErrorMessage(
          err,
          'No se pudieron cargar los turnos asignados.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
    } finally {
      setCargandoAgenda(false);
    }
  }, [fechaAgenda, miPeluqueroId]);

  useEffect(() => {
    if (activeTab === 'agenda') {
      void cargarAgendaHoy();
    } else if (activeTab === 'comisiones') {
      Promise.all([
        comisionesService.getByPeluquero(miPeluqueroId),
        liquidacionesService.getPorPeluquero(miPeluqueroId),
      ])
        .then(([comRes, liqRes]) => {
          if (comRes.success) {
            setMisComisionesConfig(
              extractPagedItems<ComisionConfigDto>(comRes.data)
            );
          }
          if (liqRes.success) {
            setMisLiquidaciones(
              extractPagedItems<LiquidacionDto>(liqRes.data)
            );
          }
        })
        .catch((err) => {
          notifyApiError(err, {
            fallbackMessage: 'Error al consultar comisiones y liquidaciones.',
          });
        });
    } else if (activeTab === 'horarios') {
      Promise.all([
        peluquerosService.getHorarios(miPeluqueroId),
        bloqueosService.getPorPeluquero(miPeluqueroId),
      ])
        .then(([horRes, bloqRes]) => {
          if (horRes.success) {
            setMisHorarios(extractPagedItems<HorarioLaboralDto>(horRes.data));
          }
          if (bloqRes.success) {
            setMisBloqueos(extractPagedItems<BloqueoHorarioDto>(bloqRes.data));
          }
        })
        .catch((err) => {
          notifyApiError(err, {
            fallbackMessage: 'Error al consultar horarios y bloqueos.',
          });
        });
    }
  }, [activeTab, cargarAgendaHoy, miPeluqueroId]);

  // Acción Táctil 1-Clic: Iniciar Atención o Finalizar Trabajo
  const cambiarEstadoRapido = async (
    turno: TurnoDto,
    nuevoEstado: EstadoTurnoEnum
  ) => {
    try {
      const res = await turnosService.cambiarEstado(turno.id, {
        nuevoEstadoId: nuevoEstado,
        observaciones:
          nuevoEstado === EstadoTurnoEnum.Completado
            ? 'Finalizado en sillón por profesional (consumo de gabinete aplicado).'
            : 'Atención iniciada en puesto de trabajo.',
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          nuevoEstado === EstadoTurnoEnum.Completado
            ? `Turno #${turno.id} completado (insumos de receta descontados del Kardex).`
            : `Turno #${turno.id} en proceso de atención.`
        );
        setFeedback(
          nuevoEstado === EstadoTurnoEnum.Completado
            ? `Turno #${turno.id} completado. El sistema descontó automáticamente del Kardex los insumos de la receta técnica.`
            : `Turno #${turno.id} en proceso de atención.`
        );
        await cargarAgendaHoy();
      }
    } catch (err: unknown) {
      const msg = notifyApiError(err, {
        fallbackMessage: 'Error al actualizar el estado del turno.',
      });
      setFeedback(msg);
    }
  };

  // Descuento manual directo de insumos de gabinete por turno
  const handleAplicarConsumoGabineteManual = async (turnoId: number) => {
    try {
      const res = await recetasServicioService.consumirPorTurno(turnoId);
      if (res.success) {
        notifyApiSuccess(
          res,
          `Consumo de receta técnica imputado en el Kardex para el Turno #${turnoId}.`
        );
        setFeedback(
          `Consumo de receta técnica imputado en el Kardex para el Turno #${turnoId}.`
        );
      }
    } catch (err: unknown) {
      const msg = notifyApiError(err, {
        fallbackMessage: 'No se pudo aplicar el consumo de gabinete.',
      });
      setFeedback(msg);
    }
  };

  // Abrir historia clínica directamente desde un turno de la agenda
  const abrirFichaDesdeTurno = async (turno: TurnoDto) => {
    setActiveTab('fichas');
    setTurnoAsociadoId(turno.id);
    const cliRes = await clientesService.getById(turno.clienteId);
    if (cliRes.success && cliRes.data) {
      await seleccionarClienteFicha(cliRes.data);
    } else {
      const fallbackCli: ClienteDto = {
        id: turno.clienteId,
        nombre: turno.clienteNombreCompleto,
        apellido: '',
        telefono: turno.clienteTelefono || '—',
        fechaNacimiento: '1995-01-01',
        fechaRegistro: new Date().toISOString(),
        activo: true,
      };
      await seleccionarClienteFicha(fallbackCli);
    }
  };

  const buscarClientes = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await clientesService.getAll(1, 15, busquedaCliente);
      if (res.success) {
        setClientesEncontrados(extractPagedItems<ClienteDto>(res.data));
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'Error al buscar clientes.',
      });
    }
  };

  const seleccionarClienteFicha = async (cli: ClienteDto) => {
    setClienteSeleccionado(cli);
    try {
      const res = await fichasTecnicasService.getPorCliente(cli.id);
      if (res.success) {
        setFichasCliente(extractPagedItems<FichaTecnicaDto>(res.data));
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudieron obtener las fichas del cliente.',
      });
    }
  };

  const handleSubirFotoFicha = async (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setSubiendoFoto(true);
    try {
      const uploadedResult = await uploadImageToCloudinary(file);
      setFotoUrl(uploadedResult.url);
    } finally {
      setSubiendoFoto(false);
      e.target.value = '';
    }
  };

  const guardarFichaTecnica = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!clienteSeleccionado) return;

    const payload = {
      clienteId: clienteSeleccionado.id,
      peluqueroId: miPeluqueroId,
      turnoId: turnoAsociadoId,
      formulaAplicada: formulaTintura,
      detalleFormulaTintura: formulaTintura,
      notasSensibilidadAlergias: notasAlergias,
      productosUtilizados: productosUsados,
      fotoResultadoUrl: fotoUrl || undefined,
    };

    try {
      const res = fichaEditando
        ? await fichasTecnicasService.actualizar(fichaEditando.id, payload)
        : await fichasTecnicasService.crear(payload);

      if (res.success) {
        notifyApiSuccess(res, 'Ficha técnica capilar guardada correctamente.');
        setModalFichaOpen(false);
        setFichaEditando(null);
        setFormulaTintura('');
        setNotasAlergias('');
        setProductosUsados('');
        setFotoUrl('');
        await seleccionarClienteFicha(clienteSeleccionado);
        setFeedback('Ficha técnica capilar guardada correctamente.');
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar la ficha técnica.',
      });
    }
  };

  const handleEliminarFicha = async (fichaId: number) => {
    if (!clienteSeleccionado) return;
    try {
      const res = await fichasTecnicasService.eliminar(fichaId);
      if (res.success) {
        notifyApiSuccess(res, `Ficha técnica #${fichaId} eliminada.`);
        await seleccionarClienteFicha(clienteSeleccionado);
        setFeedback(`Ficha técnica #${fichaId} eliminada.`);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar la ficha técnica.',
      });
    }
  };

  const abrirDetalleRecibo = async (liquidacionId: number) => {
    try {
      const res = await liquidacionesService.getById(liquidacionId);
      if (res.success && res.data) {
        setReciboDetalle(res.data);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo obtener el desglose de la liquidación.',
      });
    }
  };

  const imprimirMiRecibo = async (liquidacionId: number) => {
    try {
      const res = await liquidacionesService.getById(liquidacionId);
      if (res.success && res.data) {
        const l = res.data;
        setReciboImprimir({
          titulo: 'RECIBO DE COMISIONES PROFESIONAL',
          numeroComprobante: `LIQ-${l.id}`,
          fechaIso: l.fechaPago || l.fechaHasta,
          clienteOProfesional: l.peluqueroNombreCompleto,
          monedaIso: moneda,
          renglones: extractPagedItems(l.detallesLiquidacion).map((d) => ({
            descripcion:
              d.conceptoDescripcion ||
              `Ítem #${d.detalleVentaId || d.turnoId}`,
            monto: d.montoComisionCalculado,
          })),
          pagosODesglose: [
            { etiqueta: 'Subtotal Servicios', monto: l.totalServicios },
            { etiqueta: 'Subtotal Productos', monto: l.totalProductos },
            { etiqueta: 'Bonificaciones (+)', monto: l.bonificaciones || 0 },
            {
              etiqueta: 'Adelantos / Descuentos (-)',
              monto: l.descuentos || 0,
            },
          ],
          totalFinal: l.totalPagar,
          pieDePagina: `Período: ${formatDateOnly(
            l.fechaDesde
          )} al ${formatDateOnly(l.fechaHasta)}`,
        });
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo generar el recibo imprimible.',
      });
    }
  };

  const registrarBloqueoAusencia = async (e: React.FormEvent) => {
    e.preventDefault();
    if (new Date(bloqueoFin) <= new Date(bloqueoInicio)) {
      setFeedback('La fecha/hora de fin debe ser posterior al inicio.');
      return;
    }
    try {
      const res = await bloqueosService.crear({
        peluqueroId: miPeluqueroId,
        fechaInicio: bloqueoInicio,
        fechaFin: bloqueoFin,
        motivo: bloqueoMotivo,
      });
      if (res.success) {
        notifyApiSuccess(res, 'Bloqueo horario registrado en la agenda.');
        setBloqueoInicio('');
        setBloqueoFin('');
        setBloqueoMotivo('');
        const bloqRes = await bloqueosService.getPorPeluquero(miPeluqueroId);
        if (bloqRes.success) {
          setMisBloqueos(extractPagedItems<BloqueoHorarioDto>(bloqRes.data));
        }
        setFeedback('Bloqueo horario registrado en la agenda.');
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo registrar el bloqueo horario.',
      });
    }
  };

  const handleEliminarMiBloqueo = async (bloqueoId?: number) => {
    if (!bloqueoId) return;
    try {
      const res = await bloqueosService.eliminar(bloqueoId);
      if (res.success) {
        notifyApiSuccess(res, 'Bloqueo horario eliminado.');
        const bloqRes = await bloqueosService.getPorPeluquero(miPeluqueroId);
        if (bloqRes.success) {
          setMisBloqueos(extractPagedItems<BloqueoHorarioDto>(bloqRes.data));
        }
        setFeedback('Bloqueo horario eliminado.');
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar el bloqueo horario.',
      });
    }
  };

  const totalGanadoPeriodo = misLiquidaciones.reduce(
    (acc, l) => acc + (l.totalPagar || 0),
    0
  );

  return (
    <div className="p-6 space-y-6">
      {/* Cabecera Táctil con Selector de las 4 Pestañas del Puesto de Trabajo */}
      <header className="bg-white border border-slate-200 rounded-lg p-5 flex flex-wrap items-center justify-between gap-4">
        <div>
          <span className="text-[11px] font-mono uppercase tracking-widest text-amber-800 font-bold">
            Portal del Profesional • Legajo #{miPeluqueroId}
          </span>
          <h1 className="text-xl font-bold text-slate-900 mt-0.5">
            Puesto de Trabajo — {user?.username || 'Estilista'}
          </h1>
        </div>
        <div className="flex flex-wrap gap-2">
          {[
            { id: 'agenda', label: '1. Mi Agenda de Hoy' },
            { id: 'fichas', label: '2. Fichas Técnicas Capilares' },
            { id: 'comisiones', label: '3. Mis Comisiones y Recibos' },
            { id: 'horarios', label: '4. Mis Horarios y Ausencias' },
          ].map((tab) => (
            <Button
              key={tab.id}
              variant={activeTab === tab.id ? 'editorial' : 'secondary'}
              size="sm"
              onClick={() => setActiveTab(tab.id as typeof activeTab)}
            >
              {tab.label}
            </Button>
          ))}
        </div>
      </header>

      {feedback && (
        <div className="p-3.5 rounded-md bg-slate-900 text-white text-xs flex justify-between items-center">
          <span>{feedback}</span>
          <button type="button" onClick={() => setFeedback(null)}>
            ✕
          </button>
        </div>
      )}

      {/* PESTAÑA 1: MI AGENDA DE HOY */}
      {activeTab === 'agenda' && (
        <section className="space-y-5">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white border border-slate-200 rounded-lg p-4">
            <div>
              <h2 className="text-base font-bold text-slate-900">
                Línea de Tiempo Vertical de Atención
              </h2>
              <p className="text-xs text-slate-500">
                Al marcar &quot;Finalizar Trabajo&quot;, se descuentan
                automáticamente los insumos de la receta técnica en el Kardex.
              </p>
            </div>
            <input
              type="date"
              value={fechaAgenda}
              onChange={(e) => setFechaAgenda(e.target.value)}
              className="rounded-md border border-slate-300 px-3 py-2 text-xs bg-white"
            />
          </div>

          <AsyncStateGuard
            isLoading={cargandoAgenda}
            isError={Boolean(errorAgenda)}
            errorMessage={errorAgenda?.message}
            correlationId={errorAgenda?.correlationId}
            isEmpty={(misTurnos ?? []).length === 0}
            loadingText="Cargando turnos del día..."
            emptyTitle="Sin turnos asignados"
            emptyDescription="No tenés turnos asignados para la fecha seleccionada."
            onRetry={cargarAgendaHoy}
          >
            <div className="space-y-3">
              {(misTurnos ?? []).map((turno) => (
                <div
                  key={turno.id}
                  className="bg-white border border-slate-200 rounded-lg p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-sm"
                >
                  <div className="space-y-1.5">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-mono text-sm font-bold text-slate-900 bg-slate-100 px-2.5 py-0.5 rounded">
                        {formatDateTime(turno.fechaHoraInicio)}
                      </span>
                      <span className="text-xs font-semibold uppercase px-2.5 py-0.5 rounded bg-amber-50 text-amber-900 border border-amber-200">
                        {turno.estadoNombre}
                      </span>
                      {turno.tieneAlertaAlergia && (
                        <span className="text-xs font-bold px-2.5 py-0.5 rounded bg-red-100 text-red-800 border border-red-300">
                          ⚠️ Alerta Alergia / Sensibilidad Capilar
                        </span>
                      )}
                    </div>
                    <h3 className="text-base font-bold text-slate-900">
                      Cliente: {turno.clienteNombreCompleto}
                    </h3>
                    <p className="text-xs text-slate-600">
                      Servicios:{' '}
                      <strong>
                        {(turno.servicios ?? [])
                          .map((s) => s.servicioNombre)
                          .join(' + ') || 'Servicio de salón'}
                      </strong>
                    </p>
                    {(turno.observaciones || turno.notasAlergiaCliente) && (
                      <p className="text-xs text-amber-900 bg-amber-50 px-3 py-1.5 rounded border border-amber-200">
                        Nota previa / Sensibilidad:{' '}
                        {turno.notasAlergiaCliente || turno.observaciones}
                      </p>
                    )}
                  </div>

                  {/* Acciones Táctiles de 1 Clic */}
                  <div className="flex flex-wrap sm:flex-col gap-2 shrink-0">
                    {(turno.estadoId === EstadoTurnoEnum.Pendiente ||
                      turno.estadoId === EstadoTurnoEnum.Confirmado) && (
                      <Button
                        variant="primary"
                        size="sm"
                        onClick={() =>
                          cambiarEstadoRapido(turno, EstadoTurnoEnum.EnProceso)
                        }
                      >
                        ▶ Iniciar Atención
                      </Button>
                    )}
                    {turno.estadoId === EstadoTurnoEnum.EnProceso && (
                      <Button
                        variant="editorial"
                        size="sm"
                        onClick={() =>
                          cambiarEstadoRapido(turno, EstadoTurnoEnum.Completado)
                        }
                      >
                        ✓ Finalizar Trabajo (Descontar Receta)
                      </Button>
                    )}
                    {turno.estadoId === EstadoTurnoEnum.Completado && (
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() =>
                          handleAplicarConsumoGabineteManual(turno.id)
                        }
                      >
                        🧪 Re-aplicar Receta Gabinete
                      </Button>
                    )}
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => abrirFichaDesdeTurno(turno)}
                    >
                      📋 Ver / Cargar Ficha Técnica
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </AsyncStateGuard>
        </section>
      )}

      {/* PESTAÑA 2: FICHAS TÉCNICAS CAPILARES */}
      {activeTab === 'fichas' && (
        <section className="space-y-6">
          <form onSubmit={buscarClientes} className="flex gap-2">
            <input
              type="search"
              placeholder="Buscar cliente por nombre, apellido o teléfono..."
              value={busquedaCliente}
              onChange={(e) => setBusquedaCliente(e.target.value)}
              className="flex-1 rounded-md border border-slate-300 px-3.5 py-2 text-sm bg-white"
            />
            <Button type="submit" variant="primary">
              Buscar Cliente
            </Button>
          </form>

          {clientesEncontrados.length > 0 && (
            <div className="flex flex-wrap gap-2">
              {clientesEncontrados.map((cli) => (
                <button
                  key={cli.id}
                  type="button"
                  onClick={() => seleccionarClienteFicha(cli)}
                  className={`px-3 py-1.5 rounded-md text-xs font-semibold border ${
                    clienteSeleccionado?.id === cli.id
                      ? 'bg-slate-900 text-white border-slate-900'
                      : 'bg-white text-slate-800 border-slate-300 hover:bg-slate-100'
                  }`}
                >
                  {cli.nombre} {cli.apellido} ({cli.telefono})
                </button>
              ))}
            </div>
          )}

          {clienteSeleccionado && (
            <div className="space-y-4">
              <div className="bg-white border border-slate-200 rounded-lg p-4 flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h3 className="text-base font-bold">
                    {clienteSeleccionado.nombre} {clienteSeleccionado.apellido}
                  </h3>
                  <p className="text-xs text-slate-500">
                    Cabello:{' '}
                    {clienteSeleccionado.tipoCabelloNombre || 'Estándar'} •
                    Rostro: {clienteSeleccionado.tipoRostroNombre || 'Estándar'}
                  </p>
                </div>
                <Button
                  variant="editorial"
                  size="sm"
                  onClick={() => {
                    setFichaEditando(null);
                    setFormulaTintura('');
                    setNotasAlergias('');
                    setProductosUsados('');
                    setFotoUrl('');
                    setModalFichaOpen(true);
                  }}
                >
                  + Nueva Ficha Técnica
                </Button>
              </div>

              <div className="space-y-3">
                {fichasCliente.map((ficha) => (
                  <div
                    key={ficha.id}
                    className="bg-white border border-slate-200 rounded-lg p-5 space-y-2"
                  >
                    <div className="flex justify-between items-center text-xs text-slate-500">
                      <span className="font-mono font-semibold text-slate-800">
                        Fecha: {formatDateTime(ficha.fecha)}{' '}
                        {ficha.turnoId ? `• Turno #${ficha.turnoId}` : ''}
                      </span>
                      <div className="flex gap-3">
                        <button
                          type="button"
                          onClick={() => {
                            setFichaEditando(ficha);
                            setTurnoAsociadoId(ficha.turnoId || null);
                            setFormulaTintura(
                              ficha.detalleFormulaTintura ||
                                ficha.formulaAplicada ||
                                ''
                            );
                            setNotasAlergias(
                              ficha.notasSensibilidadAlergias ||
                                ficha.observaciones ||
                                ''
                            );
                            setProductosUsados(ficha.productosUtilizados || '');
                            setFotoUrl(ficha.fotoResultadoUrl || '');
                            setModalFichaOpen(true);
                          }}
                          className="underline text-slate-700 font-semibold"
                        >
                          Editar
                        </button>
                        <button
                          type="button"
                          onClick={() => handleEliminarFicha(ficha.id)}
                          className="underline text-red-700 font-semibold"
                        >
                          Eliminar
                        </button>
                      </div>
                    </div>
                    <p className="text-sm text-slate-900">
                      <strong>Fórmula Aplicada:</strong>{' '}
                      {ficha.detalleFormulaTintura ||
                        ficha.formulaAplicada ||
                        '—'}
                    </p>
                    {ficha.productosUtilizados && (
                      <p className="text-xs text-slate-600">
                        <strong>Productos de Gabinete:</strong>{' '}
                        {ficha.productosUtilizados}
                      </p>
                    )}
                    {(ficha.notasSensibilidadAlergias ||
                      ficha.observaciones) && (
                      <p className="text-xs text-red-800 bg-red-50 px-3 py-1.5 rounded border border-red-200 font-medium">
                        ⚠️ Alerta Sensibilidad / Alergias:{' '}
                        {ficha.notasSensibilidadAlergias || ficha.observaciones}
                      </p>
                    )}
                    {ficha.fotoResultadoUrl && (
                      <p className="text-xs">
                        <a
                          href={ficha.fotoResultadoUrl}
                          target="_blank"
                          rel="noreferrer"
                          className="underline text-amber-900 font-semibold"
                        >
                          Ver Foto del Resultado Capilar ↗
                        </a>
                      </p>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}
        </section>
      )}

      {/* PESTAÑA 3: MIS COMISIONES Y LIQUIDACIONES */}
      {activeTab === 'comisiones' && (
        <section className="space-y-6">
          <div className="bg-white border border-slate-200 rounded-lg p-5 flex items-center justify-between">
            <div>
              <span className="text-xs font-bold uppercase text-slate-500">
                Total Acumulado en Liquidaciones
              </span>
              <p className="text-2xl font-mono font-bold text-slate-900 mt-1">
                {formatCurrency(totalGanadoPeriodo, moneda)}
              </p>
            </div>
            <span className="text-xs font-mono text-slate-500">
              {misLiquidaciones.length} recibo(s) emitido(s)
            </span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            {misComisionesConfig.map((cfg) => (
              <div
                key={cfg.id}
                className="bg-white border border-slate-200 rounded-lg p-4 space-y-1"
              >
                <span className="text-xs font-bold uppercase text-amber-900">
                  {cfg.servicioNombre || 'Regla General del Profesional'}
                </span>
                <p className="text-lg font-mono font-bold text-slate-900">
                  {cfg.porcentajeServicio}% en Servicios
                </p>
                <p className="text-xs text-slate-500">
                  {cfg.porcentajeVentaProducto}% en Venta de Productos
                </p>
              </div>
            ))}
          </div>

          <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-100 text-slate-700 uppercase">
                <tr>
                  <th className="p-3">Recibo #</th>
                  <th className="p-3">Período</th>
                  <th className="p-3">Servicios</th>
                  <th className="p-3">Productos</th>
                  <th className="p-3">Total Neto</th>
                  <th className="p-3">Estado</th>
                  <th className="p-3 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {misLiquidaciones.map((liq) => (
                  <tr key={liq.id}>
                    <td className="p-3 font-mono font-bold">#{liq.id}</td>
                    <td className="p-3">
                      {formatDateOnly(liq.fechaDesde)} al{' '}
                      {formatDateOnly(liq.fechaHasta)}
                    </td>
                    <td className="p-3 font-mono">
                      {formatCurrency(liq.totalServicios, moneda)}
                    </td>
                    <td className="p-3 font-mono">
                      {formatCurrency(liq.totalProductos, moneda)}
                    </td>
                    <td className="p-3 font-mono font-bold text-slate-900">
                      {formatCurrency(liq.totalPagar, moneda)}
                    </td>
                    <td className="p-3 font-semibold">
                      {liq.estadoNombre || liq.estado}
                    </td>
                    <td className="p-3 text-right space-x-1.5">
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => abrirDetalleRecibo(liq.id)}
                      >
                        Ver Desglose
                      </Button>
                      <Button
                        variant="primary"
                        size="sm"
                        onClick={() => imprimirMiRecibo(liq.id)}
                      >
                        Imprimir Recibo
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* PESTAÑA 4: MIS HORARIOS Y BLOQUEOS */}
      {activeTab === 'horarios' && (
        <section className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="bg-white border border-slate-200 rounded-lg p-5 space-y-3">
              <h3 className="text-sm font-bold uppercase text-slate-700">
                Jornada Semanal y Descansos Asignados
              </h3>
              <div className="divide-y divide-slate-100 text-xs">
                {misHorarios.map((h, idx) => (
                  <div key={idx} className="py-2 flex justify-between">
                    <span className="font-semibold">
                      {DIAS_SEMANA[h.diaSemana] || `Día ${h.diaSemana}`}
                    </span>
                    <span className="font-mono">
                      {h.horaInicio} a {h.horaFin} hs
                      {h.horaInicioDescanso && h.horaFinDescanso
                        ? ` (Desc: ${h.horaInicioDescanso}-${h.horaFinDescanso})`
                        : ''}
                    </span>
                  </div>
                ))}
              </div>
            </div>

            <form
              onSubmit={registrarBloqueoAusencia}
              className="bg-white border border-slate-200 rounded-lg p-5 space-y-4"
            >
              <h3 className="text-sm font-bold uppercase text-slate-700">
                Registrar Ausencia / Bloqueo Horario
              </h3>
              <InputField
                label="Fecha y Hora de Inicio *"
                type="datetime-local"
                required
                value={bloqueoInicio}
                onChange={(e) => setBloqueoInicio(e.target.value)}
              />
              <InputField
                label="Fecha y Hora de Fin *"
                type="datetime-local"
                required
                value={bloqueoFin}
                onChange={(e) => setBloqueoFin(e.target.value)}
              />
              <InputField
                label="Motivo del Bloqueo *"
                required
                maxLength={200}
                value={bloqueoMotivo}
                onChange={(e) => setBloqueoMotivo(e.target.value)}
                placeholder="Ej. Turno médico / Capacitación técnica"
              />
              <Button type="submit" variant="primary" className="w-full">
                Bloquear Horario en Agenda
              </Button>
            </form>
          </div>

          <div className="bg-white border border-slate-200 rounded-lg p-5 space-y-3">
            <h3 className="text-sm font-bold uppercase text-slate-700">
              Mis Bloqueos Registrados ({misBloqueos.length})
            </h3>
            {misBloqueos.map((b) => (
              <div
                key={b.id}
                className="flex items-center justify-between border-b border-slate-100 pb-2 text-xs"
              >
                <div>
                  <span className="font-mono font-semibold">
                    {formatDateTime(b.fechaInicio)} →{' '}
                    {formatDateTime(b.fechaFin)}
                  </span>
                  <span className="ml-3 text-slate-600">{b.motivo}</span>
                </div>
                <Button
                  variant="danger"
                  size="sm"
                  onClick={() => handleEliminarMiBloqueo(b.id)}
                >
                  Eliminar
                </Button>
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Modal Nueva / Editar Ficha Técnica */}
      <Modal
        isOpen={modalFichaOpen}
        onClose={() => setModalFichaOpen(false)}
        title={
          fichaEditando
            ? 'Editar Ficha Técnica Capilar'
            : 'Nueva Ficha Técnica Capilar'
        }
      >
        <form onSubmit={guardarFichaTecnica} className="space-y-4">
          <InputField
            label="ID Turno Vinculado (Opcional)"
            type="number"
            value={turnoAsociadoId || ''}
            onChange={(e) =>
              setTurnoAsociadoId(e.target.value ? Number(e.target.value) : null)
            }
          />
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold uppercase text-slate-700">
              Fórmula de Tintura / Decoloración Aplicada *
            </label>
            <textarea
              required
              rows={3}
              value={formulaTintura}
              onChange={(e) => setFormulaTintura(e.target.value)}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
              placeholder="Ej. 60g Tono 7.1 + 90ml Oxidante 20 Vol (35 min)"
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold uppercase text-slate-700">
              Notas de Sensibilidad / Alergias en Cuero Cabelludo
            </label>
            <textarea
              rows={2}
              value={notasAlergias}
              onChange={(e) => setNotasAlergias(e.target.value)}
              className="rounded-md border border-slate-300 px-3 py-2 text-sm"
            />
          </div>
          <InputField
            label="Productos de Gabinete Utilizados"
            value={productosUsados}
            onChange={(e) => setProductosUsados(e.target.value)}
          />
          <div className="space-y-1.5">
            <label className="text-xs font-semibold uppercase text-slate-700 block">
              Subir Fotografía a Cloudinary CDN
            </label>
            <input
              type="file"
              accept="image/*"
              disabled={subiendoFoto}
              onChange={handleSubirFotoFicha}
              className="block w-full text-xs text-slate-600 file:mr-3 file:py-1.5 file:px-3 file:rounded file:border-0 file:text-xs file:font-semibold file:bg-slate-900 file:text-white hover:file:bg-slate-800"
            />
            {subiendoFoto && (
              <p className="text-[11px] text-amber-800 font-semibold">
                Subiendo imagen a Cloudinary CDN...
              </p>
            )}
          </div>
          <InputField
            label="URL Foto del Resultado (Opcional)"
            value={fotoUrl}
            onChange={(e) => setFotoUrl(e.target.value)}
            placeholder="https://..."
          />
          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setModalFichaOpen(false)}
            >
              Cancelar
            </Button>
            <Button type="submit" variant="editorial">
              Guardar Ficha Clínica
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal Detalle de Recibo de Liquidación */}
      <Modal
        isOpen={!!reciboDetalle}
        onClose={() => setReciboDetalle(null)}
        title={`Detalle de Recibo de Liquidación #${reciboDetalle?.id}`}
        maxWidthClass="max-w-2xl"
      >
        {reciboDetalle && (
          <div className="space-y-4 text-xs">
            <div className="flex justify-between bg-slate-50 p-3 rounded border border-slate-200">
              <span>
                Período:{' '}
                <strong>{formatDateOnly(reciboDetalle.fechaDesde)}</strong> al{' '}
                <strong>{formatDateOnly(reciboDetalle.fechaHasta)}</strong>
              </span>
              <span className="font-mono font-bold text-sm">
                Total a Cobrar:{' '}
                {formatCurrency(reciboDetalle.totalPagar, moneda)}
              </span>
            </div>
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-300 text-slate-700">
                  <th className="py-2">DetalleVenta / Turno</th>
                  <th className="py-2">Concepto</th>
                  <th className="py-2 text-right">Comisión Calculada</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {(reciboDetalle.detallesLiquidacion || []).map((det) => (
                  <tr key={det.id}>
                    <td className="py-2 font-mono">
                      VentaItem #{det.detalleVentaId || det.turnoId}
                    </td>
                    <td className="py-2">
                      {det.conceptoDescripcion ||
                        'Comisión por servicio/producto ejecutado'}
                    </td>
                    <td className="py-2 text-right font-mono font-bold">
                      {formatCurrency(det.montoComisionCalculado, moneda)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Modal>

      {/* Modal de Impresión Térmica 80mm / PDF */}
      <PrintReceiptModal
        data={reciboImprimir}
        onClose={() => setReciboImprimir(null)}
        nombreSalon={configSalon?.nombreComercial}
        cuitSalon={configSalon?.cuit}
      />
    </div>
  );
}