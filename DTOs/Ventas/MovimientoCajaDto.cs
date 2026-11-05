using System;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class RegistrarMovimientoCajaDto
    {
        public int SesionCajaId { get; set; }
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string? Tipo { get; set; }
    }

    public class MovimientoCajaResponseDto
    {
        public int Id { get; set; }
        public int SesionCajaId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
    }

    public class AnularVentaDto
    {
        public string Motivo { get; set; } = "Anulación de comprobante en mostrador.";
    }
}