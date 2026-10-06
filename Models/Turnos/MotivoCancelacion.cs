using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
namespace MiPeluqueria.Api.Models.Turnos
{
    public class MotivoCancelacion : AuditableEntity
    {
        [Required]
        [StringLength(150)]
        public string Descripcion { get; set; } = string.Empty;
        public ICollection<Turno> TurnosCancelados { get; set; } = new
        List<Turno>();
    }
}