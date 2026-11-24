using System;
using System.Collections.Generic;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.DTOs.Stock
{
    public class CrearOrdenCompraDto
    {
        public int ProveedorId { get; set; }
        public string? NumeroComprobanteProveedor { get; set; }
        public string? Observaciones { get; set; }
        public List<DetalleOrdenCompraItemDto> Detalles { get; set; } = new();
    }

    public class DetalleOrdenCompraItemDto
    {
        public int ProductoId { get; set; }
        public int CantidadPedida { get; set; }
        public decimal PrecioUnitarioCosto { get; set; }
    }

    public class RecepcionarOrdenCompraDto
    {
        public string? NumeroComprobanteProveedor { get; set; }
        public bool ActualizarCostoProductos { get; set; } = true;
        public bool ImputarEgresoEnCajaAbierta { get; set; } = false;
        public string? ObservacionesRecepcion { get; set; }
        public List<DetalleRecepcionItemDto>? CantidadesRecibidas { get; set; }
    }

    public class DetalleRecepcionItemDto
    {
        public int ProductoId { get; set; }
        public int CantidadRecibida { get; set; }
        public decimal? NuevoPrecioVentaSugerido { get; set; }
    }

    public class OrdenCompraResponseDto
    {
        public int Id { get; set; }
        public int ProveedorId { get; set; }
        public string ProveedorRazonSocial { get; set; } = string.Empty;
        public string? NumeroComprobanteProveedor { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public EstadoOrdenCompraEnum Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string? Observaciones { get; set; }
        public List<DetalleOrdenCompraResponseDto> Detalles { get; set; } = new();
    }

    public class DetalleOrdenCompraResponseDto
    {
        public int Id { get; set; }
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public int CantidadPedida { get; set; }
        public int CantidadRecibida { get; set; }
        public decimal PrecioUnitarioCosto { get; set; }
        public decimal Subtotal { get; set; }
    }
}