using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Stock
{
    public class OrdenCompra : AuditableEntity
    {
        [Required]
        public int ProveedorId { get; set; }

        [StringLength(50)]
        public string? NumeroComprobanteProveedor { get; set; }

        public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
        public DateTime? FechaRecepcion { get; set; }

        [Required]
        public EstadoOrdenCompraEnum Estado { get; set; } = EstadoOrdenCompraEnum.Emitida;

        public decimal Total { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        public int UsuarioCreacionId { get; set; }
        public int? UsuarioRecepcionId { get; set; }

        public Proveedor? Proveedor { get; set; }
        public ICollection<DetalleOrdenCompra> Detalles { get; set; } = new List<DetalleOrdenCompra>();
    }

    public class DetalleOrdenCompra : AuditableEntity
    {
        [Required]
        public int OrdenCompraId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public int CantidadPedida { get; set; }

        public int CantidadRecibida { get; set; }

        [Required]
        public decimal PrecioUnitarioCosto { get; set; }

        public decimal Subtotal { get; set; }

        public OrdenCompra? OrdenCompra { get; set; }
        public Producto? Producto { get; set; }
    }
}