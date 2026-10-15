namespace MiPeluqueria.Api.DTOs.Stock
{
    public class ProveedorDto
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string? Contacto { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public int CantidadProductosAsociados { get; set; }
    }
}