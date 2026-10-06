namespace MiPeluqueria.Api.DTOs.Stock
{
    public class ProductoDto
    {
        public int Id { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public int? ProveedorId { get; set; }
        public string? CodigoBarras { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioCosto { get; set; }
        public decimal PrecioVenta { get; set; }
        public int StockActual { get; set; }
        public int StockMinimoAlerta { get; set; }
        public bool EsParaVenta { get; set; }
    }
}