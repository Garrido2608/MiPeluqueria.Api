// ==============================================================================
// CLIENTES Y FICHAS TÉCNICAS CAPILARES (ClientesController & FichasTecnicasController)
// ==============================================================================

export interface ClienteDto {
  id: number;
  nombre: string;
  apellido: string;
  nombreCompleto?: string;
  telefono: string;
  email?: string | null;
  fechaNacimiento: string;
  fechaRegistro: string;
  activo: boolean;
  tipoCabelloId?: number | null;
  tipoCabelloNombre?: string | null;
  tipoRostroId?: number | null;
  tipoRostroNombre?: string | null;
  esCumpleaneroDelMes?: boolean;
}

export type ClienteResponseDto = ClienteDto;

export interface ClienteRequestDto {
  nombre: string;
  apellido: string;
  telefono: string;
  email?: string | null;
  fechaNacimiento: string;
  tipoCabelloId?: number | null;
  tipoRostroId?: number | null;
}

export interface FichaTecnicaDto {
  id: number;
  clienteId: number;
  nombreCliente?: string;
  clienteNombreCompleto?: string;
  peluqueroId: number;
  nombrePeluquero?: string;
  peluqueroNombreCompleto?: string;
  turnoId?: number | null;
  fechaRegistro?: string;
  fecha: string;
  formulaAplicada: string;
  detalleFormulaTintura?: string | null;
  observaciones?: string | null;
  notasSensibilidadAlergias?: string | null;
  productosUtilizados?: string | null;
  fotoResultadoUrl?: string | null;
}

export type FichaTecnicaResponseDto = FichaTecnicaDto;

export interface CrearFichaTecnicaDto {
  clienteId: number;
  peluqueroId: number;
  turnoId?: number | null;
  formulaAplicada?: string;
  detalleFormulaTintura?: string;
  observaciones?: string;
  notasSensibilidadAlergias?: string;
  productosUtilizados?: string;
  fotoResultadoUrl?: string;
}

export interface ActualizarFichaTecnicaDto {
  peluqueroId: number;
  turnoId?: number | null;
  formulaAplicada?: string;
  detalleFormulaTintura?: string;
  observaciones?: string;
  notasSensibilidadAlergias?: string;
  productosUtilizados?: string;
  fotoResultadoUrl?: string;
}
