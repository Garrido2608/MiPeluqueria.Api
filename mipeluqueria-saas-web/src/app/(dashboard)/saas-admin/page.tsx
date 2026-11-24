// filepath: src/app/(dashboard)/saas-admin/page.tsx
'use client';

import React, { useEffect, useMemo, useState } from 'react';
import { apiClient } from '@/lib/axios';
import {
  AuditLogDto,
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
import { formatCurrency, formatDateTime } from '@/lib/formatters';
import { AsyncStateGuard, Button, InputField } from '@/components/ui';

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

const INITIAL_AUDIT_LOGS: AuditLogDto[] = [
  {
    id: 1042,
    usuarioId: 1,
    entidadNombre: 'Producto',
    accion: 'UPDATE',
    clavePrimaria: '18',
    valoresAnterioresJson: JSON.stringify({
      Nombre: 'Tintura Majirel 7.1',
      PrecioCosto: 8500,
      PrecioVenta: 14000,
      StockActual: 12,
    }),
    valoresNuevosJson: JSON.stringify({
      Nombre: 'Tintura Majirel 7.1',
      PrecioCosto: 9800,
      PrecioVenta: 16200,
      StockActual: 24,
    }),
    ipAddress: '190.192.44.11',
    fechaUtc: '2026-10-20T18:45:00Z',
  },
];

export default function SuperAdminSaaSConsolePage() {
  const { sucursalId, switchSucursal } = useAuth();
  const [tab, setTab] = useState<'tenants' | 'planes' | 'audit' | 'webhooks'>(
    'tenants'
  );
  const [tenants, setTenants] = useState<TenantDto[]>(INITIAL_TENANTS);
  const [sucursales, setSucursales] = useState<SucursalResponseDto[]>([]);
  const [planes, setPlanes] = useState<PlanSuscripcionDto[]>([]);
  const [miSuscripcion, setMiSuscripcion] =
    useState<SuscripcionEstadoResponseDto | null>(null);
  const [cargandoSaaS, setCargandoSaaS] = useState<boolean>(true);
  const [feedback, setFeedback] = useState<string | null>(null);

  // Alta de Sucursal
  const [nombreSucursal, setNombreSucursal] = useState('');
  const [direccionSucursal, setDireccionSucursal] = useState('');
  const [telefonoSucursal, setTelefonoSucursal] = useState('');
  const [esCasaMatriz, setEsCasaMatriz] = useState(false);

  // Simulador de Webhook de Pago (WebhooksController)
  const [webhookProveedor, setWebhookProveedor] = useState<
    'MercadoPago' | 'Stripe'
  >('MercadoPago');
  const [webhookTurnoId, setWebhookTurnoId] = useState<number>(1);
  const [webhookSignature, setWebhookSignature] = useState<string>(
    'firma-hmac-valida'
  );

  // AuditLogs JSON Diff
  const [logSeleccionado] = useState<AuditLogDto>(INITIAL_AUDIT_LOGS[0]);

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
    try {
      const res = await sucursalesService.crear({
        nombre: nombreSucursal,
        direccion: direccionSucursal,
        telefono: telefonoSucursal,
        timeZoneId: 'America/Argentina/Cordoba',
        monedaIso: 'ARS',
        esCasaMatriz,
      });
      if (res.success) {
        notifyApiSuccess(res, `Sucursal #${res.data} creada exitosamente.`);
        setNombreSucursal('');
        setDireccionSucursal('');
        setTelefonoSucursal('');
        setFeedback(`Sucursal #${res.data} creada exitosamente.`);
        await cargarConsolaSaaS();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo crear la sucursal.',
      });
    }
  };

  const handleCambiarPlan = async (nuevoPlanId: number) => {
    try {
      const res = await suscripcionesService.cambiarPlan({
        nuevoPlanSuscripcionId: nuevoPlanId,
      });
      if (res.success) {
        notifyApiSuccess(res, res.message || 'Plan actualizado.');
        setFeedback(res.message);
        await cargarConsolaSaaS();
      }
    } catch (err) {
      notifyApiError(err, {
        fallbackMessage: 'No se pudo actualizar el plan de suscripción.',
      });
    }
  };

  const handleDispararWebhookPrueba = async (e: React.FormEvent) => {
    e.preventDefault();
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
      setFeedback(
        data.message ||
          `Webhook procesado: el Turno #${webhookTurnoId} pasó a estado Confirmado.`
      );
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setFeedback(
        customErr?.message || 'Error al probar el Webhook de pagos.'
      );
    }
  };

  const diffRows = useMemo(() => {
    let beforeObj: Record<string, unknown> = {};
    let afterObj: Record<string, unknown> = {};
    try {
      beforeObj = JSON.parse(
        logSeleccionado.valoresAnterioresJson || '{}'
      ) as Record<string, unknown>;
    } catch {
      beforeObj = {};
    }
    try {
      afterObj = JSON.parse(
        logSeleccionado.valoresNuevosJson || '{}'
      ) as Record<string, unknown>;
    } catch {
      afterObj = {};
    }
    const allKeys = Array.from(
      new Set([...Object.keys(beforeObj), ...Object.keys(afterObj)])
    );
    return allKeys.map((key) => ({
      key,
      prevVal: JSON.stringify(beforeObj[key]),
      nextVal: JSON.stringify(afterObj[key]),
      changed:
        JSON.stringify(beforeObj[key]) !== JSON.stringify(afterObj[key]),
    }));
  }, [logSeleccionado]);

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 p-6 space-y-6">
      <header className="border-b border-slate-800 pb-5 flex flex-wrap items-center justify-between gap-4">
        <div>
          <span className="text-[11px] font-mono uppercase tracking-widest text-amber-400">
            Master Backoffice • Rol: SuperAdminSaaS
          </span>
          <h1 className="text-2xl font-bold text-white mt-0.5">
            Consola de Comando Global SaaS — MiPeluqueria.Api
          </h1>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            variant={tab === 'tenants' ? 'editorial' : 'secondary'}
            size="sm"
            onClick={() => setTab('tenants')}
          >
            1. Tenants y Sucursales
          </Button>
          <Button
            variant={tab === 'planes' ? 'editorial' : 'secondary'}
            size="sm"
            onClick={() => setTab('planes')}
          >
            2. Planes y MRR
          </Button>
          <Button
            variant={tab === 'audit' ? 'editorial' : 'secondary'}
            size="sm"
            onClick={() => setTab('audit')}
          >
            3. AuditLogs JSON Diff
          </Button>
          <Button
            variant={tab === 'webhooks' ? 'editorial' : 'secondary'}
            size="sm"
            onClick={() => setTab('webhooks')}
          >
            4. Simulador Webhooks Pago
          </Button>
        </div>
      </header>

      {feedback && (
        <div className="p-3.5 rounded bg-amber-900/80 border border-amber-600 text-xs text-white flex justify-between">
          <span>{feedback}</span>
          <button type="button" onClick={() => setFeedback(null)}>
            ✕
          </button>
        </div>
      )}

      {tab === 'tenants' && (
        <div className="space-y-6">
          <div className="bg-slate-900 border border-slate-800 rounded-lg p-5">
            <h2 className="text-base font-bold text-white mb-3">
              Salones Suscriptos (Tenants en Plataforma)
            </h2>
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-800 text-slate-300 uppercase">
                <tr>
                  <th className="p-2.5">Tenant ID</th>
                  <th className="p-2.5">Nombre Comercial</th>
                  <th className="p-2.5">Subdominio Slug</th>
                  <th className="p-2.5">Propietario</th>
                  <th className="p-2.5">Estado</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {tenants.map((t) => (
                  <tr key={t.id}>
                    <td className="p-2.5 font-mono">#{t.id}</td>
                    <td className="p-2.5 font-bold text-white">
                      {t.nombreComercial}
                    </td>
                    <td className="p-2.5 font-mono text-amber-400">
                      {t.subdominioSlug}.mipeluqueria.com
                    </td>
                    <td className="p-2.5">{t.emailPropietario}</td>
                    <td className="p-2.5">
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
                        className="px-2 py-0.5 rounded bg-emerald-950 text-emerald-300 font-mono"
                      >
                        {t.activo ? 'ACTIVO' : 'SUSPENDIDO'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
            <div className="lg:col-span-7 bg-slate-900 border border-slate-800 rounded-lg p-5 space-y-3">
              <h2 className="text-base font-bold text-white">
                Sucursales Físicas del Tenant ({(sucursales ?? []).length})
              </h2>
              <AsyncStateGuard
                isLoading={cargandoSaaS}
                isEmpty={(sucursales ?? []).length === 0}
                loadingText="Cargando sucursales del tenant..."
                emptyTitle="Sin sucursales registradas"
                emptyDescription="Creá una sucursal usando el formulario lateral."
                onRetry={cargarConsolaSaaS}
              >
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-800 text-slate-300">
                    <tr>
                      <th className="p-2">ID</th>
                      <th className="p-2">Sucursal</th>
                      <th className="p-2">Dirección</th>
                      <th className="p-2">Matriz</th>
                      <th className="p-2 text-right">Contexto</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800">
                    {(sucursales ?? []).map((s) => (
                      <tr key={s.id}>
                        <td className="p-2 font-mono">#{s.id}</td>
                        <td className="p-2 font-bold text-white">{s.nombre}</td>
                        <td className="p-2 text-slate-300">{s.direccion}</td>
                        <td className="p-2">{s.esCasaMatriz ? 'Sí' : 'No'}</td>
                        <td className="p-2 text-right">
                          <Button
                            variant={
                              sucursalId === s.id ? 'editorial' : 'secondary'
                            }
                            size="sm"
                            onClick={() => {
                              void switchSucursal(s.id);
                              setFeedback(
                                `Contexto X-Sucursal-Id cambiado a Sucursal #${s.id} (${s.nombre}).`
                              );
                            }}
                          >
                            {sucursalId === s.id ? 'Activa' : 'Operar Aquí'}
                          </Button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </AsyncStateGuard>
            </div>

            <form
              onSubmit={handleCrearSucursal}
              className="lg:col-span-5 bg-white text-slate-900 rounded-lg p-5 space-y-3"
            >
              <h2 className="text-base font-bold">Alta de Sucursal</h2>
              <InputField
                label="Nombre *"
                required
                value={nombreSucursal}
                onChange={(e) => setNombreSucursal(e.target.value)}
              />
              <InputField
                label="Dirección *"
                required
                value={direccionSucursal}
                onChange={(e) => setDireccionSucursal(e.target.value)}
              />
              <InputField
                label="Teléfono"
                value={telefonoSucursal}
                onChange={(e) => setTelefonoSucursal(e.target.value)}
              />
              <label className="flex items-center gap-2 text-xs font-semibold">
                <input
                  type="checkbox"
                  checked={esCasaMatriz}
                  onChange={(e) => setEsCasaMatriz(e.target.checked)}
                />
                Es Casa Matriz
              </label>
              <Button type="submit" variant="primary" className="w-full">
                Crear Sucursal
              </Button>
            </form>
          </div>
        </div>
      )}

      {tab === 'planes' && (
        <div className="space-y-6">
          {miSuscripcion && (
            <div className="bg-slate-900 border border-slate-800 rounded-lg p-5 flex justify-between items-center">
              <div>
                <h2 className="text-lg font-bold text-white">
                  {miSuscripcion.nombreComercial} — Plan{' '}
                  {miSuscripcion.planActual}
                </h2>
                <p className="text-xs text-slate-400">
                  Vencimiento:{' '}
                  {formatDateTime(miSuscripcion.fechaProximoVencimiento)}
                </p>
              </div>
              <span className="text-xl font-mono font-bold text-emerald-400">
                {formatCurrency(miSuscripcion.precioMensualUsd, 'USD')} / mes
              </span>
            </div>
          )}

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {(planes.length > 0
              ? planes
              : [
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
                ]
            ).map((plan) => (
              <div
                key={plan.id}
                className="bg-slate-900 border border-slate-800 rounded-lg p-5 space-y-3"
              >
                <h3 className="text-lg font-bold text-white">{plan.nombre}</h3>
                <p className="text-xl font-mono font-bold text-amber-400">
                  {formatCurrency(plan.precioMensualUsd, 'USD')} / mes
                </p>
                <Button
                  variant="editorial"
                  className="w-full"
                  onClick={() => handleCambiarPlan(plan.id)}
                >
                  Activar Plan {plan.nombre}
                </Button>
              </div>
            ))}
          </div>
        </div>
      )}

      {tab === 'audit' && (
        <div className="bg-slate-900 border border-slate-800 rounded-lg p-5 space-y-4">
          <h2 className="text-base font-bold text-white">
            Comparador Visual JSON Diff (AuditLogs)
          </h2>
          <table className="w-full text-left text-xs font-mono">
            <thead className="bg-slate-800 text-slate-300">
              <tr>
                <th className="p-2">Propiedad</th>
                <th className="p-2">ValoresAnterioresJson</th>
                <th className="p-2">ValoresNuevosJson</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800">
              {diffRows.map((row) => (
                <tr key={row.key}>
                  <td className="p-2 font-bold">{row.key}</td>
                  <td
                    className={
                      row.changed ? 'p-2 text-red-400 line-through' : 'p-2'
                    }
                  >
                    {row.prevVal}
                  </td>
                  <td
                    className={
                      row.changed ? 'p-2 text-emerald-400 font-bold' : 'p-2'
                    }
                  >
                    {row.nextVal}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'webhooks' && (
        <form
          onSubmit={handleDispararWebhookPrueba}
          className="max-w-lg bg-white text-slate-900 rounded-lg p-6 space-y-4"
        >
          <h2 className="text-base font-bold">
            Simulador de Webhook de Seña Online (POST
            /api/webhooks/pagos/&#123;proveedor&#125;)
          </h2>
          <p className="text-xs text-slate-600">
            Simula la notificación de pago aprobado de MercadoPago o Stripe para
            acreditar la seña de un turno (
            <code>TURNO_&#123;id&#125;</code>) y pasarlo a Confirmado.
          </p>
          <select
            value={webhookProveedor}
            onChange={(e) =>
              setWebhookProveedor(e.target.value as 'MercadoPago' | 'Stripe')
            }
            className="w-full rounded border border-slate-300 px-3 py-2 text-sm"
          >
            <option value="MercadoPago">MercadoPago</option>
            <option value="Stripe">Stripe</option>
          </select>
          <InputField
            label="ID del Turno a Confirmar (ExternalReference = TURNO_ID) *"
            type="number"
            min={1}
            required
            value={webhookTurnoId}
            onChange={(e) => setWebhookTurnoId(Number(e.target.value))}
          />
          <InputField
            label="Header x-signature *"
            required
            value={webhookSignature}
            onChange={(e) => setWebhookSignature(e.target.value)}
          />
          <Button type="submit" variant="editorial" className="w-full">
            Disparar Webhook de Pago Aprobado
          </Button>
        </form>
      )}
    </div>
  );
}