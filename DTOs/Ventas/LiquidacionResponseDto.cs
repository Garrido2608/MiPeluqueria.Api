using System;
using System.Collections.Generic;

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
        public DateTime? FechaPago { get; set; }
        public decimal TotalServicios { get; set; }
        public decimal TotalProductos { get; set; }
        public decimal TotalPagar { get; set; }
        public int CantidadItemsLiquidados { get; set; }
        public string Estado { get; set; } = string.Empty;
        public List<DetalleLiquidacionItemDto> Detalles { get; set; } = new();
    }

    public class DetalleLiquidacionItemDto
    {
        public int Id { get; set; }
        public int DetalleVentaId { get; set; }
        public int? TurnoId { get; set; }
        public string TipoItem { get; set; } = string.Empty;
        public int ItemId { get; set; }
        public decimal SubtotalVenta { get; set; }
        public decimal MontoComisionCalculado { get; set; }
        public DateTime? FechaVenta { get; set; }
    }
}