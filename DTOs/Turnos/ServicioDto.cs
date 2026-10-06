namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class ServicioDto
    {
        public int Id { get; set; }
        public int CategoriaId { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal Precio { get; set; }
        public int DuracionMinutos { get; set; }
        public bool Activo { get; set; }
    }
}