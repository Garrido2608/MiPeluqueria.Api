using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Data.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Módulo Clientes
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<TipoCabello> TiposCabello { get; set; }
        public DbSet<TipoRostro> TiposRostro { get; set; }
        public DbSet<FichaTecnica> FichasTecnicas { get; set; }

        // Módulo Turnos y Staff
        public DbSet<Peluquero> Peluqueros { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<PeluqueroEspecialidad> PeluqueroEspecialidades { get; set; }
        public DbSet<HorarioLaboral> HorariosLaborales { get; set; }
        public DbSet<CategoriaServicio> CategoriasServicio { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<EstadoTurno> EstadosTurno { get; set; }
        public DbSet<MotivoCancelacion> MotivosCancelacion { get; set; }
        public DbSet<BloqueoHorario> BloqueosHorario { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<TurnoServicio> TurnoServicios { get; set; }
        public DbSet<HistorialEstadoTurno> HistorialEstadoTurnos { get; set; }

        // Módulo Stock
        public DbSet<CategoriaProducto> CategoriasProducto { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }

        // Módulo Ventas y Caja
        public DbSet<SesionCaja> SesionesCaja { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<MedioPago> MediosPago { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }

        // Módulo Liquidaciones
        public DbSet<ComisionesConfig> ComisionesConfigs { get; set; }
        public DbSet<Liquidacion> Liquidaciones { get; set; }
        public DbSet<DetalleLiquidacion> DetallesLiquidacion { get; set; }

        // Módulo Seguridad
        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<HistorialSesion> HistorialSesiones { get; set; }

        //Modulo Common
       public DbSet<ConfiguracionSalon> ConfiguracionSalon { get; set; }

        // Nuevos DbSets del Punto 3:
        public DbSet<MiPeluqueria.Api.Models.Seguridad.RefreshToken> RefreshTokens { get; set; }
        public DbSet<MiPeluqueria.Api.Models.Common.AuditLog> AuditLogs { get; set; }
        public DbSet<MiPeluqueria.Api.Models.Stock.HistorialPrecioProducto> HistorialPreciosProducto { get; set; }
        public DbSet<MiPeluqueria.Api.Models.Turnos.HistorialPrecioServicio> HistorialPreciosServicio { get; set; }

       
       

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Apagar el borrado en cascada físico por defecto para evitar ciclos en SQL Server
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // 2. Aplicar configuraciones modulares (PeluqueroEspecialidad, TurnoServicio, DetalleVenta, Pago)
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // 3. Global Query Filters para Soft Delete automático en las 21 entidades auditables
            modelBuilder.Entity<Cliente>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<TipoCabello>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<TipoRostro>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<FichaTecnica>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Peluquero>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Especialidad>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<HorarioLaboral>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CategoriaServicio>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Servicio>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<EstadoTurno>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<MotivoCancelacion>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<BloqueoHorario>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Turno>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<HistorialEstadoTurno>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CategoriaProducto>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Proveedor>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Producto>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<MovimientoStock>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<SesionCaja>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Venta>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<DetalleVenta>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<MedioPago>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Pago>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<MovimientoCaja>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ComisionesConfig>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Liquidacion>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<DetalleLiquidacion>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Rol>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Usuario>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<HistorialSesion>().HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<Usuario>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Cliente>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Peluquero>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Servicio>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Turno>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Producto>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Proveedor>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Especialidad>().HasQueryFilter(e => !e.IsDeleted);

            // 4. Precisiones decimales financieras blindadas
            modelBuilder.Entity<Turno>().Property(t => t.MontoTotalEstimado).HasPrecision(18, 2);
            modelBuilder.Entity<Servicio>().Property(s => s.Precio).HasPrecision(18, 2);
            modelBuilder.Entity<Producto>().Property(p => p.PrecioCosto).HasPrecision(18, 2);
            modelBuilder.Entity<Producto>().Property(p => p.PrecioVenta).HasPrecision(18, 2);
            modelBuilder.Entity<SesionCaja>().Property(s => s.MontoInicial).HasPrecision(18, 2);
            modelBuilder.Entity<SesionCaja>().Property(s => s.MontoFinalReal).HasPrecision(18, 2);
            modelBuilder.Entity<SesionCaja>().Property(s => s.MontoFinalEsperado).HasPrecision(18, 2);
            modelBuilder.Entity<SesionCaja>().Property(s => s.Diferencia).HasPrecision(18, 2);
            modelBuilder.Entity<Venta>().Property(v => v.Total).HasPrecision(18, 2);
            modelBuilder.Entity<MovimientoCaja>().Property(m => m.Monto).HasPrecision(18, 2);
            modelBuilder.Entity<ComisionesConfig>().Property(c => c.PorcentajeServicio).HasPrecision(5, 2);
            modelBuilder.Entity<ComisionesConfig>().Property(c => c.PorcentajeVentaProducto).HasPrecision(5, 2);
            modelBuilder.Entity<Liquidacion>().Property(l => l.TotalPagar).HasPrecision(18, 2);
            modelBuilder.Entity<Liquidacion>().Property(l => l.TotalProductos).HasPrecision(18, 2);
            modelBuilder.Entity<Liquidacion>().Property(l => l.TotalServicios).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleLiquidacion>().Property(d => d.MontoComisionCalculado).HasPrecision(18, 2);
            modelBuilder.Entity<Pago>().Property(p => p.Importe).HasPrecision(18, 2);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            // 5. Índices de Rendimiento (Ingeniería de Datos para Producción)

            modelBuilder.Entity<Turno>()
                .HasIndex(t => new { t.PeluqueroId, t.FechaHoraInicio, t.FechaHoraFin, t.EstadoId })
                .HasDatabaseName("IX_Turnos_Agenda_Solapamiento");

            modelBuilder.Entity<Cliente>()
                .HasIndex(c => c.Email)
                .HasDatabaseName("IX_Clientes_Email");

            modelBuilder.Entity<Producto>()
                .HasIndex(p => p.CodigoBarras)
                .HasDatabaseName("IX_Productos_CodigoBarras");

            modelBuilder.Entity<DetalleVenta>()
                .HasIndex(d => new { d.PeluqueroId, d.TipoItem })
                .HasDatabaseName("IX_DetallesVenta_Liquidaciones");

           
            // Solución a Warnings EF Core 10622 (Tablas intermedias N:M)
            modelBuilder.Entity<PeluqueroEspecialidad>()
                .HasQueryFilter(pe => !pe.Peluquero.IsDeleted);

            modelBuilder.Entity<TurnoServicio>()
                .HasQueryFilter(ts => !ts.Turno.IsDeleted && !ts.Servicio.IsDeleted);

            // 6. Seed Data Maestro Completo (Fecha estática para evitar diffs innecesarios en migraciones)
            var seedDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<EstadoTurno>().HasData(
                new EstadoTurno { Id = 1, Nombre = "Pendiente", CreatedAt = seedDate },
                new EstadoTurno { Id = 2, Nombre = "Confirmado", CreatedAt = seedDate },
                new EstadoTurno { Id = 3, Nombre = "En Proceso", CreatedAt = seedDate },
                new EstadoTurno { Id = 4, Nombre = "Completado", CreatedAt = seedDate },
                new EstadoTurno { Id = 5, Nombre = "Cancelado", CreatedAt = seedDate },
                new EstadoTurno { Id = 6, Nombre = "No Asistió", CreatedAt = seedDate }
            );

            modelBuilder.Entity<MotivoCancelacion>().HasData(
                new MotivoCancelacion { Id = 1, Descripcion = "Cancelación con aviso previo del cliente", CreatedAt = seedDate },
                new MotivoCancelacion { Id = 2, Descripcion = "Ausencia o indisposición del profesional", CreatedAt = seedDate },
                new MotivoCancelacion { Id = 3, Descripcion = "Reprogramación solicitada por recepción", CreatedAt = seedDate },
                new MotivoCancelacion { Id = 4, Descripcion = "Fuerza mayor / Feriado / Corte de suministro", CreatedAt = seedDate }
            );

            modelBuilder.Entity<MedioPago>().HasData(
                new MedioPago { Id = 1, Nombre = "Efectivo", CreatedAt = seedDate },
                new MedioPago { Id = 2, Nombre = "Tarjeta de Débito", CreatedAt = seedDate },
                new MedioPago { Id = 3, Nombre = "Tarjeta de Crédito", CreatedAt = seedDate },
                new MedioPago { Id = 4, Nombre = "Transferencia / Alias", CreatedAt = seedDate },
                new MedioPago { Id = 5, Nombre = "Mercado Pago QR", CreatedAt = seedDate },
                new MedioPago { Id = 6, Nombre = "Billetera Virtual (Otra)", CreatedAt = seedDate },
                new MedioPago { Id = 7, Nombre = "Canje / Cortesía", CreatedAt = seedDate }
            );

            modelBuilder.Entity<Rol>().HasData(
                new Rol { Id = 1, Nombre = "Administrador", CreatedAt = seedDate },
                new Rol { Id = 2, Nombre = "Recepcionista", CreatedAt = seedDate },
                new Rol { Id = 3, Nombre = "Peluquero", CreatedAt = seedDate },
                new Rol { Id = 4, Nombre = "Cliente", CreatedAt = seedDate },
                new Rol { Id = 5, Nombre = "Cajero", CreatedAt = seedDate }
            );

            modelBuilder.Entity<CategoriaServicio>().HasData(
                new CategoriaServicio { Id = 1, Nombre = "Peluquería", CreatedAt = seedDate },
                new CategoriaServicio { Id = 2, Nombre = "Barbería", CreatedAt = seedDate },
                new CategoriaServicio { Id = 3, Nombre = "Colorimetría", CreatedAt = seedDate },
                new CategoriaServicio { Id = 4, Nombre = "Tratamientos Capilares", CreatedAt = seedDate }
            );

            modelBuilder.Entity<Especialidad>().HasData(
                new Especialidad { Id = 1, Nombre = "Corte Clásico y Tijera", CreatedAt = seedDate },
                new Especialidad { Id = 2, Nombre = "Fade, Navaja y Perfilado de Barba", CreatedAt = seedDate },
                new Especialidad { Id = 3, Nombre = "Colorimetría, Balayage y Decoloración", CreatedAt = seedDate },
                new Especialidad { Id = 4, Nombre = "Nutrición, Keratina y Recuperación Capilar", CreatedAt = seedDate }
            );

            modelBuilder.Entity<CategoriaProducto>().HasData(
                new CategoriaProducto { Id = 1, Nombre = "Ceras y Pomadas de Modelado", CreatedAt = seedDate },
                new CategoriaProducto { Id = 2, Nombre = "Shampoos y Acondicionadores Profesionales", CreatedAt = seedDate },
                new CategoriaProducto { Id = 3, Nombre = "Aceites y Bálsamos para Barba", CreatedAt = seedDate },
                new CategoriaProducto { Id = 4, Nombre = "Insumos Técnicos de Salón (Tinturas y Oxidantes)", CreatedAt = seedDate }
            );

            modelBuilder.Entity<TipoCabello>().HasData(
                new TipoCabello { Id = 1, Nombre = "Lacio Fino", Descripcion = "Baja densidad, requiere volumen ligero", CreatedAt = seedDate },
                new TipoCabello { Id = 2, Nombre = "Ondulado Medio", Descripcion = "Textura versátil con tendencia a frizz moderado", CreatedAt = seedDate },
                new TipoCabello { Id = 3, Nombre = "Rizado / Con Rulos", Descripcion = "Alta necesidad de hidratación y definición", CreatedAt = seedDate },
                new TipoCabello { Id = 4, Nombre = "Grueso / Afro", Descripcion = "Estructura densa ideal para fades de alto contraste", CreatedAt = seedDate }
            );

            modelBuilder.Entity<TipoRostro>().HasData(
                new TipoRostro { Id = 1, Nombre = "Ovalado", Descripcion = "Proporciones equilibradas, admite todo tipo de cortes", CreatedAt = seedDate },
                new TipoRostro { Id = 2, Nombre = "Cuadrado", Descripcion = "Mandíbula marcada, favorece degradados laterales", CreatedAt = seedDate },
                new TipoRostro { Id = 3, Nombre = "Redondo", Descripcion = "Favorece volumen superior y laterales cortos", CreatedAt = seedDate },
                new TipoRostro { Id = 4, Nombre = "Alargado / Diamante", Descripcion = "Favorece estructura equilibrada en laterales", CreatedAt = seedDate }
            );

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    RolId = 1,
                    Username = "admin",
                    PasswordHash = "$2a$11$0nN01jB5P169DXZgN/.hCeeNInhV/9tYkH.fK5v4a9jR1LqFkI5C6", // "Tesis2026*"
                    Email = "admin@tesis.com",
                    EmailVerificado = true,
                    AceptaTerminos = true,
                    Activo = true,
                    CreatedAt = seedDate,
                    IsDeleted = false
                }
            );
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.IsDeleted = false;
                        break;
                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAt = DateTime.UtcNow;
                        break;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}