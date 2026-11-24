import { MotivoFalloSesionEnum, RolNombreType } from './common.types';

// ==============================================================================
// SEGURIDAD, AUTENTICACIÓN Y USUARIOS (AuthController & UsuariosController)
// ==============================================================================

export interface LoginRequestDto {
  usernameOrEmail: string;
  password: string;
}

export interface RefreshTokenRequestDto {
  token: string;
  refreshToken: string;
}

export interface AuthResponseDto {
  token: string;
  refreshToken: string;
  expiresAt: string;
  usuarioId: number;
  username: string;
  email: string;
  rol: RolNombreType;
  rolId: number;
  tenantId: number;
  sucursalId: number;
  peluqueroId?: number | null;
  clienteId?: number | null;
}

export interface RegistroUsuarioDto {
  username: string;
  email: string;
  password: string;
  rolId?: number; // Fijo en 4 (Cliente) por defecto en Backend
  nombre?: string;
  apellido?: string;
  telefono?: string;
  fechaNacimiento?: string;
  clienteId?: number | null;
  peluqueroId?: number | null;
  aceptaTerminos: boolean;
}

export interface CrearUsuarioStaffDto {
  username: string;
  email: string;
  password: string;
  rolId: number;
  peluqueroId?: number | null;
}

export interface CambiarRolUsuarioDto {
  rolId: number;
  nuevoRolId?: number;
}

export interface VerificarEmailDto {
  email: string;
  codigoVerificacion?: string;
  codigoOtp?: string;
}

export interface CambiarPasswordDto {
  passwordActual: string;
  nuevaPassword: string;
}

export interface OlvidePasswordDto {
  email: string;
}

export interface RestablecerPasswordDto {
  email: string;
  codigoOtp: string;
  nuevaPassword: string;
}

export interface UsuarioDto {
  id: number;
  rolId: number;
  rolNombre: RolNombreType;
  username: string;
  email: string;
  emailVerificado: boolean;
  aceptaTerminos?: boolean;
  fechaAceptacionTerminos?: string | null;
  intentosFallidos: number;
  bloqueadoHasta?: string | null;
  peluqueroId?: number | null;
  clienteId?: number | null;
  activo: boolean;
  createdAt?: string;
}

export type UsuarioResponseDto = UsuarioDto;

export interface ActualizarEstadoUsuarioDto {
  activo?: boolean;
  desbloquearCuenta?: boolean;
  rolId?: number;
}

export interface HistorialSesionDto {
  id: number;
  usuarioId?: number | null;
  username?: string;
  fechaHora?: string;
  fechaIntento: string;
  ipOrigen?: string | null;
  direccionIP: string;
  dispositivo?: string | null;
  exitoso: boolean;
  motivoFallo?: MotivoFalloSesionEnum | string | null;
  motivoFalloTexto?: string;
}

export type HistorialSesionResponseDto = HistorialSesionDto;
