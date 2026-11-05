using System;
using System.Security.Cryptography;
using System.Text;
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
            if (string.IsNullOrWhiteSpace(proveedor) || string.IsNullOrWhiteSpace(signatureHeader))
            {
                return false;
            }

            string providerKey = proveedor.Trim();
            var webhookSecret = _config[$"Payments:{providerKey}:WebhookSecret"];

            // Política Fail-Closed estricta: si el secreto no existe o está vacío, se rechaza siempre.
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogWarning(
                    "[PAYMENT GATEWAY] Webhook rechazado (Fail-Closed): 'Payments:{Proveedor}:WebhookSecret' no se encuentra configurado.",
                    providerKey);
                return false;
            }

            if (!TryExtraerFirmaHex(signatureHeader, out string firmaHex, out string? timestamp))
            {
                return false;
            }

            byte[] receivedHashBytes;
            try
            {
                receivedHashBytes = Convert.FromHexString(firmaHex);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] secretBytes = Encoding.UTF8.GetBytes(webhookSecret);
            string safePayload = rawBody ?? string.Empty;

            byte[] directHashBytes = HMACSHA256.HashData(secretBytes, Encoding.UTF8.GetBytes(safePayload));
            if (CryptographicOperations.FixedTimeEquals(directHashBytes, receivedHashBytes))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(timestamp))
            {
                byte[] mpManifestHash = HMACSHA256.HashData(
                    secretBytes,
                    Encoding.UTF8.GetBytes($"id:{safePayload};ts:{timestamp};"));

                if (CryptographicOperations.FixedTimeEquals(mpManifestHash, receivedHashBytes))
                {
                    return true;
                }

                byte[] stripeManifestHash = HMACSHA256.HashData(
                    secretBytes,
                    Encoding.UTF8.GetBytes($"{timestamp}.{safePayload}"));

                if (CryptographicOperations.FixedTimeEquals(stripeManifestHash, receivedHashBytes))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryExtraerFirmaHex(string header, out string firmaHex, out string? timestamp)
        {
            firmaHex = string.Empty;
            timestamp = null;

            string trimmed = header.Trim();
            if (trimmed.Length == 0)
            {
                return false;
            }

            if (trimmed.Contains(','))
            {
                var segments = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var segment in segments)
                {
                    if (segment.StartsWith("v1=", StringComparison.OrdinalIgnoreCase))
                    {
                        firmaHex = segment[3..].Trim();
                    }
                    else if (segment.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
                    {
                        firmaHex = segment[7..].Trim();
                    }
                    else if (segment.StartsWith("ts=", StringComparison.OrdinalIgnoreCase))
                    {
                        timestamp = segment[3..].Trim();
                    }
                    else if (segment.StartsWith("t=", StringComparison.OrdinalIgnoreCase))
                    {
                        timestamp = segment[2..].Trim();
                    }
                }
            }
            else if (trimmed.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            {
                firmaHex = trimmed[7..].Trim();
            }
            else if (trimmed.StartsWith("v1=", StringComparison.OrdinalIgnoreCase))
            {
                firmaHex = trimmed[3..].Trim();
            }
            else
            {
                firmaHex = trimmed;
            }

            return firmaHex.Length > 0 && firmaHex.Length % 2 == 0;
        }
    }
}