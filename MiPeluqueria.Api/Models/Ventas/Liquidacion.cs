using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Models.Ventas
{
    public class Liquidacion : AuditableEntity
    {
        [Required]
        public int PeluqueroId { get; set; }

        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }

        // Agregado para soportar el cálculo del estado "Atrasado"
        public DateTime? FechaVencimiento { get; set; }

        public decimal TotalServicios { get; set; }
        public decimal TotalProductos { get; set; }
        public decimal TotalPagar { get; set; }
        public DateTime? FechaPago { get; set; }

        // Reemplazamos el string por el Enum para integridad referencial
        [Required]
        public EstadoLiquidacionEnum Estado { get; set; } = EstadoLiquidacionEnum.Pendiente;

        public Peluquero? Peluquero { get; set; }
        public ICollection<DetalleLiquidacion> DetallesLiquidacion { get; set; } = new List<DetalleLiquidacion>();
    }
}