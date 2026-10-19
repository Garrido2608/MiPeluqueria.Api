using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Stock
{
    public class HistorialPrecioProducto : AuditableEntity
    {
        [Required]
        public int ProductoId { get; set; }

        public decimal PrecioCostoAnterior { get; set; }
        public decimal PrecioCostoNuevo { get; set; }

        public decimal PrecioVentaAnterior { get; set; }
        public decimal PrecioVentaNuevo { get; set; }

        public DateTime FechaCambio { get; set; } = DateTime.UtcNow;
        public int? UsuarioModificacionId { get; set; }

        [StringLength(250)]
        public string? Motivo { get; set; }

        public Producto? Producto { get; set; }
    }
}