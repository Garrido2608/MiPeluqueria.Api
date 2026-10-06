namespace MiPeluqueria.Api.Models.Turnos
{
    public class TurnoServicio
    {
        public int TurnoId { get; set; }
        public int ServicioId { get; set; }
        public decimal PrecioHistorico { get; set; }
        public int DuracionMinutosHistorica { get; set; }
        public Turno? Turno { get; set; }
        public Servicio? Servicio { get; set; }
    }
}