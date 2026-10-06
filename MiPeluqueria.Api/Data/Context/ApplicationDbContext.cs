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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- REGLA DE ARQUITECTURA: Apagar el borrado en cascada físico ---
            // Evita el error "may cause cycles or multiple cascade paths" en SQL Server
            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            // Aplica las configuraciones de claves compuestas y cascada de la carpeta Configurations
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Global Query Filters para Soft Delete automático
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

            // Precisiones decimales críticas blindadas
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
            modelBuilder.Entity<DetalleLiquidacion>().Property(d => d.MontoComisionCalculado).HasPrecision(18, 2);
            // Agregados para limpiar advertencias
            modelBuilder.Entity<Liquidacion>().Property(l => l.TotalProductos).HasPrecision(18, 2);
            modelBuilder.Entity<Liquidacion>().Property(l => l.TotalServicios).HasPrecision(18, 2);
            modelBuilder.Entity<Pago>().Property(p => p.Importe).HasPrecision(18, 2);

            // Seed Data Inicial
            modelBuilder.Entity<EstadoTurno>().HasData(
                new EstadoTurno { Id = 1, Nombre = "Pendiente", CreatedAt = DateTime.UtcNow },
                new EstadoTurno { Id = 2, Nombre = "Confirmado", CreatedAt = DateTime.UtcNow },
                new EstadoTurno { Id = 3, Nombre = "En Proceso", CreatedAt = DateTime.UtcNow },
                new EstadoTurno { Id = 4, Nombre = "Completado", CreatedAt = DateTime.UtcNow },
                new EstadoTurno { Id = 5, Nombre = "Cancelado", CreatedAt = DateTime.UtcNow },
                new EstadoTurno { Id = 6, Nombre = "No Asistió", CreatedAt = DateTime.UtcNow }
            );

            modelBuilder.Entity<MedioPago>().HasData(
                new MedioPago { Id = 1, Nombre = "Efectivo", CreatedAt = DateTime.UtcNow },
                new MedioPago { Id = 2, Nombre = "Tarjeta de Débito", CreatedAt = DateTime.UtcNow },
                new MedioPago { Id = 3, Nombre = "Tarjeta de Crédito", CreatedAt = DateTime.UtcNow },
                new MedioPago { Id = 4, Nombre = "Transferencia / Alias", CreatedAt = DateTime.UtcNow },
                new MedioPago { Id = 5, Nombre = "Mercado Pago QR", CreatedAt = DateTime.UtcNow },
                new MedioPago { Id = 6, Nombre = "Billetera Virtual (Otra)", CreatedAt = DateTime.UtcNow },
                new MedioPago { Id = 7, Nombre = "Canje / Cortesía", CreatedAt = DateTime.UtcNow }
            );

            modelBuilder.Entity<Rol>().HasData(
                new Rol { Id = 1, Nombre = "Administrador", CreatedAt = DateTime.UtcNow },
                new Rol { Id = 2, Nombre = "Recepcionista", CreatedAt = DateTime.UtcNow },
                new Rol { Id = 3, Nombre = "Peluquero", CreatedAt = DateTime.UtcNow },
                new Rol { Id = 4, Nombre = "Cliente", CreatedAt = DateTime.UtcNow },
                new Rol { Id = 5, Nombre = "Cajero", CreatedAt = DateTime.UtcNow }
            );

            modelBuilder.Entity<CategoriaServicio>().HasData(
                  new CategoriaServicio { Id = 1, Nombre = "Peluquería", CreatedAt = DateTime.UtcNow },
              new CategoriaServicio { Id = 2, Nombre = "Barbería", CreatedAt = DateTime.UtcNow },
             new CategoriaServicio { Id = 3, Nombre = "Colorimetría", CreatedAt = DateTime.UtcNow },
             new CategoriaServicio { Id = 4, Nombre = "Tratamientos Capilares", CreatedAt = DateTime.UtcNow }
             );


            // Seed Data: Primer Usuario Administrador
            // La contraseña es "Tesis2026*" encriptada con BCrypt
            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    RolId = 1, // 1 = Administrador
                    Username = "admin",
                    PasswordHash = "$2a$11$0nN01jB5P169DXZgN/.hCeeNInhV/9tYkH.fK5v4a9jR1LqFkI5C6", // BCrypt de "Tesis2026*"
                    Email = "admin@tesis.com",
                    EmailVerificado = true,
                    AceptaTerminos = true,
                    Activo = true,
                    CreatedAt = DateTime.UtcNow,
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