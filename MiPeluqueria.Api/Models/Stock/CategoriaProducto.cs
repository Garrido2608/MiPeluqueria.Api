using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Stock
{
    public class CategoriaProducto : AuditableTenantEntity
    {
        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty; // Ceras, Shampoos, Tinturas, Cuidado de barba

        [StringLength(250)]
        public string? Descripcion { get; set; }

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}