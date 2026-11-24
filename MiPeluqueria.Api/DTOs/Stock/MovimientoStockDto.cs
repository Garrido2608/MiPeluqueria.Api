using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Stock; // Agregamos el using del Enum

namespace MiPeluqueria.Api.DTOs.Stock
{
    public class MovimientoStockDto
    {
        [Required]
        public int ProductoId { get; set; }

        [Required]
        public TipoMovimientoStockEnum TipoMovimiento { get; set; } // Normalizado

        [Required]
        [Range(1, 1000)]
        public int Cantidad { get; set; }

        [StringLength(300)]
        public string? Motivo { get; set; }
    }
}