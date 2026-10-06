using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Stock
{
    public class CrearProductoDto
    {
        [Required]
        public int CategoriaId { get; set; }

        public int? ProveedorId { get; set; }

        [StringLength(50)]
        public string? CodigoBarras { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Range(0, 9999999.99)]
        public decimal PrecioCosto { get; set; }

        [Range(0.01, 9999999.99)]
        public decimal PrecioVenta { get; set; }

        [Range(0, 10000)]
        public int StockInicial { get; set; } = 0;

        public int StockMinimoAlerta { get; set; } = 5;

        public bool EsParaVenta { get; set; } = true;
    }
}