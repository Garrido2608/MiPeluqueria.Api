using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MiPeluqueria.Api.Integrations.Payments
{
    public class PaymentGatewayService : IPaymentGatewayService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentGatewayService> _logger;

        public PaymentGatewayService(IConfiguration config, ILogger<PaymentGatewayService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public Task<CheckoutSessionResult> CrearPreferenciaSenaTurnoAsync(
            int turnoId,
            decimal montoSena,
            string monedaIso,
            string emailCliente,
            string pasarela = "MercadoPago",
            CancellationToken ct = default)
        {
            string externalRef = $"TURNO_{turnoId}";
            string providerNorm = string.Equals(pasarela, "Stripe", StringComparison.OrdinalIgnoreCase) ? "Stripe" : "MercadoPago";

            string prefId = $"{providerNorm.ToUpperInvariant()}_{Guid.NewGuid():N}";
            string checkoutUrl = providerNorm == "Stripe"
                ? $"https://checkout.stripe.com/pay/{prefId}?client_reference_id={externalRef}"
                : $"https://www.mercadopago.com.ar/checkout/v1/redirect?pref_id={prefId}&external_reference={externalRef}";

            _logger.LogInformation(
                "[PAYMENT GATEWAY] Seña generada para Turno #{TurnoId} por {Monto} {Moneda} vía {Provider}. URL: {Url}",
                turnoId, montoSena, monedaIso, providerNorm, checkoutUrl);

            return Task.FromResult(new CheckoutSessionResult(true, checkoutUrl, prefId, providerNorm));
        }

        public bool ValidarFirmaWebhook(string proveedor, string? signatureHeader, string rawBody)
        {
            var webhookSecret = _config[$"Payments:{proveedor}:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
                return true; // Modo Sandbox / Desarrollo

            return !string.IsNullOrWhiteSpace(signatureHeader);
        }
    }
}