using System.Collections.Generic;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class PeluqueroDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string Email { get; set; } = string.Empty;
        public string ColorAgendaHex { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public List<string> Especialidades { get; set; } = new List<string>();
    }
}