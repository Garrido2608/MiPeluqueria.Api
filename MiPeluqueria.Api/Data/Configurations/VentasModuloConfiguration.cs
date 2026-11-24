using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class MedioPagoConfiguration : IEntityTypeConfiguration<MedioPago>
    {
        public void Configure(EntityTypeBuilder<MedioPago> builder)
        {
            builder.ToTable("MediosPago");

            builder.Property(m => m.Nombre).IsRequired().HasMaxLength(50);
            builder.Property(m => m.Descripcion).HasMaxLength(150);

            builder.HasIndex(m => new { m.TenantId, m.SucursalId, m.Nombre })
                .IsUnique()
                .HasDatabaseName("UX_MediosPago_Local_Nombre")
                .HasFilter("[IsDeleted] = 0");
        }
    }

    public class SesionCajaConfiguration : IEntityTypeConfiguration<SesionCaja>
    {
        public void Configure(EntityTypeBuilder<SesionCaja> builder)
        {
            builder.ToTable("SesionesCaja");

            builder.Property(s => s.MontoInicial).HasPrecision(18, 2);
            builder.Property(s => s.TotalVentasEfectivo).HasPrecision(18, 2);
            builder.Property(s => s.TotalVentasDigital).HasPrecision(18, 2);
            builder.Property(s => s.TotalIngresosManuales).HasPrecision(18, 2);
            builder.Property(s => s.TotalEgresosManuales).HasPrecision(18, 2);
            builder.Property(s => s.MontoFinalReal).HasPrecision(18, 2);
            builder.Property(s => s.MontoFinalEsperado).HasPrecision(18, 2);
            builder.Property(s => s.Diferencia).HasPrecision(18, 2);
            builder.Property(s => s.ObservacionesCierre).HasMaxLength(500);
            builder.Property(s => s.RowVersion).IsRowVersion();

            builder.HasIndex(s => new { s.TenantId, s.SucursalId, s.EstadoAbierta, s.FechaApertura })
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
            builder.Property(m => m.Monto).HasPrecision(18, 2);

            builder.HasIndex(m => new { m.TenantId, m.SucursalId, m.SesionCajaId, m.Fecha })
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

            builder.Property(v => v.Subtotal).HasPrecision(18, 2);
            builder.Property(v => v.DescuentoSenaAplicado).HasPrecision(18, 2);
            builder.Property(v => v.Total).HasPrecision(18, 2);
            builder.Property(v => v.IdempotencyKey).HasMaxLength(64);
            builder.Property(v => v.RowVersion).IsRowVersion();

            builder.HasIndex(v => new { v.TenantId, v.SucursalId, v.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("UX_Ventas_Local_IdempotencyKey")
                .HasFilter("[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasIndex(v => new { v.TenantId, v.SucursalId, v.SesionCajaId, v.Fecha })
                .HasDatabaseName("IX_Ventas_SesionCajaId_Fecha");

            builder.HasIndex(v => new { v.TenantId, v.SucursalId, v.Fecha })
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

            builder.Property(l => l.TotalServicios).HasPrecision(18, 2);
            builder.Property(l => l.TotalProductos).HasPrecision(18, 2);
            builder.Property(l => l.Bonificaciones).HasPrecision(18, 2);
            builder.Property(l => l.Descuentos).HasPrecision(18, 2);
            builder.Property(l => l.TotalPagar).HasPrecision(18, 2);
            builder.Property(l => l.Observaciones).HasMaxLength(500);
            builder.Property(l => l.RowVersion).IsRowVersion();

            builder.HasIndex(l => new { l.TenantId, l.SucursalId, l.PeluqueroId, l.Estado, l.FechaDesde })
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

            builder.Property(d => d.MontoComisionCalculado).HasPrecision(18, 2);

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

            builder.Property(c => c.PorcentajeServicio).HasPrecision(5, 2);
            builder.Property(c => c.PorcentajeVentaProducto).HasPrecision(5, 2);

            builder.HasIndex(c => new { c.TenantId, c.SucursalId, c.PeluqueroId, c.ServicioId })
                .IsUnique()
                .HasDatabaseName("UX_ComisionesConfig_Local_Peluquero_Servicio")
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
