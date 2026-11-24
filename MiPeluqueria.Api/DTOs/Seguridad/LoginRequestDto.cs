namespace MiPeluqueria.Api.DTOs.Seguridad
{
    public class LoginRequestDto
    {
        private string _identificador = string.Empty;

        public string UsernameOrEmail
        {
            get => _identificador;
            set => _identificador = value ?? string.Empty;
        }

        public string Username
        {
            get => _identificador;
            set => _identificador = value ?? string.Empty;
        }

        public string Email
        {
            get => _identificador;
            set => _identificador = value ?? string.Empty;
        }

        public string Password { get; set; } = string.Empty;
    }
}