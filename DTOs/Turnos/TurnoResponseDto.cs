using System;
using System.Collections.Generic;

namespace MiPeluqueria.Api.DTOs.Turnos
{
    public class TurnoResponseDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string? ClienteTelefono { get; set; }
        public int PeluqueroId { get; set; }
        public string PeluqueroNombre { get; set; } = string.Empty;
        public string ColorAgendaHex { get; set; } = "#3B82F6";
        public DateTime FechaHoraInicio { get; set; }
        public DateTime FechaHoraFin { get; set; }
        public int DuracionTotalMinutos { get; set; }
        public int EstadoId { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public int? MotivoCancelacionId { get; set; }
        public string? MotivoCancelacionDescripcion { get; set; }
        public decimal MontoTotalEstimado { get; set; }
        public bool RequiereSena { get; set; }
        public decimal MontoSena { get; set; }
        public bool SenaPagada { get; set; }
        public DateTime? FechaPagoSena { get; set; }
        public string? ReferenciaPagoExterno { get; set; }
        public decimal SaldoPendienteCobro { get; set; }
        public string? Observaciones { get; set; }
        public List<int> ServiciosIds { get; set; } = new List<int>();
        public List<string> Servicios { get; set; } = new List<string>();
        public List<TurnoServicioDetalleDto> ServiciosDetalle { get; set; } = new List<TurnoServicioDetalleDto>();
    }

    public class TurnoServicioDetalleDto
    {
        public int ServicioId { get; set; }
        public string ServicioNombre { get; set; } = string.Empty;
        public decimal PrecioHistorico { get; set; }
        public int DuracionMinutosHistorica { get; set; }
    }
}