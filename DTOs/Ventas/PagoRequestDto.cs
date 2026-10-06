using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class PagoRequestDto
    {
        [Required]
        public int MedioPagoId { get; set; }

        [Required]
        [Range(0.01, 9999999.99)]
        public decimal Importe { get; set; }
    }
}