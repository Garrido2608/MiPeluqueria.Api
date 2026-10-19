using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

public class Tenant : AuditableEntity
{
    [Required]
    [StringLength(150)]
    public string NombreComercial { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string SubdominioSlug { get; set; } = string.Empty; // ej: "barberia-cordoba"

    [StringLength(30)]
    public string? IdentificacionFiscal { get; set; } // CUIT / Tax ID / RUT

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string EmailPropietario { get; set; } = string.Empty;

    [StringLength(10)]
    public string PaisIso { get; set; } = "AR";

    public bool Activo { get; set; } = true;

    public ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();
    public ICollection<SuscripcionTenant> Suscripciones { get; set; } = new List<SuscripcionTenant>();
}
