using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.SaaS
{
    public class Sucursal : AuditableEntity
    {
        [Required]
        public int TenantId { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string Direccion { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Telefono { get; set; }

        [StringLength(100)]
        public string TimeZoneId { get; set; } = "America/Argentina/Cordoba";

        [StringLength(3)]
        public string MonedaIso { get; set; } = "ARS";

        public bool EsCasaMatriz { get; set; } = false;
        public bool Activa { get; set; } = true;

        public Tenant? Tenant { get; set; }
    }
}