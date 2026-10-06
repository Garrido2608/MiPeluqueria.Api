using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Ventas
{
    public class CrearVentaDto
    {
        [Required]
        public int SesionCajaId { get; set; }

        public int? ClienteId { get; set; }
        public int? TurnoId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "La venta debe contener al menos un item")]
        public List<DetalleVentaRequestDto> Detalles { get; set; } = new List<DetalleVentaRequestDto>();

        [Required]
        [MinLength(1, ErrorMessage = "Debe registrar al menos un pago")]
        public List<PagoRequestDto> Pagos { get; set; } = new List<PagoRequestDto>();
    }
}