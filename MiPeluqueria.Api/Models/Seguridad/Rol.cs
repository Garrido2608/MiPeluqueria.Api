using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Seguridad
{
    public class Rol : AuditableEntity
    {
        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty; // Administrador, Recepcionista, Peluquero, Cliente

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}