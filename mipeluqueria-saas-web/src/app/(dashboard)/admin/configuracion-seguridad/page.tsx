// filepath: src/app/(dashboard)/admin/configuracion-seguridad/page.tsx
'use client';

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Building2,
  CheckCircle2,
  CreditCard,
  Edit3,
  Layers,
  Lock,
  Plus,
  ShieldCheck,
  Sliders,
  Trash2,
  Unlock,
  UserPlus,
  Users,
  XCircle,
} from 'lucide-react';
import {
  CatalogoItemDto,
  ConfiguracionSalonDto,
  HistorialSesionDto,
  PeluqueroDto,
  SucursalResponseDto,
  SuscripcionEstadoResponseDto,
  TipoCatalogoSalon,
  UsuarioDto,
} from '@/types/api.types';
import {
  catalogosService,
  configuracionSalonService,
  peluquerosService,
  sucursalesService,
  suscripcionesService,
  usuariosService,
} from '@/services/api.services';
import { useAuth, useCatalogos } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import {
  extractErrorCorrelationId,
  extractErrorMessage,
  extractPagedItems,
  extractPaginationMeta,
} from '@/lib/apiHelpers';
import {
  formatCurrency,
  formatDateOnly,
  formatDateTime,
  formatUserAgentSummary,
} from '@/lib/formatters';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import {
  AsyncStateGuard,
  Badge,
  Button,
  ConfirmModal,
  InputField,
  Modal,
  NumericInput,
  PaginationBar,
} from '@/components/ui';

type ConfigTab = 'salon' | 'catalogos' | 'usuarios' | 'sucursales';
type CatalogoSelector = TipoCatalogoSalon | 'estados-turno' | 'roles';

const CATALOGOS_EDITABLES: readonly TipoCatalogoSalon[] = [
  'categorias-servicio',
  'especialidades',
  'motivos-cancelacion',
  'categorias-producto',
  'medios-pago',
  'tipos-cabello',
  'tipos-rostro',
];

const CATALOGO_LABELS: Record<CatalogoSelector, string> = {
  'categorias-servicio': 'Categorías de servicio',
  especialidades: 'Especialidades de peluquero',
  'motivos-cancelacion': 'Motivos de cancelación',
  'categorias-producto': 'Categorías de producto',
  'medios-pago': 'Medios de pago (POS)',
  'tipos-cabello': 'Tipos de cabello',
  'tipos-rostro': 'Tipos de rostro',
  'estados-turno': 'Estados de turno (solo lectura)',
  roles: 'Roles de acceso (solo lectura)',
};

const SESIONES_PAGE_SIZE = 10;

export default function AdminConfiguracionYSeguridadPage() {
  const { user, sucursalId, switchSucursal } = useAuth();
  const { configSalon, catalogos, recargarCatalogosYConfig } = useCatalogos();

  const isSuperAdmin = user?.rol === 'SuperAdminSaaS';

  const [subTab, setSubTab] = useState<ConfigTab>('salon');
  const [guardandoConfig, setGuardandoConfig] = useState<boolean>(false);

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

  // Estado para Catálogos Maestros
  const [tipoCatalogo, setTipoCatalogo] = useState<CatalogoSelector>(
    'categorias-servicio'
  );
  const [itemEditando, setItemEditando] = useState<CatalogoItemDto | null>(
    null
  );
  const [nuevoItemNombre, setNuevoItemNombre] = useState<string>('');
  const [nuevoItemDescripcion, setNuevoItemDescripcion] = useState<string>('');
  const [esEfectivoFisico, setEsEfectivoFisico] = useState<boolean>(false);
  const [guardandoCatalogo, setGuardandoCatalogo] = useState<boolean>(false);
  const [itemAEliminar, setItemAEliminar] = useState<CatalogoItemDto | null>(
    null
  );
  const [eliminandoCatalogo, setEliminandoCatalogo] = useState<boolean>(false);
  const nombreCatalogoInputRef = useRef<HTMLInputElement | null>(null);

  // Estado para Usuarios, Profesionales y Auditoría de Accesos
  const [usuarios, setUsuarios] = useState<UsuarioDto[]>([]);
  const [peluqueros, setPeluqueros] = useState<PeluqueroDto[]>([]);
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

  // Modal de Alta de Cuenta de Personal
  const [modalNuevoStaffOpen, setModalNuevoStaffOpen] =
    useState<boolean>(false);
  const [creandoStaff, setCreandoStaff] = useState<boolean>(false);
  const [staffUsername, setStaffUsername] = useState<string>('');
  const [staffEmail, setStaffEmail] = useState<string>('');
  const [staffPassword, setStaffPassword] = useState<string>('');
  const [staffRolId, setStaffRolId] = useState<number>(2);
  const [staffPeluqueroId, setStaffPeluqueroId] = useState<string>('');

  // Estado para Mis Sucursales y Plan (solo lectura del plan para Administrador)
  const [sucursales, setSucursales] = useState<SucursalResponseDto[]>([]);
  const [miSuscripcion, setMiSuscripcion] =
    useState<SuscripcionEstadoResponseDto | null>(null);
  const [cargandoSucursales, setCargandoSucursales] = useState<boolean>(false);
  const [creandoSucursal, setCreandoSucursal] = useState<boolean>(false);
  const [nombreSucursal, setNombreSucursal] = useState<string>('');
  const [direccionSucursal, setDireccionSucursal] = useState<string>('');
  const [telefonoSucursal, setTelefonoSucursal] = useState<string>('');
  const [esCasaMatriz, setEsCasaMatriz] = useState<boolean>(false);

  const cargarUsuariosYSesiones = useCallback(async () => {
    setCargandoUsuarios(true);
    setErrorUsuarios(null);
    try {
      const [uRes, sRes, pRes] = await Promise.all([
        usuariosService.getAll(),
        usuariosService.getHistorialSesionesPaged(
          { pageNumber: paginaSesiones, pageSize: SESIONES_PAGE_SIZE },
          filtroUsuarioSesion
        ),
        peluquerosService.getAll(false),
      ]);
      if (uRes.success) {
        setUsuarios(extractPagedItems<UsuarioDto>(uRes.data));
      }
      if (sRes.success) {
        const items = extractPagedItems<HistorialSesionDto>(sRes.data);
        const meta = extractPaginationMeta(sRes.data, {
          pageNumber: paginaSesiones,
          pageSize: SESIONES_PAGE_SIZE,
        });
        setSesiones(items);
        setTotalPaginasSesiones(meta.totalPages);
        setTotalSesiones(meta.totalCount);
      }
      if (pRes.success) {
        setPeluqueros(extractPagedItems<PeluqueroDto>(pRes.data));
      }
    } catch (err) {
      setErrorUsuarios({
        message: extractErrorMessage(
          err,
          'No se pudieron cargar las cuentas de personal y el historial de accesos.'
        ),
        correlationId: extractErrorCorrelationId(err),
      });
    } finally {
      setCargandoUsuarios(false);
    }
  }, [paginaSesiones, filtroUsuarioSesion]);

  const cargarSucursalesYPlan = useCallback(async () => {
    setCargandoSucursales(true);
    try {
      const [sucRes, subRes] = await Promise.allSettled([
        sucursalesService.getAll(),
        suscripcionesService.getMiSuscripcion(),
      ]);

      if (sucRes.status === 'fulfilled' && sucRes.value.success) {
        setSucursales(extractPagedItems<SucursalResponseDto>(sucRes.value.data));
      }
      if (
        subRes.status === 'fulfilled' &&
        subRes.value.success &&
        subRes.value.data
      ) {
        setMiSuscripcion(subRes.value.data);
      }
    } finally {
      setCargandoSucursales(false);
    }
  }, []);

  useEffect(() => {
    if (configSalon) setFormConfig(configSalon);
  }, [configSalon]);

  useEffect(() => {
    void cargarUsuariosYSesiones();
  }, [cargarUsuariosYSesiones]);

  useEffect(() => {
    if (subTab === 'sucursales') {
      void cargarSucursalesYPlan();
    }
  }, [subTab, cargarSucursalesYPlan]);

  const usuariosPorId = useMemo(() => {
    const map = new Map<number, UsuarioDto>();
    usuarios.forEach((u) => map.set(u.id, u));
    return map;
  }, [usuarios]);

  const rolesDisponibles = useMemo(() => {
    const base = catalogos?.roles || [
      { id: 1, nombre: 'Administrador' },
      { id: 2, nombre: 'Recepcionista' },
      { id: 3, nombre: 'Peluquero' },
      { id: 4, nombre: 'Cliente' },
      { id: 5, nombre: 'Cajero' },
      { id: 6, nombre: 'SuperAdminSaaS' },
    ];
    return isSuperAdmin ? base : base.filter((r) => r.id !== 6);
  }, [catalogos?.roles, isSuperAdmin]);

  const guardarConfigSalon = async (e: React.FormEvent) => {
    e.preventDefault();
    setGuardandoConfig(true);
    try {
      const res = await configuracionSalonService.actualizar(formConfig);
      if (res.success) {
        notifyApiSuccess(
          res,
          'Configuración comercial del salón actualizada correctamente.'
        );
        await recargarCatalogosYConfig();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar la configuración del salón.',
      });
    } finally {
      setGuardandoConfig(false);
    }
  };

  const esCatalogoEditable = (
    cat: CatalogoSelector
  ): cat is TipoCatalogoSalon =>
    CATALOGOS_EDITABLES.includes(cat as TipoCatalogoSalon);

  const iniciarEdicionCatalogo = (item: CatalogoItemDto) => {
    setItemEditando(item);
    setNuevoItemNombre(item.nombre || item.descripcion || '');
    setNuevoItemDescripcion(item.descripcion || '');
    setEsEfectivoFisico(Boolean(item.esEfectivoFisico));
    requestAnimationFrame(() => {
      nombreCatalogoInputRef.current?.focus();
    });
  };

  const cancelarEdicionCatalogo = () => {
    setItemEditando(null);
    setNuevoItemNombre('');
    setNuevoItemDescripcion('');
    setEsEfectivoFisico(false);
  };

  const handleGuardarItemCatalogo = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!nuevoItemNombre.trim() || !esCatalogoEditable(tipoCatalogo)) return;

    const dto = {
      nombre: nuevoItemNombre.trim(),
      descripcion: nuevoItemDescripcion.trim() || nuevoItemNombre.trim(),
      esEfectivoFisico:
        tipoCatalogo === 'medios-pago' ? esEfectivoFisico : undefined,
    };

    setGuardandoCatalogo(true);
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
            ? `Registro "${dto.nombre}" actualizado.`
            : `"${dto.nombre}" agregado a ${CATALOGO_LABELS[tipoCatalogo]}.`
        );
        cancelarEdicionCatalogo();
        await recargarCatalogosYConfig();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo guardar el registro del catálogo.',
      });
    } finally {
      setGuardandoCatalogo(false);
    }
  };

  const confirmarEliminarItemCatalogo = async () => {
    if (!itemAEliminar || !esCatalogoEditable(tipoCatalogo)) return;
    setEliminandoCatalogo(true);
    try {
      const res = await catalogosService.eliminarPorTipo(
        tipoCatalogo,
        itemAEliminar.id
      );
      if (res.success) {
        notifyApiSuccess(res, 'Registro eliminado del catálogo.');
        if (itemEditando?.id === itemAEliminar.id) {
          cancelarEdicionCatalogo();
        }
        setItemAEliminar(null);
        await recargarCatalogosYConfig();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage:
          'No se puede eliminar este registro porque tiene datos asociados.',
      });
    } finally {
      setEliminandoCatalogo(false);
    }
  };

  const abrirModalNuevoStaff = () => {
    setStaffUsername('');
    setStaffEmail('');
    setStaffPassword('');
    setStaffRolId(2);
    setStaffPeluqueroId('');
    setModalNuevoStaffOpen(true);
  };

  const handleCrearUsuarioStaff = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreandoStaff(true);
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
          `Cuenta de personal "${staffUsername.trim()}" creada exitosamente.`
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
    } finally {
      setCreandoStaff(false);
    }
  };

  const toggleEstadoUsuario = async (u: UsuarioDto) => {
    try {
      const res = await usuariosService.actualizarEstado(u.id, {
        activo: !u.activo,
        rolId: u.rolId,
      });
      if (res.success) {
        notifyApiSuccess(
          res,
          `Usuario "${u.username}" ${u.activo ? 'inactivado' : 'reactivado'}.`
        );
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
        const rolNombre =
          rolesDisponibles.find((r) => r.id === nuevoRolId)?.nombre ||
          `Rol #${nuevoRolId}`;
        notifyApiSuccess(
          res,
          `Rol de "${u.username}" actualizado a ${rolNombre}.`
        );
        await cargarUsuariosYSesiones();
      }
    } catch (err) {
      notifyApiError(err);
    }
  };

  const desbloquearUsuario = async (u: UsuarioDto) => {
    try {
      const res = await usuariosService.desbloquear(u.id);
      if (res.success) {
        notifyApiSuccess(
          res,
          `Cuenta "${u.username}" desbloqueada y reactivada.`
        );
        await cargarUsuariosYSesiones();
      }
    } catch (err) {
      notifyApiError(err);
    }
  };

  const handleCrearSucursal = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreandoSucursal(true);
    try {
      const res = await sucursalesService.crear({
        nombre: nombreSucursal.trim(),
        direccion: direccionSucursal.trim(),
        telefono: telefonoSucursal.trim(),
        timeZoneId: formConfig.timeZoneId || 'America/Argentina/Cordoba',
        monedaIso: formConfig.moneda || 'ARS',
        esCasaMatriz,
      });
      if (res.success) {
        notifyApiSuccess(res, `Sucursal "${nombreSucursal.trim()}" creada.`);
        setNombreSucursal('');
        setDireccionSucursal('');
        setTelefonoSucursal('');
        setEsCasaMatriz(false);
        await cargarSucursalesYPlan();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo crear la sucursal.',
      });
    } finally {
      setCreandoSucursal(false);
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

  const tabs: Array<{
    id: ConfigTab;
    label: string;
    icon: React.ReactNode;
    count?: number;
  }> = [
    {
      id: 'salon',
      label: 'Ajustes del Salón',
      icon: <Sliders className="h-4 w-4" aria-hidden="true" />,
    },
    {
      id: 'catalogos',
      label: 'Catálogos Maestros',
      icon: <Layers className="h-4 w-4" aria-hidden="true" />,
    },
    {
      id: 'usuarios',
      label: 'Usuarios y Sesiones',
      icon: <Users className="h-4 w-4" aria-hidden="true" />,
      count: usuarios.length,
    },
    {
      id: 'sucursales',
      label: 'Mis Sucursales y Plan',
      icon: <Building2 className="h-4 w-4" aria-hidden="true" />,
    },
  ];

  const selectClass =
    'h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary transition-colors focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25';

  return (
    <div className="max-w-[1440px] mx-auto space-y-5 min-w-0">
      {/* Cabecera limpia y barra de pestañas */}
      <header className="bg-surface border border-border-subtle rounded-xl p-5 space-y-4 shadow-sm">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary">
              Configuración y Seguridad
            </h1>
            <p className="text-sm text-ink-secondary mt-0.5">
              Datos comerciales del salón, catálogos operativos, cuentas de
              personal, sucursales y auditoría de accesos.
            </p>
          </div>
          <Badge variant="neutral">
            <ShieldCheck className="h-3.5 w-3.5" aria-hidden="true" />
            {user?.rol || 'Administrador'}
          </Badge>
        </div>

        <nav
          aria-label="Secciones de configuración"
          className="flex flex-wrap gap-1.5 border-t border-border-subtle pt-3.5"
        >
          {tabs.map((t) => {
            const active = subTab === t.id;
            return (
              <button
                key={t.id}
                type="button"
                onClick={() => setSubTab(t.id)}
                className={`inline-flex items-center gap-2 h-9 px-3.5 rounded-lg text-xs font-medium transition-colors ${
                  active
                    ? 'bg-brand-primary text-white shadow-sm'
                    : 'bg-elevated/70 text-ink-secondary hover:text-ink-primary hover:bg-elevated'
                }`}
              >
                {t.icon}
                <span>{t.label}</span>
                {typeof t.count === 'number' && (
                  <span
                    className={`font-mono-tabular px-1.5 py-0.5 rounded text-[11px] ${
                      active
                        ? 'bg-white/20 text-white'
                        : 'bg-surface text-ink-secondary border border-border-subtle'
                    }`}
                  >
                    {t.count}
                  </span>
                )}
              </button>
            );
          })}
        </nav>
      </header>

      {/* TAB 1: AJUSTES DEL SALÓN */}
      {subTab === 'salon' && (
        <form
          onSubmit={guardarConfigSalon}
          className="bg-surface border border-border-subtle rounded-xl p-5 sm:p-6 space-y-6 shadow-sm"
        >
          <div>
            <h2 className="text-base sm:text-lg font-semibold text-ink-primary">
              Identidad y datos comerciales
            </h2>
            <p className="text-xs text-ink-secondary mt-0.5">
              Esta información se muestra en el portal de reservas online y en
              los comprobantes emitidos en el punto de venta.
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <InputField
              label="Nombre comercial *"
              required
              value={formConfig.nombreComercial}
              onChange={(e) =>
                setFormConfig({
                  ...formConfig,
                  nombreComercial: e.target.value,
                })
              }
            />
            <InputField
              label="Razón social"
              value={formConfig.razonSocial}
              onChange={(e) =>
                setFormConfig({ ...formConfig, razonSocial: e.target.value })
              }
            />
            <InputField
              label="CUIT / Identificación fiscal"
              value={formConfig.cuit}
              onChange={(e) =>
                setFormConfig({ ...formConfig, cuit: e.target.value })
              }
            />
            <InputField
              label="Dirección del salón"
              value={formConfig.direccion}
              onChange={(e) =>
                setFormConfig({ ...formConfig, direccion: e.target.value })
              }
            />
            <InputField
              label="Teléfono / WhatsApp"
              value={formConfig.telefono}
              onChange={(e) =>
                setFormConfig({ ...formConfig, telefono: e.target.value })
              }
            />
            <InputField
              label="Moneda (código ISO)"
              value={formConfig.moneda}
              onChange={(e) =>
                setFormConfig({ ...formConfig, moneda: e.target.value })
              }
            />
            <InputField
              label="Zona horaria"
              value={formConfig.timeZoneId || 'America/Argentina/Cordoba'}
              onChange={(e) =>
                setFormConfig({ ...formConfig, timeZoneId: e.target.value })
              }
            />
          </div>

          <div className="border-t border-border-subtle pt-5 space-y-4">
            <div>
              <h3 className="text-sm font-semibold text-ink-primary">
                Reglas de agenda y señas
              </h3>
              <p className="text-xs text-ink-secondary mt-0.5">
                Definí la grilla de turnos, el margen de tolerancia y la política
                de cancelación online.
              </p>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              <NumericInput
                label="Intervalo de agenda"
                suffix="min"
                allowDecimals={false}
                min={5}
                max={120}
                emptyValueOnBlur={30}
                value={formConfig.intervaloTurnosMinutos}
                onValueChange={(val) =>
                  setFormConfig({
                    ...formConfig,
                    intervaloTurnosMinutos: val ?? 30,
                  })
                }
              />
              <NumericInput
                label="Tolerancia de espera"
                suffix="min"
                allowDecimals={false}
                min={0}
                max={120}
                emptyValueOnBlur={15}
                value={formConfig.minutosToleranciaEspera ?? 15}
                onValueChange={(val) =>
                  setFormConfig({
                    ...formConfig,
                    minutosToleranciaEspera: val ?? 0,
                  })
                }
              />
              <NumericInput
                label="Anticipación cancelación"
                suffix="hs"
                allowDecimals={false}
                min={0}
                max={168}
                emptyValueOnBlur={24}
                value={formConfig.horasAnticipacionCancelacion ?? 24}
                onValueChange={(val) =>
                  setFormConfig({
                    ...formConfig,
                    horasAnticipacionCancelacion: val ?? 0,
                  })
                }
              />
              <NumericInput
                label="Seña online (0 = sin seña)"
                suffix="%"
                allowDecimals={false}
                min={0}
                max={100}
                emptyValueOnBlur={0}
                value={formConfig.porcentajeSenaReserva ?? 0}
                onValueChange={(val) =>
                  setFormConfig({
                    ...formConfig,
                    porcentajeSenaReserva: val ?? 0,
                  })
                }
              />
            </div>
          </div>

          <div className="border-t border-border-subtle pt-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div className="flex flex-wrap gap-5 text-sm text-ink-primary">
              <label className="inline-flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-border-strong accent-brand-primary"
                  checked={formConfig.notificacionesWhatsAppActivas ?? true}
                  onChange={(e) =>
                    setFormConfig({
                      ...formConfig,
                      notificacionesWhatsAppActivas: e.target.checked,
                    })
                  }
                />
                <span>Recordatorios automáticos por WhatsApp (24h y 2h antes)</span>
              </label>
              <label className="inline-flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-border-strong accent-brand-primary"
                  checked={formConfig.notificacionesEmailActivas ?? true}
                  onChange={(e) =>
                    setFormConfig({
                      ...formConfig,
                      notificacionesEmailActivas: e.target.checked,
                    })
                  }
                />
                <span>Notificaciones por correo electrónico</span>
              </label>
            </div>

            <Button
              type="submit"
              variant="primary"
              isLoading={guardandoConfig}
            >
              Guardar configuración
            </Button>
          </div>
        </form>
      )}

      {/* TAB 2: CATÁLOGOS MAESTROS */}
      {subTab === 'catalogos' && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
          <form
            onSubmit={handleGuardarItemCatalogo}
            className={`lg:col-span-5 bg-surface rounded-xl p-5 space-y-4 shadow-sm border transition-colors ${
              itemEditando
                ? 'border-brand-primary ring-2 ring-brand-primary/15'
                : 'border-border-subtle'
            }`}
          >
            <div className="flex items-center justify-between gap-2">
              <div className="flex items-center gap-2 min-w-0">
                <h2 className="text-base font-semibold text-ink-primary truncate">
                  {itemEditando
                    ? `Editando: ${itemEditando.nombre || itemEditando.descripcion}`
                    : 'Nuevo registro de catálogo'}
                </h2>
                {itemEditando && <Badge variant="info">Editando</Badge>}
              </div>
              {itemEditando && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={cancelarEdicionCatalogo}
                >
                  Cancelar
                </Button>
              )}
            </div>

            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="selector-catalogo"
                className="text-xs font-medium text-ink-secondary"
              >
                Catálogo seleccionado
              </label>
              <select
                id="selector-catalogo"
                value={tipoCatalogo}
                onChange={(e) => {
                  setTipoCatalogo(e.target.value as CatalogoSelector);
                  cancelarEdicionCatalogo();
                }}
                className={selectClass}
              >
                {(Object.keys(CATALOGO_LABELS) as CatalogoSelector[]).map(
                  (key) => (
                    <option key={key} value={key}>
                      {CATALOGO_LABELS[key]}
                    </option>
                  )
                )}
              </select>
            </div>

            {esCatalogoEditable(tipoCatalogo) ? (
              <>
                <div className="flex flex-col gap-1.5">
                  <label
                    htmlFor="catalogo-nombre-input"
                    className="text-xs font-medium text-ink-secondary"
                  >
                    Nombre *
                  </label>
                  <input
                    ref={nombreCatalogoInputRef}
                    id="catalogo-nombre-input"
                    type="text"
                    required
                    placeholder="Ej. Coloración, Balayage, Efectivo..."
                    value={nuevoItemNombre}
                    onChange={(e) => setNuevoItemNombre(e.target.value)}
                    className="h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary placeholder:text-ink-secondary/60 transition-colors focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
                  />
                </div>

                <InputField
                  label="Descripción adicional"
                  placeholder="Detalle breve opcional"
                  value={nuevoItemDescripcion}
                  onChange={(e) => setNuevoItemDescripcion(e.target.value)}
                />

                {tipoCatalogo === 'medios-pago' && (
                  <label className="flex items-start gap-2.5 rounded-lg border border-border-subtle bg-elevated/50 p-3 text-xs text-ink-primary cursor-pointer">
                    <input
                      type="checkbox"
                      className="mt-0.5 h-4 w-4 rounded border-border-strong accent-brand-primary"
                      checked={esEfectivoFisico}
                      onChange={(e) => setEsEfectivoFisico(e.target.checked)}
                    />
                    <span>
                      <strong className="font-semibold block">
                        Es efectivo físico
                      </strong>
                      <span className="text-ink-secondary">
                        Suma billetes reales al saldo del cajón en Caja Diaria.
                      </span>
                    </span>
                  </label>
                )}

                <Button
                  type="submit"
                  variant="primary"
                  className="w-full"
                  isLoading={guardandoCatalogo}
                  leftIcon={
                    itemEditando ? (
                      <Edit3 className="h-4 w-4" aria-hidden="true" />
                    ) : (
                      <Plus className="h-4 w-4" aria-hidden="true" />
                    )
                  }
                >
                  {itemEditando ? 'Guardar cambios' : 'Agregar registro'}
                </Button>
              </>
            ) : (
              <div className="rounded-lg border border-border-subtle bg-elevated/60 p-4 text-xs text-ink-secondary flex items-start gap-2.5">
                <Lock
                  className="h-4 w-4 shrink-0 mt-0.5 text-ink-secondary"
                  aria-hidden="true"
                />
                <p>
                  Este catálogo es administrado por el sistema y se muestra en
                  modo solo lectura para mantener la integridad de turnos y
                  permisos.
                </p>
              </div>
            )}
          </form>

          <div className="lg:col-span-7 bg-surface border border-border-subtle rounded-xl p-5 space-y-3 shadow-sm min-w-0">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="text-base font-semibold text-ink-primary">
                {CATALOGO_LABELS[tipoCatalogo]}
              </h2>
              <Badge variant="neutral" className="font-mono-tabular">
                {obtenerItemsTablaSeleccionada().length} registros
              </Badge>
            </div>

            <div className="space-y-2">
              {obtenerItemsTablaSeleccionada().length === 0 ? (
                <p className="text-sm text-ink-secondary py-6 text-center">
                  Todavía no hay registros cargados en este catálogo.
                </p>
              ) : (
                obtenerItemsTablaSeleccionada().map((item) => {
                  const isEditingThis = itemEditando?.id === item.id;
                  return (
                    <div
                      key={item.id}
                      className={`rounded-lg px-3.5 py-2.5 flex flex-wrap items-center justify-between gap-3 border transition-colors ${
                        isEditingThis
                          ? 'border-brand-primary bg-brand-primary/5 ring-1 ring-brand-primary/30'
                          : 'border-border-subtle bg-surface hover:bg-elevated/50'
                      }`}
                    >
                      <div className="min-w-0 flex-1">
                        <div className="flex flex-wrap items-center gap-2">
                          <span className="font-mono-tabular text-xs text-ink-secondary">
                            #{item.id}
                          </span>
                          <span className="text-sm font-medium text-ink-primary">
                            {item.nombre || item.descripcion}
                          </span>
                          {isEditingThis && (
                            <Badge variant="info">Editando</Badge>
                          )}
                          {tipoCatalogo === 'medios-pago' && (
                            <Badge
                              variant={
                                item.esEfectivoFisico ? 'success' : 'info'
                              }
                            >
                              {item.esEfectivoFisico
                                ? 'Efectivo físico'
                                : 'Digital / Bancario'}
                            </Badge>
                          )}
                        </div>
                        {item.descripcion &&
                          item.nombre &&
                          item.descripcion !== item.nombre && (
                            <p className="text-xs text-ink-secondary mt-0.5 truncate">
                              {item.descripcion}
                            </p>
                          )}
                      </div>

                      {esCatalogoEditable(tipoCatalogo) && (
                        <div className="flex items-center gap-1.5 shrink-0">
                          <Button
                            type="button"
                            variant="secondary"
                            size="sm"
                            onClick={() => iniciarEdicionCatalogo(item)}
                          >
                            Editar
                          </Button>
                          <Button
                            type="button"
                            variant="destructive"
                            destructiveTone="soft"
                            size="sm"
                            onClick={() => setItemAEliminar(item)}
                            aria-label={`Eliminar ${item.nombre || item.descripcion}`}
                          >
                            <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
                            Eliminar
                          </Button>
                        </div>
                      )}
                    </div>
                  );
                })
              )}
            </div>
          </div>
        </div>
      )}

      {/* TAB 3: USUARIOS Y SESIONES */}
      {subTab === 'usuarios' && (
        <div className="space-y-5">
          {/* Cuentas de personal */}
          <section className="bg-surface border border-border-subtle rounded-xl p-5 space-y-4 shadow-sm min-w-0">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 className="text-base sm:text-lg font-semibold text-ink-primary">
                  Cuentas de personal y permisos
                </h2>
                <p className="text-xs text-ink-secondary mt-0.5">
                  Administrá roles de acceso, estado de cuentas y desbloqueo ante
                  intentos fallidos.
                </p>
              </div>
              <Button
                type="button"
                variant="primary"
                size="sm"
                leftIcon={<UserPlus className="h-4 w-4" aria-hidden="true" />}
                onClick={abrirModalNuevoStaff}
              >
                Crear Usuario Staff
              </Button>
            </div>

            <div className="overflow-x-auto rounded-lg border border-border-subtle">
              <table className="w-full text-left text-sm">
                <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                  <tr>
                    <th className="px-3.5 py-2.5">Usuario</th>
                    <th className="px-3.5 py-2.5">Correo</th>
                    <th className="px-3.5 py-2.5">Rol</th>
                    <th className="px-3.5 py-2.5">Estado</th>
                    <th className="px-3.5 py-2.5 text-right">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border-subtle">
                  {usuarios.map((u) => {
                    const isRowSuperAdmin = u.rolId === 6;
                    const bloqueado = (u.intentosFallidos ?? 0) >= 3;
                    return (
                      <tr
                        key={u.id}
                        className="hover:bg-elevated/40 transition-colors"
                      >
                        <td className="px-3.5 py-2.5 font-medium text-ink-primary whitespace-nowrap">
                          {u.username}
                        </td>
                        <td className="px-3.5 py-2.5 text-ink-secondary text-xs">
                          {u.email}
                        </td>
                        <td className="px-3.5 py-2.5">
                          <select
                            aria-label={`Rol de ${u.username}`}
                            value={u.rolId}
                            disabled={isRowSuperAdmin && !isSuperAdmin}
                            onChange={(e) =>
                              handleCambiarRolUsuario(u, Number(e.target.value))
                            }
                            className="h-8 rounded-lg border border-border-strong bg-surface px-2.5 text-xs text-ink-primary focus:border-brand-primary focus:outline-none disabled:opacity-60"
                          >
                            {(isRowSuperAdmin && !isSuperAdmin
                              ? [{ id: 6, nombre: 'SuperAdminSaaS' }]
                              : rolesDisponibles
                            ).map((r) => (
                              <option key={r.id} value={r.id}>
                                {r.nombre}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-3.5 py-2.5 whitespace-nowrap">
                          <div className="flex items-center gap-2">
                            <Badge variant={u.activo ? 'success' : 'neutral'}>
                              {u.activo ? 'Activo' : 'Inactivo'}
                            </Badge>
                            {(u.intentosFallidos ?? 0) > 0 && (
                              <Badge
                                variant={bloqueado ? 'danger' : 'warning'}
                                className="font-mono-tabular"
                              >
                                {u.intentosFallidos} fallos
                              </Badge>
                            )}
                          </div>
                        </td>
                        <td className="px-3.5 py-2.5 text-right whitespace-nowrap">
                          <div className="inline-flex items-center gap-1.5">
                            <Button
                              type="button"
                              variant="secondary"
                              size="sm"
                              disabled={isRowSuperAdmin && !isSuperAdmin}
                              onClick={() => toggleEstadoUsuario(u)}
                            >
                              {u.activo ? 'Inactivar' : 'Activar'}
                            </Button>
                            <Button
                              type="button"
                              variant="secondary"
                              size="sm"
                              disabled={isRowSuperAdmin && !isSuperAdmin}
                              leftIcon={
                                <Unlock
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                              }
                              onClick={() => desbloquearUsuario(u)}
                            >
                              Desbloquear
                            </Button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </section>

          {/* Auditoría de Accesos (10 por página, sin scroll interno atrapado) */}
          <section className="bg-surface border border-border-subtle rounded-xl p-5 space-y-4 shadow-sm min-w-0">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <h2 className="text-base sm:text-lg font-semibold text-ink-primary">
                  Auditoría de Accesos
                </h2>
                <p className="text-xs text-ink-secondary mt-0.5">
                  Registro cronológico de inicios de sesión exitosos y fallidos
                  (10 eventos por página).
                </p>
              </div>
              <select
                aria-label="Filtrar accesos por usuario"
                value={filtroUsuarioSesion || ''}
                onChange={(e) => {
                  setFiltroUsuarioSesion(
                    e.target.value ? Number(e.target.value) : undefined
                  );
                  setPaginaSesiones(1);
                }}
                className="h-9 rounded-lg border border-border-strong bg-surface px-3 text-xs text-ink-primary focus:border-brand-primary focus:outline-none"
              >
                <option value="">Todos los usuarios</option>
                {usuarios.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.username} (#{u.id})
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
              loadingText="Cargando historial de accesos..."
              emptyTitle="Sin eventos de acceso"
              emptyDescription="No se registraron intentos de inicio de sesión para el filtro actual."
              onRetry={cargarUsuariosYSesiones}
            >
              <div className="overflow-x-auto rounded-lg border border-border-subtle">
                <table className="w-full text-left text-sm">
                  <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                    <tr>
                      <th className="px-3.5 py-2.5">Fecha y hora</th>
                      <th className="px-3.5 py-2.5">Usuario</th>
                      <th className="px-3.5 py-2.5">IP</th>
                      <th className="px-3.5 py-2.5">Dispositivo</th>
                      <th className="px-3.5 py-2.5">Resultado</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border-subtle text-xs">
                    {(sesiones ?? []).map((s) => {
                      const usuarioMatch =
                        s.usuarioId != null
                          ? usuariosPorId.get(s.usuarioId)
                          : undefined;
                      const dispositivoHumano = formatUserAgentSummary(
                        s.dispositivo
                      );
                      return (
                        <tr
                          key={s.id}
                          className="hover:bg-elevated/40 transition-colors"
                        >
                          <td className="px-3.5 py-2.5 font-mono-tabular text-ink-primary whitespace-nowrap">
                            {formatDateTime(s.fechaIntento)}
                          </td>
                          <td className="px-3.5 py-2.5 whitespace-nowrap">
                            <span className="font-medium text-ink-primary">
                              {usuarioMatch?.username ||
                                (s.usuarioId != null
                                  ? `Usuario #${s.usuarioId}`
                                  : 'Anónimo')}
                            </span>
                            {s.usuarioId != null && (
                              <span className="ml-1.5 font-mono-tabular text-ink-secondary">
                                #{s.usuarioId}
                              </span>
                            )}
                          </td>
                          <td className="px-3.5 py-2.5 font-mono-tabular text-ink-secondary whitespace-nowrap">
                            {s.direccionIP || '-'}
                          </td>
                          <td className="px-3.5 py-2.5">
                            <span
                              title={s.dispositivo || 'Sin información de User-Agent'}
                              className="inline-flex items-center text-ink-primary cursor-help underline decoration-dotted decoration-border-strong underline-offset-2"
                            >
                              {dispositivoHumano}
                            </span>
                          </td>
                          <td className="px-3.5 py-2.5 whitespace-nowrap">
                            {s.exitoso ? (
                              <span
                                className={`inline-flex items-center gap-1 font-medium ${SEMANTIC_COLORS.cash.textClass}`}
                              >
                                <CheckCircle2
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                                Exitoso
                              </span>
                            ) : (
                              <span
                                className={`inline-flex items-center gap-1 font-medium ${SEMANTIC_COLORS.danger.textClass}`}
                              >
                                <XCircle
                                  className="h-3.5 w-3.5"
                                  aria-hidden="true"
                                />
                                Fallo ({s.motivoFalloTexto || s.motivoFallo || 'Credenciales'})
                              </span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </AsyncStateGuard>

            <PaginationBar
              pageNumber={paginaSesiones}
              totalPages={totalPaginasSesiones}
              totalCount={totalSesiones}
              itemLabel="accesos"
              isLoading={cargandoUsuarios}
              onPageChange={setPaginaSesiones}
            />
          </section>
        </div>
      )}

      {/* TAB 4: MIS SUCURSALES Y PLAN (SOLO LECTURA DEL PLAN) */}
      {subTab === 'sucursales' && (
        <div className="space-y-5">
          {/* Resumen de Plan Actual en modo solo lectura */}
          <section className="bg-surface border border-border-subtle rounded-xl p-5 shadow-sm">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="space-y-1 min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <CreditCard
                    className="h-4 w-4 text-brand-primary"
                    aria-hidden="true"
                  />
                  <h2 className="text-base font-semibold text-ink-primary">
                    Plan de suscripción actual
                  </h2>
                  <Badge variant="neutral">Solo lectura</Badge>
                </div>
                {miSuscripcion ? (
                  <p className="text-sm text-ink-secondary">
                    <strong className="text-ink-primary">
                      {miSuscripcion.nombreComercial}
                    </strong>{' '}
                    — Plan{' '}
                    <span className="font-semibold text-brand-primary">
                      {miSuscripcion.planActual}
                    </span>{' '}
                    • Próximo vencimiento:{' '}
                    <span className="font-mono-tabular text-ink-primary">
                      {formatDateOnly(miSuscripcion.fechaProximoVencimiento)}
                    </span>
                  </p>
                ) : (
                  <p className="text-sm text-ink-secondary">
                    Consultando el estado de tu suscripción vigente...
                  </p>
                )}
              </div>

              {miSuscripcion && (
                <div className="text-right shrink-0">
                  <p className="text-xs text-ink-secondary">Abono mensual</p>
                  <p className="text-2xl font-bold font-mono-tabular text-ink-primary">
                    {formatCurrency(miSuscripcion.precioMensualUsd, 'USD')}
                    <span className="text-xs font-normal text-ink-secondary">
                      {' '}
                      / mes
                    </span>
                  </p>
                </div>
              )}
            </div>

            <div className="mt-4 rounded-lg border border-border-subtle bg-elevated/60 px-3.5 py-2.5 text-xs text-ink-secondary flex items-center gap-2">
              <Lock className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
              <span>
                Los cambios de plan o ampliaciones de cupo se gestionan
                directamente con el equipo de soporte de plataforma.
              </span>
            </div>
          </section>

          {/* Sucursales físicas del salón */}
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
            <section className="lg:col-span-7 bg-surface border border-border-subtle rounded-xl p-5 space-y-4 shadow-sm min-w-0">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div>
                  <h2 className="text-base font-semibold text-ink-primary">
                    Mis sucursales ({sucursales.length})
                  </h2>
                  <p className="text-xs text-ink-secondary">
                    Cambiá la sucursal activa para operar agenda, caja e
                    inventario en esa sede.
                  </p>
                </div>
              </div>

              <AsyncStateGuard
                isLoading={cargandoSucursales}
                isEmpty={sucursales.length === 0}
                loadingText="Cargando sucursales..."
                emptyTitle="Sin sucursales registradas"
                emptyDescription="Agregá tu primera sucursal desde el formulario lateral."
                onRetry={cargarSucursalesYPlan}
              >
                <div className="overflow-x-auto rounded-lg border border-border-subtle">
                  <table className="w-full text-left text-sm">
                    <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                      <tr>
                        <th className="px-3 py-2.5">Sucursal</th>
                        <th className="px-3 py-2.5">Dirección</th>
                        <th className="px-3 py-2.5">Tipo</th>
                        <th className="px-3 py-2.5 text-right">Operación</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border-subtle text-xs">
                      {sucursales.map((s) => {
                        const esActiva = sucursalId === s.id;
                        return (
                          <tr
                            key={s.id}
                            className="hover:bg-elevated/40 transition-colors"
                          >
                            <td className="px-3 py-2.5 font-medium text-ink-primary">
                              <span className="font-mono-tabular text-ink-secondary mr-1.5">
                                #{s.id}
                              </span>
                              {s.nombre}
                            </td>
                            <td className="px-3 py-2.5 text-ink-secondary">
                              {s.direccion || '-'}
                            </td>
                            <td className="px-3 py-2.5 whitespace-nowrap">
                              <Badge
                                variant={s.esCasaMatriz ? 'info' : 'neutral'}
                              >
                                {s.esCasaMatriz ? 'Casa Matriz' : 'Sucursal'}
                              </Badge>
                            </td>
                            <td className="px-3 py-2.5 text-right whitespace-nowrap">
                              {esActiva ? (
                                <Badge variant="success">Activa</Badge>
                              ) : (
                                <Button
                                  type="button"
                                  variant="secondary"
                                  size="sm"
                                  onClick={() => {
                                    void switchSucursal(s.id);
                                  }}
                                >
                                  Operar aquí
                                </Button>
                              )}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </AsyncStateGuard>
            </section>

            <form
              onSubmit={handleCrearSucursal}
              className="lg:col-span-5 bg-surface border border-border-subtle rounded-xl p-5 space-y-4 shadow-sm"
            >
              <div>
                <h2 className="text-base font-semibold text-ink-primary">
                  Nueva sucursal
                </h2>
                <p className="text-xs text-ink-secondary mt-0.5">
                  Sujeta al cupo de sucursales incluido en tu plan vigente.
                </p>
              </div>
              <InputField
                label="Nombre de la sucursal *"
                required
                placeholder="Ej. Sucursal Nueva Córdoba"
                value={nombreSucursal}
                onChange={(e) => setNombreSucursal(e.target.value)}
              />
              <InputField
                label="Dirección *"
                required
                placeholder="Ej. Av. Hipólito Yrigoyen 320"
                value={direccionSucursal}
                onChange={(e) => setDireccionSucursal(e.target.value)}
              />
              <InputField
                label="Teléfono"
                placeholder="+54 9 351 ..."
                value={telefonoSucursal}
                onChange={(e) => setTelefonoSucursal(e.target.value)}
              />
              <label className="flex items-center gap-2 text-xs font-medium text-ink-primary cursor-pointer">
                <input
                  type="checkbox"
                  className="h-4 w-4 rounded border-border-strong accent-brand-primary"
                  checked={esCasaMatriz}
                  onChange={(e) => setEsCasaMatriz(e.target.checked)}
                />
                <span>Marcar como Casa Matriz</span>
              </label>
              <Button
                type="submit"
                variant="primary"
                className="w-full"
                isLoading={creandoSucursal}
                leftIcon={<Plus className="h-4 w-4" aria-hidden="true" />}
              >
                Crear sucursal
              </Button>
            </form>
          </div>
        </div>
      )}

      {/* Modal Crear Cuenta de Personal (sin autofill invasivo + desplegable de profesionales) */}
      <Modal
        isOpen={modalNuevoStaffOpen}
        onClose={() => setModalNuevoStaffOpen(false)}
        title="Crear Cuenta de Personal"
        subtitle="La cuenta queda verificada y lista para iniciar sesión en el salón."
      >
        <form
          onSubmit={handleCrearUsuarioStaff}
          autoComplete="off"
          className="space-y-4"
        >
          <InputField
            label="Nombre de usuario *"
            name="staff_new_username"
            autoComplete="off"
            required
            placeholder="Ej. sofia.estilista"
            value={staffUsername}
            onChange={(e) => setStaffUsername(e.target.value)}
          />
          <InputField
            label="Correo electrónico *"
            name="staff_new_email"
            type="email"
            autoComplete="off"
            required
            placeholder="sofia@mipeluqueria.com"
            value={staffEmail}
            onChange={(e) => setStaffEmail(e.target.value)}
          />
          <InputField
            label="Contraseña inicial *"
            name="staff_new_secret"
            type="password"
            autoComplete="new-password"
            required
            placeholder="Mínimo 8 caracteres"
            value={staffPassword}
            onChange={(e) => setStaffPassword(e.target.value)}
          />
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="staff-rol-select"
                className="text-xs font-medium text-ink-secondary"
              >
                Rol de acceso *
              </label>
              <select
                id="staff-rol-select"
                value={staffRolId}
                onChange={(e) => setStaffRolId(Number(e.target.value))}
                className={selectClass}
              >
                <option value={1}>Administrador</option>
                <option value={2}>Recepcionista</option>
                <option value={3}>Peluquero</option>
                <option value={5}>Cajero</option>
              </select>
            </div>

            <div className="flex flex-col gap-1.5">
              <label
                htmlFor="staff-peluquero-select"
                className="text-xs font-medium text-ink-secondary"
              >
                Profesional vinculado (opcional)
              </label>
              <select
                id="staff-peluquero-select"
                value={staffPeluqueroId}
                onChange={(e) => setStaffPeluqueroId(e.target.value)}
                className={selectClass}
              >
                <option value="">Sin vincular a profesional</option>
                {peluqueros.map((p) => (
                  <option key={p.id} value={String(p.id)}>
                    {p.nombreCompleto || `${p.nombre} ${p.apellido}`.trim()}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t border-border-subtle">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setModalNuevoStaffOpen(false)}
            >
              Cancelar
            </Button>
            <Button type="submit" variant="primary" isLoading={creandoStaff}>
              Crear Usuario Staff
            </Button>
          </div>
        </form>
      </Modal>

      {/* Confirmación de eliminación de registro de catálogo */}
      <ConfirmModal
        isOpen={Boolean(itemAEliminar)}
        onClose={() => setItemAEliminar(null)}
        onConfirm={confirmarEliminarItemCatalogo}
        title="Eliminar registro del catálogo"
        confirmLabel="Eliminar registro"
        variant="danger"
        isLoading={eliminandoCatalogo}
      >
        {itemAEliminar && (
          <p className="text-sm text-ink-secondary">
            ¿Confirmás eliminar{' '}
            <strong className="text-ink-primary">
              {itemAEliminar.nombre || itemAEliminar.descripcion}
            </strong>{' '}
            de {CATALOGO_LABELS[tipoCatalogo]}? Esta acción no se completará si
            existen registros vinculados.
          </p>
        )}
      </ConfirmModal>
    </div>
  );
}
