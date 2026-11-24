// filepath: src/app/(auth)/registro/page.tsx
'use client';

import React, { useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import Link from 'next/link';
import { authService } from '@/services/api.services';
import { Button, InputField } from '@/components/ui/DesignSystem';

export default function RegistroConOtpPage() {
  const router = useRouter();
  const [etapa, setEtapa] = useState<'formulario' | 'otp'>('formulario');
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [nombre, setNombre] = useState('');
  const [apellido, setApellido] = useState('');
  const [telefono, setTelefono] = useState('');
  const [fechaNacimiento, setFechaNacimiento] = useState('2000-01-01');
  const [aceptaTerminos, setAceptaTerminos] = useState(false);

  // Estado para el PIN OTP de 6 dígitos (Usuario.CodigoVerificacion)
  const [otpDigits, setOtpDigits] = useState<string[]>(['', '', '', '', '', '']);
  const inputsRef = useRef<(HTMLInputElement | null)[]>([]);

  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const handleRegistroSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMsg(null);
    if (!aceptaTerminos) {
      setErrorMsg('Es obligatorio aceptar los Términos de Uso y la Política de Privacidad.');
      return;
    }

    setLoading(true);
    try {
      const res = await authService.registro({
        username,
        email,
        password,
        nombre,
        apellido,
        telefono,
        fechaNacimiento,
        aceptaTerminos,
      });
      if (res.success) {
        setEtapa('otp');
      } else {
        setErrorMsg(res.message);
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setErrorMsg(customErr?.message || 'Error al registrar la cuenta.');
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

  const handleVerificarOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    const codigoOtp = otpDigits.join('');
    if (codigoOtp.length !== 6) {
      setErrorMsg('Ingresá los 6 dígitos del código enviado a tu email.');
      return;
    }

    setLoading(true);
    setErrorMsg(null);
    try {
      const res = await authService.verificarEmail({ email, codigoOtp });
      if (res.success) {
        router.push('/login?verified=true');
      } else {
        setErrorMsg(res.message || 'Código OTP inválido o expirado (validez: 15 min).');
      }
    } catch (err: unknown) {
      const customErr = err as { message?: string };
      setErrorMsg(customErr?.message || 'Código OTP incorrecto.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#FAF8F5] flex items-center justify-center p-4">
      <div className="w-full max-w-lg bg-white border border-stone-200 rounded-lg p-8 shadow-sm space-y-6">
        {etapa === 'formulario' ? (
          <>
            <div>
              <h1 className="text-xl font-bold text-stone-900">Crear Cuenta</h1>
              <p className="text-xs text-stone-500 mt-1">
                Te enviaremos un PIN OTP de 6 dígitos por correo para verificar tu identidad.
              </p>
            </div>

            {errorMsg && (
              <div className="p-3 rounded bg-red-50 border border-red-200 text-xs text-red-700">
                {errorMsg}
              </div>
            )}

            <form onSubmit={handleRegistroSubmit} className="space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <InputField
                  label="Nombre *"
                  required
                  value={nombre}
                  onChange={(e) => setNombre(e.target.value)}
                />
                <InputField
                  label="Apellido *"
                  required
                  value={apellido}
                  onChange={(e) => setApellido(e.target.value)}
                />
                <InputField
                  label="Nombre de Usuario *"
                  required
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                />
                <InputField
                  label="Teléfono / WhatsApp *"
                  required
                  value={telefono}
                  onChange={(e) => setTelefono(e.target.value)}
                />
                <InputField
                  label="Correo Electrónico *"
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
                <InputField
                  label="Fecha de Nacimiento *"
                  type="date"
                  required
                  value={fechaNacimiento}
                  onChange={(e) => setFechaNacimiento(e.target.value)}
                />
              </div>

              <InputField
                label="Contraseña *"
                type="password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />

              <label className="flex items-start gap-2 text-xs text-stone-600">
                <input
                  type="checkbox"
                  checked={aceptaTerminos}
                  onChange={(e) => setAceptaTerminos(e.target.checked)}
                  className="mt-0.5"
                />
                <span>
                  Acepto los{' '}
                  <Link href="/legal/terminos" className="underline font-semibold">
                    Términos y Condiciones
                  </Link>{' '}
                  y la{' '}
                  <Link href="/legal/privacidad" className="underline font-semibold">
                    Política de Privacidad
                  </Link>
                  .
                </span>
              </label>

              <Button type="submit" variant="primary" className="w-full" isLoading={loading}>
                Registrarme y Recibir PIN OTP
              </Button>
            </form>
          </>
        ) : (
          <form onSubmit={handleVerificarOtp} className="space-y-6 text-center">
            <div>
              <h2 className="text-xl font-bold text-stone-900">Verificá tu Correo</h2>
              <p className="text-xs text-stone-600 mt-1">
                Ingresá el código de 6 dígitos enviado a <strong>{email}</strong> (expira en 15
                minutos).
              </p>
            </div>

            {errorMsg && (
              <div className="p-3 rounded bg-red-50 border border-red-200 text-xs text-red-700">
                {errorMsg}
              </div>
            )}

            <div className="flex justify-center gap-2.5">
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

            <Button type="submit" variant="editorial" className="w-full" isLoading={loading}>
              Verificar Código OTP
            </Button>
          </form>
        )}
      </div>
    </div>
  );
}