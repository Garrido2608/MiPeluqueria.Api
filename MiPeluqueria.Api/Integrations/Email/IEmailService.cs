using System.Threading.Tasks;

namespace MiPeluqueria.Api.Integrations.Email
{
    public interface IEmailService
    {
        Task<bool> EnviarEmailAsync(string destinatario, string asunto, string cuerpoHtml);
        Task<bool> EnviarCodigoOtpAsync(string destinatario, string nombreUsuario, string codigoOtp, string motivo);
        Task<bool> EnviarConfirmacionTurnoAsync(string destinatario, string nombreCliente, string fechaHora, string peluquero);
    }
}