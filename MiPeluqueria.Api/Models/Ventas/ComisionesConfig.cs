using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Models.Ventas
{
    public class ComisionesConfig : AuditableEntity
    {
        [Required]
        public int PeluqueroId { get; set; }

        public int? ServicioId { get; set; } // Opcional: si es nulo, es el porcentaje general del peluquero

        public decimal PorcentajeServicio { get; set; } = 50.0m; // 50% de honorarios sobre corte

        public decimal PorcentajeVentaProducto { get; set; } = 10.0m; // Ej: 10% por venta de pomada

        public Peluquero? Peluquero { get; set; }
        public Servicio? Servicio { get; set; }
    }
}