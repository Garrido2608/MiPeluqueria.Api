namespace MiPeluqueria.Api.DTOs.Clientes
{
    public class FichaTecnicaResponseDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public int? TurnoId { get; set; }
        public int? PeluqueroId { get; set; }
        public string NombrePeluquero { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
        public string FormulaAplicada { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public string? ProductosUtilizados { get; set; }
        public string? FotoResultadoUrl { get; set; }
    }
}