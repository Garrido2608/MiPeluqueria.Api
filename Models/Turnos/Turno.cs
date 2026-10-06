using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Common;
namespace MiPeluqueria.Api.Models.Turnos
{
    public class Turno : AuditableEntity
    {
        [Required]
        public int ClienteId { get; set; }
        [Required]
        public int PeluqueroId { get; set; }
        [Required]
        public DateTime FechaHoraInicio { get; set; }
        [Required]
        public DateTime FechaHoraFin { get; set; }
        [Required]
        public int EstadoId { get; set; } = (int)EstadoTurnoEnum.Pendiente;
        public int? MotivoCancelacionId { get; set; }
        [Required]
        public decimal MontoTotalEstimado { get; set; }
        [StringLength(500)]
        public string? Observaciones { get; set; }
        public Cliente? Cliente { get; set; }
        public Peluquero? Peluquero { get; set; }
        public EstadoTurno? Estado { get; set; }
        public MotivoCancelacion? MotivoCancelacion { get; set; }
        public ICollection<TurnoServicio> TurnoServicios { get; set; } = new List<TurnoServicio>();

        public ICollection<HistorialEstadoTurno> HistorialEstados { get; set; } = new List<HistorialEstadoTurno>();

    }
}