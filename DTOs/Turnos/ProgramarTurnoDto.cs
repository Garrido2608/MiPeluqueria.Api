using System;
using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class ReprogramarTurnoDto
    {
        [Required]
        public int PeluqueroId { get; set; }

        [Required]
        public DateTime NuevaFechaHoraInicio { get; set; }

        [StringLength(300)]
        public string? Observaciones { get; set; }
    }
}