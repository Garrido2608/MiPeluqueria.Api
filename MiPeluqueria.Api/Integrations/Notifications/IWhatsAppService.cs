using System.Threading.Tasks;

namespace MiPeluqueria.Api.Integrations.Notifications
{
    public interface IWhatsAppService
    {
        Task<bool> EnviarMensajeAsync(string telefono, string mensaje);
        Task<bool> EnviarRecordatorioTurnoAsync(string telefono, string nombreCliente, string fechaHora, string peluquero);
    }
}