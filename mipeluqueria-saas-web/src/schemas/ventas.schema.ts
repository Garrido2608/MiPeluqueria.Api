import { z } from 'zod';
import { TipoItemVentaEnum } from '@/types/api.types';

export const abrirCajaSchema = z.object({
  montoInicial: z
    .number()
    .min(0, 'El monto inicial de apertura en efectivo no puede ser negativo.'),
  observaciones: z.string().trim().max(500).optional(),
});

export const cerrarCajaSchema = z.object({
  sesionCajaId: z.number().int().positive().optional(),
  montoFinalReal: z
    .number()
    .min(0, 'El efectivo físico contado en cajón no puede ser negativo.'),
  observacionesCierre: z
    .string()
    .trim()
    .max(500, 'Las observaciones de cierre no pueden superar los 500 caracteres.')
    .optional(),
});

export const crearMovimientoCajaManualSchema = z.object({
  sesionCajaId: z.number().int().positive(),
  tipo: z.enum(['IngresoManual', 'EgresoGasto']).optional(),
  concepto: z
    .string()
    .trim()
    .min(3, 'Especificá el concepto del movimiento de caja (mínimo 3 caracteres).')
    .max(200, 'El concepto no puede superar los 200 caracteres.'),
  monto: z.number().positive('El monto del movimiento debe ser mayor a cero.'),
});

export const crearDetalleVentaItemSchema = z.object({
  tipoItem: z.nativeEnum(TipoItemVentaEnum),
  itemId: z.number().int().positive('Ítem inválido.'),
  nombreItem: z.string().optional(),
  cantidad: z.number().int().positive('La cantidad debe ser al menos 1.'),
  precioUnitario: z.number().min(0, 'El precio unitario no puede ser negativo.'),
  peluqueroId: z.number().int().positive().nullable().optional(),
});

export const crearPagoVentaItemSchema = z.object({
  medioPagoId: z.number().int().positive('Seleccioná un medio de pago.'),
  medioPagoNombre: z.string().optional(),
  importe: z.number().min(0, 'El importe del pago no puede ser negativo.'),
  referenciaOperacion: z.string().trim().max(100).nullable().optional(),
});

export const crearVentaSchema = z.object({
  sesionCajaId: z.number().int().positive('Debés tener una sesión de caja abierta.'),
  clienteId: z.number().int().positive().nullable().optional(),
  turnoId: z.number().int().positive().nullable().optional(),
  idempotencyKey: z.string().uuid().nullable().optional(),
  detalles: z
    .array(crearDetalleVentaItemSchema)
    .min(1, 'El carrito POS debe contener al menos un servicio o producto.'),
  pagos: z
    .array(crearPagoVentaItemSchema)
    .min(1, 'Debés registrar al menos un medio de pago.'),
});

export const generarLiquidacionSchema = z.object({
  peluqueroId: z.number().int().positive('Seleccioná el profesional a liquidar.'),
  fechaDesde: z.string().min(10, 'La fecha desde es obligatoria.'),
  fechaHasta: z.string().min(10, 'La fecha hasta es obligatoria.'),
  fechaVencimiento: z.string().optional(),
  bonificaciones: z.number().min(0, 'Las bonificaciones no pueden ser negativas.').optional(),
  descuentos: z.number().min(0, 'Los descuentos no pueden ser negativos.').optional(),
  observaciones: z.string().trim().max(500).optional(),
});

export type AbrirCajaFormValues = z.infer<typeof abrirCajaSchema>;
export type CerrarCajaFormValues = z.infer<typeof cerrarCajaSchema>;
export type CrearMovimientoCajaManualFormValues = z.infer<typeof crearMovimientoCajaManualSchema>;
export type CrearVentaFormValues = z.infer<typeof crearVentaSchema>;
export type GenerarLiquidacionFormValues = z.infer<typeof generarLiquidacionSchema>;
