using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
namespace MiPeluqueria.Api.Models.Ventas
{
    public class SesionCaja : AuditableEntity
    {
        public int UsuarioAperturaId { get; set; }
        public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
        public decimal MontoInicial { get; set; } // Fondo fijo para cambio
        public int? UsuarioCierreId { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal? MontoFinalReal { get; set; } // Dinero físico contado por el cajero
        public decimal? MontoFinalEsperado { get; set; } // Calculado por el sistema
        public decimal? Diferencia { get; set; } // Real - Esperado (Sobrante o Faltante)
        public bool EstadoAbierta { get; set; } = true;
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<MovimientoCaja> MovimientosCaja { get; set; } = new
        List<MovimientoCaja>();
    }
}