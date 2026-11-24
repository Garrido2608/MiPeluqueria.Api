using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;


namespace MiPeluqueria.Api.Models.Ventas
{
    public class Pago : AuditableTenantEntity
    {
        [Required]
        public int VentaId { get; set; }
        [Required]
        public int MedioPagoId { get; set; }
        [Required]
        public decimal Importe { get; set; }
        [StringLength(100)]
        public string? ReferenciaOperacion { get; set; }
        public Venta? Venta { get; set; }
        public MedioPago? MedioPago { get; set; }
    }
}