using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
    {
        public void Configure(EntityTypeBuilder<DetalleVenta> builder)
        {
            builder.ToTable("DetallesVenta");

            builder.Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            builder.Property(d => d.Subtotal).HasPrecision(18, 2);

            builder.HasOne(d => d.Venta)
                   .WithMany(v => v.DetallesVenta)
                   .HasForeignKey(d => d.VentaId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}