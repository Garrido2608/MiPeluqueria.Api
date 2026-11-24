using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class EspecialidadConfiguration : IEntityTypeConfiguration<Especialidad>
    {
        public void Configure(EntityTypeBuilder<Especialidad> builder)
        {
            builder.ToTable("Especialidades");

            builder.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(e => e.Descripcion).HasMaxLength(250);

            builder.HasIndex(e => new { e.TenantId, e.SucursalId, e.Nombre })
                .IsUnique()
                .HasDatabaseName("UX_Especialidades_Local_Nombre")
                .HasFilter("[IsDeleted] = 0");
        }
    }

    public class CategoriaServicioConfiguration : IEntityTypeConfiguration<CategoriaServicio>
    {
        public void Configure(EntityTypeBuilder<CategoriaServicio> builder)
        {
            builder.ToTable("CategoriasServicio");

            builder.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Descripcion).HasMaxLength(250);

            builder.HasIndex(c => new { c.TenantId, c.SucursalId, c.Nombre })
                .IsUnique()
                .HasDatabaseName("UX_CategoriasServicio_Local_Nombre")
                .HasFilter("[IsDeleted] = 0");
        }
    }

    public class EstadoTurnoConfiguration : IEntityTypeConfiguration<EstadoTurno>
    {
        public void Configure(EntityTypeBuilder<EstadoTurno> builder)
        {
            builder.ToTable("EstadosTurno");

            builder.Property(e => e.Nombre).IsRequired().HasMaxLength(50);

            builder.HasIndex(e => e.Nombre)
                .IsUnique()
                .HasDatabaseName("UX_EstadosTurno_Nombre")
                .HasFilter("[IsDeleted] = 0");
        }
    }

    public class MotivoCancelacionConfiguration : IEntityTypeConfiguration<MotivoCancelacion>
    {
        public void Configure(EntityTypeBuilder<MotivoCancelacion> builder)
        {
            builder.ToTable("MotivosCancelacion");

            builder.Property(m => m.Descripcion).IsRequired().HasMaxLength(150);

            builder.HasIndex(m => new { m.TenantId, m.SucursalId, m.Descripcion })
                .IsUnique()
                .HasDatabaseName("UX_MotivosCancelacion_Local_Descripcion")
                .HasFilter("[IsDeleted] = 0");
        }
    }

    public class PeluqueroConfiguration : IEntityTypeConfiguration<Peluquero>
    {
        public void Configure(EntityTypeBuilder<Peluquero> builder)
        {
            builder.ToTable("Peluqueros");

            builder.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Apellido).IsRequired().HasMaxLength(100);
            builder.Property(p => p.Telefono).HasMaxLength(30);
            builder.Property(p => p.Email).IsRequired().HasMaxLength(150);
            builder.Property(p => p.ColorAgendaHex).HasMaxLength(7).HasDefaultValue("#3B82F6");
            builder.Property(p => p.RowVersion).IsRowVersion();

            builder.HasIndex(p => new { p.TenantId, p.SucursalId, p.Email })
                .IsUnique()
                .HasDatabaseName("UX_Peluqueros_Local_Email")
                .HasFilter("[IsDeleted] = 0");
        }
    }

    public class ServicioConfiguration : IEntityTypeConfiguration<Servicio>
    {
        public void Configure(EntityTypeBuilder<Servicio> builder)
        {
            builder.ToTable("Servicios");

            builder.Property(s => s.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(s => s.Descripcion).HasMaxLength(500);
            builder.Property(s => s.Precio).HasPrecision(18, 2);
            builder.Property(s => s.RowVersion).IsRowVersion();

            builder.HasIndex(s => new { s.TenantId, s.SucursalId, s.Nombre })
                .IsUnique()
                .HasDatabaseName("UX_Servicios_Local_Nombre")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(s => new { s.TenantId, s.SucursalId, s.CategoriaId, s.Activo })
                .HasDatabaseName("IX_Servicios_CategoriaId_Activo");

            builder.HasOne(s => s.Categoria)
                .WithMany(c => c.Servicios)
                .HasForeignKey(s => s.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class TurnoConfiguration : IEntityTypeConfiguration<Turno>
    {
        public void Configure(EntityTypeBuilder<Turno> builder)
        {
            builder.ToTable("Turnos");

            builder.Property(t => t.MontoTotalEstimado).HasPrecision(18, 2);
            builder.Property(t => t.MontoSena).HasPrecision(18, 2);
            builder.Property(t => t.ReferenciaPagoExterno).HasMaxLength(150);
            builder.Property(t => t.Observaciones).HasMaxLength(500);
            builder.Property(t => t.RowVersion).IsRowVersion();

            // Índice compuesto clave para el motor ObtenerHorariosDisponiblesAsync y el Dashboard
            builder.HasIndex(t => new { t.TenantId, t.SucursalId, t.PeluqueroId, t.FechaHoraInicio, t.EstadoId })
                .HasDatabaseName("IX_Turnos_Disponibilidad_Peluquero_Fecha_Estado")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(t => new { t.TenantId, t.SucursalId, t.ClienteId, t.FechaHoraInicio })
                .HasDatabaseName("IX_Turnos_ClienteId_FechaHoraInicio");

            builder.HasOne(t => t.Cliente)
                .WithMany(c => c.Turnos)
                .HasForeignKey(t => t.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Peluquero)
                .WithMany(p => p.Turnos)
                .HasForeignKey(t => t.PeluqueroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Estado)
                .WithMany(e => e.Turnos)
                .HasForeignKey(t => t.EstadoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.MotivoCancelacion)
                .WithMany(m => m.TurnosCancelados)
                .HasForeignKey(t => t.MotivoCancelacionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class HorarioLaboralConfiguration : IEntityTypeConfiguration<HorarioLaboral>
    {
        public void Configure(EntityTypeBuilder<HorarioLaboral> builder)
        {
            builder.ToTable("HorariosLaborales");

            builder.HasIndex(h => new { h.TenantId, h.SucursalId, h.PeluqueroId, h.DiaSemana })
                .HasDatabaseName("IX_HorariosLaborales_PeluqueroId_DiaSemana");

            builder.HasOne(h => h.Peluquero)
                .WithMany(p => p.HorariosLaborales)
                .HasForeignKey(h => h.PeluqueroId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class BloqueoHorarioConfiguration : IEntityTypeConfiguration<BloqueoHorario>
    {
        public void Configure(EntityTypeBuilder<BloqueoHorario> builder)
        {
            builder.ToTable("BloqueosHorario");

            builder.Property(b => b.Motivo).IsRequired().HasMaxLength(200);

            builder.HasIndex(b => new { b.TenantId, b.SucursalId, b.PeluqueroId, b.FechaInicio, b.FechaFin })
                .HasDatabaseName("IX_BloqueosHorario_PeluqueroId_Fechas");

            builder.HasOne(b => b.Peluquero)
                .WithMany(p => p.BloqueosHorario)
                .HasForeignKey(b => b.PeluqueroId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class HistorialEstadoTurnoConfiguration : IEntityTypeConfiguration<HistorialEstadoTurno>
    {
        public void Configure(EntityTypeBuilder<HistorialEstadoTurno> builder)
        {
            builder.ToTable("HistorialEstadosTurno");

            builder.Property(h => h.Observaciones).HasMaxLength(300);

            builder.HasIndex(h => new { h.TenantId, h.SucursalId, h.TurnoId, h.FechaCambio })
                .HasDatabaseName("IX_HistorialEstadosTurno_TurnoId_FechaCambio");

            builder.HasOne(h => h.Turno)
                .WithMany(t => t.HistorialEstados)
                .HasForeignKey(h => h.TurnoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(h => h.EstadoAnterior)
                .WithMany()
                .HasForeignKey(h => h.EstadoAnteriorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(h => h.EstadoNuevo)
                .WithMany()
                .HasForeignKey(h => h.EstadoNuevoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class HistorialPrecioServicioConfiguration : IEntityTypeConfiguration<HistorialPrecioServicio>
    {
        public void Configure(EntityTypeBuilder<HistorialPrecioServicio> builder)
        {
            builder.ToTable("HistorialPreciosServicio");

            builder.Property(h => h.PrecioAnterior).HasPrecision(18, 2);
            builder.Property(h => h.PrecioNuevo).HasPrecision(18, 2);
            builder.Property(h => h.Motivo).HasMaxLength(250);

            builder.HasOne(h => h.Servicio)
                .WithMany()
                .HasForeignKey(h => h.ServicioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
