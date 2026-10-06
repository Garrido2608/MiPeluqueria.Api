using System;

namespace MiPeluqueria.Api.DTOs.Clientes
{
    public class ClienteResponseDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public bool EsCumpleaneroDelMes { get; set; }
        public int? TipoCabelloId { get; set; }
        public string? TipoCabelloNombre { get; set; }
        public int? TipoRostroId { get; set; }
        public string? TipoRostroNombre { get; set; }
    }
}