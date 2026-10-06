namespace MiPeluqueria.Api.Models.Turnos
{
    public class PeluqueroEspecialidad
    {
        public int PeluqueroId { get; set; }
        public int EspecialidadId { get; set; }
        public Peluquero? Peluquero { get; set; }
        public Especialidad? Especialidad { get; set; }
    }
}