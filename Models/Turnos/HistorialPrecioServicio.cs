using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Turnos
{
    public class HistorialPrecioServicio : AuditableEntity
    {
        [Required]
        public int ServicioId { get; set; }

        public decimal PrecioAnterior { get; set; }
        public decimal PrecioNuevo { get; set; }

        public DateTime FechaCambio { get; set; } = DateTime.UtcNow;
        public int? UsuarioModificacionId { get; set; }

        [StringLength(250)]
        public string? Motivo { get; set; }

        public Servicio? Servicio { get; set; }
    }
}