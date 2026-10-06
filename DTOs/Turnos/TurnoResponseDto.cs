using System;
using System.Collections.Generic;


namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class TurnoResponseDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public int PeluqueroId { get; set; }
        public string PeluqueroNombre { get; set; } = string.Empty;
        public DateTime FechaHoraInicio { get; set; }
        public DateTime FechaHoraFin { get; set; }
        public int EstadoId { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public decimal MontoTotalEstimado { get; set; }
        public string? Observaciones { get; set; }
        public List<string> Servicios { get; set; } = new List<string>();
    }
}