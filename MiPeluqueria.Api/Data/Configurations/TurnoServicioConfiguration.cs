using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Turnos;
namespace MiPeluqueria.Api.Data.Configurations
{
    public class TurnoServicioConfiguration :
    IEntityTypeConfiguration<TurnoServicio>
    {
        public void Configure(EntityTypeBuilder<TurnoServicio> builder)
        {
            builder.ToTable("TurnoServicios");
            builder.HasKey(ts => new { ts.TurnoId, ts.ServicioId });
            builder.Property(ts => ts.PrecioHistorico).HasPrecision(18, 2);
            builder.HasOne(ts => ts.Turno)
            .WithMany(t => t.TurnoServicios)
            .HasForeignKey(ts => ts.TurnoId);
            builder.HasOne(ts => ts.Servicio)
            .WithMany(s => s.TurnoServicios)
            .HasForeignKey(ts => ts.ServicioId);
        }
    }
}