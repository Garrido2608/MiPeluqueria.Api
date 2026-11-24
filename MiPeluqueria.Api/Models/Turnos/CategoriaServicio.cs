using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;


namespace MiPeluqueria.Api.Models.Turnos
{
    public class CategoriaServicio : AuditableTenantEntity
    {
        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Descripcion { get; set; }

        public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
    }
}