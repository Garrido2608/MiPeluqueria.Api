import { z } from 'zod';
import { TipoMovimientoStockEnum } from '@/types/api.types';

export const crearProductoSchema = z.object({
  codigoBarras: z
    .string()
    .trim()
    .max(60, 'El código no puede superar los 60 caracteres.')
    .nullable()
    .optional(),
  nombre: z
    .string()
    .trim()
    .min(2, 'El nombre del producto es obligatorio.')
    .max(120, 'El nombre no puede superar los 120 caracteres.'),
  categoriaId: z.number().int().positive('Seleccioná una categoría de producto.'),
  proveedorId: z.number().int().positive().nullable().optional(),
  precioCosto: z.number().min(0, 'El precio de costo no puede ser negativo.'),
  precioVenta: z.number().min(0, 'El precio de venta no puede ser negativo.'),
  stockActual: z.number().int().min(0, 'El stock inicial no puede ser negativo.'),
  stockMinimoAlerta: z.number().int().min(0, 'El stock mínimo no puede ser negativo.'),
  esParaVenta: z.boolean(),
});

export const actualizarProductoSchema = crearProductoSchema;

export const ajusteManualStockSchema = z.object({
  productoId: z.number().int().positive('Seleccioná un producto.'),
  tipoMovimiento: z.nativeEnum(TipoMovimientoStockEnum),
  cantidad: z
    .number()
    .int()
    .refine((val) => val > 0, 'La cantidad del movimiento de stock debe ser mayor a cero.'),
  motivo: z
    .string()
    .trim()
    .min(3, 'Especificá el motivo del ajuste o merma para el Kardex.')
    .max(250, 'El motivo no puede superar los 250 caracteres.'),
});

export const crearProveedorSchema = z.object({
  razonSocial: z
    .string()
    .trim()
    .min(2, 'La razón social del proveedor es obligatoria.')
    .max(120),
  cuit: z.string().trim().max(20).nullable().optional(),
  contacto: z.string().trim().max(100).optional(),
  telefono: z.string().trim().max(30).optional(),
  email: z.string().trim().email('Ingresá un correo electrónico válido.').max(120).optional().or(z.literal('')),
});

export const actualizarProveedorSchema = crearProveedorSchema;

export const crearOrdenCompraItemSchema = z.object({
  productoId: z.number().int().positive('Seleccioná un producto.'),
  cantidadPedida: z.number().int().positive('La cantidad pedida debe ser mayor a cero.'),
  precioUnitarioCosto: z.number().min(0, 'El costo unitario no puede ser negativo.'),
});

export const crearOrdenCompraSchema = z.object({
  proveedorId: z.number().int().positive('Seleccioná un proveedor.'),
  numeroComprobanteProveedor: z.string().trim().max(60).optional(),
  observaciones: z.string().trim().max(500).optional(),
  detalles: z
    .array(crearOrdenCompraItemSchema)
    .min(1, 'La orden de compra debe incluir al menos un ítem.'),
});

export const recibirOrdenCompraSchema = z.object({
  numeroComprobanteProveedor: z.string().trim().max(60).optional(),
  actualizarCostoProductos: z.boolean().optional(),
  imputarEgresoEnCajaAbierta: z.boolean().optional(),
  observacionesRecepcion: z.string().trim().max(500).optional(),
  cantidadesRecibidas: z
    .array(
      z.object({
        detalleOrdenCompraId: z.number().int().positive().optional(),
        productoId: z.number().int().positive(),
        cantidadRecibida: z.number().int().min(0, 'La cantidad recibida no puede ser negativa.'),
        precioUnitarioCostoReal: z.number().min(0).optional(),
      })
    )
    .min(1, 'Debés informar los ítems recibidos.'),
});

export type CrearProductoFormValues = z.infer<typeof crearProductoSchema>;
export type ActualizarProductoFormValues = z.infer<typeof actualizarProductoSchema>;
export type AjusteManualStockFormValues = z.infer<typeof ajusteManualStockSchema>;
export type CrearProveedorFormValues = z.infer<typeof crearProveedorSchema>;
export type ActualizarProveedorFormValues = z.infer<typeof actualizarProveedorSchema>;
export type CrearOrdenCompraFormValues = z.infer<typeof crearOrdenCompraSchema>;
export type RecibirOrdenCompraFormValues = z.infer<typeof recibirOrdenCompraSchema>;
