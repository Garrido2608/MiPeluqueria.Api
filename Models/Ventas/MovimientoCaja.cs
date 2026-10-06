using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Ventas
{
    public class MovimientoCaja : AuditableEntity
    {
        [Required]
        public int SesionCajaId { get; set; }

        [Required]
        [StringLength(50)]
        public string Tipo { get; set; } = string.Empty; // 'EgresoInsumoMenor', 'RetiroDuenio', 'PagoDelivery'

        [Required]
        public decimal Monto { get; set; }

        [Required]
        [StringLength(250)]
        public string Concepto { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public SesionCaja? SesionCaja { get; set; }
    }
}