// filepath: src/app/(dashboard)/recepcion/clientes/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
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
import { uploadImageToCloudinary } from '@/lib/cloudinary';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
  extractPaginationMeta,
} from '@/lib/apiHelpers';
import { formatDateOnly, formatDateTime } from '@/lib/formatters';
import {
  AsyncStateGuard,
  Badge,
  Button,
  InputField,
  Modal,
  PageHeader,
  PaginationBar,
} from '@/components/ui';

export default function ClientesCrm360Page() {
  const { catalogos } = useCatalogos();

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

  // Modal / Drawer de Fichas Técnicas Capilares por Cliente
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
    cargarClientes();
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
      c.fechaNacimiento
        ? c.fechaNacimiento.split('T')[0]
        : '1995-05-15'
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
            ? 'Ficha del cliente actualizada.'
            : 'Nuevo cliente registrado en el CRM.'
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
        notifyApiSuccess(res, `Cliente ${c.nombreCompleto} dado de baja.`);
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
  };

  const limpiarFormFicha = () => {
    setFichaEditando(null);
    setFichaFormula('');
    setFichaObservaciones('');
    setFichaAlergias('');
    setFichaProductos('');
    setFichaFotoUrl('');
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
    <div className="flex-1 flex flex-col min-h-0">
      <PageHeader
        eyebrow="CRM 360° & Diagnóstico Capilar • ClientesController + FichasTecnicasController"
        title="Padrón de Clientes y Fichas Técnicas Capilares"
        subtitle={`Total de clientes registrados en la sucursal: ${totalCount} • Alertas de cumpleaños y sensibilidad cutánea`}
        actions={
          <Button variant="editorial" size="sm" onClick={abrirNuevoCliente}>
            + Nuevo Cliente
          </Button>
        }
      />

      <div className="p-6 space-y-4">
        {/* Barra de Búsqueda */}
        <div className="bg-white border border-slate-200 rounded-lg p-4 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2 flex-1 max-w-md">
            <input
              type="search"
              placeholder="Buscar por nombre, apellido, teléfono o email..."
              value={busqueda}
              onChange={(e) => {
                setBusqueda(e.target.value);
                setPageNumber(1);
              }}
              className="w-full rounded-md border border-slate-300 px-3 py-2 text-xs"
            />
          </div>
          <div className="text-xs text-slate-500">
            Página <strong>{pageNumber}</strong> de{' '}
            <strong>{totalPages}</strong> ({totalCount} registros)
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
          <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse text-xs">
                <thead>
                  <tr className="bg-slate-50 border-b border-slate-200 text-[11px] uppercase text-slate-500">
                    <th className="py-3 px-4">Cliente</th>
                    <th className="py-3 px-4">Contacto</th>
                    <th className="py-3 px-4">Diagnóstico (Cabello / Rostro)</th>
                    <th className="py-3 px-4">Nacimiento</th>
                    <th className="py-3 px-4 text-right">Acciones CRM</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {(clientes ?? []).map((c) => (
                    <tr key={c.id} className="hover:bg-slate-50">
                      <td className="py-3 px-4">
                        <div className="flex items-center gap-2">
                          <span className="font-bold text-slate-900">
                            {c.nombreCompleto || `${c.nombre} ${c.apellido}`}
                          </span>
                          {c.esCumpleaneroDelMes && (
                            <Badge variant="warning">🎂 Cumpleañero del Mes</Badge>
                          )}
                        </div>
                        <span className="text-[11px] text-slate-400 font-mono">
                          ID #{c.id}
                        </span>
                      </td>
                      <td className="py-3 px-4">
                        <p className="font-mono font-semibold text-slate-800">
                          {c.telefono}
                        </p>
                        <p className="text-[11px] text-slate-500">
                          {c.email || 'Sin correo'}
                        </p>
                      </td>
                      <td className="py-3 px-4">
                        <div className="flex flex-wrap gap-1.5">
                          <Badge variant="neutral">
                            Cabello: {c.tipoCabelloNombre || 'No especificado'}
                          </Badge>
                          <Badge variant="neutral">
                            Rostro: {c.tipoRostroNombre || 'No especificado'}
                          </Badge>
                        </div>
                      </td>
                      <td className="py-3 px-4 text-slate-600">
                        {formatDateOnly(c.fechaNacimiento)}
                      </td>
                      <td className="py-3 px-4 text-right space-x-1.5">
                        <Button
                          size="sm"
                          variant="editorial"
                          onClick={() => cargarFichasDeCliente(c)}
                        >
                          🧪 Fichas Técnicas
                        </Button>
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => abrirEditarCliente(c)}
                        >
                          Editar
                        </Button>
                        <Button
                          size="sm"
                          variant="danger"
                          onClick={() => handleEliminarCliente(c)}
                        >
                          Baja
                        </Button>
                      </td>
                    </tr>
                  ))}
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
            ? `Editar Cliente #${clienteEditando.id}`
            : 'Registrar Nuevo Cliente'
        }
        subtitle="Completá los datos personales y el perfil morfológico capilar."
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
              label="Correo Electrónico"
              type="email"
              value={cliEmail}
              onChange={(e) => setCliEmail(e.target.value)}
            />
            <InputField
              label="Fecha de Nacimiento *"
              type="date"
              required
              value={cliFechaNac}
              onChange={(e) => setCliFechaNac(e.target.value)}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-slate-700 block mb-1">
                Tipo de Cabello
              </label>
              <select
                value={cliTipoCabelloId}
                onChange={(e) => setCliTipoCabelloId(Number(e.target.value))}
                className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
              >
                <option value={0}>Sin especificar</option>
                {(catalogos?.tiposCabello || []).map((tc) => (
                  <option key={tc.id} value={tc.id}>
                    {tc.nombre}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-slate-700 block mb-1">
                Tipo de Rostro
              </label>
              <select
                value={cliTipoRostroId}
                onChange={(e) => setCliTipoRostroId(Number(e.target.value))}
                className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm"
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
              Guardar Cliente
            </Button>
          </div>
        </form>
      </Modal>

      {/* Modal / Drawer de Fichas Técnicas Capilares y Subida Cloudinary */}
      <Modal
        isOpen={!!clienteFichaActivo}
        onClose={() => setClienteFichaActivo(null)}
        title={`Historial de Fichas Técnicas — ${
          clienteFichaActivo?.nombreCompleto || ''
        }`}
        subtitle="Fórmulas de colorimetría, oxidantes, notas de alergia y fotografía Direct-to-Cloud (Cloudinary)."
        maxWidthClass="max-w-4xl"
      >
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          {/* Formulario de Nueva / Edición de Ficha Técnica */}
          <form
            onSubmit={handleGuardarFichaTecnica}
            className="lg:col-span-5 space-y-3 bg-slate-50 p-4 rounded-lg border border-slate-200"
          >
            <div className="flex items-center justify-between">
              <h3 className="text-xs font-bold uppercase tracking-wider text-slate-800">
                {fichaEditando
                  ? `Editar Ficha #${fichaEditando.id}`
                  : 'Nueva Ficha Técnica Capilar'}
              </h3>
              {fichaEditando && (
                <button
                  type="button"
                  onClick={limpiarFormFicha}
                  className="text-xs text-amber-900 underline font-semibold"
                >
                  + Crear Nueva
                </button>
              )}
            </div>

            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-slate-700 block mb-1">
                Profesional Responsable *
              </label>
              <select
                value={fichaPeluqueroId}
                onChange={(e) => setFichaPeluqueroId(Number(e.target.value))}
                className="w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-xs"
              >
                {peluqueros.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.nombreCompleto || `${p.nombre} ${p.apellido}`}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-xs font-semibold uppercase tracking-wider text-slate-700">
                Fórmula Aplicada (Colorimetría / Técnica) *
              </label>
              <textarea
                rows={3}
                required
                value={fichaFormula}
                onChange={(e) => setFichaFormula(e.target.value)}
                placeholder="Ej. Pomo 7.1 (45g) + 8.11 (15g) con Oxidante 20 Vol. Tiempo de pose: 35 min."
                className="rounded-md border border-slate-300 bg-white px-3 py-2 text-xs"
              />
            </div>

            <div className="flex flex-col gap-1">
              <label className="text-xs font-semibold uppercase tracking-wider text-red-700">
                ⚠️ Notas de Sensibilidad o Alergias Cutáneas
              </label>
              <textarea
                rows={2}
                value={fichaAlergias}
                onChange={(e) => setFichaAlergias(e.target.value)}
                placeholder="Ej. Sensibilidad a decolorantes con amoníaco en nuca."
                className="rounded-md border border-red-300 bg-red-50/40 px-3 py-2 text-xs"
              />
            </div>

            <InputField
              label="Productos de Gabinete Utilizados"
              value={fichaProductos}
              onChange={(e) => setFichaProductos(e.target.value)}
              placeholder="Ej. Ampolla Cauterización + Shampoo Ácido"
            />

            <InputField
              label="Observaciones Generales"
              value={fichaObservaciones}
              onChange={(e) => setFichaObservaciones(e.target.value)}
            />

            {/* Subida Direct-to-Cloud a Cloudinary */}
            <div className="space-y-1.5 pt-1 border-t border-slate-200">
              <label className="text-xs font-semibold uppercase tracking-wider text-slate-700 block">
                Fotografía del Resultado Capilar (Cloudinary CDN)
              </label>
              <input
                type="file"
                accept="image/*"
                disabled={subiendoFoto}
                onChange={handleSubirFotoCloudinary}
                className="block w-full text-xs text-slate-600 file:mr-3 file:py-1.5 file:px-3 file:rounded file:border-0 file:text-xs file:font-semibold file:bg-slate-900 file:text-white hover:file:bg-slate-800"
              />
              {subiendoFoto && (
                <p className="text-[11px] text-amber-800 font-semibold">
                  Subiendo imagen a Cloudinary CDN...
                </p>
              )}
              <InputField
                label="URL de la Fotografía (Autocompletada o Manual)"
                value={fichaFotoUrl}
                onChange={(e) => setFichaFotoUrl(e.target.value)}
                placeholder="https://res.cloudinary.com/..."
              />
            </div>

            <Button
              type="submit"
              variant="editorial"
              className="w-full"
              isLoading={guardandoFicha}
            >
              {fichaEditando
                ? 'Guardar Cambios en Ficha'
                : 'Registrar Ficha Técnica'}
            </Button>
          </form>

          {/* Listado Cronológico de Fichas del Cliente */}
          <div className="lg:col-span-7 max-h-[540px] overflow-y-auto pr-1">
            <AsyncStateGuard
              isLoading={cargandoFichas}
              isError={Boolean(errorFichas)}
              errorMessage={errorFichas ?? undefined}
              isEmpty={(fichasCliente ?? []).length === 0}
              loadingText="Cargando historial técnico capilar..."
              emptyTitle="Sin fichas técnicas capilares"
              emptyDescription="Este cliente aún no registra fichas técnicas capilares."
              onRetry={() => {
                if (clienteFichaActivo) {
                  void cargarFichasDeCliente(clienteFichaActivo);
                }
              }}
              className="space-y-3"
            >
              {(fichasCliente ?? []).map((f) => (
                <div
                  key={f.id}
                  className="p-4 rounded-lg border border-slate-200 bg-white space-y-2 text-xs shadow-sm"
                >
                  <div className="flex items-center justify-between border-b border-slate-100 pb-2">
                    <div>
                      <span className="font-mono font-bold text-slate-800">
                        Ficha #{f.id}
                      </span>{' '}
                      •{' '}
                      <span className="text-slate-500">
                        {formatDateTime(f.fecha || f.fechaRegistro || '')}
                      </span>
                    </div>
                    <Badge variant="info">
                      Estilista:{' '}
                      {f.peluqueroNombreCompleto ||
                        f.nombrePeluquero ||
                        `#${f.peluqueroId}`}
                    </Badge>
                  </div>

                  <div>
                    <p className="text-[11px] uppercase font-bold text-slate-500">
                      Fórmula Aplicada:
                    </p>
                    <p className="font-mono text-slate-900 bg-slate-50 p-2 rounded border border-slate-200 mt-0.5">
                      {f.formulaAplicada || f.detalleFormulaTintura}
                    </p>
                  </div>

                  {f.notasSensibilidadAlergias && (
                    <div className="p-2 rounded bg-red-50 border border-red-200 text-red-900">
                      <strong>⚠️ Alerta Sensibilidad / Alergias:</strong>{' '}
                      {f.notasSensibilidadAlergias}
                    </div>
                  )}

                  {f.productosUtilizados && (
                    <p className="text-slate-600">
                      <strong>Productos:</strong> {f.productosUtilizados}
                    </p>
                  )}

                  {f.observaciones && (
                    <p className="text-slate-600">
                      <strong>Observaciones:</strong> {f.observaciones}
                    </p>
                  )}

                  {f.fotoResultadoUrl && (
                    <div className="pt-1">
                      <a
                        href={f.fotoResultadoUrl}
                        target="_blank"
                        rel="noreferrer"
                        className="text-amber-900 underline font-semibold"
                      >
                        📷 Ver Fotografía del Resultado Capilar
                      </a>
                    </div>
                  )}

                  <div className="flex justify-end gap-2 pt-2 border-t border-slate-100">
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => seleccionarFichaParaEditar(f)}
                    >
                      Editar
                    </Button>
                    <Button
                      size="sm"
                      variant="danger"
                      onClick={() => handleEliminarFicha(f.id)}
                    >
                      Eliminar
                    </Button>
                  </div>
                </div>
              ))}
            </AsyncStateGuard>
          </div>
        </div>
      </Modal>
    </div>
  );
}
