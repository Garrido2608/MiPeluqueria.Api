using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Turnos;


namespace MiPeluqueria.Api.Models.Ventas
{
    public class Venta : AuditableEntity
    {
        [Required]
        public int SesionCajaId { get; set; }
        public int? ClienteId { get; set; }
        public int? TurnoId { get; set; } // Opcional: nulo si solo compró producto al paso
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        public decimal Total { get; set; }
        public int UsuarioId { get; set; }
        public SesionCaja? SesionCaja { get; set; }
        public Cliente? Cliente { get; set; }
        public Turno? Turno { get; set; }
        public ICollection<DetalleVenta> DetallesVenta { get; set; } = new List<DetalleVenta>();

        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}