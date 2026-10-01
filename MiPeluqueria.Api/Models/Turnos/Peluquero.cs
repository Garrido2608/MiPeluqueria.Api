using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Clientes; 
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Turnos
{
    public class Peluquero : AuditableEntity
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100)]
        public string Apellido { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [StringLength(7)]
        public string ColorAgendaHex { get; set; } = "#3B82F6"; 

        public bool Activo { get; set; } = true;

        
        public ICollection<HorarioLaboral> HorariosLaborales { get; set; } = new List<HorarioLaboral>();
        public ICollection<BloqueoHorario> BloqueosHorario { get; set; } = new List<BloqueoHorario>();
        public ICollection<PeluqueroEspecialidad> PeluqueroEspecialidades { get; set; } = new List<PeluqueroEspecialidad>();
        public ICollection<Turno> Turnos { get; set; } = new List<Turno>();

        
        public ICollection<FichaTecnica> FichasTecnicas { get; set; } = new List<FichaTecnica>();
    }
}