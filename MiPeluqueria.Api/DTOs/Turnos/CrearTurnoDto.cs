using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class CrearTurnoDto
    {
        [Required(ErrorMessage = "El cliente es obligatorio")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "El peluquero es obligatorio")]
        public int PeluqueroId { get; set; }

        [Required(ErrorMessage = "La fecha y hora del turno es obligatoria")]
        public DateTime FechaHoraInicio { get; set; }

        [Required(ErrorMessage = "Debe seleccionar al menos un servicio")]
        [MinLength(1, ErrorMessage = "Debe incluir al menos un servicio")]
        public List<int> ServiciosIds { get; set; } = new List<int>();

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}