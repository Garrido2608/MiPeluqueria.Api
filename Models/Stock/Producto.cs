using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Stock
{
    public class Producto : AuditableEntity
    {
        [Required]
        public int CategoriaId { get; set; }

        public int? ProveedorId { get; set; }

        [StringLength(50)]
        public string? CodigoBarras { get; set; }

        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        public decimal PrecioCosto { get; set; }
        public decimal PrecioVenta { get; set; }
        public int StockActual { get; set; }
        public int StockMinimoAlerta { get; set; } = 5;

        public bool EsParaVenta { get; set; } = true; // true = Reventa mostrador, false = Insumo de uso interno (tinturas, oxidantes)

        public CategoriaProducto? Categoria { get; set; }
        public Proveedor? Proveedor { get; set; }
        public ICollection<MovimientoStock> MovimientosStock { get; set; } = new List<MovimientoStock>();
    }
}