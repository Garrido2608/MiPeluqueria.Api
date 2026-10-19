using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Models.Turnos
{
    public class ServicioInsumoReceta : AuditableEntity
    {
        [Required]
        public int ServicioId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public int CantidadUnidadesDescontar { get; set; } = 1;

        public decimal CantidadMedidaEstandar { get; set; } = 1m; // Ej: 60 (gramos/ml informativos de receta)

        [StringLength(20)]
        public string UnidadMedida { get; set; } = "Unid"; // "gr", "ml", "Unid", "Ampolla"

        public bool DescuentoAutomaticoAlCompletarTurno { get; set; } = true;

        public Servicio? Servicio { get; set; }
        public Producto? Producto { get; set; }
    }
}