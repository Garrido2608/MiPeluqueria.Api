using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Stock
{
    public class MovimientoStock : AuditableEntity
    {
        [Required]
        public int ProductoId { get; set; }

        public TipoMovimientoStockEnum TipoMovimiento { get; set; }

        [Required]
        public int Cantidad { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public int? UsuarioId { get; set; }

        [StringLength(300)]
        public string? Motivo { get; set; }

        public Producto? Producto { get; set; }
    }
}