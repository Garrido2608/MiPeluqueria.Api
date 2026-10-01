using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class AperturaCajaDto
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [Range(0, 9999999.99, ErrorMessage = "El fondo inicial debe ser positivo")]
        public decimal MontoInicial { get; set; }
    }
}