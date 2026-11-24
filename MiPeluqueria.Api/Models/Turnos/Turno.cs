using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Common;
namespace MiPeluqueria.Api.Models.Turnos
{
    public class Turno : AuditableTenantEntity
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
        public bool RequiereSena { get; set; } = false;
        public decimal MontoSena { get; set; } = 0m;
        public bool SenaPagada { get; set; } = false;
        public DateTime? FechaPagoSena { get; set; }
        [StringLength(150)]
        public string? ReferenciaPagoExterno { get; set; }
        [StringLength(500)]
        public string? Observaciones { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public Cliente? Cliente { get; set; }
        public Peluquero? Peluquero { get; set; }
        public EstadoTurno? Estado { get; set; }
        public MotivoCancelacion? MotivoCancelacion { get; set; }
        public ICollection<TurnoServicio> TurnoServicios { get; set; } = new List<TurnoServicio>();

        public ICollection<HistorialEstadoTurno> HistorialEstados { get; set; } = new List<HistorialEstadoTurno>();

    }
}