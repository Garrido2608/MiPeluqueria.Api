using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class ServicioInsumoRecetaConfiguration : IEntityTypeConfiguration<ServicioInsumoReceta>
    {
        public void Configure(EntityTypeBuilder<ServicioInsumoReceta> builder)
        {
            builder.ToTable("ServiciosInsumosRecetas");

            builder.Property(r => r.CantidadMedidaEstandar).HasColumnType("decimal(10,2)");
            builder.Property(r => r.UnidadMedida).IsRequired().HasMaxLength(20);

            builder.HasIndex(r => new { r.ServicioId, r.ProductoId })
                .IsUnique()
                .HasDatabaseName("UX_ServiciosInsumosRecetas_Servicio_Producto")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(r => r.Servicio)
                .WithMany()
                .HasForeignKey(r => r.ServicioId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Producto)
                .WithMany()
                .HasForeignKey(r => r.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}