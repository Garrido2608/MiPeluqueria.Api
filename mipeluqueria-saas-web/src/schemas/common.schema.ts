import { z } from 'zod';

export const crearItemCatalogoSchema = z.object({
  nombre: z
    .string()
    .trim()
    .min(2, 'El nombre del ítem debe tener al menos 2 caracteres.')
    .max(100, 'El nombre no puede superar los 100 caracteres.'),
  descripcion: z.string().trim().max(250).optional(),
  esEfectivoFisico: z.boolean().optional(),
});

export const actualizarItemCatalogoSchema = crearItemCatalogoSchema.extend({
  activo: z.boolean(),
});

export const itemRecetaServicioSchema = z.object({
  productoId: z.number().int().positive('Seleccioná un insumo interno.'),
  productoNombre: z.string().optional(),
  cantidadConsumo: z.number().positive('La cantidad de consumo debe ser mayor a cero.'),
  unidadMedida: z.string().trim().min(1, 'Indicá la unidad (gr, ml, un).').max(20),
});

export const crearServicioSchema = z.object({
  nombre: z.string().trim().min(2, 'El nombre del servicio es obligatorio.').max(120),
  descripcion: z.string().trim().max(500).optional(),
  precio: z.number().min(0, 'El precio del servicio no puede ser negativo.'),
  duracionMinutos: z
    .number()
    .int()
    .min(5, 'La duración mínima de un servicio es de 5 minutos.')
    .max(480, 'La duración máxima es de 480 minutos.'),
  categoriaId: z.number().int().positive('Seleccioná una categoría de servicio.'),
  recetaInsumos: z.array(itemRecetaServicioSchema).optional(),
});

export const actualizarServicioSchema = crearServicioSchema.extend({
  activo: z.boolean(),
});

export const crearPeluqueroSchema = z.object({
  nombre: z.string().trim().min(2, 'El nombre es obligatorio.').max(80),
  apellido: z.string().trim().min(2, 'El apellido es obligatorio.').max(80),
  dni: z.string().trim().min(6, 'El DNI es obligatorio.').max(20),
  telefono: z.string().trim().min(6, 'El teléfono es obligatorio.').max(30),
  email: z.string().trim().email('Ingresá un correo electrónico válido.').max(120),
  horaInicioJornada: z.string().min(4, 'Hora de inicio obligatoria.'),
  horaFinJornada: z.string().min(4, 'Hora de fin obligatoria.'),
  colorAgendaHex: z
    .string()
    .regex(/^#[0-9A-Fa-f]{6}$/, 'El color de agenda debe estar en formato hexadecimal (#RRGGBB).'),
  porcentajeComisionServicios: z.number().min(0).max(100),
  porcentajeComisionProductos: z.number().min(0).max(100),
  sueldoBaseFijo: z.number().min(0),
  especialidadesIds: z.array(z.number().int().positive()),
});

export const actualizarPeluqueroSchema = crearPeluqueroSchema.extend({
  activo: z.boolean(),
});

export type CrearItemCatalogoFormValues = z.infer<typeof crearItemCatalogoSchema>;
export type ActualizarItemCatalogoFormValues = z.infer<typeof actualizarItemCatalogoSchema>;
export type CrearServicioFormValues = z.infer<typeof crearServicioSchema>;
export type ActualizarServicioFormValues = z.infer<typeof actualizarServicioSchema>;
export type CrearPeluqueroFormValues = z.infer<typeof crearPeluqueroSchema>;
export type ActualizarPeluqueroFormValues = z.infer<typeof actualizarPeluqueroSchema>;
