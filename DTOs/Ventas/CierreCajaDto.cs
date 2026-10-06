using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class CierreCajaDto
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [Range(0, 9999999.99, ErrorMessage = "El dinero contado debe ser positivo")]
        public decimal MontoFinalReal { get; set; } // Lo que contó físicamente en el cajón
    }
}