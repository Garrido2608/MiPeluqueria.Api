using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Clientes;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class TipoCabelloConfiguration : IEntityTypeConfiguration<TipoCabello>
    {
        public void Configure(EntityTypeBuilder<TipoCabello> builder)
        {
            builder.ToTable("TiposCabello");

            builder.Property(tc => tc.Nombre).IsRequired().HasMaxLength(50);
            builder.Property(tc => tc.Descripcion).HasMaxLength(250);

            builder.HasIndex(tc => new { tc.TenantId, tc.SucursalId, tc.Nombre })
                .IsUnique()
                .HasDatabaseName("UX_TiposCabello_Local_Nombre")
                .HasFilter("[IsDeleted] = 0");
        }
    }

    public class TipoRostroConfiguration : IEntityTypeConfiguration<TipoRostro>
    {
        public void Configure(EntityTypeBuilder<TipoRostro> builder)
        {
            builder.ToTable("TiposRostro");

            builder.Property(tr => tr.Nombre).IsRequired().HasMaxLength(50);
            builder.Property(tr => tr.Descripcion).HasMaxLength(250);

            builder.HasIndex(tr => new { tr.TenantId, tr.SucursalId, tr.Nombre })
                .IsUnique()
                .HasDatabaseName("UX_TiposRostro_Local_Nombre")
                .HasFilter("[IsDeleted] = 0");
        }
    }

    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");

            builder.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Apellido).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Telefono).IsRequired().HasMaxLength(30);
            builder.Property(c => c.Email).HasMaxLength(150);
            builder.Property(c => c.RowVersion).IsRowVersion();

            // Índices únicos compuestos por local filtrados por SoftDelete
            builder.HasIndex(c => new { c.TenantId, c.SucursalId, c.Telefono })
                .IsUnique()
                .HasDatabaseName("UX_Clientes_Local_Telefono")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(c => new { c.TenantId, c.SucursalId, c.Email })
                .IsUnique()
                .HasDatabaseName("UX_Clientes_Local_Email")
                .HasFilter("[Email] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasOne(c => c.TipoCabello)
                .WithMany(tc => tc.Clientes)
                .HasForeignKey(c => c.TipoCabelloId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.TipoRostro)
                .WithMany(tr => tr.Clientes)
                .HasForeignKey(c => c.TipoRostroId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class FichaTecnicaConfiguration : IEntityTypeConfiguration<FichaTecnica>
    {
        public void Configure(EntityTypeBuilder<FichaTecnica> builder)
        {
            builder.ToTable("FichasTecnicas");

            builder.Property(f => f.FechaRegistro).IsRequired();
            builder.Property(f => f.FormulaAplicada).IsRequired().HasMaxLength(1000);
            builder.Property(f => f.Observaciones).HasMaxLength(1000);
            builder.Property(f => f.NotasSensibilidadAlergias).HasMaxLength(1000);
            builder.Property(f => f.ProductosUtilizados).HasMaxLength(500);
            builder.Property(f => f.FotoResultadoUrl).HasMaxLength(500);

            builder.HasIndex(f => new { f.TenantId, f.SucursalId, f.ClienteId, f.FechaRegistro })
                .HasDatabaseName("IX_FichasTecnicas_ClienteId_FechaRegistro");

            builder.HasOne(f => f.Cliente)
                .WithMany(c => c.FichasTecnicas)
                .HasForeignKey(f => f.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Peluquero)
                .WithMany(p => p.FichasTecnicas)
                .HasForeignKey(f => f.PeluqueroId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(f => f.Turno)
                .WithMany()
                .HasForeignKey(f => f.TurnoId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
