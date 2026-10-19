using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Clientes;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");

            builder.Property(c => c.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Apellido).IsRequired().HasMaxLength(100);
            builder.Property(c => c.Telefono).IsRequired().HasMaxLength(30);
            builder.Property(c => c.Email).HasMaxLength(150);

            // Índices únicos filtrados por SoftDelete para permitir re-registrar clientes dados de baja
            builder.HasIndex(c => c.Telefono)
                .HasDatabaseName("IX_Clientes_Telefono")
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(c => c.Email)
                .IsUnique()
                .HasDatabaseName("UX_Clientes_Email_Active")
                .HasFilter("[Email] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasOne(c => c.TipoCabello)
                .WithMany()
                .HasForeignKey(c => c.TipoCabelloId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.TipoRostro)
                .WithMany()
                .HasForeignKey(c => c.TipoRostroId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class FichaTecnicaConfiguration : IEntityTypeConfiguration<FichaTecnica>
    {
        public void Configure(EntityTypeBuilder<FichaTecnica> builder)
        {
            builder.ToTable("FichasTecnicas");

            builder.Property(f => f.DetalleFormulaTintura).HasMaxLength(1000);
            builder.Property(f => f.NotasSensibilidadAlergias).HasMaxLength(1000);

            builder.HasIndex(f => new { f.ClienteId, f.Fecha })
                .HasDatabaseName("IX_FichasTecnicas_ClienteId_Fecha");

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