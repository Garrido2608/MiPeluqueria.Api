using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
public class SuscripcionTenant : AuditableEntity
{
    [Required]
    public int TenantId { get; set; }

    [Required]
    public int PlanSuscripcionId { get; set; }

    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime FechaProximoVencimiento { get; set; } = DateTime.UtcNow.AddDays(30);

    public EstadoSuscripcionEnum Estado { get; set; } = EstadoSuscripcionEnum.Activa;

    [StringLength(150)]
    public string? ExternalSubscriptionId { get; set; } // ID de suscripción en Stripe / MercadoPago

    public Tenant? Tenant { get; set; }
    public PlanSuscripcion? PlanSuscripcion { get; set; }
}
}