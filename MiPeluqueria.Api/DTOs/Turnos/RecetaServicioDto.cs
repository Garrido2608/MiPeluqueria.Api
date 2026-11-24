using System.Collections.Generic;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class ConfigurarRecetaServicioDto
    {
        public int ServicioId { get; set; }
        public List<ItemRecetaInputDto> Insumos { get; set; } = new();
    }

    public class ItemRecetaInputDto
    {
        public int ProductoId { get; set; }
        public int CantidadUnidadesDescontar { get; set; } = 1;
        public decimal CantidadMedidaEstandar { get; set; } = 1m;
        public string UnidadMedida { get; set; } = "Unid";
        public bool DescuentoAutomaticoAlCompletarTurno { get; set; } = true;
    }

    public class RecetaItemResponseDto
    {
        public int Id { get; set; }
        public int ServicioId { get; set; }
        public string ServicioNombre { get; set; } = string.Empty;
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public int StockActualProducto { get; set; }
        public int CantidadUnidadesDescontar { get; set; }
        public decimal CantidadMedidaEstandar { get; set; }
        public string UnidadMedida { get; set; } = "Unid";
        public decimal CostoEstimadoInsumo { get; set; }
    }
}