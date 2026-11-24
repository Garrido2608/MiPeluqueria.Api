using System;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.DTOs.Stock
{
    public class KardexItemResponseDto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public TipoMovimientoStockEnum TipoMovimiento { get; set; }
        public string TipoMovimientoDescripcion { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public int SaldoResultante { get; set; }
        public DateTime Fecha { get; set; }
        public int? UsuarioId { get; set; }
        public string? Motivo { get; set; }
    }
}