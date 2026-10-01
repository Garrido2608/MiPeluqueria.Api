using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Stock
{
    public class Proveedor : AuditableEntity
    {
        [Required(ErrorMessage = "La razón social es obligatoria")]
        [StringLength(150)]
        public string RazonSocial { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Contacto { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}