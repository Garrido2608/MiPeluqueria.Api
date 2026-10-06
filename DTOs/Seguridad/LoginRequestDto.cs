using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Seguridad
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "El nombre de usuario o email es obligatorio.")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; } = string.Empty;
    }
}