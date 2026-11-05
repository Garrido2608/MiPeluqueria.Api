using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class TurnoServicioConfiguration : IEntityTypeConfiguration<TurnoServicio>
    {
        public void Configure(EntityTypeBuilder<TurnoServicio> builder)
        {
            builder.ToTable("TurnoServicios");
            builder.HasKey(ts => ts.Id);

            builder.Property(ts => ts.PrecioHistorico).HasPrecision(18, 2);

            builder.HasIndex(ts => new { ts.TenantId, ts.SucursalId, ts.TurnoId, ts.ServicioId })
                .IsUnique()
                .HasDatabaseName("UX_TurnoServicios_Turno_Servicio")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(ts => ts.Turno)
                .WithMany(t => t.TurnoServicios)
                .HasForeignKey(ts => ts.TurnoId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ts => ts.Servicio)
                .WithMany(s => s.TurnoServicios)
                .HasForeignKey(ts => ts.ServicioId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
