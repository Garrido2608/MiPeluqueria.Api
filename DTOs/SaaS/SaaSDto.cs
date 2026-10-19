using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.SaaS;

namespace MiPeluqueria.Api.DTOs.SaaS
{
    public class CrearSucursalDto
    {
        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string Direccion { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Telefono { get; set; }

        public string TimeZoneId { get; set; } = "America/Argentina/Cordoba";
        public string MonedaIso { get; set; } = "ARS";
        public bool EsCasaMatriz { get; set; } = false;
    }

    public class SucursalResponseDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string TimeZoneId { get; set; } = string.Empty;
        public string MonedaIso { get; set; } = string.Empty;
        public bool EsCasaMatriz { get; set; }
        public bool Activa { get; set; }
    }

    public class SuscripcionEstadoResponseDto
    {
        public int TenantId { get; set; }
        public string NombreComercial { get; set; } = string.Empty;
        public string PlanActual { get; set; } = string.Empty;
        public decimal PrecioMensualUsd { get; set; }
        public EstadoSuscripcionEnum Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public DateTime FechaProximoVencimiento { get; set; }
        public int MaxSucursalesPermitidas { get; set; }
        public int MaxPeluquerosPermitidos { get; set; }
    }

    public class CambiarPlanSuscripcionDto
    {
        [Required]
        public int NuevoPlanSuscripcionId { get; set; }
    }
}