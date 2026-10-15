namespace MiPeluqueria.Api.DTOs.Stock
{
    public class CrearProveedorDto
    {
        public string RazonSocial { get; set; } = string.Empty;
        public string? Contacto { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }
}