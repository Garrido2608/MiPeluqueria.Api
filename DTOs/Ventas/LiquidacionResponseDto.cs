using System;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class LiquidacionResponseDto
    {
        public int Id { get; set; }
        public int PeluqueroId { get; set; }
        public string PeluqueroNombre { get; set; } = string.Empty;
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public decimal TotalServicios { get; set; }
        public decimal TotalProductos { get; set; }
        public decimal TotalPagar { get; set; }
        public int CantidadItemsLiquidados { get; set; }
        public string Estado { get; set; } = string.Empty;
    }
}