using MiPeluqueria.Api.Models.Ventas;
using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class DetalleVentaRequestDto
    {
        [Required]
        public TipoItemVentaEnum TipoItem { get; set; } // Normalizado con Enum

        [Required]
        public int ItemId { get; set; }

        [Required]
        [Range(1, 100)]
        public int Cantidad { get; set; } = 1;

        public int? PeluqueroId { get; set; }
    }
}