using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class SesionCajaConfiguration : IEntityTypeConfiguration<SesionCaja>
    {
        public void Configure(EntityTypeBuilder<SesionCaja> builder)
        {
            builder.ToTable("SesionesCaja");

            builder.Property(s => s.MontoInicial).HasColumnType("decimal(18,2)");
            builder.Property(s => s.MontoFinalReal).HasColumnType("decimal(18,2)");
            builder.Property(s => s.MontoFinalEsperado).HasColumnType("decimal(18,2)");
            builder.Property(s => s.Diferencia).HasColumnType("decimal(18,2)");

            builder.HasIndex(s => new { s.EstadoAbierta, s.FechaApertura })
                .HasDatabaseName("IX_SesionesCaja_EstadoAbierta_FechaApertura");
        }
    }

    public class MovimientoCajaConfiguration : IEntityTypeConfiguration<MovimientoCaja>
    {
        public void Configure(EntityTypeBuilder<MovimientoCaja> builder)
        {
            builder.ToTable("MovimientosCaja");

            builder.Property(m => m.Tipo).IsRequired().HasMaxLength(50);
            builder.Property(m => m.Concepto).IsRequired().HasMaxLength(250);
            builder.Property(m => m.Monto).HasColumnType("decimal(18,2)");

            builder.HasIndex(m => new { m.SesionCajaId, m.Fecha })
                .HasDatabaseName("IX_MovimientosCaja_SesionCajaId_Fecha");

            builder.HasOne(m => m.SesionCaja)
                .WithMany(s => s.MovimientosCaja)
                .HasForeignKey(m => m.SesionCajaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class VentaConfiguration : IEntityTypeConfiguration<Venta>
    {
        public void Configure(EntityTypeBuilder<Venta> builder)
        {
            builder.ToTable("Ventas");

            builder.Property(v => v.Total).HasColumnType("decimal(18,2)");

            builder.HasIndex(v => new { v.SesionCajaId, v.Fecha })
                .HasDatabaseName("IX_Ventas_SesionCajaId_Fecha");

            builder.HasIndex(v => v.Fecha)
                .HasDatabaseName("IX_Ventas_Fecha_Dashboard");

            builder.HasOne(v => v.SesionCaja)
                .WithMany(s => s.Ventas)
                .HasForeignKey(v => v.SesionCajaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.Cliente)
                .WithMany()
                .HasForeignKey(v => v.ClienteId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(v => v.Turno)
                .WithMany()
                .HasForeignKey(v => v.TurnoId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public class LiquidacionConfiguration : IEntityTypeConfiguration<Liquidacion>
    {
        public void Configure(EntityTypeBuilder<Liquidacion> builder)
        {
            builder.ToTable("Liquidaciones");

            builder.Property(l => l.TotalServicios).HasColumnType("decimal(18,2)");
            builder.Property(l => l.TotalProductos).HasColumnType("decimal(18,2)");
            builder.Property(l => l.TotalPagar).HasColumnType("decimal(18,2)");

            builder.HasIndex(l => new { l.PeluqueroId, l.Estado, l.FechaDesde })
                .HasDatabaseName("IX_Liquidaciones_PeluqueroId_Estado_FechaDesde");

            builder.HasOne(l => l.Peluquero)
                .WithMany()
                .HasForeignKey(l => l.PeluqueroId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class DetalleLiquidacionConfiguration : IEntityTypeConfiguration<DetalleLiquidacion>
    {
        public void Configure(EntityTypeBuilder<DetalleLiquidacion> builder)
        {
            builder.ToTable("DetallesLiquidacion");

            builder.Property(d => d.MontoComisionCalculado).HasColumnType("decimal(18,2)");

            builder.HasOne(d => d.Liquidacion)
                .WithMany(l => l.DetallesLiquidacion)
                .HasForeignKey(d => d.LiquidacionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class ComisionesConfigConfiguration : IEntityTypeConfiguration<ComisionesConfig>
    {
        public void Configure(EntityTypeBuilder<ComisionesConfig> builder)
        {
            builder.ToTable("ComisionesConfig");

            builder.Property(c => c.PorcentajeServicio).HasColumnType("decimal(5,2)");
            builder.Property(c => c.PorcentajeVentaProducto).HasColumnType("decimal(5,2)");

            builder.HasIndex(c => new { c.PeluqueroId, c.ServicioId })
                .IsUnique()
                .HasDatabaseName("UX_ComisionesConfig_Peluquero_Servicio")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(c => c.Peluquero)
                .WithMany()
                .HasForeignKey(c => c.PeluqueroId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.Servicio)
                .WithMany()
                .HasForeignKey(c => c.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}