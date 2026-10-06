namespace MiPeluqueria.Api.DTOs.Seguridad
{
    public class AuthResponseDto
    {
        public int UsuarioId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;

        // Esta es la llave digital que el frontend mandará en cada petición
        public string Token { get; set; } = string.Empty;
    }
}