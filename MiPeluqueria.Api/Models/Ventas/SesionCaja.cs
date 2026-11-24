using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;
namespace MiPeluqueria.Api.Models.Ventas
{
    public class SesionCaja : AuditableTenantEntity
    {
        public int UsuarioAperturaId { get; set; }
        public DateTime FechaApertura { get; set; } = DateTime.UtcNow;
        public decimal MontoInicial { get; set; } // Fondo fijo para cambio
        public int? UsuarioCierreId { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal TotalVentasEfectivo { get; set; }
        public decimal TotalVentasDigital { get; set; }
        public decimal TotalIngresosManuales { get; set; }
        public decimal TotalEgresosManuales { get; set; }
        public decimal? MontoFinalReal { get; set; } // Dinero físico contado por el cajero
        public decimal? MontoFinalEsperado { get; set; } // Calculado por el sistema
        public decimal? Diferencia { get; set; } // Real - Esperado (Sobrante o Faltante)
        [StringLength(500)]
        public string? ObservacionesCierre { get; set; }
        public bool EstadoAbierta { get; set; } = true;
        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        public ICollection<Venta> Ventas { get; set; } = new List<Venta>();
        public ICollection<MovimientoCaja> MovimientosCaja { get; set; } = new
        List<MovimientoCaja>();
    }
}