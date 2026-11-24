// filepath: src/app/(public)/mi-cuenta/perfil/page.tsx
'use client';

import React, { useState } from 'react';
import Link from 'next/link';
import { authService } from '@/services/api.services';
import { useAuth } from '@/context/AppProviders';
import { Button, InputField } from '@/components/ui/DesignSystem';

export default function MiPerfilSeguridadClientePage() {
  const { user, logout } = useAuth();
  const [passwordActual, setPasswordActual] = useState<string>('');
  const [nuevaPassword, setNuevaPassword] = useState<string>('');
  const [confirmacion, setConfirmacion] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [mensaje, setMensaje] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleCambiarPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setMensaje(null);
    setErrorMsg(null);
    if (nuevaPassword !== confirmacion) {
      setErrorMsg('La confirmación de la nueva contraseña no coincide.');
      return;
    }
    setLoading(true);
    try {
      const res = await authService.cambiarPassword({
        passwordActual,
        nuevaPassword,
      });
      if (res.success) {
        setMensaje(
          'Tu contraseña se actualizó correctamente y se revocaron los Refresh Tokens anteriores.'
        );
        setPasswordActual('');
        setNuevaPassword('');
        setConfirmacion('');
      } else {
        setErrorMsg(res.message || 'No se pudo actualizar la contraseña.');
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setErrorMsg(customErr?.message || 'Error al actualizar la contraseña.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#FAF8F5] text-stone-900 py-8 px-4">
      <div className="max-w-2xl mx-auto space-y-6">
        <div className="flex items-center justify-between border-b border-stone-200 pb-4">
          <div>
            <h1 className="text-xl font-bold">Mi Perfil y Seguridad</h1>
            <p className="text-xs text-stone-500">
              Gestión de credenciales y sesiones de tu cuenta personal.
            </p>
          </div>
          <div className="flex gap-2">
            <Link
              href="/mi-cuenta/turnos"
              className="px-3 py-1.5 rounded border border-stone-300 bg-white text-xs font-semibold hover:bg-stone-100"
            >
              ← Mis Turnos
            </Link>
            <Button variant="danger" size="sm" onClick={logout}>
              Cerrar Sesión
            </Button>
          </div>
        </div>

        <div className="bg-white border border-stone-200 rounded-lg p-6 space-y-2 text-xs">
          <h2 className="text-sm font-bold uppercase text-stone-700">
            Datos de Sesión
          </h2>
          <p>
            <strong>Usuario:</strong> {user?.username || 'Cliente'}
          </p>
          <p>
            <strong>Correo Verificado:</strong> {user?.email || '—'}
          </p>
          <p>
            <strong>Rol Asignado:</strong> {user?.rol || 'Cliente'} (Cliente ID
            #{user?.clienteId || 1})
          </p>
        </div>

        <form
          onSubmit={handleCambiarPassword}
          className="bg-white border border-stone-200 rounded-lg p-6 space-y-4"
        >
          <h2 className="text-base font-bold">Cambiar Contraseña</h2>
          {mensaje && (
            <div className="p-3 rounded bg-emerald-50 border border-emerald-200 text-xs text-emerald-900">
              {mensaje}
            </div>
          )}
          {errorMsg && (
            <div className="p-3 rounded bg-red-50 border border-red-200 text-xs text-red-700">
              {errorMsg}
            </div>
          )}

          <InputField
            label="Contraseña Actual *"
            type="password"
            required
            value={passwordActual}
            onChange={(e) => setPasswordActual(e.target.value)}
          />
          <InputField
            label="Nueva Contraseña *"
            type="password"
            required
            value={nuevaPassword}
            onChange={(e) => setNuevaPassword(e.target.value)}
          />
          <InputField
            label="Repetir Nueva Contraseña *"
            type="password"
            required
            value={confirmacion}
            onChange={(e) => setConfirmacion(e.target.value)}
          />

          <Button type="submit" variant="primary" isLoading={loading}>
            Actualizar Contraseña
          </Button>
        </form>
      </div>
    </div>
  );
}