using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.SaaS;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.SaaS;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // 0. Módulo Common, Catálogos, Seguridad y SaaS
            CreateMap<ConfiguracionSalon, ConfiguracionSalonDto>()
                .ForMember(d => d.Moneda, opt => opt.MapFrom(s => s.MonedaIso))
                .ReverseMap();
            CreateMap<ActualizarConfiguracionSalonDto, ConfiguracionSalon>();

            CreateMap<TipoCabello, CatalogoItemDto>();
            CreateMap<TipoRostro, CatalogoItemDto>();
            CreateMap<CategoriaServicio, CatalogoItemDto>();
            CreateMap<Especialidad, CatalogoItemDto>();
            CreateMap<EstadoTurno, CatalogoItemDto>();
            CreateMap<MotivoCancelacion, CatalogoItemDto>();
            CreateMap<CategoriaProducto, CatalogoItemDto>();
            CreateMap<MedioPago, CatalogoItemDto>();
            CreateMap<Rol, CatalogoItemDto>();

            CreateMap<Usuario, UsuarioResponseDto>()
                .ForMember(d => d.RolNombre, opt => opt.MapFrom(s => s.Rol != null ? s.Rol.Nombre : string.Empty));
            CreateMap<HistorialSesion, HistorialSesionResponseDto>()
                .ForMember(d => d.Username, opt => opt.MapFrom(s => s.Usuario != null ? s.Usuario.Username : string.Empty))
                .ForMember(d => d.FechaHora, opt => opt.MapFrom(s => s.FechaIntento))
                .ForMember(d => d.IpOrigen, opt => opt.MapFrom(s => s.DireccionIP))
                .ForMember(d => d.MotivoFallo, opt => opt.MapFrom(s => s.MotivoFallo != null ? s.MotivoFallo.ToString() : null));

            CreateMap<PlanSuscripcion, PlanSuscripcionResponseDto>();
            CreateMap<Sucursal, SucursalResponseDto>();
            CreateMap<CrearSucursalDto, Sucursal>();

            // 1. Módulo Clientes y Fichas Técnicas
            CreateMap<Cliente, ClienteResponseDto>()
                .ForMember(d => d.NombreCompleto, opt => opt.MapFrom(s => $"{s.Nombre} {s.Apellido}".Trim()))
                .ForMember(d => d.TipoCabelloNombre, opt => opt.MapFrom(s => s.TipoCabello != null ? s.TipoCabello.Nombre : null))
                .ForMember(d => d.TipoRostroNombre, opt => opt.MapFrom(s => s.TipoRostro != null ? s.TipoRostro.Nombre : null))
                .ForMember(d => d.EsCumpleaneroDelMes, opt => opt.MapFrom(s => s.FechaNacimiento.Month == DateTime.UtcNow.Month));
            CreateMap<ClienteRequestDto, Cliente>();

            CreateMap<FichaTecnica, FichaTecnicaResponseDto>()
                .ForMember(d => d.NombreCliente, opt => opt.MapFrom(s => s.Cliente != null ? $"{s.Cliente.Nombre} {s.Cliente.Apellido}".Trim() : string.Empty))
                .ForMember(d => d.NombrePeluquero, opt => opt.MapFrom(s => s.Peluquero != null ? $"{s.Peluquero.Nombre} {s.Peluquero.Apellido}".Trim() : string.Empty));
            CreateMap<CrearFichaTecnicaDto, FichaTecnica>();
            CreateMap<ActualizarFichaTecnicaDto, FichaTecnica>();

            // 2. Módulo Turnos y Agenda
            CreateMap<TurnoServicio, TurnoServicioDetalleDto>()
                .ForMember(d => d.ServicioNombre, opt => opt.MapFrom(s => s.Servicio != null ? s.Servicio.Nombre : string.Empty));

            CreateMap<Turno, TurnoResponseDto>()
                .ForMember(d => d.ClienteNombre, opt => opt.MapFrom(s => s.Cliente != null ? $"{s.Cliente.Nombre} {s.Cliente.Apellido}".Trim() : string.Empty))
                .ForMember(d => d.ClienteTelefono, opt => opt.MapFrom(s => s.Cliente != null ? s.Cliente.Telefono : null))
                .ForMember(d => d.PeluqueroNombre, opt => opt.MapFrom(s => s.Peluquero != null ? $"{s.Peluquero.Nombre} {s.Peluquero.Apellido}".Trim() : string.Empty))
                .ForMember(d => d.ColorAgendaHex, opt => opt.MapFrom(s => s.Peluquero != null ? s.Peluquero.ColorAgendaHex : "#3B82F6"))
                .ForMember(d => d.DuracionTotalMinutos, opt => opt.MapFrom(s => (int)Math.Max(0, (s.FechaHoraFin - s.FechaHoraInicio).TotalMinutes)))
                .ForMember(d => d.EstadoNombre, opt => opt.MapFrom(s => s.Estado != null ? s.Estado.Nombre : string.Empty))
                .ForMember(d => d.MotivoCancelacionDescripcion, opt => opt.MapFrom(s => s.MotivoCancelacion != null ? s.MotivoCancelacion.Descripcion : null))
                .ForMember(d => d.SaldoPendienteCobro, opt => opt.MapFrom(s => s.SenaPagada ? Math.Max(0m, s.MontoTotalEstimado - s.MontoSena) : s.MontoTotalEstimado))
                .ForMember(d => d.ServiciosIds, opt => opt.MapFrom(s => s.TurnoServicios != null ? s.TurnoServicios.Select(ts => ts.ServicioId).ToList() : new List<int>()))
                .ForMember(d => d.Servicios, opt => opt.MapFrom(s => s.TurnoServicios != null ? s.TurnoServicios.Where(ts => ts.Servicio != null).Select(ts => ts.Servicio!.Nombre).ToList() : new List<string>()))
                .ForMember(d => d.ServiciosDetalle, opt => opt.MapFrom(s => s.TurnoServicios != null ? s.TurnoServicios.ToList() : new List<TurnoServicio>()));
            CreateMap<CrearTurnoDto, Turno>();

            CreateMap<BloqueoHorarioDto, BloqueoHorario>();
            CreateMap<BloqueoHorario, BloqueoHorarioResponseDto>()
                .ForMember(d => d.PeluqueroNombre, opt => opt.MapFrom(s => s.Peluquero != null ? $"{s.Peluquero.Nombre} {s.Peluquero.Apellido}".Trim() : string.Empty));

            CreateMap<HistorialEstadoTurno, HistorialEstadoTurnoResponseDto>()
                .ForMember(d => d.EstadoAnteriorNombre, opt => opt.MapFrom(s => s.EstadoAnteriorId.HasValue ? ((EstadoTurnoEnum)s.EstadoAnteriorId.Value).ToString() : null))
                .ForMember(d => d.EstadoNuevoNombre, opt => opt.MapFrom(s => ((EstadoTurnoEnum)s.EstadoNuevoId).ToString()));

            CreateMap<HorarioLaboral, HorarioLaboralDto>()
                .ForMember(d => d.DiaSemana, opt => opt.MapFrom(s => (int)s.DiaSemana))
                .ReverseMap()
                .ForMember(d => d.DiaSemana, opt => opt.MapFrom(s => (DayOfWeek)s.DiaSemana));

            // 3. Módulo Servicios, Recetas y Staff Profesional
            CreateMap<Servicio, ServicioDto>()
                .ForMember(d => d.CategoriaNombre, opt => opt.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : string.Empty));
            CreateMap<CrearServicioDto, Servicio>();
            CreateMap<ActualizarServicioDto, Servicio>();

            CreateMap<ServicioInsumoReceta, RecetaItemResponseDto>()
                .ForMember(d => d.ServicioNombre, opt => opt.MapFrom(s => s.Servicio != null ? s.Servicio.Nombre : string.Empty))
                .ForMember(d => d.ProductoNombre, opt => opt.MapFrom(s => s.Producto != null ? s.Producto.Nombre : string.Empty))
                .ForMember(d => d.StockActualProducto, opt => opt.MapFrom(s => s.Producto != null ? s.Producto.StockActual : 0))
                .ForMember(d => d.CostoEstimadoInsumo, opt => opt.MapFrom(s => s.Producto != null ? s.Producto.PrecioCosto * s.CantidadUnidadesDescontar : 0m));

            CreateMap<CrearPeluqueroDto, Peluquero>();
            CreateMap<ActualizarPeluqueroDto, Peluquero>();
            CreateMap<Peluquero, PeluqueroDto>()
                .ForMember(d => d.Especialidades, opt => opt.MapFrom(s => s.PeluqueroEspecialidades != null ? s.PeluqueroEspecialidades.Where(pe => pe.Especialidad != null).Select(pe => pe.Especialidad!.Nombre).ToList() : new List<string>()));

            // 4. Módulo Stock, Proveedores y Órdenes de Compra
            CreateMap<Producto, ProductoDto>()
                .ForMember(d => d.CategoriaNombre, opt => opt.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : string.Empty));
            CreateMap<CrearProductoDto, Producto>()
                .ForMember(d => d.StockActual, opt => opt.MapFrom(s => s.StockInicial));
            CreateMap<ActualizarProductoDto, Producto>();
            CreateMap<MovimientoStockDto, MovimientoStock>();
            CreateMap<MovimientoStock, KardexItemResponseDto>()
                .ForMember(d => d.ProductoNombre, opt => opt.MapFrom(s => s.Producto != null ? s.Producto.Nombre : string.Empty))
                .ForMember(d => d.TipoMovimientoDescripcion, opt => opt.MapFrom(s => s.TipoMovimiento.ToString()));

            CreateMap<Proveedor, ProveedorDto>();
            CreateMap<CrearProveedorDto, Proveedor>();
            CreateMap<ActualizarProveedorDto, Proveedor>();

            CreateMap<OrdenCompra, OrdenCompraResponseDto>()
                .ForMember(d => d.ProveedorRazonSocial, opt => opt.MapFrom(s => s.Proveedor != null ? s.Proveedor.RazonSocial : string.Empty))
                .ForMember(d => d.EstadoNombre, opt => opt.MapFrom(s => s.Estado.ToString()));
            CreateMap<DetalleOrdenCompra, DetalleOrdenCompraResponseDto>()
                .ForMember(d => d.ProductoNombre, opt => opt.MapFrom(s => s.Producto != null ? s.Producto.Nombre : string.Empty));

            // 5. Módulo Ventas, Caja, Comisiones y Liquidaciones
            CreateMap<SesionCaja, SesionCajaResponseDto>()
                .ForMember(d => d.MontoFinalEsperado, opt => opt.MapFrom(s => s.MontoFinalEsperado ?? (s.MontoInicial + s.TotalVentasEfectivo + s.TotalIngresosManuales - s.TotalEgresosManuales)))
                .ForMember(d => d.Movimientos, opt => opt.MapFrom(s => s.MovimientosCaja != null ? s.MovimientosCaja.Where(m => !m.IsDeleted).ToList() : new List<MovimientoCaja>()));
            CreateMap<MovimientoCaja, MovimientoCajaResponseDto>();

            CreateMap<DetalleVentaRequestDto, DetalleVenta>();
            CreateMap<PagoRequestDto, Pago>();
            CreateMap<Pago, VentaPagoItemResponseDto>()
                .ForMember(d => d.MedioPagoNombre, opt => opt.MapFrom(s => s.MedioPago != null ? s.MedioPago.Nombre : string.Empty));
            CreateMap<Venta, VentaResponseDto>()
                .ForMember(d => d.ClienteNombre, opt => opt.MapFrom(s => s.Cliente != null ? $"{s.Cliente.Nombre} {s.Cliente.Apellido}".Trim() : "Consumidor Final"));

            CreateMap<ComisionesConfig, ComisionConfigDto>()
                .ForMember(d => d.NombrePeluquero, opt => opt.MapFrom(s => s.Peluquero != null ? $"{s.Peluquero.Nombre} {s.Peluquero.Apellido}".Trim() : string.Empty))
                .ForMember(d => d.NombreServicio, opt => opt.MapFrom(s => s.Servicio != null ? s.Servicio.Nombre : "Esquema General (Todos los servicios)"));
            CreateMap<GuardarComisionConfigDto, ComisionesConfig>();

            CreateMap<Liquidacion, LiquidacionResponseDto>()
                .ForMember(d => d.PeluqueroNombre, opt => opt.MapFrom(s => s.Peluquero != null ? $"{s.Peluquero.Nombre} {s.Peluquero.Apellido}".Trim() : string.Empty))
                .ForMember(d => d.Estado, opt => opt.MapFrom(s => s.Estado.ToString()))
                .ForMember(d => d.CantidadItemsLiquidados, opt => opt.MapFrom(s => s.DetallesLiquidacion != null ? s.DetallesLiquidacion.Count : 0))
                .ForMember(d => d.Detalles, opt => opt.MapFrom(s => s.DetallesLiquidacion));
            CreateMap<DetalleLiquidacion, DetalleLiquidacionItemDto>();
        }
    }
}