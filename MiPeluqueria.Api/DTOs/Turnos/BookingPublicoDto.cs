using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class ReservaPublicaRequestDto
    {
        [Required]
        [StringLength(100)]
        public string NombreCliente { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ApellidoCliente { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string TelefonoCliente { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        public string? EmailCliente { get; set; }

        [Required]
        public int PeluqueroId { get; set; }

        [Required]
        public DateTime FechaHoraInicio { get; set; }

        [Required]
        [MinLength(1)]
        public List<int> ServiciosIds { get; set; } = new();

        public string? Observaciones { get; set; }
        public string PasarelaPreferida { get; set; } = "MercadoPago"; // "MercadoPago" o "Stripe"
    }

    public class ReservaPublicaResponseDto
    {
        public int TurnoId { get; set; }
        public int ClienteId { get; set; }
        public string EstadoTurno { get; set; } = string.Empty;
        public decimal MontoTotalEstimado { get; set; }
        public bool RequiereSena { get; set; }
        public decimal MontoSena { get; set; }
        public string? CheckoutPagoUrl { get; set; }
        public string? PreferenciaPagoId { get; set; }
    }

    public class WebhookPagoNotificacionDto
    {
        public string? Id { get; set; }
        public string? Topic { get; set; }
        public string? Type { get; set; }
        public string? ExternalReference { get; set; } // Formato: "TURNO_{TurnoId}"
        public string? Status { get; set; }            // "approved" / "paid"
        public decimal? TransactionAmount { get; set; }
    }
}