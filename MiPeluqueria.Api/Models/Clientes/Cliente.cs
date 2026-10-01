using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Turnos; 

namespace MiPeluqueria.Api.Models.Clientes
{
    public class Cliente : AuditableEntity
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(30)]
        public string Telefono { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "El formato de correo no es válido")]
        [StringLength(150)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria para el programa de fidelización y beneficios")]
        public DateTime FechaNacimiento { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;

        
        public int? TipoCabelloId { get; set; }
        public int? TipoRostroId { get; set; }

        
        public TipoCabello? TipoCabello { get; set; }
        public TipoRostro? TipoRostro { get; set; }
        public ICollection<FichaTecnica> FichasTecnicas { get; set; } = new List<FichaTecnica>();
        public ICollection<Turno> Turnos { get; set; } = new List<Turno>(); 
    }
}