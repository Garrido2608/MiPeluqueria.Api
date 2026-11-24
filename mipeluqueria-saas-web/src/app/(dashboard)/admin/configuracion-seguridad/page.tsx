// filepath: src/app/(dashboard)/admin/configuracion-seguridad/page.tsx
'use client';

import React, { useCallback, useEffect, useState } from 'react';
import {
  CatalogoItemDto,
  ConfiguracionSalonDto,
  HistorialSesionDto,
  TipoCatalogoSalon,
  UsuarioDto,
} from '@/types/api.types';
import {
  catalogosService,
  configuracionSalonService,
  usuariosService,
} from '@/services/api.services';
import { useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
  extractPaginationMeta,
} from '@/lib/apiHelpers';
import { formatDateTime } from '@/lib/formatters';
import {
  AsyncStateGuard,
  Badge,
  Button,
  InputField,
  Modal,
  PaginationBar,
} from '@/components/ui';

const CATALOGOS_EDITABLES: TipoCatalogoSalon[] = [
  'categorias-servicio',
  'especialidades',
  'motivos-cancelacion',
  'categorias-producto',
  'medios-pago',
  'tipos-cabello',
  'tipos-rostro',
];

export default function AdminConfiguracionYSeguridadPage() {
  const { configSalon, catalogos, recargarCatalogosYConfig } = useCatalogos();

  const [subTab, setSubTab] = useState<'salon' | 'catalogos' | 'usuarios'>(
    'salon'
  );
  const [feedback, setFeedback] = useState<string | null>(null);

  const [formConfig, setFormConfig] = useState<ConfiguracionSalonDto>({
    nombreComercial: 'Mi Peluquería & Barbería SaaS',
    razonSocial: 'Estudio Capilar S.R.L.',
    cuit: '30-71548963-9',
    direccion: 'Av. Colón 1250, Córdoba, Argentina',
    telefono: '+54 9 351 555-0199',
    emailContacto: 'contacto@mipeluqueria.com.ar',
    moneda: 'ARS',
    timeZoneId: 'America/Argentina/Cordoba',
    intervaloTurnosMinutos: 30,
    minutosToleranciaEspera: 15,
    horasAnticipacionCancelacion: 24,
    porcentajeSenaReserva: 0,
    notificacionesWhatsAppActivas: true,
    notificacionesEmailActivas: true,
  });

  // Estado para CRUD completo de los 7 Catálogos del Salón (+ 2 lectura)
  const [tipoCatalogo, setTipoCatalogo] = useState<
    TipoCatalogoSalon | 'estados-turno' | 'roles'
  >('categorias-servicio');
  const [itemEditando, setItemEditando] = useState<CatalogoItemDto | null>(
    null
  );
  const [nuevoItemNombre, setNuevoItemNombre] = useState<string>('');
  const [nuevoItemDescripcion, setNuevoItemDescripcion] = useState<string>('');
  const [esEfectivoFisico, setEsEfectivoFisico] = useState<boolean>(false);

  // Estado para Gestión de Usuarios Staff y Sesiones Paginadas
  const [usuarios, setUsuarios] = useState<UsuarioDto[]>([]);
  const [sesiones, setSesiones] = useState<HistorialSesionDto[]>([]);
  const [paginaSesiones, setPaginaSesiones] = useState<number>(1);
  const [totalPaginasSesiones, setTotalPaginasSesiones] = useState<number>(1);
  const [totalSesiones, setTotalSesiones] = useState<number>(0);
  const [cargandoUsuarios, setCargandoUsuarios] = useState<boolean>(true);
  const [errorUsuarios, setErrorUsuarios] = useState<{
    message: string;
    correlationId?: string;
  } | null>(null);
  const [filtroUsuarioSesion, setFiltroUsuarioSesion] = useState<
    number | undefined
  >(undefined);

  // Modal de Alta de Cuenta de Personal (POST /api/usuarios)
  const [modalNuevoStaffOpen, setModalNuevoStaffOpen] =
    useState<boolean>(false);
  const [staffUsername, setStaffUsername] = useState<string>('');
  const [staffEmail, setStaffEmail] = useState<string>('');
  const [staffPassword, setStaffPassword] = useState<string>('');
  const [staffRolId, setStaffRolId] = useState<number>(2);
  const [staffPeluqueroId, setStaffPeluqueroId] = useState<string>('');

  const cargarUsuariosYSesiones = useCallback(async () => {
    setCargandoUsuarios(true);
    setErrorUsuarios(null);
    try {
      const [uRes, sRes] = await Promise.all([
        usuariosService.getAll(),
        usuariosService.getHistorialSesionesPaged(
          { pageNumber: paginaSesiones, pageSize: 15 },
          filtroUsuarioSesion
        ),
      ]);
      if (uRes.success) {
        setUsuarios(extractPagedItems<UsuarioDto>(uRes.data));
      }
      if (sRes.success) {
        const items = extractPagedItems<HistorialSesionDto>(sRes.data);
        const meta = extractPaginationMeta(sRes.data, {
          pageNumber: paginaSesiones,
          pageSize: 15,
        });
        setSesiones(items);
        setTotalPaginasSesiones(meta.totalPages);
        setTotalSesiones(meta.totalCount);
      }
    } catch (err) {
      setErrorUsuarios({
        message: extractErrorMessage(
          err,
          'No se pudieron cargar las cuentas de usuario y el historial de sesiones.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
    } finally {
      setCargandoUsuarios(false);
    }
  }, [paginaSesiones, filtroUsuarioSesion]);

  useEffect(() => {
    if (configSalon) setFormConfig(configSalon);
  }, [configSalon]);

  useEffect(() => {
    cargarUsuariosYSesiones();
  }, [cargarUsuariosYSesiones]);

  const guardarConfigSalon = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await configuracionSalonService.actualizar(formConfig);
      if (res.success) {
        notifyApiSuccess(
          res,
          'Configuración comercial del salón actualizada.'
        );
        await recargarCatalogosYConfig();
        setFeedback(
          'Configuración comercial del salón actualizada en caché y almacenamiento.'
        );
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'Error al guardar la configuración del salón.',
      });
    }
  };

  const esCatalogoEditable = (
    cat: typeof tipoCatalogo
  ): cat is TipoCatalogoSalon =>
    CATALOGOS_EDITABLES.includes(cat as TipoCatalogoSalon);

  const handleGuardarItemCatalogo = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!nuevoItemNombre.trim() || !esCatalogoEditable(tipoCatalogo)) return;

    const dto = {
      nombre: nuevoItemNombre.trim(),
      descripcion: nuevoItemDescripcion.trim() || nuevoItemNombre.trim(),
      esEfectivoFisico:
        tipoCatalogo === 'medios-pago' ? esEfectivoFisico : undefined,
    };

    try {
      const res = itemEditando
        ? await catalogosService.actualizarPorTipo(
            tipoCatalogo,
            itemEditando.id,
            dto
          )
        : await catalogosService.crearPorTipo(tipoCatalogo, dto);

      if (res.success) {
        notifyApiSuccess(
          res,
          itemEditando
            ? `Registro #${itemEditando.id} actualizado en "${tipoCatalogo}".`
            : `Ítem agregado al catálogo "${tipoCatalogo}".`
        );
        setItemEditando(null);
        setNuevoItemNombre('');
        setNuevoItemDescripcion('');
        setEsEfectivoFisico(false);
        await recargarCatalogosYConfig();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el registro del catálogo.',
      });
    }
  };

  const handleEliminarItemCatalogo = async (id: number) => {
    if (!esCatalogoEditable(tipoCatalogo)) return;
    try {
      const res = await catalogosService.eliminarPorTipo(tipoCatalogo, id);
      if (res.success) {
        notifyApiSuccess(res, 'Registro eliminado del catálogo.');
        await recargarCatalogosYConfig();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage:
          'No se puede eliminar este registro porque posee entidades asociadas.',
      });
    }
  };

  const handleCrearUsuarioStaff = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await usuariosService.crearStaff({
        username: staffUsername.trim(),
        email: staffEmail.trim(),
        password: staffPassword,
        rolId: staffRolId,
        peluqueroId: staffPeluqueroId ? Number(staffPeluqueroId) : null,
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          `Cuenta de personal "${staffUsername}" creada exitosamente.`
        );
        setModalNuevoStaffOpen(false);
        setStaffUsername('');
        setStaffEmail('');
        setStaffPassword('');
        setStaffPeluqueroId('');
        await cargarUsuariosYSesiones();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo crear la cuenta de personal.',
      });
    }
  };

  const toggleEstadoUsuario = async (u: UsuarioDto) => {
    try {
      const res = await usuariosService.actualizarEstado(u.id, {
        activo: !u.activo,
        rolId: u.rolId,
      });
      if (res.success) {
        notifyApiSuccess(res, `Estado del usuario ${u.username} actualizado.`);
        await cargarUsuariosYSesiones();
      }
    } catch (err) {
      notifyApiError(err);
    }
  };

  const handleCambiarRolUsuario = async (
    u: UsuarioDto,
    nuevoRolId: number
  ) => {
    try {
      const res = await usuariosService.cambiarRol(u.id, nuevoRolId, u.activo);
      if (res.success) {
        notifyApiSuccess(
          res,
          `Rol del usuario ${u.username} actualizado a Rol #${nuevoRolId}.`
        );
        await cargarUsuariosYSesiones();
      }
    } catch (err) {
      notifyApiError(err);
    }
  };

  const desbloquearUsuario = async (id: number) => {
    try {
      const res = await usuariosService.desbloquear(id);
      if (res.success) {
        notifyApiSuccess(
          res,
          `Cuenta de usuario #${id} desbloqueada y reactivada.`
        );
        await cargarUsuariosYSesiones();
      }
    } catch (err) {
      notifyApiError(err);
    }
  };

  const obtenerItemsTablaSeleccionada = (): CatalogoItemDto[] => {
    if (!catalogos) return [];
    switch (tipoCatalogo) {
      case 'categorias-servicio':
        return catalogos.categoriasServicio || [];
      case 'especialidades':
        return catalogos.especialidades || [];
      case 'motivos-cancelacion':
        return catalogos.motivosCancelacion || [];
      case 'categorias-producto':
        return catalogos.categoriasProducto || [];
      case 'medios-pago':
        return catalogos.mediosPago || [];
      case 'tipos-cabello':
        return catalogos.tiposCabello || [];
      case 'tipos-rostro':
        return catalogos.tiposRostro || [];
      case 'estados-turno':
        return catalogos.estadosTurno || [];
      case 'roles':
        return catalogos.roles || [];
    }
  };

  return (
    <div className="p-6 space-y-6">
      <header className="bg-white border border-slate-200 rounded-lg p-5 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold text-slate-900">
            Configuración del Salón, Catálogos Maestros y Seguridad
          </h1>
          <p className="text-xs text-slate-500">
            Parámetros de operación, CRUD completo en los 7 catálogos del salón,
            cuentas de personal RBAC y auditoría de accesos.
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant={subTab === 'salon' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('salon')}
          >
            1. Ajustes del Salón
          </Button>
          <Button
            variant={subTab === 'catalogos' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('catalogos')}
          >
            2. Catálogos Maestros (7 CRUD + 2 Sistema)
          </Button>
          <Button
            variant={subTab === 'usuarios' ? 'primary' : 'secondary'}
            size="sm"
            onClick={() => setSubTab('usuarios')}
          >
            3. Usuarios y Sesiones ({usuarios.length})
          </Button>
        </div>
      </header>

      {feedback && (
        <div className="p-3 rounded bg-slate-900 text-white text-xs flex justify-between">
          <span>{feedback}</span>
          <button type="button" onClick={() => setFeedback(null)}>
            ✕
          </button>
        </div>
      )}

      {subTab === 'salon' && (
        <form
          onSubmit={guardarConfigSalon}
          className="bg-white border border-slate-200 rounded-lg p-6 space-y-4"
        >
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <InputField
              label="Nombre Comercial *"
              value={formConfig.nombreComercial}
              onChange={(e) =>
                setFormConfig({
                  ...formConfig,
                  nombreComercial: e.target.value,
                })
              }
            />
            <InputField
              label="Razón Social"
              value={formConfig.razonSocial}
              onChange={(e) =>
                setFormConfig({ ...formConfig, razonSocial: e.target.value })
              }
            />
            <InputField
              label="CUIT"
              value={formConfig.cuit}
              onChange={(e) =>
                setFormConfig({ ...formConfig, cuit: e.target.value })
              }
            />
            <InputField
              label="Dirección"
              value={formConfig.direccion}
              onChange={(e) =>
                setFormConfig({ ...formConfig, direccion: e.target.value })
              }
            />
            <InputField
              label="Teléfono WhatsApp"
              value={formConfig.telefono}
              onChange={(e) =>
                setFormConfig({ ...formConfig, telefono: e.target.value })
              }
            />
            <InputField
              label="Moneda ISO (ARS/USD)"
              value={formConfig.moneda}
              onChange={(e) =>
                setFormConfig({ ...formConfig, moneda: e.target.value })
              }
            />
            <InputField
              label="Huso Horario IANA (TimeZoneId)"
              value={formConfig.timeZoneId || 'America/Argentina/Cordoba'}
              onChange={(e) =>
                setFormConfig({ ...formConfig, timeZoneId: e.target.value })
              }
            />
            <InputField
              label="Intervalo Agenda (Minutos)"
              type="number"
              value={formConfig.intervaloTurnosMinutos}
              onChange={(e) =>
                setFormConfig({
                  ...formConfig,
                  intervaloTurnosMinutos: Number(e.target.value),
                })
              }
            />
            <InputField
              label="Tolerancia de Espera (Minutos)"
              type="number"
              value={formConfig.minutosToleranciaEspera ?? 15}
              onChange={(e) =>
                setFormConfig({
                  ...formConfig,
                  minutosToleranciaEspera: Number(e.target.value),
                })
              }
            />
            <InputField
              label="Anticipación Cancelación (Horas)"
              type="number"
              value={formConfig.horasAnticipacionCancelacion ?? 24}
              onChange={(e) =>
                setFormConfig({
                  ...formConfig,
                  horasAnticipacionCancelacion: Number(e.target.value),
                })
              }
            />
            <InputField
              label="% Seña Online (0 = Sin Seña)"
              type="number"
              value={formConfig.porcentajeSenaReserva ?? 0}
              onChange={(e) =>
                setFormConfig({
                  ...formConfig,
                  porcentajeSenaReserva: Number(e.target.value),
                })
              }
            />
          </div>
          <div className="flex flex-wrap gap-6 pt-2 text-xs font-semibold">
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={formConfig.notificacionesWhatsAppActivas ?? true}
                onChange={(e) =>
                  setFormConfig({
                    ...formConfig,
                    notificacionesWhatsAppActivas: e.target.checked,
                  })
                }
              />
              Recordatorios Automáticos WhatsApp (24h y 2h antes) Activos
            </label>
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={formConfig.notificacionesEmailActivas ?? true}
                onChange={(e) =>
                  setFormConfig({
                    ...formConfig,
                    notificacionesEmailActivas: e.target.checked,
                  })
                }
              />
              Notificaciones Email SMTP Activas
            </label>
          </div>
          <Button type="submit" variant="primary">
            Guardar Configuración del Salón
          </Button>
        </form>
      )}

      {subTab === 'catalogos' && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          <form
            onSubmit={handleGuardarItemCatalogo}
            className="lg:col-span-5 bg-white border border-slate-200 rounded-lg p-5 space-y-4"
          >
            <div className="flex items-center justify-between">
              <h2 className="text-base font-bold">
                {itemEditando
                  ? `Editar Registro #${itemEditando.id}`
                  : 'Gestión de Catálogos Maestros'}
              </h2>
              {itemEditando && (
                <button
                  type="button"
                  onClick={() => {
                    setItemEditando(null);
                    setNuevoItemNombre('');
                    setNuevoItemDescripcion('');
                    setEsEfectivoFisico(false);
                  }}
                  className="text-xs text-amber-900 underline font-semibold"
                >
                  Cancelar Edición
                </button>
              )}
            </div>

            <select
              value={tipoCatalogo}
              onChange={(e) => {
                setTipoCatalogo(e.target.value as typeof tipoCatalogo);
                setItemEditando(null);
                setNuevoItemNombre('');
                setNuevoItemDescripcion('');
                setEsEfectivoFisico(false);
              }}
              className="w-full rounded border border-slate-300 px-3 py-2 text-sm"
            >
              <option value="categorias-servicio">
                1. Categorías de Servicio (CRUD Completo)
              </option>
              <option value="especialidades">
                2. Especialidades de Peluquero (CRUD Completo)
              </option>
              <option value="motivos-cancelacion">
                3. Motivos de Cancelación (CRUD Completo)
              </option>
              <option value="categorias-producto">
                4. Categorías de Producto (CRUD Completo)
              </option>
              <option value="medios-pago">
                5. Medios de Pago POS (CRUD Completo + Efectivo Físico)
              </option>
              <option value="tipos-cabello">
                6. Tipos de Cabello (CRUD Completo)
              </option>
              <option value="tipos-rostro">
                7. Tipos de Rostro (CRUD Completo)
              </option>
              <option value="estados-turno">
                8. Estados de Turno (Catálogo Fijo de Sistema)
              </option>
              <option value="roles">
                9. Roles RBAC (Catálogo Fijo de Sistema)
              </option>
            </select>

            {esCatalogoEditable(tipoCatalogo) && (
              <>
                <InputField
                  label="Nombre del Registro *"
                  required
                  value={nuevoItemNombre}
                  onChange={(e) => setNuevoItemNombre(e.target.value)}
                />
                <InputField
                  label="Descripción Adicional"
                  value={nuevoItemDescripcion}
                  onChange={(e) => setNuevoItemDescripcion(e.target.value)}
                />
                {tipoCatalogo === 'medios-pago' && (
                  <label className="flex items-center gap-2 text-xs font-semibold text-slate-700 pt-1">
                    <input
                      type="checkbox"
                      checked={esEfectivoFisico}
                      onChange={(e) => setEsEfectivoFisico(e.target.checked)}
                    />
                    ¿Es Efectivo Físico? (Suma billetes reales al cajón de
                    arqueo)
                  </label>
                )}
                <Button type="submit" variant="primary" className="w-full">
                  {itemEditando
                    ? 'Guardar Cambios (PUT)'
                    : 'Crear Registro en Catálogo (POST)'}
                </Button>
              </>
            )}
          </form>

          <div className="lg:col-span-7 bg-white border border-slate-200 rounded-lg p-5 space-y-3 text-xs">
            <h2 className="text-base font-bold uppercase text-slate-700">
              Registros de la Tabla: {tipoCatalogo}
            </h2>
            <div className="divide-y divide-slate-100">
              {obtenerItemsTablaSeleccionada().map((item) => (
                <div
                  key={item.id}
                  className="py-2.5 flex justify-between items-center gap-2"
                >
                  <div>
                    <span className="font-semibold text-slate-900">
                      #{item.id} — {item.nombre || item.descripcion}
                    </span>
                    {tipoCatalogo === 'medios-pago' && (
                      <span className="ml-2">
                        <Badge
                          variant={item.esEfectivoFisico ? 'success' : 'info'}
                        >
                          {item.esEfectivoFisico
                            ? 'Billetes Físicos'
                            : 'Digital / Bancario'}
                        </Badge>
                      </span>
                    )}
                  </div>
                  {esCatalogoEditable(tipoCatalogo) && (
                    <div className="flex gap-1.5">
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => {
                          setItemEditando(item);
                          setNuevoItemNombre(
                            item.nombre || item.descripcion || ''
                          );
                          setNuevoItemDescripcion(item.descripcion || '');
                          setEsEfectivoFisico(Boolean(item.esEfectivoFisico));
                        }}
                      >
                        Editar
                      </Button>
                      <Button
                        variant="danger"
                        size="sm"
                        onClick={() => handleEliminarItemCatalogo(item.id)}
                      >
                        Eliminar
                      </Button>
                    </div>
                  )}
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {subTab === 'usuarios' && (
        <div className="space-y-6">
          <div className="bg-white border border-slate-200 rounded-lg p-6 space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="text-base font-bold">
                Cuentas de Personal, Cambio de Rol RBAC y Desbloqueo Antibot
              </h2>
              <Button
                variant="editorial"
                size="sm"
                onClick={() => setModalNuevoStaffOpen(true)}
              >
                + Crear Usuario Staff
              </Button>
            </div>
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-600">
                <tr>
                  <th className="p-2">Usuario</th>
                  <th className="p-2">Email</th>
                  <th className="p-2">Rol Asignado (RBAC)</th>
                  <th className="p-2">Intentos / Estado</th>
                  <th className="p-2 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {usuarios.map((u) => (
                  <tr key={u.id}>
                    <td className="p-2 font-bold">{u.username}</td>
                    <td className="p-2">{u.email}</td>
                    <td className="p-2">
                      <select
                        value={u.rolId}
                        onChange={(e) =>
                          handleCambiarRolUsuario(u, Number(e.target.value))
                        }
                        className="rounded border border-slate-300 px-2 py-1 text-xs"
                      >
                        {(
                          catalogos?.roles || [
                            { id: 1, nombre: 'Administrador' },
                            { id: 2, nombre: 'Recepcionista' },
                            { id: 3, nombre: 'Peluquero' },
                            { id: 4, nombre: 'Cliente' },
                            { id: 5, nombre: 'Cajero' },
                            { id: 6, nombre: 'SuperAdminSaaS' },
                          ]
                        ).map((r) => (
                          <option key={r.id} value={r.id}>
                            {r.nombre}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className="p-2 font-mono">
                      {u.activo ? 'Activo' : 'Inactivo'} • Fallos:{' '}
                      {u.intentosFallidos}
                    </td>
                    <td className="p-2 text-right space-x-1.5">
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => toggleEstadoUsuario(u)}
                      >
                        {u.activo ? 'Inactivar' : 'Activar'}
                      </Button>
                      <Button
                        variant="editorial"
                        size="sm"
                        onClick={() => desbloquearUsuario(u.id)}
                      >
                        Desbloquear Cuenta
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="bg-white border border-slate-200 rounded-lg p-6 space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="text-base font-bold">
                Auditoría Forense de Accesos (HistorialSesiones Paginado)
              </h2>
              <select
                value={filtroUsuarioSesion || ''}
                onChange={(e) => {
                  setFiltroUsuarioSesion(
                    e.target.value ? Number(e.target.value) : undefined
                  );
                  setPaginaSesiones(1);
                }}
                className="rounded border border-slate-300 px-2.5 py-1 text-xs"
              >
                <option value="">Todos los Usuarios</option>
                {usuarios.map((u) => (
                  <option key={u.id} value={u.id}>
                    #{u.id} - {u.username}
                  </option>
                ))}
              </select>
            </div>
            <AsyncStateGuard
              isLoading={cargandoUsuarios}
              isError={Boolean(errorUsuarios)}
              errorMessage={errorUsuarios?.message}
              correlationId={errorUsuarios?.correlationId}
              isEmpty={(sesiones ?? []).length === 0}
              loadingText="Cargando auditoría forense de sesiones..."
              emptyTitle="Sin eventos de acceso"
              emptyDescription="No se registraron intentos de inicio de sesión para el filtro actual."
              onRetry={cargarUsuariosYSesiones}
            >
              <div className="max-h-72 overflow-y-auto">
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-50 text-slate-600">
                    <tr>
                      <th className="p-2">Fecha</th>
                      <th className="p-2">Usuario ID</th>
                      <th className="p-2">IP</th>
                      <th className="p-2">Dispositivo</th>
                      <th className="p-2">Resultado</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {(sesiones ?? []).map((s) => (
                      <tr key={s.id}>
                        <td className="p-2 font-mono">
                          {formatDateTime(s.fechaIntento)}
                        </td>
                        <td className="p-2 font-mono">#{s.usuarioId}</td>
                        <td className="p-2 font-mono">{s.direccionIP}</td>
                        <td className="p-2 truncate max-w-xs">
                          {s.dispositivo}
                        </td>
                        <td
                          className={`p-2 font-bold ${
                            s.exitoso ? 'text-emerald-800' : 'text-red-700'
                          }`}
                        >
                          {s.exitoso
                            ? '✓ Exitoso'
                            : `✕ Fallo (${
                                s.motivoFalloTexto || s.motivoFallo
                              })`}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </AsyncStateGuard>
            <PaginationBar
              pageNumber={paginaSesiones}
              totalPages={totalPaginasSesiones}
              totalCount={totalSesiones}
              itemLabel="eventos"
              isLoading={cargandoUsuarios}
              onPageChange={setPaginaSesiones}
            />
          </div>
        </div>
      )}

      {/* Modal Crear Cuenta de Personal (POST /api/usuarios) */}
      <Modal
        isOpen={modalNuevoStaffOpen}
        onClose={() => setModalNuevoStaffOpen(false)}
        title="Crear Cuenta de Personal (Staff)"
        subtitle="La cuenta se crea con email verificado y lista para operar."
      >
        <form onSubmit={handleCrearUsuarioStaff} className="space-y-3">
          <InputField
            label="Nombre de Usuario *"
            required
            value={staffUsername}
            onChange={(e) => setStaffUsername(e.target.value)}
          />
          <InputField
            label="Correo Electrónico *"
            type="email"
            required
            value={staffEmail}
            onChange={(e) => setStaffEmail(e.target.value)}
          />
          <InputField
            label="Contraseña Inicial *"
            type="password"
            required
            value={staffPassword}
            onChange={(e) => setStaffPassword(e.target.value)}
          />
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-semibold uppercase text-slate-700 block mb-1">
                Rol RBAC *
              </label>
              <select
                value={staffRolId}
                onChange={(e) => setStaffRolId(Number(e.target.value))}
                className="w-full rounded border border-slate-300 px-3 py-2 text-xs"
              >
                <option value={1}>1 - Administrador</option>
                <option value={2}>2 - Recepcionista</option>
                <option value={3}>3 - Peluquero</option>
                <option value={5}>5 - Cajero</option>
              </select>
            </div>
            <InputField
              label="ID Legajo Peluquero (Opcional)"
              type="number"
              value={staffPeluqueroId}
              onChange={(e) => setStaffPeluqueroId(e.target.value)}
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setModalNuevoStaffOpen(false)}
            >
              Cancelar
            </Button>
            <Button type="submit" variant="primary">
              Crear Usuario Staff
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
