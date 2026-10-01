using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Ventas
{
    public class DetalleLiquidacion : AuditableEntity
    {
        [Required]
        public int LiquidacionId { get; set; }

        public int? TurnoId { get; set; }
        public int? DetalleVentaId { get; set; }

        public decimal MontoComisionCalculado { get; set; }

        public Liquidacion? Liquidacion { get; set; }
    }
}