namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class ComisionConfigDto
    {
        public int Id { get; set; }
        public int PeluqueroId { get; set; }
        public string NombrePeluquero { get; set; } = string.Empty;
        public int? ServicioId { get; set; }
        public string NombreServicio { get; set; } = "Esquema General (Todos los servicios)";
        public decimal PorcentajeServicio { get; set; }
        public decimal PorcentajeVentaProducto { get; set; }
    }
}