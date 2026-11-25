// filepath: src/app/(dashboard)/saas-admin/page.tsx
'use client';

import React, { useEffect, useState } from 'react';
import {
  Building2,
  CheckCircle2,
  CreditCard,
  Globe,
  Plus,
  ShieldAlert,
  Webhook,
} from 'lucide-react';
import { apiClient } from '@/lib/axios';
import {
  PlanSuscripcionDto,
  SucursalResponseDto,
  SuscripcionEstadoResponseDto,
  TenantDto,
} from '@/types/api.types';
import {
  sucursalesService,
  suscripcionesService,
} from '@/services/api.services';
import { useAuth } from '@/context/AppProviders';
import { notifyApiError, notifyApiSuccess } from '@/hooks';
import { extractPagedItems } from '@/lib/apiHelpers';
import { formatCurrency, formatDateOnly } from '@/lib/formatters';
import { SEMANTIC_COLORS } from '@/lib/semanticColors';
import {
  AsyncStateGuard,
  Badge,
  Button,
  InputField,
  NumericInput,
} from '@/components/ui';

type SaasTab = 'tenants' | 'planes' | 'webhooks';

const IS_DEVELOPMENT = process.env.NODE_ENV === 'development';

const INITIAL_TENANTS: TenantDto[] = [
  {
    id: 1,
    nombreComercial: 'Mi Peluquería & Barbería SaaS',
    subdominioSlug: 'barberia-central',
    identificacionFiscal: '30-71548963-9',
    emailPropietario: 'duenio@mipeluqueria.com.ar',
    paisIso: 'AR',
    activo: true,
  },
];

const FALLBACK_PLANES: PlanSuscripcionDto[] = [
  {
    id: 1,
    nombre: 'Starter',
    precioMensualUsd: 19.99,
    maxSucursales: 1,
    maxPeluqueros: 3,
    incluyeWhatsAppRecordatorios: true,
    incluyePortalReservasOnline: true,
    activo: true,
  },
  {
    id: 2,
    nombre: 'Professional',
    precioMensualUsd: 29.99,
    maxSucursales: 2,
    maxPeluqueros: 10,
    incluyeWhatsAppRecordatorios: true,
    incluyePortalReservasOnline: true,
    activo: true,
  },
  {
    id: 3,
    nombre: 'Enterprise',
    precioMensualUsd: 59.99,
    maxSucursales: 10,
    maxPeluqueros: 50,
    incluyeWhatsAppRecordatorios: true,
    incluyePortalReservasOnline: true,
    activo: true,
  },
];

export default function SuperAdminSaaSConsolePage() {
  const { sucursalId, switchSucursal } = useAuth();
  const [tab, setTab] = useState<SaasTab>('tenants');
  const [tenants, setTenants] = useState<TenantDto[]>(INITIAL_TENANTS);
  const [sucursales, setSucursales] = useState<SucursalResponseDto[]>([]);
  const [planes, setPlanes] = useState<PlanSuscripcionDto[]>([]);
  const [miSuscripcion, setMiSuscripcion] =
    useState<SuscripcionEstadoResponseDto | null>(null);
  const [cargandoSaaS, setCargandoSaaS] = useState<boolean>(true);
  const [creandoSucursal, setCreandoSucursal] = useState<boolean>(false);
  const [cambiandoPlanId, setCambiandoPlanId] = useState<number | null>(null);

  // Alta de Sucursal
  const [nombreSucursal, setNombreSucursal] = useState('');
  const [direccionSucursal, setDireccionSucursal] = useState('');
  const [telefonoSucursal, setTelefonoSucursal] = useState('');
  const [esCasaMatriz, setEsCasaMatriz] = useState(false);

  // Simulador de Webhook de Pago (exclusivo entorno development)
  const [webhookProveedor, setWebhookProveedor] = useState<
    'MercadoPago' | 'Stripe'
  >('MercadoPago');
  const [webhookTurnoId, setWebhookTurnoId] = useState<number>(1);
  const [webhookSignature, setWebhookSignature] = useState<string>(
    'firma-hmac-valida'
  );
  const [disparandoWebhook, setDisparandoWebhook] = useState<boolean>(false);

  const cargarConsolaSaaS = async () => {
    setCargandoSaaS(true);
    try {
      const [sucRes, planRes, subRes] = await Promise.allSettled([
        sucursalesService.getAll(),
        suscripcionesService.getPlanes(),
        suscripcionesService.getMiSuscripcion(),
      ]);

      if (sucRes.status === 'fulfilled' && sucRes.value.success) {
        setSucursales(extractPagedItems<SucursalResponseDto>(sucRes.value.data));
      }
      if (planRes.status === 'fulfilled' && planRes.value.success) {
        setPlanes(extractPagedItems<PlanSuscripcionDto>(planRes.value.data));
      }
      if (
        subRes.status === 'fulfilled' &&
        subRes.value.success &&
        subRes.value.data
      ) {
        setMiSuscripcion(subRes.value.data);
      }
    } finally {
      setCargandoSaaS(false);
    }
  };

  useEffect(() => {
    void cargarConsolaSaaS();
  }, []);

  const handleCrearSucursal = async (e: React.FormEvent) => {
    e.preventDefault();
    setCreandoSucursal(true);
    try {
      const res = await sucursalesService.crear({
        nombre: nombreSucursal.trim(),
        direccion: direccionSucursal.trim(),
        telefono: telefonoSucursal.trim(),
        timeZoneId: 'America/Argentina/Cordoba',
        monedaIso: 'ARS',
        esCasaMatriz,
      });
      if (res.success) {
        notifyApiSuccess(res, `Sucursal #${res.data} creada exitosamente.`);
        setNombreSucursal('');
        setDireccionSucursal('');
        setTelefonoSucursal('');
        setEsCasaMatriz(false);
        await cargarConsolaSaaS();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo crear la sucursal.',
      });
    } finally {
      setCreandoSucursal(false);
    }
  };

  const handleCambiarPlan = async (nuevoPlanId: number) => {
    setCambiandoPlanId(nuevoPlanId);
    try {
      const res = await suscripcionesService.cambiarPlan({
        nuevoPlanSuscripcionId: nuevoPlanId,
      });
      if (res.success) {
        notifyApiSuccess(res, res.message || 'Plan actualizado correctamente.');
        await cargarConsolaSaaS();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo actualizar el plan de suscripción.',
      });
    } finally {
      setCambiandoPlanId(null);
    }
  };

  const handleDispararWebhookPrueba = async (e: React.FormEvent) => {
    e.preventDefault();
    setDisparandoWebhook(true);
    try {
      const { data } = await apiClient.post(
        `/webhooks/pagos/${webhookProveedor}`,
        {
          externalReference: `TURNO_${webhookTurnoId}`,
          status: 'approved',
          paymentId: `PAY_${Date.now()}`,
        },
        {
          headers: {
            'x-signature': webhookSignature,
          },
        }
      );
      notifyApiSuccess(
        data,
        `Webhook procesado: Turno #${webhookTurnoId} confirmado.`
      );
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'Error al probar el webhook de pagos.',
      });
    } finally {
      setDisparandoWebhook(false);
    }
  };

  const planesLista = planes.length > 0 ? planes : FALLBACK_PLANES;

  const tabs: Array<{ id: SaasTab; label: string; icon: React.ReactNode }> = [
    {
      id: 'tenants',
      label: 'Tenants y Sucursales',
      icon: <Building2 className="h-4 w-4" aria-hidden="true" />,
    },
    {
      id: 'planes',
      label: 'Planes y Suscripción',
      icon: <CreditCard className="h-4 w-4" aria-hidden="true" />,
    },
    ...(IS_DEVELOPMENT
      ? [
          {
            id: 'webhooks' as const,
            label: 'Simulador Webhooks (Dev)',
            icon: <Webhook className="h-4 w-4" aria-hidden="true" />,
          },
        ]
      : []),
  ];

  return (
    <div className="max-w-[1440px] mx-auto space-y-5 min-w-0">
      <header className="bg-surface border border-border-subtle rounded-xl p-5 space-y-4 shadow-sm">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2 mb-1">
              <Badge variant="courtesy">
                <ShieldAlert className="h-3.5 w-3.5" aria-hidden="true" />
                SuperAdminSaaS
              </Badge>
              <span className="text-xs text-ink-secondary">
                Backoffice exclusivo de plataforma
              </span>
            </div>
            <h1 className="text-xl sm:text-2xl font-semibold tracking-tight text-ink-primary">
              Consola SaaS
            </h1>
            <p className="text-sm text-ink-secondary mt-0.5">
              Administración multi-tenant de salones, sucursales activas y
              asignación de planes comerciales.
            </p>
          </div>
        </div>

        <nav
          aria-label="Secciones de Consola SaaS"
          className="flex flex-wrap gap-1.5 border-t border-border-subtle pt-3.5"
        >
          {tabs.map((t) => {
            const active = tab === t.id;
            return (
              <button
                key={t.id}
                type="button"
                onClick={() => setTab(t.id)}
                className={`inline-flex items-center gap-2 h-9 px-3.5 rounded-lg text-xs font-medium transition-colors ${
                  active
                    ? 'bg-brand-primary text-white shadow-sm'
                    : 'bg-elevated/70 text-ink-secondary hover:text-ink-primary hover:bg-elevated'
                }`}
              >
                {t.icon}
                <span>{t.label}</span>
              </button>
            );
          })}
        </nav>
      </header>

      {/* TAB 1: TENANTS Y SUCURSALES */}
      {tab === 'tenants' && (
        <div className="space-y-5">
          <section className="bg-surface border border-border-subtle rounded-xl p-5 space-y-3 shadow-sm min-w-0">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h2 className="text-base font-semibold text-ink-primary flex items-center gap-2">
                <Globe className="h-4 w-4 text-brand-primary" aria-hidden="true" />
                Salones suscriptos en plataforma
              </h2>
              <Badge variant="neutral" className="font-mono-tabular">
                {tenants.length} tenant(s)
              </Badge>
            </div>

            <div className="overflow-x-auto rounded-lg border border-border-subtle">
              <table className="w-full text-left text-sm">
                <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                  <tr>
                    <th className="px-3.5 py-2.5">ID</th>
                    <th className="px-3.5 py-2.5">Nombre comercial</th>
                    <th className="px-3.5 py-2.5">Subdominio</th>
                    <th className="px-3.5 py-2.5">Propietario</th>
                    <th className="px-3.5 py-2.5 text-right">Estado</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border-subtle text-xs">
                  {tenants.map((t) => (
                    <tr
                      key={t.id}
                      className="hover:bg-elevated/40 transition-colors"
                    >
                      <td className="px-3.5 py-2.5 font-mono-tabular text-ink-secondary">
                        #{t.id}
                      </td>
                      <td className="px-3.5 py-2.5 font-medium text-ink-primary">
                        {t.nombreComercial}
                      </td>
                      <td className="px-3.5 py-2.5 font-mono-tabular text-brand-primary">
                        {t.subdominioSlug}.mipeluqueria.com
                      </td>
                      <td className="px-3.5 py-2.5 text-ink-secondary">
                        {t.emailPropietario}
                      </td>
                      <td className="px-3.5 py-2.5 text-right">
                        <button
                          type="button"
                          onClick={() =>
                            setTenants((prev) =>
                              prev.map((item) =>
                                item.id === t.id
                                  ? { ...item, activo: !item.activo }
                                  : item
                              )
                            )
                          }
                          className={`inline-flex items-center px-2.5 py-1 rounded-md border text-xs font-medium transition-colors ${
                            t.activo
                              ? SEMANTIC_COLORS.cash.badgeClass
                              : SEMANTIC_COLORS.danger.badgeClass
                          }`}
                        >
                          {t.activo ? 'Activo' : 'Suspendido'}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
            <section className="lg:col-span-7 bg-surface border border-border-subtle rounded-xl p-5 space-y-3 shadow-sm min-w-0">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <h2 className="text-base font-semibold text-ink-primary">
                  Sucursales físicas del tenant
                </h2>
                <Badge variant="neutral" className="font-mono-tabular">
                  {sucursales.length} registradas
                </Badge>
              </div>

              <AsyncStateGuard
                isLoading={cargandoSaaS}
                isEmpty={sucursales.length === 0}
                loadingText="Cargando sucursales del tenant..."
                emptyTitle="Sin sucursales registradas"
                emptyDescription="Creá una sucursal usando el formulario lateral."
                onRetry={cargarConsolaSaaS}
              >
                <div className="overflow-x-auto rounded-lg border border-border-subtle">
                  <table className="w-full text-left text-sm">
                    <thead className="bg-elevated text-xs font-medium text-ink-secondary border-b border-border-subtle">
                      <tr>
                        <th className="px-3 py-2.5">Sucursal</th>
                        <th className="px-3 py-2.5">Dirección</th>
                        <th className="px-3 py-2.5">Tipo</th>
                        <th className="px-3 py-2.5 text-right">Contexto</th>
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
              <h2 className="text-base font-semibold text-ink-primary">
                Alta de sucursal
              </h2>
              <InputField
                label="Nombre *"
                required
                placeholder="Ej. Sucursal Cerro"
                value={nombreSucursal}
                onChange={(e) => setNombreSucursal(e.target.value)}
              />
              <InputField
                label="Dirección *"
                required
                placeholder="Ej. Av. Rafael Núñez 4120"
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
                <span>Es Casa Matriz</span>
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

      {/* TAB 2: PLANES Y SUSCRIPCIÓN */}
      {tab === 'planes' && (
        <div className="space-y-5">
          {miSuscripcion && (
            <section
              className={`rounded-xl border p-5 flex flex-wrap items-center justify-between gap-4 shadow-sm ${SEMANTIC_COLORS.cash.surfaceClass}`}
            >
              <div className="space-y-1">
                <div className="flex flex-wrap items-center gap-2">
                  <Badge variant="success">Suscripción vigente</Badge>
                  <span className="text-xs text-ink-secondary">
                    Vencimiento:{' '}
                    <strong className="font-mono-tabular text-ink-primary">
                      {formatDateOnly(miSuscripcion.fechaProximoVencimiento)}
                    </strong>
                  </span>
                </div>
                <h2 className="text-lg font-semibold text-ink-primary">
                  {miSuscripcion.nombreComercial} — Plan{' '}
                  {miSuscripcion.planActual}
                </h2>
              </div>
              <div className="text-right">
                <span
                  className={`text-2xl font-bold font-mono-tabular ${SEMANTIC_COLORS.cash.textClass}`}
                >
                  {formatCurrency(miSuscripcion.precioMensualUsd, 'USD')}
                </span>
                <span className="text-xs text-ink-secondary ml-1">/ mes</span>
              </div>
            </section>
          )}

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {planesLista.map((plan) => {
              const esPlanActual =
                miSuscripcion?.planActual?.toLowerCase() ===
                plan.nombre.toLowerCase();
              return (
                <div
                  key={plan.id}
                  className={`bg-surface rounded-xl p-5 flex flex-col justify-between gap-4 shadow-sm border ${
                    esPlanActual
                      ? 'border-brand-primary ring-2 ring-brand-primary/15'
                      : 'border-border-subtle'
                  }`}
                >
                  <div className="space-y-3">
                    <div className="flex items-center justify-between gap-2">
                      <h3 className="text-lg font-semibold text-ink-primary">
                        {plan.nombre}
                      </h3>
                      {esPlanActual && <Badge variant="success">Actual</Badge>}
                    </div>
                    <p className="text-2xl font-bold font-mono-tabular text-ink-primary">
                      {formatCurrency(plan.precioMensualUsd, 'USD')}
                      <span className="text-xs font-normal text-ink-secondary">
                        {' '}
                        / mes
                      </span>
                    </p>
                    <ul className="space-y-1.5 text-xs text-ink-secondary pt-2 border-t border-border-subtle">
                      <li className="flex items-center gap-2">
                        <CheckCircle2
                          className={`h-3.5 w-3.5 shrink-0 ${SEMANTIC_COLORS.cash.textClass}`}
                          aria-hidden="true"
                        />
                        <span>
                          Hasta{' '}
                          <strong className="font-mono-tabular text-ink-primary">
                            {plan.maxSucursales}
                          </strong>{' '}
                          sucursal(es)
                        </span>
                      </li>
                      <li className="flex items-center gap-2">
                        <CheckCircle2
                          className={`h-3.5 w-3.5 shrink-0 ${SEMANTIC_COLORS.cash.textClass}`}
                          aria-hidden="true"
                        />
                        <span>
                          Hasta{' '}
                          <strong className="font-mono-tabular text-ink-primary">
                            {plan.maxPeluqueros}
                          </strong>{' '}
                          profesionales
                        </span>
                      </li>
                      {plan.incluyePortalReservasOnline && (
                        <li className="flex items-center gap-2">
                          <CheckCircle2
                            className={`h-3.5 w-3.5 shrink-0 ${SEMANTIC_COLORS.cash.textClass}`}
                            aria-hidden="true"
                          />
                          <span>Portal de reservas online 24/7</span>
                        </li>
                      )}
                      {plan.incluyeWhatsAppRecordatorios && (
                        <li className="flex items-center gap-2">
                          <CheckCircle2
                            className={`h-3.5 w-3.5 shrink-0 ${SEMANTIC_COLORS.cash.textClass}`}
                            aria-hidden="true"
                          />
                          <span>Recordatorios automáticos WhatsApp</span>
                        </li>
                      )}
                    </ul>
                  </div>

                  <Button
                    type="button"
                    variant={esPlanActual ? 'secondary' : 'primary'}
                    className="w-full"
                    disabled={esPlanActual}
                    isLoading={cambiandoPlanId === plan.id}
                    onClick={() => handleCambiarPlan(plan.id)}
                  >
                    {esPlanActual ? 'Plan vigente' : `Activar ${plan.nombre}`}
                  </Button>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* TAB 3: SIMULADOR DE WEBHOOKS (EXCLUSIVO DEVELOPMENT) */}
      {IS_DEVELOPMENT && tab === 'webhooks' && (
        <form
          onSubmit={handleDispararWebhookPrueba}
          className="max-w-xl bg-surface border border-border-subtle rounded-xl p-6 space-y-4 shadow-sm"
        >
          <div className="flex items-center justify-between gap-2">
            <h2 className="text-base font-semibold text-ink-primary">
              Simulador de webhook de seña online
            </h2>
            <Badge variant="warning">Solo desarrollo</Badge>
          </div>
          <p className="text-xs text-ink-secondary">
            Simula una notificación de pago aprobado desde Mercado Pago o Stripe
            para acreditar la seña de un turno y pasarlo a estado Confirmado.
          </p>

          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="webhook-proveedor"
              className="text-xs font-medium text-ink-secondary"
            >
              Pasarela de pago
            </label>
            <select
              id="webhook-proveedor"
              value={webhookProveedor}
              onChange={(e) =>
                setWebhookProveedor(e.target.value as 'MercadoPago' | 'Stripe')
              }
              className="h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-sm text-ink-primary focus:border-brand-primary focus:outline-none focus:ring-2 focus:ring-brand-accent/25"
            >
              <option value="MercadoPago">Mercado Pago</option>
              <option value="Stripe">Stripe</option>
            </select>
          </div>

          <NumericInput
            label="ID del turno a confirmar *"
            allowDecimals={false}
            min={1}
            emptyValueOnBlur={1}
            value={webhookTurnoId}
            onValueChange={(val) => setWebhookTurnoId(val ?? 1)}
          />

          <InputField
            label="Firma de verificación (x-signature) *"
            required
            value={webhookSignature}
            onChange={(e) => setWebhookSignature(e.target.value)}
          />

          <Button
            type="submit"
            variant="primary"
            className="w-full"
            isLoading={disparandoWebhook}
          >
            Disparar webhook de pago aprobado
          </Button>
        </form>
      )}
    </div>
  );
}
