using System;
using System.Collections.Generic;

namespace MiPeluqueria.Api.DTOs.Dashboard
{
    public class DashboardKpiResponseDto
    {
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }

        // Métricas financieras y operativas del período consultado
        public decimal FacturacionTotalMes { get; set; }
        public decimal IngresosTotalesPeriodo => FacturacionTotalMes;
        public int CantidadVentasMes { get; set; }
        public int TotalVentasPeriodo => CantidadVentasMes;
        public decimal TicketPromedio { get; set; }

        // Métricas de turnos y agenda
        public int TotalTurnosAtendidosMes { get; set; }
        public int TurnosCompletadosPeriodo => TotalTurnosAtendidosMes;
        public int TurnosCanceladosPeriodo { get; set; }
        public int TurnosPendientesHoy { get; set; }
        public double TasaCancelacionPorcentaje { get; set; }

        // Métricas comerciales, clientes y stock
        public int ClientesNuevosPeriodo { get; set; }
        public int ProductosEnStockCritico { get; set; }
        public int ProductosBajoStockCritico => ProductosEnStockCritico;
        public string ServicioMasVendido { get; set; } = "Sin datos";
        public string PeluqueroEstrella { get; set; } = "Sin datos";
        public string PeluqueroTopPeriodo => PeluqueroEstrella;

        // Desgloses analíticos para gráficos del Frontend
        public List<IngresoPorMedioPagoDto> IngresosPorMedioPago { get; set; } = new();
        public List<RankingPeluqueroDto> RankingPeluqueros { get; set; } = new();
    }

    public class IngresoPorMedioPagoDto
    {
        public string MedioPago { get; set; } = string.Empty;
        public decimal TotalRecaudado { get; set; }
        public int CantidadOperaciones { get; set; }
    }

    public class RankingPeluqueroDto
    {
        public int PeluqueroId { get; set; }
        public string NombrePeluquero { get; set; } = string.Empty;
        public int ServiciosRealizados { get; set; }
        public decimal TotalFacturado { get; set; }
    }
}