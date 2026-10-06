using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class CambiarEstadoTurnoDto
    {
        [Required(ErrorMessage = "El nuevo estado es obligatorio")]
        public int NuevoEstadoId { get; set; }

        public int? MotivoCancelacionId { get; set; } // Obligatorio si el estado es Cancelado

        [StringLength(300)]
        public string? Observaciones { get; set; }
    }
}