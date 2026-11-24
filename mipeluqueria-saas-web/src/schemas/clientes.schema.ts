import { z } from 'zod';

export const crearClienteSchema = z.object({
  nombre: z
    .string()
    .trim()
    .min(2, 'El nombre del cliente debe tener al menos 2 caracteres.')
    .max(80, 'El nombre no puede superar los 80 caracteres.'),
  apellido: z
    .string()
    .trim()
    .min(2, 'El apellido del cliente debe tener al menos 2 caracteres.')
    .max(80, 'El apellido no puede superar los 80 caracteres.'),
  telefono: z
    .string()
    .trim()
    .min(6, 'El teléfono o WhatsApp del cliente es obligatorio.')
    .max(30, 'El teléfono no puede superar los 30 caracteres.'),
  email: z
    .string()
    .trim()
    .email('El correo electrónico no tiene un formato válido.')
    .max(120)
    .optional()
    .or(z.literal('')),
  fechaNacimiento: z
    .string()
    .min(10, 'La fecha de nacimiento es obligatoria.'),
  tipoCabelloId: z.number().int().positive().nullable().optional(),
  tipoRostroId: z.number().int().positive().nullable().optional(),
});

export const actualizarClienteSchema = crearClienteSchema.extend({
  activo: z.boolean(),
});

export const crearFichaTecnicaSchema = z.object({
  clienteId: z.number().int().positive('El cliente es obligatorio.'),
  peluqueroId: z.number().int().positive('El profesional estilista es obligatorio.'),
  turnoId: z.number().int().positive().nullable().optional(),
  formulaAplicada: z
    .string()
    .trim()
    .min(3, 'Especificá la fórmula técnica o trabajo capilar aplicado.')
    .max(1000, 'La fórmula aplicada no puede superar los 1000 caracteres.'),
  observaciones: z
    .string()
    .trim()
    .max(1000, 'Las observaciones no pueden superar los 1000 caracteres.')
    .optional()
    .default(''),
  notasSensibilidadAlergias: z
    .string()
    .trim()
    .max(500, 'Las notas de sensibilidad/alergias no pueden superar los 500 caracteres.')
    .optional(),
  productosUtilizados: z
    .string()
    .trim()
    .max(500, 'El detalle de productos utilizados no puede superar los 500 caracteres.')
    .optional()
    .default(''),
  fotoResultadoUrl: z
    .string()
    .trim()
    .url('La URL de la fotografía debe ser válida.')
    .max(500)
    .nullable()
    .optional()
    .or(z.literal('')),
});

export const actualizarFichaTecnicaSchema = z.object({
  peluqueroId: z.number().int().positive('El profesional estilista es obligatorio.'),
  formulaAplicada: z
    .string()
    .trim()
    .min(3, 'Especificá la fórmula técnica o trabajo capilar aplicado.')
    .max(1000, 'La fórmula aplicada no puede superar los 1000 caracteres.'),
  observaciones: z.string().trim().max(1000).default(''),
  notasSensibilidadAlergias: z.string().trim().max(500).optional(),
  productosUtilizados: z.string().trim().max(500).default(''),
  fotoResultadoUrl: z
    .string()
    .trim()
    .max(500)
    .nullable()
    .optional(),
});

export type CrearClienteFormValues = z.infer<typeof crearClienteSchema>;
export type ActualizarClienteFormValues = z.infer<typeof actualizarClienteSchema>;
export type CrearFichaTecnicaFormValues = z.infer<typeof crearFichaTecnicaSchema>;
export type ActualizarFichaTecnicaFormValues = z.infer<typeof actualizarFichaTecnicaSchema>;
