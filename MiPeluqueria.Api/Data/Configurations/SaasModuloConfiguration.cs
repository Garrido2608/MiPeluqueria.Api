using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.SaaS;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("Tenants");
            builder.Property(t => t.NombreComercial).IsRequired().HasMaxLength(150);
            builder.Property(t => t.SubdominioSlug).IsRequired().HasMaxLength(100);
            builder.Property(t => t.IdentificacionFiscal).HasMaxLength(30);
            builder.Property(t => t.EmailPropietario).IsRequired().HasMaxLength(150);
            builder.Property(t => t.PaisIso).HasMaxLength(10);

            builder.HasIndex(t => t.SubdominioSlug)
                .IsUnique()
                .HasDatabaseName("UX_Tenants_SubdominioSlug")
                .HasFilter("[IsDeleted] = 0");
        }
    }

    public class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
    {
        public void Configure(EntityTypeBuilder<Sucursal> builder)
        {
            builder.ToTable("Sucursales");
            builder.Property(s => s.Nombre).IsRequired().HasMaxLength(150);
            builder.Property(s => s.Direccion).HasMaxLength(250);
            builder.Property(s => s.Telefono).HasMaxLength(50);
            builder.Property(s => s.MonedaIso).HasMaxLength(3).HasDefaultValue("ARS");
            builder.Property(s => s.TimeZoneId).HasMaxLength(100).HasDefaultValue("America/Argentina/Cordoba");

            builder.HasIndex(s => new { s.TenantId, s.Nombre })
                .IsUnique()
                .HasDatabaseName("UX_Sucursales_Tenant_Nombre")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(s => s.Tenant)
                .WithMany(t => t.Sucursales)
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class PlanSuscripcionConfiguration : IEntityTypeConfiguration<PlanSuscripcion>
    {
        public void Configure(EntityTypeBuilder<PlanSuscripcion> builder)
        {
            builder.ToTable("PlanesSuscripcion");
            builder.Property(p => p.Nombre).IsRequired().HasMaxLength(100);
            builder.Property(p => p.PrecioMensualUsd).HasPrecision(18, 2);
        }
    }

    public class SuscripcionTenantConfiguration : IEntityTypeConfiguration<SuscripcionTenant>
    {
        public void Configure(EntityTypeBuilder<SuscripcionTenant> builder)
        {
            builder.ToTable("SuscripcionesTenant");
            builder.Property(s => s.ExternalSubscriptionId).HasMaxLength(150);

            builder.HasOne(s => s.Tenant)
                .WithMany(t => t.Suscripciones)
                .HasForeignKey(s => s.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.PlanSuscripcion)
                .WithMany()
                .HasForeignKey(s => s.PlanSuscripcionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
