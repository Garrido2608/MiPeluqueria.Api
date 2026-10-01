using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Clientes
{
    public class TipoRostro : AuditableEntity
    {
        [Required(ErrorMessage = "El tipo de rostro es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty; 

        [StringLength(250)]
        public string? Descripcion { get; set; }

        public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
    }
}