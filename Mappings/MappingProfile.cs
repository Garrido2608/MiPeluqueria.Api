using System.Linq;
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
            // 1. Módulo Clientes
            CreateMap<Cliente, ClienteResponseDto>()
                .ForMember(d => d.NombreCompleto, opt => opt.MapFrom(s => $"{s.Nombre} {s.Apellido}".Trim()))
                .ForMember(d => d.TipoCabelloNombre, opt => opt.MapFrom(s => s.TipoCabello != null ? s.TipoCabello.Nombre : null))
                .ForMember(d => d.TipoRostroNombre, opt => opt.MapFrom(s => s.TipoRostro != null ? s.TipoRostro.Nombre : null))
                .ForMember(d => d.EsCumpleaneroDelMes, opt => opt.MapFrom(s => s.FechaNacimiento.Month == System.DateTime.UtcNow.Month));
            CreateMap<ClienteRequestDto, Cliente>();

            // 2. Módulo Turnos y Agenda
            CreateMap<Turno, TurnoResponseDto>()
                .ForMember(d => d.ClienteNombre, opt => opt.MapFrom(s => s.Cliente != null ? $"{s.Cliente.Nombre} {s.Cliente.Apellido}".Trim() : string.Empty))
                .ForMember(d => d.PeluqueroNombre, opt => opt.MapFrom(s => s.Peluquero != null ? $"{s.Peluquero.Nombre} {s.Peluquero.Apellido}".Trim() : string.Empty))
                .ForMember(d => d.EstadoNombre, opt => opt.MapFrom(s => s.Estado != null ? s.Estado.Nombre : string.Empty))
                .ForMember(d => d.Servicios, opt => opt.MapFrom(s => s.TurnoServicios != null ? s.TurnoServicios.Where(ts => ts.Servicio != null).Select(ts => ts.Servicio!.Nombre).ToList() : new System.Collections.Generic.List<string>()));
            CreateMap<CrearTurnoDto, Turno>();
            CreateMap<BloqueoHorarioDto, BloqueoHorario>();

            // 3. Módulo Servicios y Staff Profesional
            CreateMap<Servicio, ServicioDto>()
                .ForMember(d => d.CategoriaNombre, opt => opt.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : string.Empty));
            CreateMap<CrearServicioDto, Servicio>();
            CreateMap<CrearPeluqueroDto, Peluquero>();
            CreateMap<Peluquero, PeluqueroDto>()
                .ForMember(d => d.Especialidades, opt => opt.MapFrom(s => s.PeluqueroEspecialidades != null ? s.PeluqueroEspecialidades.Where(pe => pe.Especialidad != null).Select(pe => pe.Especialidad!.Nombre).ToList() : new System.Collections.Generic.List<string>()));

            // 4. Módulo Stock e Insumos
            CreateMap<Producto, ProductoDto>()
                .ForMember(d => d.CategoriaNombre, opt => opt.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : string.Empty));
            CreateMap<CrearProductoDto, Producto>()
                .ForMember(d => d.StockActual, opt => opt.MapFrom(s => s.StockInicial));
            CreateMap<MovimientoStockDto, MovimientoStock>();

            // 5. Módulo Ventas y Caja
            CreateMap<DetalleVentaRequestDto, DetalleVenta>();
            CreateMap<PagoRequestDto, Pago>();
        }
    }
}