using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Turnos
{
    public class PeluqueroEspecialidad : AuditableTenantEntity
    {
        public int PeluqueroId { get; set; }
        public int EspecialidadId { get; set; }
        public Peluquero? Peluquero { get; set; }
        public Especialidad? Especialidad { get; set; }
    }
}