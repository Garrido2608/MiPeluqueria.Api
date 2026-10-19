using System;

namespace MiPeluqueria.Api.DTOs.Seguridad
{
    public class AuthResponseDto
    {
        public int UsuarioId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiracion { get; set; }
        public int TenantId { get; set; } = 1;
        public int? SucursalId { get; set; } = 1;
    }
}