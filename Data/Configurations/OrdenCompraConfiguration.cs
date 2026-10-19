using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class OrdenCompraConfiguration : IEntityTypeConfiguration<OrdenCompra>
    {
        public void Configure(EntityTypeBuilder<OrdenCompra> builder)
        {
            builder.ToTable("OrdenesCompra");

            builder.Property(o => o.NumeroComprobanteProveedor).HasMaxLength(50);
            builder.Property(o => o.Observaciones).HasMaxLength(500);
            builder.Property(o => o.Total).HasColumnType("decimal(18,2)");

            builder.HasIndex(o => new { o.ProveedorId, o.Estado, o.FechaEmision })
                .HasDatabaseName("IX_OrdenesCompra_Proveedor_Estado_Fecha");

            builder.HasOne(o => o.Proveedor)
                .WithMany()
                .HasForeignKey(o => o.ProveedorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class DetalleOrdenCompraConfiguration : IEntityTypeConfiguration<DetalleOrdenCompra>
    {
        public void Configure(EntityTypeBuilder<DetalleOrdenCompra> builder)
        {
            builder.ToTable("DetallesOrdenCompra");

            builder.Property(d => d.PrecioUnitarioCosto).HasColumnType("decimal(18,2)");
            builder.Property(d => d.Subtotal).HasColumnType("decimal(18,2)");

            builder.HasOne(d => d.OrdenCompra)
                .WithMany(o => o.Detalles)
                .HasForeignKey(d => d.OrdenCompraId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.Producto)
                .WithMany()
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}