using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MiPeluqueria.Api.Integrations.Email
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> EnviarEmailAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            var host = _config["Smtp:Host"];
            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Password"];
            var portStr = _config["Smtp:Port"];

            // Modo Simulación si no hay servidor SMTP configurado en appsettings.json
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                _logger.LogInformation("==================================================================");
                _logger.LogInformation("[SIMULACIÓN SMTP EMAIL] Para: {Destinatario} | Asunto: {Asunto}", destinatario, asunto);
                _logger.LogInformation("[CONTENIDO]: {Cuerpo}", cuerpoHtml);
                _logger.LogInformation("==================================================================");
                return true;
            }

            try
            {
                int port = int.TryParse(portStr, out int p) ? p : 587;
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = true
                };

                using var mail = new MailMessage(user, destinatario, asunto, cuerpoHtml)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar el correo vía SMTP real. Se registró en consola local.");
                return false;
            }
        }

        public async Task<bool> EnviarCodigoOtpAsync(string destinatario, string nombreUsuario, string codigoOtp, string motivo)
        {
            string asunto = $"MiPeluquería - Código de Seguridad ({motivo})";
            string html = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eaeaea; border-radius: 8px;'>
                    <h2>Hola, {nombreUsuario}</h2>
                    <p>Has solicitado un código de verificación para: <strong>{motivo}</strong>.</p>
                    <p>Tu código de 6 dígitos es:</p>
                    <h1 style='color: #2563eb; letter-spacing: 4px;'>{codigoOtp}</h1>
                    <p>Este código expira en 15 minutos.</p>
                </div>";

            return await EnviarEmailAsync(destinatario, asunto, html);
        }

        public async Task<bool> EnviarConfirmacionTurnoAsync(string destinatario, string nombreCliente, string fechaHora, string peluquero)
        {
            string asunto = "MiPeluquería - Confirmación de Reserva de Turno";
            string html = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>¡Reserva Confirmada, {nombreCliente}!</h2>
                    <p>Te esperamos el día <strong>{fechaHora}</strong> con el profesional <strong>{peluquero}</strong>.</p>
                </div>";

            return await EnviarEmailAsync(destinatario, asunto, html);
        }
    }
}