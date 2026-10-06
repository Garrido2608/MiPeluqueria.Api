using System;

using System.ComponentModel.DataAnnotations;

using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Turnos

{

    public class HorarioLaboral : AuditableEntity

    {

        [Required]
        public int PeluqueroId { get; set; }

        public DayOfWeek DiaSemana { get; set; }

        public TimeSpan HoraInicio { get; set; }

        public TimeSpan HoraFin { get; set; }

        public Peluquero? Peluquero { get; set; }

    }

}
