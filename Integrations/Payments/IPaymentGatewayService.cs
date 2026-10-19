using System.Threading;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Integrations.Payments
{
    public record CheckoutSessionResult(bool Success, string CheckoutUrl, string PreferenceId, string Provider);

    public interface IPaymentGatewayService
    {
        Task<CheckoutSessionResult> CrearPreferenciaSenaTurnoAsync(
            int turnoId,
            decimal montoSena,
            string monedaIso,
            string emailCliente,
            string pasarela = "MercadoPago",
            CancellationToken ct = default);

        bool ValidarFirmaWebhook(string proveedor, string? signatureHeader, string rawBody);
    }
}