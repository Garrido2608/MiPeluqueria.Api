// filepath: src/app/(auth)/recuperar-clave/page.tsx
'use client';

import React, { useRef, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { authService } from '@/services/api.services';
import { Button, InputField } from '@/components/ui/DesignSystem';

export default function RecuperarClaveConOtpPage() {
  const router = useRouter();
  const [paso, setPaso] = useState<1 | 2>(1);
  const [email, setEmail] = useState<string>('');
  const [otpDigits, setOtpDigits] = useState<string[]>([
    '',
    '',
    '',
    '',
    '',
    '',
  ]);
  const [nuevaPassword, setNuevaPassword] = useState<string>('');
  const [confirmarPassword, setConfirmarPassword] = useState<string>('');
  const inputsRef = useRef<(HTMLInputElement | null)[]>([]);

  const [loading, setLoading] = useState<boolean>(false);
  const [mensaje, setMensaje] = useState<string | null>(null);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleSolicitarOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);
    setMensaje(null);
    setLoading(true);
    try {
      const res = await authService.olvidePassword({ email: email.trim() });
      if (res.success) {
        setMensaje(
          'Si el correo existe en nuestra base, te hemos enviado un PIN OTP de 6 dígitos (válido por 15 minutos).'
        );
        setPaso(2);
      } else {
        setErrorMsg(res.message || 'No se pudo procesar la solicitud.');
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setErrorMsg(customErr?.message || 'Error al solicitar código.');
    } finally {
      setLoading(false);
    }
  };

  const handleOtpChange = (index: number, value: string) => {
    const clean = value.replace(/\D/g, '').slice(-1);
    const next = [...otpDigits];
    next[index] = clean;
    setOtpDigits(next);
    if (clean && index < 5) {
      inputsRef.current[index + 1]?.focus();
    }
  };

  const handleRestablecerPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);
    const codigoOtp = otpDigits.join('');
    if (codigoOtp.length !== 6) {
      setErrorMsg(
        'Debés ingresar los 6 dígitos del código OTP recibido en tu correo.'
      );
      return;
    }
    if (nuevaPassword !== confirmarPassword) {
      setErrorMsg('Las contraseñas no coinciden.');
      return;
    }

    setLoading(true);
    try {
      const res = await authService.restablecerPassword({
        email: email.trim(),
        codigoOtp,
        nuevaPassword,
      });
      if (res.success) {
        router.push('/login?reset=success');
      } else {
        setErrorMsg(res.message || 'Código OTP inválido o expirado.');
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setErrorMsg(customErr?.message || 'Error al restablecer la contraseña.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#FAF8F5] flex items-center justify-center p-4">
      <div className="w-full max-w-md bg-white border border-stone-200 rounded-lg p-8 shadow-sm space-y-6">
        <div>
          <span className="text-xs font-bold uppercase tracking-widest text-amber-900">
            Seguridad • Recuperación por PIN OTP
          </span>
          <h1 className="text-2xl font-bold text-stone-900 mt-1">
            Restablecer Contraseña
          </h1>
          <p className="text-xs text-stone-500 mt-1">
            Al restablecer tu clave se revocarán automáticamente todas las
            sesiones activas.
          </p>
        </div>

        {mensaje && (
          <div className="p-3 rounded bg-emerald-50 border border-emerald-200 text-xs text-emerald-900">
            {mensaje}
          </div>
        )}
        {errorMsg && (
          <div className="p-3 rounded bg-red-50 border border-red-200 text-xs text-red-700 font-medium">
            {errorMsg}
          </div>
        )}

        {paso === 1 ? (
          <form onSubmit={handleSolicitarOtp} className="space-y-4">
            <InputField
              label="Correo Electrónico de tu Cuenta *"
              type="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="cliente@correo.com"
            />
            <Button
              type="submit"
              variant="primary"
              className="w-full"
              isLoading={loading}
            >
              Enviar Código OTP de 6 Dígitos
            </Button>
          </form>
        ) : (
          <form onSubmit={handleRestablecerPassword} className="space-y-4">
            <div>
              <label className="text-xs font-semibold uppercase tracking-wider text-slate-700 block mb-2">
                Código PIN OTP (6 Dígitos) *
              </label>
              <div className="flex justify-between gap-2">
                {otpDigits.map((digit, idx) => (
                  <input
                    key={idx}
                    ref={(el) => {
                      inputsRef.current[idx] = el;
                    }}
                    type="text"
                    inputMode="numeric"
                    maxLength={1}
                    value={digit}
                    onChange={(e) => handleOtpChange(idx, e.target.value)}
                    className="w-11 h-12 text-center font-mono text-lg font-bold border border-stone-300 rounded-md focus:border-stone-900 focus:outline-none"
                  />
                ))}
              </div>
            </div>

            <InputField
              label="Nueva Contraseña *"
              type="password"
              required
              value={nuevaPassword}
              onChange={(e) => setNuevaPassword(e.target.value)}
            />
            <InputField
              label="Confirmar Nueva Contraseña *"
              type="password"
              required
              value={confirmarPassword}
              onChange={(e) => setConfirmarPassword(e.target.value)}
            />

            <Button
              type="submit"
              variant="editorial"
              className="w-full"
              isLoading={loading}
            >
              Confirmar Nueva Contraseña
            </Button>
          </form>
        )}

        <div className="pt-3 border-t border-stone-100 text-xs text-center">
          <Link
            href="/login"
            className="underline text-stone-600 hover:text-stone-900"
          >
            ← Volver a Iniciar Sesión
          </Link>
        </div>
      </div>
    </div>
  );
}