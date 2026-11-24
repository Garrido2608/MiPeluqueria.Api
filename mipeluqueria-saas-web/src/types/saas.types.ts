import { EstadoSuscripcionEnum } from './common.types';

// ==============================================================================
// SAAS MULTI-TENANT, SUCURSALES, SUSCRIPCIONES Y AUDITORÍA FORENSE
// ==============================================================================

export interface SucursalResponseDto {
  id: number;
  tenantId: number;
  nombre: string;
  direccion: string;
  telefono?: string | null;
  timeZoneId: string;
  monedaIso: string;
  esCasaMatriz: boolean;
  activa: boolean;
}

export interface CrearSucursalDto {
  nombre: string;
  direccion: string;
  telefono?: string;
  timeZoneId: string;
  monedaIso: string;
  esCasaMatriz: boolean;
}

export interface PlanSuscripcionDto {
  id: number;
  nombre: string;
  precioMensualUsd: number;
  maxSucursales: number;
  maxPeluqueros: number;
  incluyeWhatsAppRecordatorios: boolean;
  incluyePortalReservasOnline: boolean;
  activo: boolean;
}

export type PlanSuscripcionResponseDto = PlanSuscripcionDto;

export interface SuscripcionEstadoResponseDto {
  tenantId: number;
  nombreComercial: string;
  planActual: string;
  precioMensualUsd: number;
  estado: EstadoSuscripcionEnum;
  estadoNombre: string;
  fechaProximoVencimiento: string;
  maxSucursalesPermitidas: number;
  maxPeluquerosPermitidos: number;
}

export interface CambiarPlanSuscripcionDto {
  nuevoPlanSuscripcionId: number;
}

export interface TenantDto {
  id: number;
  nombreComercial: string;
  subdominioSlug: string;
  identificacionFiscal?: string | null;
  emailPropietario: string;
  paisIso: string;
  activo: boolean;
  sucursales?: SucursalResponseDto[];
}

export interface AuditLogDto {
  id: number;
  usuarioId?: number | null;
  entidadNombre: string;
  accion: 'CREATE' | 'UPDATE' | 'DELETE';
  clavePrimaria: string;
  valoresAnterioresJson?: string | null;
  valoresNuevosJson?: string | null;
  ipAddress?: string | null;
  fechaUtc: string;
}
