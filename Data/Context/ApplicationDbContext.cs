using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace MiPeluqueria.Api.Data.Context
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<TipoCabello> TiposCabello { get; set; }
        public DbSet<TipoRostro> TiposRostro { get; set; }
        public DbSet<FichaTecnica> FichasTecnicas { get; set; }

        public DbSet<Peluquero> Peluqueros { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<PeluqueroEspecialidad> PeluqueroEspecialidades { get; set; }
        public DbSet<HorarioLaboral> HorariosLaborales { get; set; }
        public DbSet<CategoriaServicio> CategoriasServicio { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<ServicioInsumoReceta> ServiciosInsumosRecetas { get; set; }
        public DbSet<HistorialPrecioServicio> HistorialPreciosServicio { get; set; }
        public DbSet<EstadoTurno> EstadosTurno { get; set; }
        public DbSet<MotivoCancelacion> MotivosCancelacion { get; set; }
        public DbSet<BloqueoHorario> BloqueosHorario { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<TurnoServicio> TurnoServicios { get; set; }
        public DbSet<HistorialEstadoTurno> HistorialEstadoTurnos { get; set; }

        public DbSet<CategoriaProducto> CategoriasProducto { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }
        public DbSet<HistorialPrecioProducto> HistorialPreciosProducto { get; set; }
        public DbSet<OrdenCompra> OrdenesCompra { get; set; }
        public DbSet<DetalleOrdenCompra> DetallesOrdenCompra { get; set; }

        public DbSet<SesionCaja> SesionesCaja { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetallesVenta { get; set; }
        public DbSet<MedioPago> MediosPago { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<MovimientoCaja> MovimientosCaja { get; set; }

        public DbSet<ComisionesConfig> ComisionesConfigs { get; set; }
        public DbSet<Liquidacion> Liquidaciones { get; set; }
        public DbSet<DetalleLiquidacion> DetallesLiquidacion { get; set; }

        public DbSet<Rol> Roles { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<HistorialSesion> HistorialSesiones { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<ConfiguracionSalon> ConfiguracionSalon { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Sucursal> Sucursales { get; set; }
        public DbSet<PlanSuscripcion> PlanesSuscripcion { get; set; }
        public DbSet<SuscripcionTenant> SuscripcionesTenant { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            modelBuilder.Entity<Cliente>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<TipoCabello>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<TipoRostro>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<FichaTecnica>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Peluquero>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Especialidad>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<HorarioLaboral>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CategoriaServicio>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Servicio>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ServicioInsumoReceta>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<HistorialPrecioServicio>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<EstadoTurno>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<MotivoCancelacion>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<BloqueoHorario>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Turno>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<HistorialEstadoTurno>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CategoriaProducto>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Proveedor>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Producto>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<MovimientoStock>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<HistorialPrecioProducto>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<OrdenCompra>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<DetalleOrdenCompra>().HasQueryFilter(e => !e.IsDeleted);
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
            modelBuilder.Entity<RefreshToken>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ConfiguracionSalon>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<AuditLog>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Sucursal>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<PlanSuscripcion>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<SuscripcionTenant>().HasQueryFilter(e => !e.IsDeleted);

            modelBuilder.Entity<PeluqueroEspecialidad>()
                .HasQueryFilter(pe => !pe.Peluquero.IsDeleted);

            modelBuilder.Entity<TurnoServicio>()
                .HasQueryFilter(ts => !ts.Turno.IsDeleted && !ts.Servicio.IsDeleted);

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

            modelBuilder.Entity<PlanSuscripcion>().HasData(
                new PlanSuscripcion { Id = 1, Nombre = "Starter", PrecioMensualUsd = 19.99m, MaxSucursales = 1, MaxPeluqueros = 3, Activo = true, CreatedAt = seedDate },
                new PlanSuscripcion { Id = 2, Nombre = "Professional", PrecioMensualUsd = 39.99m, MaxSucursales = 3, MaxPeluqueros = 15, Activo = true, CreatedAt = seedDate },
                new PlanSuscripcion { Id = 3, Nombre = "Enterprise", PrecioMensualUsd = 79.99m, MaxSucursales = 99, MaxPeluqueros = 999, Activo = true, CreatedAt = seedDate }
            );

            modelBuilder.Entity<SuscripcionTenant>().HasData(
                new SuscripcionTenant
                {
                    Id = 1,
                    TenantId = 1,
                    PlanSuscripcionId = 2,
                    FechaInicio = seedDate,
                    FechaProximoVencimiento = new DateTime(2027, 10, 1, 0, 0, 0, DateTimeKind.Utc),
                    Estado = EstadoSuscripcionEnum.Activa,
                    CreatedAt = seedDate
                }
            );

            modelBuilder.Entity<Usuario>().HasData(
                new Usuario
                {
                    Id = 1,
                    RolId = 1,
                    Username = "admin",
                    PasswordHash = "$2a$11$0nN01jB5P169DXZgN/.hCeeNInhV/9tYkH.fK5v4a9jR1LqFkI5C6",
                    Email = "admin@tesis.com",
                    EmailVerificado = true,
                    AceptaTerminos = true,
                    Activo = true,
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

                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = ahoraUtc;
                        entry.Entity.IsDeleted = false;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = ahoraUtc;

                        if (esEntidadAuditable)
                        {
                            var valoresAnteriores = new Dictionary<string, object?>();
                            var valoresNuevos = new Dictionary<string, object?>();

                            foreach (var prop in entry.Properties)
                            {
                                if (prop.IsTemporary || prop.Metadata.Name == nameof(Usuario.PasswordHash))
                                    continue;

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
                                    UsuarioId = entry.Entity.CreatedByUserId,
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

                        if (esEntidadAuditable)
                        {
                            auditLogsPendientes.Add(new AuditLog
                            {
                                UsuarioId = entry.Entity.CreatedByUserId,
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