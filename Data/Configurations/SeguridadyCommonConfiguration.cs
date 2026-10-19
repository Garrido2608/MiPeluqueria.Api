using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Models.Seguridad;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("Usuarios");

            builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
            builder.Property(u => u.CodigoVerificacion).HasMaxLength(6);

            builder.HasIndex(u => u.Username)
                .IsUnique()
                .HasDatabaseName("UX_Usuarios_Username_Active")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("UX_Usuarios_Email_Active")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(u => u.Rol)
                .WithMany()
                .HasForeignKey(u => u.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.Peluquero)
                .WithMany()
                .HasForeignKey(u => u.PeluqueroId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(u => u.Cliente)
                .WithMany()
                .HasForeignKey(u => u.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class HistorialSesionConfiguration : IEntityTypeConfiguration<HistorialSesion>
    {
        public void Configure(EntityTypeBuilder<HistorialSesion> builder)
        {
            builder.ToTable("HistorialSesiones");

            builder.Property(h => h.DireccionIP).IsRequired().HasMaxLength(50);
            builder.Property(h => h.Dispositivo).HasMaxLength(500);

            builder.HasIndex(h => new { h.UsuarioId, h.FechaIntento })
                .HasDatabaseName("IX_HistorialSesiones_UsuarioId_FechaIntento");

            builder.HasOne(h => h.Usuario)
                .WithMany()
                .HasForeignKey(h => h.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.Property(r => r.Token).IsRequired().HasMaxLength(256);
            builder.Property(r => r.CreatedByIp).HasMaxLength(50);
            builder.Property(r => r.RevokedByIp).HasMaxLength(50);
            builder.Property(r => r.ReplacedByToken).HasMaxLength(256);
            builder.Property(r => r.ReasonRevoked).HasMaxLength(200);

            builder.HasIndex(r => r.Token)
                .IsUnique()
                .HasDatabaseName("UX_RefreshTokens_Token");

            builder.HasOne(r => r.Usuario)
                .WithMany()
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ConfiguracionSalonConfiguration : IEntityTypeConfiguration<ConfiguracionSalon>
    {
        public void Configure(EntityTypeBuilder<ConfiguracionSalon> builder)
        {
            builder.ToTable("ConfiguracionSalon");

            builder.Property(c => c.NombreSalon).IsRequired().HasMaxLength(150);
            builder.Property(c => c.RazonSocial).HasMaxLength(150);
            builder.Property(c => c.Cuit).HasMaxLength(20);
            builder.Property(c => c.Direccion).HasMaxLength(250);
            builder.Property(c => c.Telefono).HasMaxLength(50);
            builder.Property(c => c.EmailContacto).HasMaxLength(150);
            builder.Property(c => c.MonedaIso).IsRequired().HasMaxLength(3).HasDefaultValue("ARS");
            builder.Property(c => c.TimeZoneId).IsRequired().HasMaxLength(100).HasDefaultValue("America/Argentina/Cordoba");
            builder.Property(c => c.PorcentajeSenaReserva).HasColumnType("decimal(5,2)");
        }
    }

    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");

            builder.Property(a => a.EntidadNombre).IsRequired().HasMaxLength(100);
            builder.Property(a => a.Accion).IsRequired().HasMaxLength(30);
            builder.Property(a => a.ClavePrimaria).IsRequired().HasMaxLength(50);
            builder.Property(a => a.IpAddress).HasMaxLength(50);

            builder.HasIndex(a => new { a.EntidadNombre, a.ClavePrimaria, a.FechaUtc })
                .HasDatabaseName("IX_AuditLogs_Entidad_Clave_Fecha");
        }
    }
}