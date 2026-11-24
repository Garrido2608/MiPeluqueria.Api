import { z } from 'zod';

const PASSWORD_FUERTE_REGEX = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;

export const loginRequestSchema = z.object({
  usernameOrEmail: z
    .string()
    .trim()
    .min(3, 'Ingresá tu nombre de usuario o correo electrónico (mínimo 3 caracteres).')
    .max(100, 'El identificador no puede superar los 100 caracteres.'),
  password: z
    .string()
    .min(6, 'La contraseña debe tener al menos 6 caracteres.'),
});

export const registroUsuarioSchema = z.object({
  username: z
    .string()
    .trim()
    .min(3, 'El nombre de usuario debe tener entre 3 y 50 caracteres.')
    .max(50, 'El nombre de usuario no puede superar los 50 caracteres.')
    .regex(/^[a-zA-Z0-9._-]+$/, 'El usuario solo puede contener letras, números, puntos y guiones.'),
  email: z
    .string()
    .trim()
    .email('Ingresá un correo electrónico con formato válido.')
    .max(120, 'El correo no puede superar los 120 caracteres.'),
  password: z
    .string()
    .min(8, 'La contraseña debe tener al menos 8 caracteres.')
    .max(100, 'La contraseña no puede superar los 100 caracteres.')
    .regex(
      PASSWORD_FUERTE_REGEX,
      'La contraseña debe incluir al menos una mayúscula, una minúscula y un número.'
    ),
  nombre: z
    .string()
    .trim()
    .min(2, 'El nombre debe tener al menos 2 caracteres.')
    .max(80, 'El nombre no puede superar los 80 caracteres.'),
  apellido: z
    .string()
    .trim()
    .min(2, 'El apellido debe tener al menos 2 caracteres.')
    .max(80, 'El apellido no puede superar los 80 caracteres.'),
  telefono: z
    .string()
    .trim()
    .min(6, 'Ingresá un número de teléfono o WhatsApp válido.')
    .max(30, 'El teléfono no puede superar los 30 caracteres.'),
  fechaNacimiento: z
    .string()
    .min(10, 'La fecha de nacimiento es obligatoria.'),
  aceptaTerminos: z
    .boolean()
    .refine((val) => val === true, {
      message: 'Es obligatorio aceptar los Términos de Uso y la Política de Privacidad.',
    }),
});

export const verificarEmailOtpSchema = z.object({
  email: z.string().trim().email('Correo electrónico inválido.'),
  codigoOtp: z
    .string()
    .trim()
    .regex(/^\d{6}$/, 'El código PIN OTP debe contener exactamente 6 dígitos numéricos.'),
});

export const olvidePasswordSchema = z.object({
  email: z.string().trim().email('Ingresá un correo electrónico válido.'),
});

export const restablecerPasswordSchema = z.object({
  email: z.string().trim().email('Correo electrónico inválido.'),
  codigoOtp: z
    .string()
    .trim()
    .regex(/^\d{6}$/, 'El código PIN OTP debe contener exactamente 6 dígitos numéricos.'),
  nuevaPassword: z
    .string()
    .min(8, 'La nueva contraseña debe tener al menos 8 caracteres.')
    .regex(
      PASSWORD_FUERTE_REGEX,
      'La nueva contraseña debe contener al menos una mayúscula, una minúscula y un número.'
    ),
});

export const cambiarPasswordSchema = z
  .object({
    passwordActual: z
      .string()
      .min(6, 'Ingresá tu contraseña actual.'),
    nuevaPassword: z
      .string()
      .min(8, 'La nueva contraseña debe tener al menos 8 caracteres.')
      .regex(
        PASSWORD_FUERTE_REGEX,
        'La nueva contraseña debe contener al menos una mayúscula, una minúscula y un número.'
      ),
  })
  .refine((data) => data.passwordActual !== data.nuevaPassword, {
    message: 'La nueva contraseña debe ser diferente de la contraseña actual.',
    path: ['nuevaPassword'],
  });

export const crearUsuarioStaffSchema = z.object({
  username: z
    .string()
    .trim()
    .min(3, 'El nombre de usuario debe tener al menos 3 caracteres.')
    .max(50, 'El nombre de usuario no puede superar los 50 caracteres.'),
  email: z
    .string()
    .trim()
    .email('Ingresá un correo electrónico válido.'),
  passwordInicial: z
    .string()
    .min(8, 'La contraseña inicial debe tener al menos 8 caracteres.')
    .regex(
      PASSWORD_FUERTE_REGEX,
      'La contraseña inicial debe contener al menos una mayúscula, una minúscula y un número.'
    ),
  rol: z.string().min(2, 'El rol del usuario es obligatorio.'),
  peluqueroId: z.number().int().positive().nullable().optional(),
  sucursalId: z.number().int().positive().nullable().optional(),
});

export type LoginRequestFormValues = z.infer<typeof loginRequestSchema>;
export type RegistroUsuarioFormValues = z.infer<typeof registroUsuarioSchema>;
export type VerificarEmailOtpFormValues = z.infer<typeof verificarEmailOtpSchema>;
export type OlvidePasswordFormValues = z.infer<typeof olvidePasswordSchema>;
export type RestablecerPasswordFormValues = z.infer<typeof restablecerPasswordSchema>;
export type CambiarPasswordFormValues = z.infer<typeof cambiarPasswordSchema>;
export type CrearUsuarioStaffFormValues = z.infer<typeof crearUsuarioStaffSchema>;
