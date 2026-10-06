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
            builder.HasKey(pe => new { pe.PeluqueroId, pe.EspecialidadId });

            builder.HasOne(pe => pe.Peluquero)
                   .WithMany(p => p.PeluqueroEspecialidades)
                   .HasForeignKey(pe => pe.PeluqueroId);

            builder.HasOne(pe => pe.Especialidad)
                   .WithMany(e => e.PeluqueroEspecialidades)
                   .HasForeignKey(pe => pe.EspecialidadId);
        }
    }
}