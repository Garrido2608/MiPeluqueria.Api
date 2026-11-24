// filepath: src/app/(auth)/login/page.tsx
'use client';

import React, { useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { authService } from '@/services/api.services';
import { useAuth } from '@/context/AppProviders';
import { Button, InputField } from '@/components/ui/DesignSystem';

export default function LoginPage() {
  const router = useRouter();
  const { setSession } = useAuth();
  const [usernameOrEmail, setUsernameOrEmail] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const [loading, setLoading] = useState<boolean>(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);
    setLoading(true);
    try {
      const res = await authService.login({ usernameOrEmail, password });
      if (res.success && res.data) {
        setSession(res.data);
        // Redirección adaptativa según los 5 portales por Rol
        switch (res.data.rol) {
          case 'SuperAdminSaaS':
            router.push('/saas-admin');
            break;
          case 'Administrador':
            router.push('/admin/dashboard');
            break;
          case 'Recepcionista':
          case 'Cajero':
            router.push('/recepcion/calendario');
            break;
          case 'Peluquero':
            router.push('/peluquero');
            break;
          default:
            router.push('/mi-cuenta/turnos');
            break;
        }
      } else {
        setErrorMsg(res.message || 'Credenciales inválidas.');
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setErrorMsg(
        customErr?.message ||
          'Error al iniciar sesión. Recordá que tras 5 intentos fallidos la cuenta se bloquea por 15 minutos.'
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#FAF8F5] flex items-center justify-center p-4">
      <div className="w-full max-w-md bg-white border border-stone-200 rounded-lg p-8 shadow-sm space-y-6">
        <div>
          <p className="text-xs font-bold uppercase tracking-widest text-amber-900">
            Acceso Seguro • MiPeluqueria SaaS
          </p>
          <h1 className="text-2xl font-bold text-stone-900 mt-1">Iniciar Sesión</h1>
          <p className="text-xs text-stone-500 mt-1">
            Protegido con bloqueo antibot (5 intentos / 15 min) y política Rate Limit.
          </p>
        </div>

        {errorMsg && (
          <div className="p-3.5 rounded bg-red-50 border border-red-200 text-xs text-red-700 font-medium">
            {errorMsg}
          </div>
        )}

        <form onSubmit={handleLogin} className="space-y-4">
          <InputField
            label="Usuario o Correo Electrónico"
            required
            value={usernameOrEmail}
            onChange={(e) => setUsernameOrEmail(e.target.value)}
            placeholder="admin@mipeluqueria.com"
          />
          <InputField
            label="Contraseña"
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
          />
          <Button type="submit" variant="primary" className="w-full" isLoading={loading}>
            Ingresar al Sistema
          </Button>
        </form>

        <div className="flex items-center justify-between text-xs text-stone-600 pt-2 border-t border-stone-100">
          <Link href="/recuperar-clave" className="underline hover:text-stone-900">
            ¿Olvidaste tu contraseña?
          </Link>
          <Link href="/registro" className="font-semibold text-amber-900 hover:underline">
            Crear cuenta con PIN OTP
          </Link>
        </div>
      </div>
    </div>
  );
}