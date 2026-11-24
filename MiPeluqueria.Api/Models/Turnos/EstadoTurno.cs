using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;


namespace MiPeluqueria.Api.Models.Turnos
{
    public class EstadoTurno : AuditableEntity
    {
        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;
        public ICollection<Turno> Turnos { get; set; } = new List<Turno>();
    }
}