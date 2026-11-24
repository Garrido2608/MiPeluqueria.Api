import { z } from 'zod';
import { EstadoTurnoEnum } from '@/types/api.types';

export const crearTurnoSchema = z.object({
  clienteId: z.number().int().positive('Debés seleccionar un cliente válido.'),
  peluqueroId: z.number().int().positive('Debés seleccionar un profesional estilista.'),
  fechaHoraInicio: z.string().min(10, 'La fecha y hora de inicio son obligatorias.'),
  observaciones: z.string().trim().max(500).optional(),
  serviciosIds: z
    .array(z.number().int().positive())
    .min(1, 'Debés seleccionar al menos un servicio para el turno.'),
});

export const reprogramarTurnoSchema = z.object({
  peluqueroId: z.number().int().positive('Debés seleccionar un profesional estilista.'),
  nuevaFechaHoraInicio: z.string().min(10, 'La nueva fecha y hora de inicio son obligatorias.'),
  observaciones: z.string().trim().max(500).optional(),
});

export const cambiarEstadoTurnoSchema = z
  .object({
    nuevoEstadoId: z.nativeEnum(EstadoTurnoEnum),
    motivoCancelacionId: z.number().int().positive().nullable().optional(),
    observaciones: z.string().trim().max(500).optional(),
  })
  .refine(
    (data) =>
      data.nuevoEstadoId !== EstadoTurnoEnum.Cancelado ||
      (typeof data.motivoCancelacionId === 'number' && data.motivoCancelacionId > 0),
    {
      message: 'Para cancelar un turno es obligatorio seleccionar un motivo de cancelación.',
      path: ['motivoCancelacionId'],
    }
  );

export const crearBloqueoAgendaSchema = z
  .object({
    peluqueroId: z.number().int().positive('Debés seleccionar el profesional.'),
    fechaInicio: z.string().min(10, 'La fecha y hora de inicio son obligatorias.'),
    fechaFin: z.string().min(10, 'La fecha y hora de fin son obligatorias.'),
    motivo: z
      .string()
      .trim()
      .min(3, 'Especificá el motivo del bloqueo de agenda (mínimo 3 caracteres).')
      .max(250, 'El motivo no puede superar los 250 caracteres.'),
  })
  .refine(
    (data) => new Date(data.fechaFin).getTime() > new Date(data.fechaInicio).getTime(),
    {
      message: 'La fecha/hora de fin debe ser posterior a la fecha/hora de inicio.',
      path: ['fechaFin'],
    }
  );

export const reservaPublicaRequestSchema = z.object({
  nombreCliente: z.string().trim().min(2, 'Ingresá tu nombre.').max(80),
  apellidoCliente: z.string().trim().min(2, 'Ingresá tu apellido.').max(80),
  telefonoCliente: z.string().trim().min(6, 'Ingresá tu WhatsApp o teléfono.').max(30),
  emailCliente: z.string().trim().email('Correo electrónico inválido.').optional().or(z.literal('')),
  peluqueroId: z.number().int().positive('Seleccioná un profesional.'),
  fechaHoraInicio: z.string().min(10, 'Seleccioná fecha y horario.'),
  serviciosIds: z
    .array(z.number().int().positive())
    .min(1, 'Elegí al menos un servicio para reservar.'),
  observaciones: z.string().trim().max(500).optional(),
  pasarelaPreferida: z.enum(['MercadoPago', 'Stripe']).optional(),
});

export type CrearTurnoFormValues = z.infer<typeof crearTurnoSchema>;
export type ReprogramarTurnoFormValues = z.infer<typeof reprogramarTurnoSchema>;
export type CambiarEstadoTurnoFormValues = z.infer<typeof cambiarEstadoTurnoSchema>;
export type CrearBloqueoAgendaFormValues = z.infer<typeof crearBloqueoAgendaSchema>;
export type ReservaPublicaFormValues = z.infer<typeof reservaPublicaRequestSchema>;
