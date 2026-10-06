using System;
using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class BloqueoHorarioDto
    {
        [Required]
        public int PeluqueroId { get; set; }

        [Required]
        public DateTime FechaInicio { get; set; }

        [Required]
        public DateTime FechaFin { get; set; }

        [Required]
        [StringLength(200)]
        public string Motivo { get; set; } = string.Empty;
    }
}