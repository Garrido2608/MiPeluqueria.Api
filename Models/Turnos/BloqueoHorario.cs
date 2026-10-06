using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Turnos
{
    public class BloqueoHorario : AuditableEntity
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

        public Peluquero? Peluquero { get; set; }
    }
}