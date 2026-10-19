using System;
using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.Models.Common
{
    public class AuditLog : AuditableEntity
    {
        public int? UsuarioId { get; set; }

        [Required]
        [StringLength(100)]
        public string EntidadNombre { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Accion { get; set; } = string.Empty; // "CREATE", "UPDATE", "DELETE"

        [Required]
        [StringLength(50)]
        public string ClavePrimaria { get; set; } = string.Empty;

        public string? ValoresAnterioresJson { get; set; }
        public string? ValoresNuevosJson { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    }
}