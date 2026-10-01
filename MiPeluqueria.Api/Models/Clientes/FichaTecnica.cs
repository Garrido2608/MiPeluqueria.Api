using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Turnos;
namespace MiPeluqueria.Api.Models.Clientes
{
    public class FichaTecnica : AuditableEntity
    {
        [Required]
        public int ClienteId { get; set; }
        [Required]
        public int PeluqueroId { get; set; }
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
        [StringLength(1000)]
        public string? DetalleFormulaTintura { get; set; }
        [StringLength(1000)]
        public string? NotasSensibilidadAlergias { get; set; }
        public Cliente? Cliente { get; set; }
        public Peluquero? Peluquero { get; set; }

        public int? TurnoId { get; set; }
        public Turno? Turno { get; set; }
    }
}