using System;

namespace MiPeluqueria.Api.DTOs.Seguridad
{
    public class RegistroUsuarioDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int RolId { get; set; } = 5; // 5: Cliente por defecto (1: Admin, 2: Recepcionista, 3: Cajero, 4: Peluquero)
        public int? ClienteId { get; set; }
        public int? PeluqueroId { get; set; }
        public bool AceptaTerminos { get; set; } = true;
    }

    public class VerificarEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string CodigoVerificacion { get; set; } = string.Empty;
    }

    public class CambiarPasswordDto
    {
        public string PasswordActual { get; set; } = string.Empty;
        public string NuevaPassword { get; set; } = string.Empty;
    }

    public class OlvidePasswordDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class RestablecerPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string CodigoOtp { get; set; } = string.Empty;
        public string NuevaPassword { get; set; } = string.Empty;
    }

    public class ActualizarEstadoUsuarioDto
    {
        public int? RolId { get; set; }
        public bool? Activo { get; set; }
        public bool DesbloquearCuenta { get; set; }
    }

    public class UsuarioResponseDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int RolId { get; set; }
        public string RolNombre { get; set; } = string.Empty;
        public bool EmailVerificado { get; set; }
        public bool Activo { get; set; }
        public int IntentosFallidos { get; set; }
        public DateTime? BloqueadoHasta { get; set; }
        public int? ClienteId { get; set; }
        public int? PeluqueroId { get; set; }
    }

    public class HistorialSesionResponseDto
    {
        public int Id { get; set; }
        public int? UsuarioId { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; }
        public string? IpOrigen { get; set; }
        public string? Dispositivo { get; set; }
        public bool Exitoso { get; set; }
        public string? MotivoFallo { get; set; }
    }
}