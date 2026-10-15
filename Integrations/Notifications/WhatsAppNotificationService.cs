using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiPeluqueria.Api.Integrations.Notifications
{
    public class WhatsAppNotificationService : IWhatsAppService
    {
        private readonly ILogger<WhatsAppNotificationService> _logger;

        public WhatsAppNotificationService(ILogger<WhatsAppNotificationService> logger)
        {
            _logger = logger;
        }

        public Task<bool> EnviarMensajeAsync(string telefono, string mensaje)
        {
            _logger.LogInformation("==================================================================");
            _logger.LogInformation("[WHATSAPP GATEWAY] Teléfono: {Telefono} | Mensaje: {Mensaje}", telefono, mensaje);
            _logger.LogInformation("==================================================================");
            return Task.FromResult(true);
        }

        public Task<bool> EnviarRecordatorioTurnoAsync(string telefono, string nombreCliente, string fechaHora, string peluquero)
        {
            string msg = $"Hola {nombreCliente}, te recordamos tu turno en MiPeluquería para el {fechaHora} con {peluquero}.";
            return EnviarMensajeAsync(telefono, msg);
        }
    }
}