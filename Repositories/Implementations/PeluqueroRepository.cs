using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class PeluqueroRepository : GenericRepository<Peluquero>, IPeluqueroRepository
    {
        public PeluqueroRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override async Task<IReadOnlyList<Peluquero>> GetAllAsync(CancellationToken ct = default)
        {
            return await GetAllWithDetailsAsync(false, ct);
        }

        public async Task<IReadOnlyList<Peluquero>> GetAllWithDetailsAsync(bool soloActivos = false, CancellationToken ct = default)
        {
            var query = _dbSet
                .AsNoTracking()
                .Include(p => p.PeluqueroEspecialidades)
                    .ThenInclude(pe => pe.Especialidad)
                .Include(p => p.HorariosLaborales)
                .AsQueryable();

            if (soloActivos)
                query = query.Where(p => p.Activo);

            return await query
                .OrderBy(p => p.Apellido)
                .ThenBy(p => p.Nombre)
                .ToListAsync(ct);
        }

        public override async Task<Peluquero?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await GetByIdWithDetailsAsync(id, true, ct);
        }

        public async Task<Peluquero?> GetByIdWithDetailsAsync(int id, bool tracking = false, CancellationToken ct = default)
        {
            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
            return await query
                .Include(p => p.PeluqueroEspecialidades)
                    .ThenInclude(pe => pe.Especialidad)
                .Include(p => p.HorariosLaborales)
                .FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<bool> ExistsEmailAsync(string email, int? excludeId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var norm = email.Trim().ToLower();
            return await _dbSet.AnyAsync(
                p => p.Email.ToLower() == norm && (!excludeId.HasValue || p.Id != excludeId.Value),
                ct);
        }

        public async Task<int> CountActivosAsync(CancellationToken ct = default)
        {
            return await _dbSet.CountAsync(p => p.Activo && !p.IsDeleted, ct);
        }

        public async Task ReemplazarEspecialidadesAsync(int peluqueroId, IEnumerable<int> especialidadesIds, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var existentes = await _context.PeluqueroEspecialidades
                .Where(pe => pe.PeluqueroId == peluqueroId)
                .ToListAsync(ct);

            foreach (var ex in existentes)
            {
                ex.IsDeleted = true;
                ex.DeletedAt = now;
                ex.UpdatedAt = now;
            }

            if (especialidadesIds != null)
            {
                foreach (var espId in especialidadesIds.Distinct())
                {
                    await _context.PeluqueroEspecialidades.AddAsync(new PeluqueroEspecialidad
                    {
                        PeluqueroId = peluqueroId,
                        EspecialidadId = espId,
                        CreatedAt = now
                    }, ct);
                }
            }
        }

        public async Task<IReadOnlyList<HorarioLaboral>> GetHorariosByPeluqueroIdAsync(int peluqueroId, CancellationToken ct = default)
        {
            return await _context.HorariosLaborales
                .AsNoTracking()
                .Where(h => h.PeluqueroId == peluqueroId)
                .OrderBy(h => h.DiaSemana)
                .ToListAsync(ct);
        }

        public async Task<HorarioLaboral?> GetHorarioDiaAsync(int peluqueroId, DayOfWeek diaSemana, CancellationToken ct = default)
        {
            return await _context.HorariosLaborales
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.PeluqueroId == peluqueroId && h.DiaSemana == diaSemana, ct);
        }

        public async Task ReemplazarHorariosLaboralesAsync(int peluqueroId, IEnumerable<HorarioLaboral> horarios, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var existentes = await _context.HorariosLaborales
                .Where(h => h.PeluqueroId == peluqueroId)
                .ToListAsync(ct);

            foreach (var ex in existentes)
            {
                ex.IsDeleted = true;
                ex.DeletedAt = now;
                ex.UpdatedAt = now;
            }

            if (horarios != null)
            {
                foreach (var item in horarios)
                {
                    item.PeluqueroId = peluqueroId;
                    item.CreatedAt = now;
                    await _context.HorariosLaborales.AddAsync(item, ct);
                }
            }
        }

        public async Task<bool> TieneTurnosFuturosActivosAsync(int peluqueroId, DateTime desdeUtc, CancellationToken ct = default)
        {
            return await _context.Turnos
                .AsNoTracking()
                .AnyAsync(t => t.PeluqueroId == peluqueroId
                            && t.FechaHoraInicio >= desdeUtc
                            && (t.EstadoId == (int)EstadoTurnoEnum.Pendiente
                                || t.EstadoId == (int)EstadoTurnoEnum.Confirmado
                                || t.EstadoId == (int)EstadoTurnoEnum.EnProceso), ct);
        }

        public async Task<bool> TieneLiquidacionesPendientesAsync(int peluqueroId, CancellationToken ct = default)
        {
            return await _context.Liquidaciones
                .AsNoTracking()
                .AnyAsync(l => l.PeluqueroId == peluqueroId
                            && l.Estado == EstadoLiquidacionEnum.Pendiente
                            && !l.IsDeleted, ct);
        }

        public Task UpdateAsync(Peluquero peluquero, CancellationToken ct = default)
        {
            Update(peluquero);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(int id, CancellationToken ct = default)
        {
            var peluquero = await _dbSet.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (peluquero != null)
            {
                peluquero.Activo = false;
                Delete(peluquero);
            }
        }
    }
}
