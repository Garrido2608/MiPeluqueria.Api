using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Turnos;



namespace MiPeluqueria.Api.Models.Seguridad
{
    public class Usuario : AuditableEntity
    {
        [Required]
        public int RolId { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        // Verificación de Email
        public bool EmailVerificado { get; set; } = false;

        [StringLength(6)]
        public string? CodigoVerificacion { get; set; }
        public DateTime? FechaExpiracionCodigo { get; set; }

        // Legales y Términos de Uso
        public bool AceptaTerminos { get; set; } = false;
        public DateTime? FechaAceptacionTerminos { get; set; }

        // Prevención de ataques de Fuerza Bruta (Anti-hackeo)
        public int IntentosFallidos { get; set; } = 0;
        public DateTime? BloqueadoHasta { get; set; }

        public int? PeluqueroId { get; set; } // Vinculado si es cuenta de profesional
        public int? ClienteId { get; set; } // Vinculado si es cuenta de cliente

        public bool Activo { get; set; } = true;

        public Rol? Rol { get; set; }
        public Peluquero? Peluquero { get; set; }
        public Cliente? Cliente { get; set; }
    }
}