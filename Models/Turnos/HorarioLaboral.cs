using System;

using System.ComponentModel.DataAnnotations;

using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Turnos
{
    public class HorarioLaboral : AuditableTenantEntity
    {
        [Required]
        public int PeluqueroId { get; set; }

        public DayOfWeek DiaSemana { get; set; }

        public TimeSpan HoraInicio { get; set; }

        public TimeSpan HoraFin { get; set; }

        public TimeSpan? HoraInicioDescanso { get; set; }

        public TimeSpan? HoraFinDescanso { get; set; }

        public bool Activo { get; set; } = true;

        public Peluquero? Peluquero { get; set; }
    }
}
