using System;

namespace MiPeluqueria.Api.DTOs.Clientes
{
    public class ClienteRequestDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public int? TipoCabelloId { get; set; }
        public int? TipoRostroId { get; set; }
    }
}