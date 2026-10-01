using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Seguridad
{
    public class HistorialSesion : AuditableEntity
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [StringLength(50)]
        public string DireccionIP { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Dispositivo { get; set; }

        public bool Exitoso { get; set; }

        public MotivoFalloSesionEnum? MotivoFallo { get; set; }

        public DateTime FechaIntento { get; set; } = DateTime.UtcNow;

        public Usuario? Usuario { get; set; }
    }
}