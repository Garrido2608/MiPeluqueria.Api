using AutoMapper;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- Módulo Clientes ---
            CreateMap<Cliente, ClienteResponseDto>()
                .ForMember(d => d.NombreCompleto, opt => opt.MapFrom(s => $"{s.Nombre} {s.Apellido}".Trim()))
                .ForMember(d => d.TipoCabelloNombre, opt => opt.MapFrom(s => s.TipoCabello != null ? s.TipoCabello.Nombre : null))
                .ForMember(d => d.TipoRostroNombre, opt => opt.MapFrom(s => s.TipoRostro != null ? s.TipoRostro.Nombre : null));
            CreateMap<ClienteRequestDto, Cliente>();

            // --- Módulo Turnos y Agenda ---
            CreateMap<Turno, TurnoResponseDto>()
                .ForMember(d => d.ClienteNombre, opt => opt.MapFrom(s => s.Cliente != null ? $"{s.Cliente.Nombre} {s.Cliente.Apellido}" : string.Empty))
                .ForMember(d => d.PeluqueroNombre, opt => opt.MapFrom(s => s.Peluquero != null ? $"{s.Peluquero.Nombre} {s.Peluquero.Apellido}" : string.Empty))
                .ForMember(d => d.EstadoNombre, opt => opt.MapFrom(s => s.Estado != null ? s.Estado.Nombre : string.Empty));
            CreateMap<CrearTurnoDto, Turno>();

            // --- Módulo Servicios y Staff ---
            CreateMap<Servicio, ServicioDto>()
                .ForMember(d => d.CategoriaNombre, opt => opt.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : string.Empty));
            CreateMap<CrearServicioDto, Servicio>();

            

            // Si tenés el PeluqueroDto, se mapea directo
            CreateMap<CrearPeluqueroDto, Peluquero>();
            CreateMap<Peluquero, PeluqueroDto>();

            // --- Módulo Stock e Insumos ---
            // AutoMapper convierte inteligentemente nuestro TipoMovimientoStockEnum al Enum interno automáticamente
            CreateMap<Producto, ProductoDto>()
                .ForMember(d => d.CategoriaNombre, opt => opt.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : string.Empty));
            CreateMap<CrearProductoDto, Producto>();
            CreateMap<MovimientoStockDto, MovimientoStock>();

            // --- Módulo Ventas y Caja ---
            // Mapeamos los Request separados a sus respectivas entidades. 
            // AutoMapper convierte TipoItemVentaEnum sin que tengamos que programar conversiones manuales.
            CreateMap<DetalleVentaRequestDto, DetalleVenta>();
            CreateMap<PagoRequestDto, Pago>();
        }
    }
}