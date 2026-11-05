using System;
using System.Collections.Generic;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class SesionCajaResponseDto
    {
        public int Id { get; set; }
        public int UsuarioAperturaId { get; set; }
        public string? UsuarioAperturaNombre { get; set; }
        public DateTime FechaApertura { get; set; }
        public decimal MontoInicial { get; set; }
        public int? UsuarioCierreId { get; set; }
        public string? UsuarioCierreNombre { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal TotalVentasEfectivo { get; set; }
        public decimal TotalVentasDigital { get; set; }
        public decimal TotalIngresosManuales { get; set; }
        public decimal TotalEgresosManuales { get; set; }
        public decimal MontoFinalEsperado { get; set; }
        public decimal? MontoFinalReal { get; set; }
        public decimal? Diferencia { get; set; }
        public string? ObservacionesCierre { get; set; }
        public bool EstadoAbierta { get; set; }
        public List<MovimientoCajaResponseDto> Movimientos { get; set; } = new();
    }
}
