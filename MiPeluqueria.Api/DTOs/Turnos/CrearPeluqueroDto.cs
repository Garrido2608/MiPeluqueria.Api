using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class CrearPeluqueroDto
    {
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(7)]
        public string ColorAgendaHex { get; set; } = "#3B82F6";

        public List<int> EspecialidadesIds { get; set; } = new List<int>();
    }
}