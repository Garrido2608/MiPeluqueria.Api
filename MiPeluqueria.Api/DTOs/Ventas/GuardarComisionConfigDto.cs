namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class GuardarComisionConfigDto
    {
        public int PeluqueroId { get; set; }
        public int? ServicioId { get; set; }
        public decimal PorcentajeServicio { get; set; }
        public decimal PorcentajeVentaProducto { get; set; }
    }
}