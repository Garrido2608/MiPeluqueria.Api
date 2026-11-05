using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MiPeluqueria.Api.Integrations.Email
{
    public class SmtpEmailService : IEmailService
    {
        private const int DefaultTimeoutMilliseconds = 5000;
        private const int MaxTimeoutMilliseconds = 5000;

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

            // Cortocircuito defensivo ODS: si Smtp:Host está vacío, jamás instanciar SmtpClient
            if (string.IsNullOrWhiteSpace(host))
            {
                _logger.LogWarning(
                    "SMTP no configurado en entorno local. Se omite el envío real de correo a {Destinatario}.",
                    destinatario);
                return false;
            }

            if (string.IsNullOrWhiteSpace(destinatario))
            {
                _logger.LogWarning("Se omitió el envío de correo porque el destinatario está vacío.");
                return false;
            }

            var user = _config["Smtp:User"];
            var pass = _config["Smtp:Password"];
            var portStr = _config["Smtp:Port"];
            var enableSslStr = _config["Smtp:EnableSsl"];
            var timeoutStr = _config["Smtp:TimeoutMilliseconds"];
            var fromEmailConfig = _config["Smtp:FromEmail"];
            var fromNameConfig = _config["Smtp:FromName"];

            int port = int.TryParse(portStr, out int p) && p > 0 ? p : 587;
            bool enableSsl = !bool.TryParse(enableSslStr, out bool ssl) || ssl;
            int timeoutMs = int.TryParse(timeoutStr, out int configuredTimeout) && configuredTimeout > 0
                ? Math.Min(configuredTimeout, MaxTimeoutMilliseconds)
                : DefaultTimeoutMilliseconds;

            string fromEmail = !string.IsNullOrWhiteSpace(fromEmailConfig)
                ? fromEmailConfig.Trim()
                : (!string.IsNullOrWhiteSpace(user) ? user.Trim() : "no-reply@mipeluqueria.com");
            string fromName = !string.IsNullOrWhiteSpace(fromNameConfig)
                ? fromNameConfig.Trim()
                : "Mi Peluquería SaaS";

            try
            {
                using var client = new SmtpClient(host.Trim(), port)
                {
                    EnableSsl = enableSsl,
                    Timeout = timeoutMs
                };

                if (!string.IsNullOrWhiteSpace(user))
                {
                    client.Credentials = new NetworkCredential(user.Trim(), pass ?? string.Empty);
                }

                using var mail = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true
                };
                mail.To.Add(destinatario.Trim());

                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
                await client.SendMailAsync(mail, cts.Token);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "No se pudo enviar el correo vía SMTP a {Destinatario} dentro del límite de {TimeoutMs}ms.",
                    destinatario,
                    timeoutMs);
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