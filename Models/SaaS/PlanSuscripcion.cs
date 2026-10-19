using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.SaaS
{
    public class PlanSuscripcion : AuditableEntity
    {
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty; // "Starter", "Professional", "Enterprise"

        public decimal PrecioMensualUsd { get; set; }
        public int MaxSucursales { get; set; } = 1;
        public int MaxPeluqueros { get; set; } = 5;
        public bool IncluyeWhatsAppRecordatorios { get; set; } = true;
        public bool IncluyePortalReservasOnline { get; set; } = true;
        public bool Activo { get; set; } = true;
    }
}