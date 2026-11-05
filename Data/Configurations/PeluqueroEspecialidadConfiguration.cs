using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Data.Configurations
{
    public class PeluqueroEspecialidadConfiguration : IEntityTypeConfiguration<PeluqueroEspecialidad>
    {
        public void Configure(EntityTypeBuilder<PeluqueroEspecialidad> builder)
        {
            builder.ToTable("PeluqueroEspecialidades");
            builder.HasKey(pe => pe.Id);

            builder.HasIndex(pe => new { pe.TenantId, pe.SucursalId, pe.PeluqueroId, pe.EspecialidadId })
                .IsUnique()
                .HasDatabaseName("UX_PeluqueroEspecialidades_Activa")
                .HasFilter("[IsDeleted] = 0");

            builder.HasOne(pe => pe.Peluquero)
                .WithMany(p => p.PeluqueroEspecialidades)
                .HasForeignKey(pe => pe.PeluqueroId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pe => pe.Especialidad)
                .WithMany(e => e.PeluqueroEspecialidades)
                .HasForeignKey(pe => pe.EspecialidadId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
