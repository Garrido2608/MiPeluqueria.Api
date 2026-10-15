using System;
using System.Collections.Generic;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class VentaResponseDto
    {
        public int Id { get; set; }
        public int SesionCajaId { get; set; }
        public int? ClienteId { get; set; }
        public string ClienteNombre { get; set; } = "Consumidor Final";
        public int? TurnoId { get; set; }
        public int UsuarioId { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public List<VentaDetalleItemResponseDto> Detalles { get; set; } = new();
        public List<VentaPagoItemResponseDto> Pagos { get; set; } = new();
    }

    public class VentaDetalleItemResponseDto
    {
        public int Id { get; set; }
        public string TipoItem { get; set; } = string.Empty;
        public int ItemId { get; set; }
        public string ConceptoNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public int? PeluqueroId { get; set; }
        public string? PeluqueroNombre { get; set; }
    }

    public class VentaPagoItemResponseDto
    {
        public int Id { get; set; }
        public int MedioPagoId { get; set; }
        public string MedioPagoNombre { get; set; } = string.Empty;
        public decimal Importe { get; set; }
    }
}