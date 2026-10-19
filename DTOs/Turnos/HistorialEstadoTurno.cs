using System;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class HistorialEstadoTurnoResponseDto
    {
        public int Id { get; set; }
        public int TurnoId { get; set; }
        public int? EstadoAnteriorId { get; set; }
        public string? EstadoAnteriorNombre { get; set; }
        public int EstadoNuevoId { get; set; }
        public string EstadoNuevoNombre { get; set; } = string.Empty;
        public DateTime FechaCambio { get; set; }
        public int? UsuarioId { get; set; }
        public string? Observaciones { get; set; }
    }
}