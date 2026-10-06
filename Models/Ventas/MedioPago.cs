using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;


namespace MiPeluqueria.Api.Models.Ventas
{
    public class MedioPago : AuditableEntity
    {
        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty; // Efectivo, Débito, Crédito,Transferencia/Alias, Mercado Pago QR

        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}