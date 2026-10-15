namespace MiPeluqueria.Api.DTOs.Clientes
{
    public class CrearFichaTecnicaDto
    {
        public int ClienteId { get; set; }
        public int? TurnoId { get; set; }
        public int PeluqueroId { get; set; }
        public string FormulaAplicada { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public string? ProductosUtilizados { get; set; }
        public string? FotoResultadoUrl { get; set; }
    }
}