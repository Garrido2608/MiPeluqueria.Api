using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
namespace MiPeluqueria.Api.Models.Turnos
{
    public class HistorialEstadoTurno : AuditableEntity
    {
        [Required]
        public int TurnoId { get; set; }
        public int? EstadoAnteriorId { get; set; }
        [Required]
        public int EstadoNuevoId { get; set; }
        public DateTime FechaCambio { get; set; } = DateTime.UtcNow;
        public int? UsuarioId { get; set; }
        [StringLength(300)]
        public string? Observaciones { get; set; }
        public Turno? Turno { get; set; }
        public EstadoTurno? EstadoAnterior { get; set; }
        public EstadoTurno? EstadoNuevo { get; set; }
    }
}