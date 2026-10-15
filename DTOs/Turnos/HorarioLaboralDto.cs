using System;
using System.Collections.Generic;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class HorarioLaboralDto
    {
        public int Id { get; set; }
        public int PeluqueroId { get; set; }
        public int DiaSemana { get; set; } // 0: Domingo, 1: Lunes, ..., 6: Sábado
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
        public TimeSpan? HoraInicioDescanso { get; set; }
        public TimeSpan? HoraFinDescanso { get; set; }
        public bool Activo { get; set; } = true;
    }

    public class GuardarHorariosPeluqueroDto
    {
        public List<HorarioLaboralDto> Horarios { get; set; } = new();
    }
}