using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Turnos;
using System.ComponentModel.DataAnnotations;
namespace MiPeluqueria.Api.Models.Ventas
{
    public class DetalleVenta : AuditableEntity
    {
        [Required]
        public int VentaId { get; set; }
        [Required]
        public TipoItemVentaEnum TipoItem { get; set; }
        [Required]
        public int ItemId { get; set; } // Id del Servicio o del Producto
        [Required]
        public int Cantidad { get; set; } = 1;
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public int? PeluqueroId { get; set; } // Profesional que ejecutó servicio o vendió el producto
    public Venta? Venta { get; set; }
    }
}