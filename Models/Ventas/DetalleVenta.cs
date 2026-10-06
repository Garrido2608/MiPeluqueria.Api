using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
namespace MiPeluqueria.Api.Models.Ventas
{
    public class DetalleVenta : AuditableEntity
    {
        [Required]
        public int VentaId { get; set; }
        [Required]
        [StringLength(20)]
        public TipoItemVentaEnum TipoItem { get; set; }
        [Required]
        public int ItemId { get; set; } // Id del Servicio o del Producto
        [Required]
        public int Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public int? PeluqueroId { get; set; } // Profesional que ejecutó el servicio (paraliquidar comisión)

        public Venta? Venta { get; set; }
    }
}