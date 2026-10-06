using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Turnos
{
    public class Servicio : AuditableEntity
    {
        [Required]
        public int CategoriaId { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public decimal Precio { get; set; }

        public int DuracionMinutos { get; set; }

        public bool Activo { get; set; } = true;

        public CategoriaServicio? Categoria { get; set; }

        public ICollection<TurnoServicio> TurnoServicios { get; set; } = new List<TurnoServicio>();
    }
}