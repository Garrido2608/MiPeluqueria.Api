using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Ventas;
namespace MiPeluqueria.Api.Data.Configurations
{
    public class PagoConfiguration : IEntityTypeConfiguration<Pago>
    {
        public void Configure(EntityTypeBuilder<Pago> builder)
        {
            builder.ToTable("Pagos");
            builder.Property(p => p.Importe)
            .HasPrecision(18, 2)
            .IsRequired();
            builder.HasOne(p => p.Venta)
            .WithMany(v => v.Pagos)
            .HasForeignKey(p => p.VentaId)
            .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(p => p.MedioPago)
            .WithMany(m => m.Pagos)
            .HasForeignKey(p => p.MedioPagoId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}