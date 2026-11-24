using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.SaaS;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Data.Context
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantProvider? _tenantProvider;

        public int CurrentTenantId => _tenantProvider is { CurrentTenantId: > 0 } ? _tenantProvider.CurrentTenantId : 1;
        public int? CurrentSucursalId => _tenantProvider?.CurrentSucursalId;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ITenantProvider? tenantProvider = null) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<TipoCabello> TiposCabello => Set<TipoCabello>();
        public DbSet<TipoRostro> TiposRostro => Set<TipoRostro>();
        public DbSet<FichaTecnica> FichasTecnicas => Set<FichaTecnica>();

        public DbSet<Peluquero> Peluqueros => Set<Peluquero>();
        public DbSet<Especialidad> Especialidades => Set<Especialidad>();
        public DbSet<PeluqueroEspecialidad> PeluqueroEspecialidades => Set<PeluqueroEspecialidad>();
        public DbSet<HorarioLaboral> HorariosLaborales => Set<HorarioLaboral>();
        public DbSet<CategoriaServicio> CategoriasServicio => Set<CategoriaServicio>();
        public DbSet<Servicio> Servicios => Set<Servicio>();
        public DbSet<ServicioInsumoReceta> ServiciosInsumosRecetas => Set<ServicioInsumoReceta>();
        public DbSet<HistorialPrecioServicio> HistorialPreciosServicio => Set<HistorialPrecioServicio>();
        public DbSet<EstadoTurno> EstadosTurno => Set<EstadoTurno>();
        public DbSet<MotivoCancelacion> MotivosCancelacion => Set<MotivoCancelacion>();
        public DbSet<BloqueoHorario> BloqueosHorario => Set<BloqueoHorario>();
        public DbSet<Turno> Turnos => Set<Turno>();
        public DbSet<TurnoServicio> TurnoServicios => Set<TurnoServicio>();
        public DbSet<HistorialEstadoTurno> HistorialEstadoTurnos => Set<HistorialEstadoTurno>();

        public DbSet<CategoriaProducto> CategoriasProducto => Set<CategoriaProducto>();
        public DbSet<Proveedor> Proveedores => Set<Proveedor>();
        public DbSet<Producto> Productos => Set<Producto>();
        public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
        public DbSet<HistorialPrecioProducto> HistorialPreciosProducto => Set<HistorialPrecioProducto>();
        public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
        public DbSet<DetalleOrdenCompra> DetallesOrdenCompra => Set<DetalleOrdenCompra>();

        public DbSet<SesionCaja> SesionesCaja => Set<SesionCaja>();
        public DbSet<Venta> Ventas => Set<Venta>();
        public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();
        public DbSet<MedioPago> MediosPago => Set<MedioPago>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<MovimientoCaja> MovimientosCaja => Set<MovimientoCaja>();

        public DbSet<ComisionesConfig> ComisionesConfigs => Set<ComisionesConfig>();
        public DbSet<Liquidacion> Liquidaciones => Set<Liquidacion>();
        public DbSet<DetalleLiquidacion> DetallesLiquidacion => Set<DetalleLiquidacion>();

        public DbSet<Rol> Roles => Set<Rol>();
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<HistorialSesion> HistorialSesiones => Set<HistorialSesion>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<ConfiguracionSalon> ConfiguracionSalon => Set<ConfiguracionSalon>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Sucursal> Sucursales => Set<Sucursal>();
        public DbSet<PlanSuscripcion> PlanesSuscripcion => Set<PlanSuscripcion>();
        public DbSet<SuscripcionTenant> SuscripcionesTenant => Set<SuscripcionTenant>();

        private void ApplyTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : AuditableTenantEntity
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
                !e.IsDeleted &&
                e.TenantId == CurrentTenantId &&
                (CurrentSucursalId == null || e.SucursalId == null || e.SucursalId == CurrentSucursalId));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // ========================================================================
            // 1. FILTROS GLOBALES: ENTIDADES DE PLATAFORMA / CATÁLOGO FIJO (6 TABLAS)
            // ========================================================================
            modelBuilder.Entity<Rol>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<EstadoTurno>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Sucursal>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PlanSuscripcion>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<SuscripcionTenant>().HasQueryFilter(e => !e.IsDeleted);

            // ========================================================================
            // 2. FILTROS GLOBALES MULTI-TENANT POR LOCAL (ENTIDADES OPERATIVAS)
            // ========================================================================
            ApplyTenantQueryFilter<Cliente>(modelBuilder);
            ApplyTenantQueryFilter<TipoCabello>(modelBuilder);
            ApplyTenantQueryFilter<TipoRostro>(modelBuilder);
            ApplyTenantQueryFilter<FichaTecnica>(modelBuilder);

            ApplyTenantQueryFilter<Peluquero>(modelBuilder);
            ApplyTenantQueryFilter<Especialidad>(modelBuilder);
            ApplyTenantQueryFilter<PeluqueroEspecialidad>(modelBuilder);
            ApplyTenantQueryFilter<HorarioLaboral>(modelBuilder);
            ApplyTenantQueryFilter<CategoriaServicio>(modelBuilder);
            ApplyTenantQueryFilter<Servicio>(modelBuilder);
            ApplyTenantQueryFilter<ServicioInsumoReceta>(modelBuilder);
            ApplyTenantQueryFilter<HistorialPrecioServicio>(modelBuilder);
            ApplyTenantQueryFilter<MotivoCancelacion>(modelBuilder);
            ApplyTenantQueryFilter<BloqueoHorario>(modelBuilder);
            ApplyTenantQueryFilter<Turno>(modelBuilder);
            ApplyTenantQueryFilter<TurnoServicio>(modelBuilder);
            ApplyTenantQueryFilter<HistorialEstadoTurno>(modelBuilder);

            ApplyTenantQueryFilter<CategoriaProducto>(modelBuilder);
            ApplyTenantQueryFilter<Proveedor>(modelBuilder);
            ApplyTenantQueryFilter<Producto>(modelBuilder);
            ApplyTenantQueryFilter<MovimientoStock>(modelBuilder);
            ApplyTenantQueryFilter<HistorialPrecioProducto>(modelBuilder);
            ApplyTenantQueryFilter<OrdenCompra>(modelBuilder);
            ApplyTenantQueryFilter<DetalleOrdenCompra>(modelBuilder);

            ApplyTenantQueryFilter<SesionCaja>(modelBuilder);
            ApplyTenantQueryFilter<Venta>(modelBuilder);
            ApplyTenantQueryFilter<DetalleVenta>(modelBuilder);
            ApplyTenantQueryFilter<MedioPago>(modelBuilder);
            ApplyTenantQueryFilter<Pago>(modelBuilder);
            ApplyTenantQueryFilter<MovimientoCaja>(modelBuilder);
            ApplyTenantQueryFilter<ComisionesConfig>(modelBuilder);
            ApplyTenantQueryFilter<Liquidacion>(modelBuilder);
            ApplyTenantQueryFilter<DetalleLiquidacion>(modelBuilder);

            ApplyTenantQueryFilter<Usuario>(modelBuilder);
            ApplyTenantQueryFilter<HistorialSesion>(modelBuilder);
            ApplyTenantQueryFilter<RefreshToken>(modelBuilder);

            ApplyTenantQueryFilter<ConfiguracionSalon>(modelBuilder);
            ApplyTenantQueryFilter<AuditLog>(modelBuilder);

            // ========================================================================
            // 3. DATA SEEDING DETERMINISTA (FECHAS ESTÁTICAS UTC + HASH BCRYPT FIJO)
            // ========================================================================
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Rol>().HasData(
                new Rol { Id = 1, Nombre = "Administrador", CreatedAt = seedDate, IsDeleted = false },
                new Rol { Id = 2, Nombre = "Recepcionista", CreatedAt = seedDate, IsDeleted = false },
                new Rol { Id = 3, Nombre = "Peluquero", CreatedAt = seedDate, IsDeleted = false },
                new Rol { Id = 4, Nombre = "Cliente", CreatedAt = seedDate, IsDeleted = false },
                new Rol { Id = 5, Nombre = "Cajero", CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<EstadoTurno>().HasData(
                new EstadoTurno { Id = 1, Nombre = "Pendiente", CreatedAt = seedDate, IsDeleted = false },
                new EstadoTurno { Id = 2, Nombre = "Confirmado", CreatedAt = seedDate, IsDeleted = false },
                new EstadoTurno { Id = 3, Nombre = "En Proceso", CreatedAt = seedDate, IsDeleted = false },
                new EstadoTurno { Id = 4, Nombre = "Completado", CreatedAt = seedDate, IsDeleted = false },
                new EstadoTurno { Id = 5, Nombre = "Cancelado", CreatedAt = seedDate, IsDeleted = false },
                new EstadoTurno { Id = 6, Nombre = "No Asistió", CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<PlanSuscripcion>().HasData(
                new PlanSuscripcion
                {
                    Id = 1,
                    Nombre = "Starter",
                    PrecioMensualUsd = 19.99m,
                    MaxSucursales = 1,
                    MaxPeluqueros = 3,
                    IncluyeWhatsAppRecordatorios = false,
                    IncluyePortalReservasOnline = true,
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new PlanSuscripcion
                {
                    Id = 2,
                    Nombre = "Professional",
                    PrecioMensualUsd = 39.99m,
                    MaxSucursales = 3,
                    MaxPeluqueros = 15,
                    IncluyeWhatsAppRecordatorios = true,
                    IncluyePortalReservasOnline = true,
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false
                },
                new PlanSuscripcion
                {
                    Id = 3,
                    Nombre = "Enterprise",
                    PrecioMensualUsd = 79.99m,
                    MaxSucursales = 99,
                    MaxPeluqueros = 999,
                    IncluyeWhatsAppRecordatorios = true,
                    IncluyePortalReservasOnline = true,
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );

            modelBuilder.Entity<Tenant>().HasData(
                new Tenant
                {
                    Id = 1,
                    NombreComercial = "Mi Peluquería Central",
                    SubdominioSlug = "mi-peluqueria-central",
                    IdentificacionFiscal = "30-71234567-9",
                    EmailPropietario = "admin@tesis.com",
                    PaisIso = "AR",
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );

            modelBuilder.Entity<Sucursal>().HasData(
                new Sucursal
                {
                    Id = 1,
                    TenantId = 1,
                    Nombre = "Sucursal Casa Matriz (Córdoba)",
                    Direccion = "Av. Colón 500, Córdoba",
                    Telefono = "+54 9 351 000-0000",
                    TimeZoneId = "America/Argentina/Cordoba",
                    MonedaIso = "ARS",
                    EsCasaMatriz = true,
                    Activa = true,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );

            modelBuilder.Entity<SuscripcionTenant>().HasData(
                new SuscripcionTenant
                {
                    Id = 1,
                    TenantId = 1,
                    PlanSuscripcionId = 2,
                    FechaInicio = seedDate,
                    FechaProximoVencimiento = new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Estado = EstadoSuscripcionEnum.Activa,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );

            modelBuilder.Entity<ConfiguracionSalon>().HasData(
                new ConfiguracionSalon
                {
                    Id = 1,
                    TenantId = 1,
                    SucursalId = 1,
                    NombreSalon = "Mi Peluquería Central",
                    NombreComercial = "Mi Peluquería Central",
                    RazonSocial = "Mi Peluquería S.A.",
                    Cuit = "30-71234567-9",
                    Direccion = "Av. Colón 500, Córdoba",
                    Telefono = "+54 9 351 000-0000",
                    EmailContacto = "admin@tesis.com",
                    MonedaIso = "ARS",
                    TimeZoneId = "America/Argentina/Cordoba",
                    HorarioAtencionTexto = "Lunes a Sábados de 09:00 a 20:00 hs",
                    IntervaloTurnosMinutos = 30,
                    MinutosToleranciaEspera = 15,
                    HorasAnticipacionCancelacion = 24,
                    PorcentajeSenaReserva = 0.00m,
                    NotificacionesWhatsAppActivas = true,
                    NotificacionesEmailActivas = true,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    TenantId = 1,
                    SucursalId = 1,
                    RolId = 1,
                    Username = "admin",
                    // Hash BCrypt estático verificado para "Admin123!"
                    PasswordHash = "$2a$11$r4llG6xDC0cihMfzpC0/sOKZSMe1jQTVy01fKSfCkcq70nhWJHVhK",
                    Email = "admin@tesis.com",
                    EmailVerificado = true,
                    AceptaTerminos = true,
                    FechaAceptacionTerminos = seedDate,
                    IntentosFallidos = 0,
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false,
                    RowVersion = null!
                }
            );

            modelBuilder.Entity<MedioPago>().HasData(
                new MedioPago { Id = 1, TenantId = 1, SucursalId = 1, Nombre = "Efectivo", Descripcion = "Dinero físico en cajón", EsEfectivoFisico = true, CreatedAt = seedDate, IsDeleted = false },
                new MedioPago { Id = 2, TenantId = 1, SucursalId = 1, Nombre = "Tarjeta de Débito", Descripcion = "Cobro electrónico por terminal débito", EsEfectivoFisico = false, CreatedAt = seedDate, IsDeleted = false },
                new MedioPago { Id = 3, TenantId = 1, SucursalId = 1, Nombre = "Tarjeta de Crédito", Descripcion = "Cobro electrónico por terminal crédito", EsEfectivoFisico = false, CreatedAt = seedDate, IsDeleted = false },
                new MedioPago { Id = 4, TenantId = 1, SucursalId = 1, Nombre = "Transferencia / Alias", Descripcion = "Transferencia bancaria o CVU", EsEfectivoFisico = false, CreatedAt = seedDate, IsDeleted = false },
                new MedioPago { Id = 5, TenantId = 1, SucursalId = 1, Nombre = "Mercado Pago QR", Descripcion = "Pago digital mediante QR Mercado Pago", EsEfectivoFisico = false, CreatedAt = seedDate, IsDeleted = false },
                new MedioPago { Id = 6, TenantId = 1, SucursalId = 1, Nombre = "Billetera Virtual (Otra)", Descripcion = "MODO u otra billetera digital", EsEfectivoFisico = false, CreatedAt = seedDate, IsDeleted = false },
                new MedioPago { Id = 7, TenantId = 1, SucursalId = 1, Nombre = "Canje / Cortesía", Descripcion = "Atención bonificada sin ingreso en caja", EsEfectivoFisico = false, CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<MotivoCancelacion>().HasData(
                new MotivoCancelacion { Id = 1, TenantId = 1, SucursalId = 1, Descripcion = "Cancelación con aviso previo del cliente", CreatedAt = seedDate, IsDeleted = false },
                new MotivoCancelacion { Id = 2, TenantId = 1, SucursalId = 1, Descripcion = "Ausencia o indisposición del profesional", CreatedAt = seedDate, IsDeleted = false },
                new MotivoCancelacion { Id = 3, TenantId = 1, SucursalId = 1, Descripcion = "Reprogramación solicitada por recepción", CreatedAt = seedDate, IsDeleted = false },
                new MotivoCancelacion { Id = 4, TenantId = 1, SucursalId = 1, Descripcion = "Fuerza mayor / Feriado / Corte de suministro", CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<TipoCabello>().HasData(
                new TipoCabello { Id = 1, TenantId = 1, SucursalId = 1, Nombre = "Lacio", Descripcion = "Cabello sin ondas ni rulos naturales", CreatedAt = seedDate, IsDeleted = false },
                new TipoCabello { Id = 2, TenantId = 1, SucursalId = 1, Nombre = "Ondulado", Descripcion = "Cabello con ondas suaves en medios y puntas", CreatedAt = seedDate, IsDeleted = false },
                new TipoCabello { Id = 3, TenantId = 1, SucursalId = 1, Nombre = "Rizado", Descripcion = "Cabello con rulos definidos", CreatedAt = seedDate, IsDeleted = false },
                new TipoCabello { Id = 4, TenantId = 1, SucursalId = 1, Nombre = "Afro / Muy Rizado", Descripcion = "Cabello con rizo muy cerrado y alta porosidad", CreatedAt = seedDate, IsDeleted = false },
                new TipoCabello { Id = 5, TenantId = 1, SucursalId = 1, Nombre = "Fino / Quebradizo", Descripcion = "Fibra capilar delgada que requiere cuidado térmico", CreatedAt = seedDate, IsDeleted = false },
                new TipoCabello { Id = 6, TenantId = 1, SucursalId = 1, Nombre = "Graso / Mixto", Descripcion = "Raíz con tendencia oleosa y puntas secas", CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<TipoRostro>().HasData(
                new TipoRostro { Id = 1, TenantId = 1, SucursalId = 1, Nombre = "Ovalado", Descripcion = "Proporciones equilibradas y simétricas", CreatedAt = seedDate, IsDeleted = false },
                new TipoRostro { Id = 2, TenantId = 1, SucursalId = 1, Nombre = "Redondo", Descripcion = "Pómulos anchos y contornos suaves", CreatedAt = seedDate, IsDeleted = false },
                new TipoRostro { Id = 3, TenantId = 1, SucursalId = 1, Nombre = "Cuadrado", Descripcion = "Mandíbula y frente marcadas del mismo ancho", CreatedAt = seedDate, IsDeleted = false },
                new TipoRostro { Id = 4, TenantId = 1, SucursalId = 1, Nombre = "Alargado / Rectangular", Descripcion = "Mayor longitud vertical que ancho facial", CreatedAt = seedDate, IsDeleted = false },
                new TipoRostro { Id = 5, TenantId = 1, SucursalId = 1, Nombre = "Corazón / Diamante", Descripcion = "Pómulos destacados y mentón fino", CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<Cliente>().HasData(
                new Cliente
                {
                    Id = 1,
                    TenantId = 1,
                    SucursalId = 1,
                    Nombre = "Juan",
                    Apellido = "Pérez",
                    Telefono = "+5493516000001",
                    Email = "juan.perez@cliente.com",
                    FechaNacimiento = new DateTime(1995, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                    FechaRegistro = seedDate,
                    Activo = true,
                    TipoCabelloId = 1,
                    TipoRostroId = 1,
                    CreatedAt = seedDate,
                    IsDeleted = false,
                    RowVersion = null!
                }
            );

            modelBuilder.Entity<Especialidad>().HasData(
                new Especialidad { Id = 1, TenantId = 1, SucursalId = 1, Nombre = "Corte y Estilismo", Descripcion = "Cortes clásicos, modernos y diseño personalizado", CreatedAt = seedDate, IsDeleted = false },
                new Especialidad { Id = 2, TenantId = 1, SucursalId = 1, Nombre = "Colorimetría y Mechas", Descripcion = "Balayage, tintura global, reflejos y decoloración", CreatedAt = seedDate, IsDeleted = false },
                new Especialidad { Id = 3, TenantId = 1, SucursalId = 1, Nombre = "Barbería Clásica y Fade", Descripcion = "Perfilado de barba con navaja y degradados", CreatedAt = seedDate, IsDeleted = false },
                new Especialidad { Id = 4, TenantId = 1, SucursalId = 1, Nombre = "Tratamientos Capilares", Descripcion = "Hidratación, nutrición, cauterización y keratina", CreatedAt = seedDate, IsDeleted = false },
                new Especialidad { Id = 5, TenantId = 1, SucursalId = 1, Nombre = "Peinados y Eventos", Descripcion = "Brushing, ondas y recogidos sociales", CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<CategoriaServicio>().HasData(
                new CategoriaServicio { Id = 1, TenantId = 1, SucursalId = 1, Nombre = "Cortes y Barbería", Descripcion = "Servicios de corte de cabello y arreglo de barba", CreatedAt = seedDate, IsDeleted = false },
                new CategoriaServicio { Id = 2, TenantId = 1, SucursalId = 1, Nombre = "Colorimetría", Descripcion = "Servicios técnicos de color y decoloración", CreatedAt = seedDate, IsDeleted = false },
                new CategoriaServicio { Id = 3, TenantId = 1, SucursalId = 1, Nombre = "Tratamientos y Nutrición", Descripcion = "Recuperación y salud de la fibra capilar", CreatedAt = seedDate, IsDeleted = false },
                new CategoriaServicio { Id = 4, TenantId = 1, SucursalId = 1, Nombre = "Peinados y Styling", Descripcion = "Modelado y acabado profesional", CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<CategoriaProducto>().HasData(
                new CategoriaProducto { Id = 1, TenantId = 1, SucursalId = 1, Nombre = "Shampoos y Acondicionadores", Descripcion = "Línea de lavado profesional y reventa", CreatedAt = seedDate, IsDeleted = false },
                new CategoriaProducto { Id = 2, TenantId = 1, SucursalId = 1, Nombre = "Fijación, Ceras y Styling", Descripcion = "Pomadas, ceras mate, geles y lacas", CreatedAt = seedDate, IsDeleted = false },
                new CategoriaProducto { Id = 3, TenantId = 1, SucursalId = 1, Nombre = "Tinturas y Oxidantes (Gabinete)", Descripcion = "Insumos técnicos de uso interno en salón", CreatedAt = seedDate, IsDeleted = false },
                new CategoriaProducto { Id = 4, TenantId = 1, SucursalId = 1, Nombre = "Tratamientos y Ampollas", Descripcion = "Máscaras nutritivas, óleos y ampollas capilares", CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<Peluquero>().HasData(
                new Peluquero
                {
                    Id = 1,
                    TenantId = 1,
                    SucursalId = 1,
                    Nombre = "Carlos",
                    Apellido = "Gómez",
                    Telefono = "+54 9 351 555-0101",
                    Email = "carlos.gomez@tesis.com",
                    ColorAgendaHex = "#3B82F6",
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false,
                    RowVersion = null!
                }
            );

            modelBuilder.Entity<PeluqueroEspecialidad>().HasData(
                new PeluqueroEspecialidad { Id = 1, TenantId = 1, SucursalId = 1, PeluqueroId = 1, EspecialidadId = 1, CreatedAt = seedDate, IsDeleted = false },
                new PeluqueroEspecialidad { Id = 2, TenantId = 1, SucursalId = 1, PeluqueroId = 1, EspecialidadId = 3, CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<HorarioLaboral>().HasData(
                new HorarioLaboral { Id = 1, TenantId = 1, SucursalId = 1, PeluqueroId = 1, DiaSemana = DayOfWeek.Monday, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(20, 0, 0), HoraInicioDescanso = new TimeSpan(13, 0, 0), HoraFinDescanso = new TimeSpan(14, 0, 0), Activo = true, CreatedAt = seedDate, IsDeleted = false },
                new HorarioLaboral { Id = 2, TenantId = 1, SucursalId = 1, PeluqueroId = 1, DiaSemana = DayOfWeek.Tuesday, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(20, 0, 0), HoraInicioDescanso = new TimeSpan(13, 0, 0), HoraFinDescanso = new TimeSpan(14, 0, 0), Activo = true, CreatedAt = seedDate, IsDeleted = false },
                new HorarioLaboral { Id = 3, TenantId = 1, SucursalId = 1, PeluqueroId = 1, DiaSemana = DayOfWeek.Wednesday, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(20, 0, 0), HoraInicioDescanso = new TimeSpan(13, 0, 0), HoraFinDescanso = new TimeSpan(14, 0, 0), Activo = true, CreatedAt = seedDate, IsDeleted = false },
                new HorarioLaboral { Id = 4, TenantId = 1, SucursalId = 1, PeluqueroId = 1, DiaSemana = DayOfWeek.Thursday, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(20, 0, 0), HoraInicioDescanso = new TimeSpan(13, 0, 0), HoraFinDescanso = new TimeSpan(14, 0, 0), Activo = true, CreatedAt = seedDate, IsDeleted = false },
                new HorarioLaboral { Id = 5, TenantId = 1, SucursalId = 1, PeluqueroId = 1, DiaSemana = DayOfWeek.Friday, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(20, 0, 0), HoraInicioDescanso = new TimeSpan(13, 0, 0), HoraFinDescanso = new TimeSpan(14, 0, 0), Activo = true, CreatedAt = seedDate, IsDeleted = false },
                new HorarioLaboral { Id = 6, TenantId = 1, SucursalId = 1, PeluqueroId = 1, DiaSemana = DayOfWeek.Saturday, HoraInicio = new TimeSpan(9, 0, 0), HoraFin = new TimeSpan(20, 0, 0), HoraInicioDescanso = new TimeSpan(13, 0, 0), HoraFinDescanso = new TimeSpan(14, 0, 0), Activo = true, CreatedAt = seedDate, IsDeleted = false }
            );

            modelBuilder.Entity<Servicio>().HasData(
                new Servicio
                {
                    Id = 1,
                    TenantId = 1,
                    SucursalId = 1,
                    CategoriaId = 1,
                    Nombre = "Corte Clásico / Fade",
                    Descripcion = "Corte personalizado con máquina y tijera, incluye lavado.",
                    Precio = 12000.00m,
                    DuracionMinutos = 30,
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false,
                    RowVersion = null!
                },
                new Servicio
                {
                    Id = 2,
                    TenantId = 1,
                    SucursalId = 1,
                    CategoriaId = 2,
                    Nombre = "Coloración Global / Tintura",
                    Descripcion = "Aplicación de color completo con productos profesionales.",
                    Precio = 28000.00m,
                    DuracionMinutos = 90,
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false,
                    RowVersion = null!
                },
                new Servicio
                {
                    Id = 3,
                    TenantId = 1,
                    SucursalId = 1,
                    CategoriaId = 3,
                    Nombre = "Nutrición Capilar Intensiva",
                    Descripcion = "Baño de crema e hidratación profunda.",
                    Precio = 18000.00m,
                    DuracionMinutos = 45,
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false,
                    RowVersion = null!
                }
            );

            modelBuilder.Entity<ComisionesConfig>().HasData(
                new ComisionesConfig
                {
                    Id = 1,
                    TenantId = 1,
                    SucursalId = 1,
                    PeluqueroId = 1,
                    ServicioId = null,
                    PorcentajeServicio = 50.00m,
                    PorcentajeVentaProducto = 10.00m,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );

            modelBuilder.Entity<Proveedor>().HasData(
                new Proveedor
                {
                    Id = 1,
                    TenantId = 1,
                    SucursalId = 1,
                    RazonSocial = "Distribuidora Capilar Córdoba S.R.L.",
                    Cuit = "30-70998877-1",
                    Contacto = "Laura Martínez",
                    Telefono = "+54 9 351 444-9900",
                    Email = "ventas@distribuidoracapilar.com",
                    CreatedAt = seedDate,
                    IsDeleted = false,
                    RowVersion = null!
                }
            );

            modelBuilder.Entity<Producto>().HasData(
                new Producto
                {
                    Id = 1,
                    TenantId = 1,
                    SucursalId = 1,
                    CategoriaId = 2,
                    ProveedorId = 1,
                    CodigoBarras = "7790001000011",
                    Nombre = "Cera Modeladora Mate 100g",
                    PrecioCosto = 4500.00m,
                    PrecioVenta = 9500.00m,
                    StockActual = 20,
                    StockMinimoAlerta = 5,
                    EsParaVenta = true,
                    CreatedAt = seedDate,
                    IsDeleted = false,
                    RowVersion = null!
                },
                new Producto
                {
                    Id = 2,
                    TenantId = 1,
                    SucursalId = 1,
                    CategoriaId = 3,
                    ProveedorId = 1,
                    CodigoBarras = "7790001000028",
                    Nombre = "Pomo Tintura Profesional 60g (Insumo)",
                    PrecioCosto = 3800.00m,
                    PrecioVenta = 0.00m,
                    StockActual = 15,
                    StockMinimoAlerta = 5,
                    EsParaVenta = false,
                    CreatedAt = seedDate,
                    IsDeleted = false,
                    RowVersion = null!
                }
            );

            modelBuilder.Entity<ServicioInsumoReceta>().HasData(
                new ServicioInsumoReceta
                {
                    Id = 1,
                    TenantId = 1,
                    SucursalId = 1,
                    ServicioId = 2,
                    ProductoId = 2,
                    CantidadUnidadesDescontar = 1,
                    CantidadMedidaEstandar = 60.00m,
                    UnidadMedida = "gr",
                    DescuentoAutomaticoAlCompletarTurno = true,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var auditLogsPendientes = new List<AuditLog>();
            var ahoraUtc = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>().ToList())
            {
                bool esEntidadAuditable = entry.Entity is not AuditLog && entry.Entity is not HistorialSesion;
                int entidadTenantId = CurrentTenantId;
                int? entidadSucursalId = CurrentSucursalId ?? 1;

                if (entry.Entity is ITenantEntity tenantEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        if (_tenantProvider != null && _tenantProvider.CurrentTenantId > 0)
                        {
                            if (tenantEntity.TenantId <= 0 || tenantEntity.TenantId == 1)
                                tenantEntity.TenantId = _tenantProvider.CurrentTenantId;

                            if (_tenantProvider.CurrentSucursalId.HasValue &&
                                _tenantProvider.CurrentSucursalId.Value > 0 &&
                                (tenantEntity.SucursalId == null || tenantEntity.SucursalId <= 0 || tenantEntity.SucursalId == 1))
                            {
                                tenantEntity.SucursalId = _tenantProvider.CurrentSucursalId.Value;
                            }
                        }

                        if (tenantEntity.TenantId <= 0)
                            tenantEntity.TenantId = CurrentTenantId;

                        if (tenantEntity.SucursalId == null || tenantEntity.SucursalId <= 0)
                            tenantEntity.SucursalId = CurrentSucursalId ?? 1;
                    }

                    entidadTenantId = tenantEntity.TenantId;
                    entidadSucursalId = tenantEntity.SucursalId;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = ahoraUtc;
                        entry.Entity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = ahoraUtc;
                        entry.Entity.UpdatedByUserId ??= entry.Entity.CreatedByUserId;

                        if (entry.Entity.IsDeleted)
                        {
                            entry.Entity.DeletedAt ??= ahoraUtc;
                            entry.Entity.DeletedByUserId ??= entry.Entity.UpdatedByUserId ?? entry.Entity.CreatedByUserId;
                        }

                        if (esEntidadAuditable)
                        {
                            var valoresAnteriores = new Dictionary<string, object?>();
                            var valoresNuevos = new Dictionary<string, object?>();

                            foreach (var prop in entry.Properties)
                            {
                                if (prop.IsTemporary ||
                                    prop.Metadata.Name == nameof(Usuario.PasswordHash) ||
                                    prop.Metadata.Name == "RowVersion")
                                {
                                    continue;
                                }

                                if (prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                                {
                                    valoresAnteriores[prop.Metadata.Name] = prop.OriginalValue;
                                    valoresNuevos[prop.Metadata.Name] = prop.CurrentValue;
                                }
                            }

                            if (valoresNuevos.Count > 0)
                            {
                                string accion = (entry.Entity.IsDeleted && valoresNuevos.ContainsKey(nameof(AuditableEntity.IsDeleted)))
                                    ? "SOFT_DELETE"
                                    : "UPDATE";

                                auditLogsPendientes.Add(new AuditLog
                                {
                                    TenantId = entidadTenantId,
                                    SucursalId = entidadSucursalId,
                                    UsuarioId = entry.Entity.DeletedByUserId ?? entry.Entity.UpdatedByUserId ?? entry.Entity.CreatedByUserId,
                                    EntidadNombre = entry.Metadata.ClrType.Name,
                                    Accion = accion,
                                    ClavePrimaria = entry.Entity.Id.ToString(),
                                    ValoresAnterioresJson = JsonSerializer.Serialize(valoresAnteriores),
                                    ValoresNuevosJson = JsonSerializer.Serialize(valoresNuevos),
                                    FechaUtc = ahoraUtc,
                                    CreatedAt = ahoraUtc
                                });
                            }
                        }
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = ahoraUtc;
                        entry.Entity.DeletedByUserId ??= entry.Entity.UpdatedByUserId ?? entry.Entity.CreatedByUserId;

                        if (esEntidadAuditable)
                        {
                            auditLogsPendientes.Add(new AuditLog
                            {
                                TenantId = entidadTenantId,
                                SucursalId = entidadSucursalId,
                                UsuarioId = entry.Entity.DeletedByUserId ?? entry.Entity.UpdatedByUserId ?? entry.Entity.CreatedByUserId,
                                EntidadNombre = entry.Metadata.ClrType.Name,
                                Accion = "DELETE",
                                ClavePrimaria = entry.Entity.Id.ToString(),
                                ValoresAnterioresJson = JsonSerializer.Serialize(new { IsDeleted = false }),
                                ValoresNuevosJson = JsonSerializer.Serialize(new { IsDeleted = true, DeletedAt = ahoraUtc }),
                                FechaUtc = ahoraUtc,
                                CreatedAt = ahoraUtc
                            });
                        }
                        break;
                }
            }

            if (auditLogsPendientes.Count > 0)
            {
                await AuditLogs.AddRangeAsync(auditLogsPendientes, cancellationToken);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
