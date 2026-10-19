using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
    {
        public void Configure(EntityTypeBuilder<Proveedor> builder)
        {
            builder.ToTable("Proveedores");

            builder.Property(p => p.RazonSocial).IsRequired().HasMaxLength(150);
            builder.Property(p => p.Contacto).HasMaxLength(100);
            builder.Property(p => p.Telefono).HasMaxLength(30);
            builder.Property(p => p.Email).HasMaxLength(150);
        }
    }

    public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
    {
        public void Configure(EntityTypeBuilder<Producto> builder)
        {
            builder.ToTable("Productos");

            builder.Property(p => p.CodigoBarras).HasMaxLength(50);
            builder.Property(p => p.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(p => p.PrecioCosto).HasColumnType("decimal(18,2)");
            builder.Property(p => p.PrecioVenta).HasColumnType("decimal(18,2)");

            builder.HasIndex(p => p.CodigoBarras)
                .IsUnique()
                .HasDatabaseName("UX_Productos_CodigoBarras_Active")
                .HasFilter("[CodigoBarras] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasIndex(p => new { p.StockActual, p.StockMinimoAlerta })
                .HasDatabaseName("IX_Productos_StockCritico")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(p => p.Categoria)
                .WithMany()
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Proveedor)
                .WithMany(pr => pr.Productos)
                .HasForeignKey(p => p.ProveedorId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class MovimientoStockConfiguration : IEntityTypeConfiguration<MovimientoStock>
    {
        public void Configure(EntityTypeBuilder<MovimientoStock> builder)
        {
            builder.ToTable("MovimientosStock");

            builder.Property(m => m.Motivo).HasMaxLength(300);

            builder.HasIndex(m => new { m.ProductoId, m.Fecha })
                .HasDatabaseName("IX_MovimientosStock_Kardex_ProductoId_Fecha");

            builder.HasOne(m => m.Producto)
                .WithMany(p => p.MovimientosStock)
                .HasForeignKey(m => m.ProductoId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class HistorialPrecioProductoConfiguration : IEntityTypeConfiguration<HistorialPrecioProducto>
    {
        public void Configure(EntityTypeBuilder<HistorialPrecioProducto> builder)
        {
            builder.ToTable("HistorialPreciosProducto");

            builder.Property(h => h.PrecioCostoAnterior).HasColumnType("decimal(18,2)");
            builder.Property(h => h.PrecioCostoNuevo).HasColumnType("decimal(18,2)");
            builder.Property(h => h.PrecioVentaAnterior).HasColumnType("decimal(18,2)");
            builder.Property(h => h.PrecioVentaNuevo).HasColumnType("decimal(18,2)");
            builder.Property(h => h.Motivo).HasMaxLength(250);

            builder.HasOne(h => h.Producto)
                .WithMany()
                .HasForeignKey(h => h.ProductoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}