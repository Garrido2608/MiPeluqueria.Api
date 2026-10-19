using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Data.Configurations
{
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

            builder.HasIndex(p => p.Email)
                .IsUnique()
                .HasDatabaseName("UX_Peluqueros_Email_Active")
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
            builder.Property(s => s.Precio).HasColumnType("decimal(18,2)");

            builder.HasIndex(s => new { s.CategoriaId, s.Activo })
                .HasDatabaseName("IX_Servicios_CategoriaId_Activo");

            builder.HasOne(s => s.Categoria)
                .WithMany()
                .HasForeignKey(s => s.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class TurnoConfiguration : IEntityTypeConfiguration<Turno>
    {
        public void Configure(EntityTypeBuilder<Turno> builder)
        {
            builder.ToTable("Turnos");

            builder.Property(t => t.MontoTotalEstimado).HasColumnType("decimal(18,2)");
            builder.Property(t => t.Observaciones).HasMaxLength(500);

            // Índice compuesto clave para el motor ObtenerHorariosDisponiblesAsync y el Dashboard
            builder.HasIndex(t => new { t.PeluqueroId, t.FechaHoraInicio, t.EstadoId })
                .HasDatabaseName("IX_Turnos_Disponibilidad_Peluquero_Fecha_Estado")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(t => new { t.ClienteId, t.FechaHoraInicio })
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
                .WithMany()
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

            builder.HasIndex(h => new { h.PeluqueroId, h.DiaSemana })
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

            builder.HasIndex(b => new { b.PeluqueroId, b.FechaInicio, b.FechaFin })
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

            builder.HasIndex(h => new { h.TurnoId, h.FechaCambio })
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

            builder.Property(h => h.PrecioAnterior).HasColumnType("decimal(18,2)");
            builder.Property(h => h.PrecioNuevo).HasColumnType("decimal(18,2)");
            builder.Property(h => h.Motivo).HasMaxLength(250);

            builder.HasOne(h => h.Servicio)
                .WithMany()
                .HasForeignKey(h => h.ServicioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}