using System;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class GenerarLiquidacionDto
    {
        public int PeluqueroId { get; set; }
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
        public decimal Bonificaciones { get; set; }
        public decimal Descuentos { get; set; }
        public string? Observaciones { get; set; }
    }
}