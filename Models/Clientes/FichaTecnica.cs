using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Models.Clientes
{
    public class FichaTecnica : AuditableTenantEntity
    {
        [Required]
        public int ClienteId { get; set; }

        [Required]
        public int PeluqueroId { get; set; }

        public int? TurnoId { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(1000)]
        public string FormulaAplicada { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Observaciones { get; set; }

        [StringLength(1000)]
        public string? NotasSensibilidadAlergias { get; set; }

        [StringLength(500)]
        public string? ProductosUtilizados { get; set; }

        [StringLength(500)]
        public string? FotoResultadoUrl { get; set; }

        public Cliente? Cliente { get; set; }
        public Peluquero? Peluquero { get; set; }
        public Turno? Turno { get; set; }
    }
}
