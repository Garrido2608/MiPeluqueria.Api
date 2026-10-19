using System;
using System.Collections.Generic;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class DisponibilidadHorarioResponseDto
    {
        public int PeluqueroId { get; set; }
        public string PeluqueroNombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public int DuracionTotalMinutos { get; set; }
        public List<SlotHorarioDisponibleDto> Slots { get; set; } = new();
    }

    public class SlotHorarioDisponibleDto
    {
        public DateTime FechaHoraInicio { get; set; }
        public DateTime FechaHoraFin { get; set; }
        public string HoraInicioTexto { get; set; } = string.Empty; // Ej: "10:30"
        public string HoraFinTexto { get; set; } = string.Empty;    // Ej: "11:15"
        public bool Disponible { get; set; } = true;
    }
}