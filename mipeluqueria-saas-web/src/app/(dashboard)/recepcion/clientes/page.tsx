// filepath: src/app/(dashboard)/recepcion/clientes/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  AlertTriangle,
  Cake,
  Camera,
  CheckCircle2,
  ExternalLink,
  FlaskConical,
  Info,
  Pencil,
  Plus,
  Search,
  Trash2,
  UserPlus,
  X,
} from 'lucide-react';
import {
  ClienteDto,
  FichaTecnicaDto,
  PeluqueroDto,
} from '@/types/api.types';
import {
  clientesService,
  fichasTecnicasService,
  peluquerosService,
} from '@/services/api.services';
import { useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  isCloudinaryConfigured,
  uploadImageToCloudinary,
} from '@/lib/cloudinary';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
  extractPaginationMeta,
} from '@/lib/apiHelpers';
import { formatDateOnly, formatDateTime } from '@/lib/formatters';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import {
  AsyncStateGuard,
  Badge,
  Button,
  InputField,
  Modal,
  PageHeader,
  PaginationBar,
} from '@/components/ui';

function getClientInitials(cliente: ClienteDto): string {
  const first = (cliente.nombre || '').trim().charAt(0);
  const last = (cliente.apellido || '').trim().charAt(0);
  if (first || last) {
    return `${first}${last}`.toUpperCase();
  }
  const full = (cliente.nombreCompleto || 'CL').trim().split(/\s+/);
  return full
    .slice(0, 2)
    .map((part) => part.charAt(0))
    .join('')
    .toUpperCase();
}

export default function ClientesCrm360Page() {
  const { catalogos } = useCatalogos();
  const cloudinaryActivo = useMemo(() => isCloudinaryConfigured(), []);

  // Listado paginado de Clientes
  const [clientes, setClientes] = useState<ClienteDto[]>([]);
  const [peluqueros, setPeluqueros] = useState<PeluqueroDto[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [pageSize] = useState<number>(15);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [busqueda, setBusqueda] = useState<string>('');
  const [cargando, setCargando] = useState<boolean>(true);
  const [errorClientes, setErrorClientes] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);

  // Modal de Alta / Edición de Cliente
  const [modalClienteOpen, setModalClienteOpen] = useState<boolean>(false);
  const [clienteEditando, setClienteEditando] = useState<ClienteDto | null>(
    null
  );
  const [cliNombre, setCliNombre] = useState<string>('');
  const [cliApellido, setCliApellido] = useState<string>('');
  const [cliTelefono, setCliTelefono] = useState<string>('');
  const [cliEmail, setCliEmail] = useState<string>('');
  const [cliFechaNac, setCliFechaNac] = useState<string>('1995-05-15');
  const [cliTipoCabelloId, setCliTipoCabelloId] = useState<number>(0);
  const [cliTipoRostroId, setCliTipoRostroId] = useState<number>(0);
  const [guardandoCliente, setGuardandoCliente] = useState<boolean>(false);

  // Modal de Fichas Técnicas Capilares por Cliente
  const [clienteFichaActivo, setClienteFichaActivo] =
    useState<ClienteDto | null>(null);
  const [fichasCliente, setFichasCliente] = useState<FichaTecnicaDto[]>([]);
  const [cargandoFichas, setCargandoFichas] = useState<boolean>(false);
  const [errorFichas, setErrorFichas] = useState<string | null>(null);

  // Formulario de Ficha Técnica (Crear / Editar)
  const [fichaEditando, setFichaEditando] = useState<FichaTecnicaDto | null>(
    null
  );
  const [fichaPeluqueroId, setFichaPeluqueroId] = useState<number>(1);
  const [fichaFormula, setFichaFormula] = useState<string>('');
  const [fichaObservaciones, setFichaObservaciones] = useState<string>('');
  const [fichaAlergias, setFichaAlergias] = useState<string>('');
  const [fichaProductos, setFichaProductos] = useState<string>('');
  const [fichaFotoUrl, setFichaFotoUrl] = useState<string>('');
  const [fotoPreviewError, setFotoPreviewError] = useState<boolean>(false);
  const [subiendoFoto, setSubiendoFoto] = useState<boolean>(false);
  const [guardandoFicha, setGuardandoFicha] = useState<boolean>(false);

  const cargarClientes = useCallback(async () => {
    setCargando(true);
    setErrorClientes(null);
    try {
      const res = await clientesService.getAll(
        pageNumber,
        pageSize,
        busqueda.trim() || undefined
      );
      if (res.success) {
        const items = extractPagedItems<ClienteDto>(res.data);
        const meta = extractPaginationMeta(res.data, { pageNumber, pageSize });
        setClientes(items);
        setTotalCount(meta.totalCount);
        setTotalPages(meta.totalPages);
      } else {
        setClientes([]);
        setErrorClientes({
          message: res.message || 'Error al cargar el padrón de clientes.',
        });
      }
    } catch (err) {
      setClientes([]);
      setErrorClientes({
        message: extractErrorMessage(
          err,
          'Error al cargar el padrón de clientes.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
      notifyApiError(err, {
        fallbackMessage: 'Error al cargar el padrón de clientes.',
      });
    } finally {
      setCargando(false);
    }
  }, [pageNumber, pageSize, busqueda]);

  useEffect(() => {
    void cargarClientes();
  }, [cargarClientes]);

  useEffect(() => {
    peluquerosService
      .getAll(true)
      .then((res) => {
        if (res.success) {
          const items = extractPagedItems<PeluqueroDto>(res.data);
          setPeluqueros(items);
          if (items.length > 0) {
            setFichaPeluqueroId(items[0].id);
          }
        }
      })
      .catch(() => {
        // Silencioso
      });
  }, []);

  const alertasAlergiasCliente = useMemo(
    () =>
      fichasCliente
        .map((f) => (f.notasSensibilidadAlergias || '').trim())
        .filter((nota) => nota.length > 0),
    [fichasCliente]
  );

  const abrirNuevoCliente = () => {
    setClienteEditando(null);
    setCliNombre('');
    setCliApellido('');
    setCliTelefono('');
    setCliEmail('');
    setCliFechaNac('1995-05-15');
    setCliTipoCabelloId(0);
    setCliTipoRostroId(0);
    setModalClienteOpen(true);
  };

  const abrirEditarCliente = (c: ClienteDto) => {
    setClienteEditando(c);
    setCliNombre(c.nombre);
    setCliApellido(c.apellido);
    setCliTelefono(c.telefono);
    setCliEmail(c.email || '');
    setCliFechaNac(
      c.fechaNacimiento ? c.fechaNacimiento.split('T')[0] : '1995-05-15'
    );
    setCliTipoCabelloId(c.tipoCabelloId || 0);
    setCliTipoRostroId(c.tipoRostroId || 0);
    setModalClienteOpen(true);
  };

  const handleGuardarCliente = async (e: React.FormEvent) => {
    e.preventDefault();
    setGuardandoCliente(true);
    try {
      const payload = {
        nombre: cliNombre.trim(),
        apellido: cliApellido.trim(),
        telefono: cliTelefono.trim(),
        email: cliEmail.trim() || null,
        fechaNacimiento: cliFechaNac,
        tipoCabelloId: cliTipoCabelloId > 0 ? cliTipoCabelloId : null,
        tipoRostroId: cliTipoRostroId > 0 ? cliTipoRostroId : null,
      };

      const res = clienteEditando
        ? await clientesService.update(clienteEditando.id, payload)
        : await clientesService.create(payload);

      if (res.success) {
        notifyApiSuccess(
          res,
          clienteEditando
            ? 'Datos del cliente actualizados.'
            : 'Nuevo cliente registrado correctamente.'
        );
        setModalClienteOpen(false);
        await cargarClientes();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el cliente.',
      });
    } finally {
      setGuardandoCliente(false);
    }
  };

  const handleEliminarCliente = async (c: ClienteDto) => {
    try {
      const res = await clientesService.delete(c.id);
      if (res.success) {
        notifyApiSuccess(
          res,
          `Cliente ${c.nombreCompleto || `${c.nombre} ${c.apellido}`} dado de baja.`
        );
        await cargarClientes();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo dar de baja al cliente.',
      });
    }
  };

  // Gestión de Fichas Técnicas Capilares
  const cargarFichasDeCliente = async (cliente: ClienteDto) => {
    setClienteFichaActivo(cliente);
    setFichaEditando(null);
    setFichaFormula('');
    setFichaObservaciones('');
    setFichaAlergias('');
    setFichaProductos('');
    setFichaFotoUrl('');
    setFotoPreviewError(false);
    setErrorFichas(null);
    setCargandoFichas(true);
    try {
      const res = await fichasTecnicasService.getPorCliente(cliente.id);
      if (res.success) {
        setFichasCliente(extractPagedItems<FichaTecnicaDto>(res.data));
      } else {
        setFichasCliente([]);
        setErrorFichas(
          res.message || 'No se pudieron obtener las fichas técnicas.'
        );
      }
    } catch (err) {
      setFichasCliente([]);
      setErrorFichas(
        extractErrorMessage(err, 'No se pudieron obtener las fichas técnicas.')
      );
      notifyApiError(err, {
        fallbackMessage: 'No se pudieron obtener las fichas técnicas.',
      });
    } finally {
      setCargandoFichas(false);
    }
  };

  const seleccionarFichaParaEditar = (f: FichaTecnicaDto) => {
    setFichaEditando(f);
    setFichaPeluqueroId(f.peluqueroId);
    setFichaFormula(f.formulaAplicada || f.detalleFormulaTintura || '');
    setFichaObservaciones(f.observaciones || '');
    setFichaAlergias(f.notasSensibilidadAlergias || '');
    setFichaProductos(f.productosUtilizados || '');
    setFichaFotoUrl(f.fotoResultadoUrl || '');
    setFotoPreviewError(false);
  };

  const limpiarFormFicha = () => {
    setFichaEditando(null);
    setFichaFormula('');
    setFichaObservaciones('');
    setFichaAlergias('');
    setFichaProductos('');
    setFichaFotoUrl('');
    setFotoPreviewError(false);
  };

  const handleSubirFotoCloudinary = async (
    e: React.ChangeEvent<HTMLInputElement>
  ) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setSubiendoFoto(true);
    try {
      const uploadResult = await uploadImageToCloudinary(file);
      setFichaFotoUrl(uploadResult.url);
      setFotoPreviewError(false);
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo procesar la fotografía.',
      });
    } finally {
      setSubiendoFoto(false);
      e.target.value = '';
    }
  };

  const handleGuardarFichaTecnica = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!clienteFichaActivo) return;
    setGuardandoFicha(true);
    try {
      if (fichaEditando) {
        const res = await fichasTecnicasService.actualizar(fichaEditando.id, {
          peluqueroId: fichaPeluqueroId,
          formulaAplicada: fichaFormula.trim(),
          observaciones: fichaObservaciones.trim(),
          notasSensibilidadAlergias: fichaAlergias.trim() || undefined,
          productosUtilizados: fichaProductos.trim() || undefined,
          fotoResultadoUrl: fichaFotoUrl.trim() || undefined,
        });
        if (res.success) {
          notifyApiSuccess(res, 'Ficha técnica capilar actualizada.');
          limpiarFormFicha();
          await cargarFichasDeCliente(clienteFichaActivo);
        }
      } else {
        const res = await fichasTecnicasService.crear({
          clienteId: clienteFichaActivo.id,
          peluqueroId: fichaPeluqueroId,
          formulaAplicada: fichaFormula.trim(),
          observaciones: fichaObservaciones.trim(),
          notasSensibilidadAlergias: fichaAlergias.trim() || undefined,
          productosUtilizados: fichaProductos.trim() || undefined,
          fotoResultadoUrl: fichaFotoUrl.trim() || undefined,
        });
        if (res.success) {
          notifyApiSuccess(res, 'Nueva ficha técnica capilar registrada.');
          limpiarFormFicha();
          await cargarFichasDeCliente(clienteFichaActivo);
        }
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar la ficha técnica.',
      });
    } finally {
      setGuardandoFicha(false);
    }
  };

  const handleEliminarFicha = async (fichaId: number) => {
    if (!clienteFichaActivo) return;
    try {
      const res = await fichasTecnicasService.eliminar(fichaId);
      if (res.success) {
        notifyApiSuccess(res, 'Ficha técnica eliminada.');
        await cargarFichasDeCliente(clienteFichaActivo);
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo eliminar la ficha técnica.',
      });
    }
  };

  return (
    <div className="flex-1 flex flex-col min-h-0 min-w-0">
      <PageHeader
        title="Clientes"
        actions={
          <div className="flex items-center gap-2.5">
            <Badge variant="neutral">
              <span className="font-mono-tabular">{totalCount}</span> registrados
            </Badge>
            <Button
              variant="primary"
              size="sm"
              leftIcon={<UserPlus className="h-4 w-4" aria-hidden="true" />}
              onClick={abrirNuevoCliente}
            >
              Nuevo Cliente
            </Button>
          </div>
        }
      />

      <div className="p-4 lg:p-5 space-y-4 max-w-[1440px] w-full mx-auto min-w-0">
        {/* Barra de Búsqueda */}
        <div className="bg-surface border border-border-subtle rounded-xl p-3.5 flex flex-wrap items-center justify-between gap-3 shadow-sm">
          <div className="relative flex-1 min-w-[220px] max-w-md">
            <Search
              className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-ink-secondary"
              aria-hidden="true"
            />
            <input
              type="search"
              placeholder="Buscar por nombre, apellido, teléfono o email..."
              value={busqueda}
              onChange={(e) => {
                setBusqueda(e.target.value);
                setPageNumber(1);
              }}
              className="h-10 w-full rounded-lg border border-border-strong bg-surface pl-9 pr-3 text-sm text-ink-primary placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
            />
          </div>
          <div className="text-xs text-ink-secondary font-mono-tabular">
            Página <strong className="text-ink-primary">{pageNumber}</strong> de{' '}
            <strong className="text-ink-primary">{totalPages}</strong> ·{' '}
            {totalCount} clientes
          </div>
        </div>

        {/* Tabla Paginada de Clientes */}
        <AsyncStateGuard
          isLoading={cargando}
          isError={Boolean(errorClientes)}
          errorMessage={errorClientes?.message}
          correlationId={errorClientes?.correlationId}
          isEmpty={(clientes ?? []).length === 0}
          loadingText="Cargando padrón de clientes..."
          emptyTitle="No se encontraron clientes"
          emptyDescription="No existen clientes registrados que coincidan con el criterio ingresado."
          onRetry={cargarClientes}
        >
          <div className="bg-surface border border-border-subtle rounded-xl overflow-hidden shadow-sm">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-xs">
                <thead>
                  <tr className="bg-elevated/80 border-b border-border-subtle text-ink-secondary">
                    <th className="py-3 px-4 font-medium">Cliente</th>
                    <th className="py-3 px-4 font-medium">Contacto</th>
                    <th className="py-3 px-4 font-medium">Perfil capilar</th>
                    <th className="py-3 px-4 font-medium">Nacimiento</th>
                    <th className="py-3 px-4 font-medium text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border-subtle">
                  {(clientes ?? []).map((c) => {
                    const displayName =
                      c.nombreCompleto || `${c.nombre} ${c.apellido}`;
                    return (
                      <tr
                        key={c.id}
                        className="hover:bg-elevated/50 transition-colors"
                      >
                        <td className="py-3 px-4">
                          <div className="flex items-center gap-3 min-w-0">
                            <div
                              aria-hidden="true"
                              className="h-9 w-9 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200 dark:bg-indigo-950/60 dark:text-indigo-300 dark:border-indigo-800/70 flex items-center justify-center text-xs font-semibold shrink-0"
                            >
                              {getClientInitials(c)}
                            </div>
                            <div className="min-w-0">
                              <div className="flex flex-wrap items-center gap-1.5">
                                <span className="text-sm font-medium text-ink-primary truncate">
                                  {displayName}
                                </span>
                                {c.esCumpleaneroDelMes && (
                                  <Badge variant="warning">
                                    <Cake
                                      className="h-3 w-3"
                                      aria-hidden="true"
                                    />
                                    <span>Cumpleaños</span>
                                  </Badge>
                                )}
                              </div>
                              <span className="text-[11px] text-ink-secondary font-mono-tabular">
                                #{c.id}
                              </span>
                            </div>
                          </div>
                        </td>
                        <td className="py-3 px-4 whitespace-nowrap">
                          <p className="font-mono-tabular font-medium text-ink-primary">
                            {c.telefono}
                          </p>
                          <p className="text-[11px] text-ink-secondary truncate max-w-[200px]">
                            {c.email || 'Sin correo'}
                          </p>
                        </td>
                        <td className="py-3 px-4">
                          <div className="flex flex-wrap gap-1.5">
                            <Badge variant="neutral">
                              Cabello: {c.tipoCabelloNombre || 'Sin definir'}
                            </Badge>
                            <Badge variant="neutral">
                              Rostro: {c.tipoRostroNombre || 'Sin definir'}
                            </Badge>
                          </div>
                        </td>
                        <td className="py-3 px-4 font-mono-tabular text-ink-secondary whitespace-nowrap">
                          {formatDateOnly(c.fechaNacimiento)}
                        </td>
                        <td className="py-3 px-4 text-right whitespace-nowrap">
                          <div className="inline-flex items-center gap-1.5">
                            <Button
                              size="sm"
                              variant="secondary"
                              leftIcon={
                                <FlaskConical
                                  className="h-3.5 w-3.5 text-brand-primary"
                                  aria-hidden="true"
                                />
                              }
                              onClick={() => cargarFichasDeCliente(c)}
                            >
                              Fichas Técnicas
                            </Button>
                            <Button
                              size="sm"
                              variant="secondary"
                              leftIcon={
                                <Pencil
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                              }
                              onClick={() => abrirEditarCliente(c)}
                            >
                              Editar
                            </Button>
                            <Button
                              size="sm"
                              variant="destructive"
                              destructiveTone="soft"
                              onClick={() => handleEliminarCliente(c)}
                            >
                              Baja
                            </Button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </AsyncStateGuard>

        <PaginationBar
          pageNumber={pageNumber}
          totalPages={totalPages}
          totalCount={totalCount}
          itemLabel="clientes"
          isLoading={cargando}
          onPageChange={setPageNumber}
        />
      </div>

      {/* Modal de Alta / Edición de Cliente */}
      <Modal
        isOpen={modalClienteOpen}
        onClose={() => setModalClienteOpen(false)}
        title={
          clienteEditando
            ? `Editar cliente #${clienteEditando.id}`
            : 'Registrar nuevo cliente'
        }
        subtitle="Completá los datos de contacto y el perfil capilar."
      >
        <form onSubmit={handleGuardarCliente} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <InputField
              label="Nombre *"
              required
              value={cliNombre}
              onChange={(e) => setCliNombre(e.target.value)}
            />
            <InputField
              label="Apellido *"
              required
              value={cliApellido}
              onChange={(e) => setCliApellido(e.target.value)}
            />
            <InputField
              label="WhatsApp / Teléfono *"
              required
              value={cliTelefono}
              onChange={(e) => setCliTelefono(e.target.value)}
            />
            <InputField
              label="Correo electrónico"
              type="email"
              value={cliEmail}
              onChange={(e) => setCliEmail(e.target.value)}
            />
            <InputField
              label="Fecha de nacimiento *"
              type="date"
              required
              value={cliFechaNac}
              onChange={(e) => setCliFechaNac(e.target.value)}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="cliente-tipo-cabello"
                className="text-xs font-medium text-ink-secondary"
              >
                Tipo de cabello
              </label>
              <select
                id="cliente-tipo-cabello"
                value={cliTipoCabelloId}
                onChange={(e) => setCliTipoCabelloId(Number(e.target.value))}
                className="h-10 w-full rounded-lg border border-border-strong bg-surface text-ink-primary px-3 text-sm focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                <option value={0}>Sin especificar</option>
                {(catalogos?.tiposCabello || []).map((tc) => (
                  <option key={tc.id} value={tc.id}>
                    {tc.nombre}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="cliente-tipo-rostro"
                className="text-xs font-medium text-ink-secondary"
              >
                Tipo de rostro
              </label>
              <select
                id="cliente-tipo-rostro"
                value={cliTipoRostroId}
                onChange={(e) => setCliTipoRostroId(Number(e.target.value))}
                className="h-10 w-full rounded-lg border border-border-strong bg-surface text-ink-primary px-3 text-sm focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
              >
                <option value={0}>Sin especificar</option>
                {(catalogos?.tiposRostro || []).map((tr) => (
                  <option key={tr.id} value={tr.id}>
                    {tr.nombre}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setModalClienteOpen(false)}
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              variant="primary"
              isLoading={guardandoCliente}
            >
              Guardar cliente
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal de Fichas Técnicas Capilares */}
      <Modal
        isOpen={Boolean(clienteFichaActivo)}
        onClose={() => setClienteFichaActivo(null)}
        title={`Fichas técnicas — ${
          clienteFichaActivo?.nombreCompleto ||
          (clienteFichaActivo
            ? `${clienteFichaActivo.nombre} ${clienteFichaActivo.apellido}`
            : '')
        }`}
        subtitle="Fórmulas de colorimetría, alertas de sensibilidad y registro fotográfico."
        maxWidthClass="max-w-5xl"
      >
        <div className="space-y-4">
          {/* Banner destacado de alergias si existe alguna alerta en el historial */}
          {alertasAlergiasCliente.length > 0 && (
            <div
              role="alert"
              className={`rounded-xl border p-3.5 flex items-start gap-3 ${SEMANTIC_COLORS.danger.surfaceClass}`}
            >
              <AlertTriangle
                className={`h-5 w-5 shrink-0 mt-0.5 ${SEMANTIC_COLORS.danger.textClass}`}
                aria-hidden="true"
              />
              <div className="text-xs space-y-1 min-w-0">
                <p
                  className={`font-semibold ${SEMANTIC_COLORS.danger.textClass}`}
                >
                  Alerta de sensibilidad o alergias registradas
                </p>
                <p className="text-ink-primary leading-relaxed">
                  {alertasAlergiasCliente[0]}
                  {alertasAlergiasCliente.length > 1
                    ? ` (+${alertasAlergiasCliente.length - 1} registro adicional en historial)`
                    : ''}
                </p>
              </div>
            </div>
          )}

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-5">
            {/* Formulario de Nueva / Edición de Ficha Técnica */}
            <form
              onSubmit={handleGuardarFichaTecnica}
              className="lg:col-span-5 space-y-3.5 bg-elevated/60 p-4 rounded-xl border border-border-subtle"
            >
              <div className="flex items-center justify-between gap-2">
                <h3 className="text-sm font-semibold text-ink-primary">
                  {fichaEditando
                    ? `Editar ficha #${fichaEditando.id}`
                    : 'Nueva ficha técnica'}
                </h3>
                {fichaEditando && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    leftIcon={<Plus className="h-3.5 w-3.5" aria-hidden="true" />}
                    onClick={limpiarFormFicha}
                  >
                    Crear nueva
                  </Button>
                )}
              </div>

              <div className="flex flex-col gap-1.5">
                <label
                  htmlFor="ficha-peluquero"
                  className="text-xs font-medium text-ink-secondary"
                >
                  Profesional responsable *
                </label>
                <select
                  id="ficha-peluquero"
                  value={fichaPeluqueroId}
                  onChange={(e) => setFichaPeluqueroId(Number(e.target.value))}
                  className="h-10 w-full rounded-lg border border-border-strong bg-surface text-ink-primary px-3 text-sm focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                >
                  {peluqueros.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nombreCompleto || `${p.nombre} ${p.apellido}`}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex flex-col gap-1.5">
                <label
                  htmlFor="ficha-formula"
                  className="text-xs font-medium text-ink-secondary"
                >
                  Fórmula aplicada (colorimetría / técnica) *
                </label>
                <textarea
                  id="ficha-formula"
                  rows={3}
                  required
                  value={fichaFormula}
                  onChange={(e) => setFichaFormula(e.target.value)}
                  placeholder="Ej. Pomo 7.1 (45g) + 8.11 (15g) con oxidante 20 Vol. Tiempo de pose: 35 min."
                  className="rounded-lg border border-border-strong bg-surface text-ink-primary px-3 py-2 text-sm placeholder:text-ink-secondary/70 focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <label
                  htmlFor="ficha-alergias"
                  className={`text-xs font-semibold flex items-center gap-1.5 ${SEMANTIC_COLORS.danger.textClass}`}
                >
                  <AlertTriangle className="h-3.5 w-3.5" aria-hidden="true" />
                  <span>Notas de sensibilidad o alergias</span>
                </label>
                <textarea
                  id="ficha-alergias"
                  rows={2}
                  value={fichaAlergias}
                  onChange={(e) => setFichaAlergias(e.target.value)}
                  placeholder="Ej. Sensibilidad a decolorantes con amoníaco en nuca."
                  className="rounded-lg border border-red-300 dark:border-rose-800/80 bg-red-50/50 dark:bg-rose-950/30 text-ink-primary px-3 py-2 text-xs placeholder:text-ink-secondary/70 focus:border-red-500 focus:outline-none focus:ring-2 focus:ring-red-500/20"
                />
              </div>

              <InputField
                label="Productos utilizados"
                value={fichaProductos}
                onChange={(e) => setFichaProductos(e.target.value)}
                placeholder="Ej. Ampolla cauterización + shampoo ácido"
              />

              <InputField
                label="Observaciones generales"
                value={fichaObservaciones}
                onChange={(e) => setFichaObservaciones(e.target.value)}
              />

              {/* Fotografía con vista previa y estado Cloudinary */}
              <div className="space-y-2.5 pt-2 border-t border-border-subtle">
                <div className="flex items-center justify-between gap-2">
                  <span className="text-xs font-medium text-ink-secondary flex items-center gap-1.5">
                    <Camera className="h-3.5 w-3.5" aria-hidden="true" />
                    <span>Fotografía del resultado</span>
                  </span>
                  {cloudinaryActivo ? (
                    <Badge variant="success">
                      <CheckCircle2 className="h-3 w-3" aria-hidden="true" />
                      <span>Nube activa</span>
                    </Badge>
                  ) : (
                    <Badge variant="warning">
                      <Info className="h-3 w-3" aria-hidden="true" />
                      <span>Modo demo local</span>
                    </Badge>
                  )}
                </div>

                {!cloudinaryActivo && (
                  <p className="text-[11px] leading-relaxed text-ink-secondary bg-amber-50/70 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800/60 rounded-lg px-2.5 py-2">
                    Cloudinary está en modo demo local. Al subir un archivo se
                    asignará una imagen de respaldo, o podés pegar directamente
                    una URL manual abajo.
                  </p>
                )}

                <input
                  type="file"
                  accept="image/*"
                  disabled={subiendoFoto}
                  onChange={handleSubirFotoCloudinary}
                  className="block w-full text-xs text-ink-secondary file:mr-3 file:h-8 file:px-3 file:rounded-lg file:border file:border-border-strong file:text-xs file:font-medium file:bg-surface file:text-ink-primary hover:file:bg-elevated file:cursor-pointer"
                />

                {subiendoFoto && (
                  <p
                    className={`text-xs font-medium ${SEMANTIC_COLORS.digital.textClass}`}
                  >
                    Procesando imagen...
                  </p>
                )}

                <InputField
                  label="URL de la fotografía (opcional)"
                  value={fichaFotoUrl}
                  onChange={(e) => {
                    setFichaFotoUrl(e.target.value);
                    setFotoPreviewError(false);
                  }}
                  placeholder="https://..."
                />

                {fichaFotoUrl.trim().length > 0 && (
                  <div className="relative rounded-lg border border-border-subtle bg-surface p-2 flex items-center gap-3">
                    {!fotoPreviewError ? (
                      // eslint-disable-next-line @next/next/no-img-element
                      <img
                        src={fichaFotoUrl}
                        alt="Vista previa del resultado capilar"
                        onError={() => setFotoPreviewError(true)}
                        className="h-16 w-16 rounded-md object-cover border border-border-subtle shrink-0"
                      />
                    ) : (
                      <div className="h-16 w-16 rounded-md bg-elevated border border-border-subtle flex items-center justify-center text-ink-secondary shrink-0">
                        <Camera className="h-5 w-5" aria-hidden="true" />
                      </div>
                    )}
                    <div className="min-w-0 flex-1 text-xs">
                      <p className="font-medium text-ink-primary">
                        Vista previa cargada
                      </p>
                      <p className="text-[11px] text-ink-secondary truncate">
                        {fotoPreviewError
                          ? 'No se pudo previsualizar la URL ingresada'
                          : fichaFotoUrl}
                      </p>
                    </div>
                    <button
                      type="button"
                      onClick={() => {
                        setFichaFotoUrl('');
                        setFotoPreviewError(false);
                      }}
                      aria-label="Quitar foto"
                      className="inline-flex h-7 w-7 items-center justify-center rounded-md text-ink-secondary hover:text-ink-primary hover:bg-elevated shrink-0"
                    >
                      <X className="h-3.5 w-3.5" aria-hidden="true" />
                    </button>
                  </div>
                )}
              </div>

              <Button
                type="submit"
                variant="primary"
                className="w-full"
                isLoading={guardandoFicha}
              >
                {fichaEditando
                  ? 'Guardar cambios en ficha'
                  : 'Registrar ficha técnica'}
              </Button>
            </form>

            {/* Listado Cronológico de Fichas del Cliente */}
            <div className="lg:col-span-7 max-h-[560px] overflow-y-auto pr-1">
              <AsyncStateGuard
                isLoading={cargandoFichas}
                isError={Boolean(errorFichas)}
                errorMessage={errorFichas ?? undefined}
                isEmpty={(fichasCliente ?? []).length === 0}
                loadingText="Cargando historial técnico capilar..."
                emptyTitle="Sin fichas técnicas capilares"
                emptyDescription="Este cliente aún no registra fichas técnicas."
                onRetry={() => {
                  if (clienteFichaActivo) {
                    void cargarFichasDeCliente(clienteFichaActivo);
                  }
                }}
                className="space-y-3"
              >
                {(fichasCliente ?? []).map((f) => (
                  <article
                    key={f.id}
                    className="p-4 rounded-xl border border-border-subtle bg-surface space-y-3 text-xs shadow-sm"
                  >
                    <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border-subtle pb-2.5">
                      <div className="flex items-center gap-2">
                        <span className="font-mono-tabular font-semibold text-ink-primary">
                          Ficha #{f.id}
                        </span>
                        <span className="text-ink-secondary">·</span>
                        <span className="font-mono-tabular text-ink-secondary">
                          {formatDateTime(f.fecha || f.fechaRegistro || '')}
                        </span>
                      </div>
                      <Badge variant="info">
                        {f.peluqueroNombreCompleto ||
                          f.nombrePeluquero ||
                          `Profesional #${f.peluqueroId}`}
                      </Badge>
                    </div>

                    {f.notasSensibilidadAlergias && (
                      <div
                        className={`p-2.5 rounded-lg border flex items-start gap-2 ${SEMANTIC_COLORS.danger.surfaceClass}`}
                      >
                        <AlertTriangle
                          className={`h-4 w-4 shrink-0 mt-0.5 ${SEMANTIC_COLORS.danger.textClass}`}
                          aria-hidden="true"
                        />
                        <div className="text-ink-primary">
                          <strong className={SEMANTIC_COLORS.danger.textClass}>
                            Sensibilidad / Alergias:
                          </strong>{' '}
                          {f.notasSensibilidadAlergias}
                        </div>
                      </div>
                    )}

                    <div className="space-y-1">
                      <p className="text-[11px] font-medium text-ink-secondary">
                        Fórmula aplicada
                      </p>
                      <p className="font-mono-tabular text-ink-primary bg-elevated/70 p-2.5 rounded-lg border border-border-subtle whitespace-pre-wrap">
                        {f.formulaAplicada || f.detalleFormulaTintura}
                      </p>
                    </div>

                    {f.productosUtilizados && (
                      <p className="text-ink-secondary">
                        <strong className="text-ink-primary">Productos:</strong>{' '}
                        {f.productosUtilizados}
                      </p>
                    )}

                    {f.observaciones && (
                      <p className="text-ink-secondary">
                        <strong className="text-ink-primary">
                          Observaciones:
                        </strong>{' '}
                        {f.observaciones}
                      </p>
                    )}

                    {f.fotoResultadoUrl && (
                      <div className="flex items-center gap-3 p-2.5 rounded-lg bg-elevated/50 border border-border-subtle">
                        {/* eslint-disable-next-line @next/next/no-img-element */}
                        <img
                          src={f.fotoResultadoUrl}
                          alt={`Resultado ficha #${f.id}`}
                          className="h-14 w-14 rounded-md object-cover border border-border-subtle shrink-0"
                        />
                        <div className="min-w-0 flex-1">
                          <p className="font-medium text-ink-primary">
                            Fotografía del resultado
                          </p>
                          <a
                            href={f.fotoResultadoUrl}
                            target="_blank"
                            rel="noreferrer"
                            className="inline-flex items-center gap-1 text-brand-primary hover:underline font-medium mt-0.5"
                          >
                            <span>Abrir en tamaño completo</span>
                            <ExternalLink
                              className="h-3 w-3"
                              aria-hidden="true"
                            />
                          </a>
                        </div>
                      </div>
                    )}

                    <div className="flex justify-end gap-1.5 pt-2 border-t border-border-subtle">
                      <Button
                        size="sm"
                        variant="secondary"
                        leftIcon={
                          <Pencil className="h-3.5 w-3.5" aria-hidden="true" />
                        }
                        onClick={() => seleccionarFichaParaEditar(f)}
                      >
                        Editar
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        destructiveTone="soft"
                        leftIcon={
                          <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
                        }
                        onClick={() => handleEliminarFicha(f.id)}
                      >
                        Eliminar
                      </Button>
                    </div>
                  </article>
                ))}
              </AsyncStateGuard>
            </div>
          </div>
        </div>
      </Modal>
    </div>
  );
}
