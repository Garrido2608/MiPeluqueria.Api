using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Stock
{
    public class ActualizarProveedorDto
    {
        [Required]
        [StringLength(150)]
        public string RazonSocial { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Contacto { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }
    }
}