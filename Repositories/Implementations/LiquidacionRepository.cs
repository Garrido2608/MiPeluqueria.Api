using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class LiquidacionRepository : ILiquidacionRepository
    {
        private readonly ApplicationDbContext _context;

        public LiquidacionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Liquidacion?> GetByIdWithDetallesAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Liquidacion>()
                .Include(l => l.Peluquero)
                .Include(l => l.DetallesLiquidacion)
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Liquidacion>> GetAllWithPeluqueroAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<Liquidacion>()
                .AsNoTracking()
                .Include(l => l.Peluquero)
                .Include(l => l.DetallesLiquidacion)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Liquidacion>> GetByPeluqueroIdAsync(int peluqueroId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Liquidacion>()
                .AsNoTracking()
                .Include(l => l.Peluquero)
                .Include(l => l.DetallesLiquidacion)
                .Where(l => l.PeluqueroId == peluqueroId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExisteLiquidacionEnPeriodoAsync(int peluqueroId, DateTime fechaDesde, DateTime fechaHasta, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Liquidacion>()
                .AsNoTracking()
                .AnyAsync(l => l.PeluqueroId == peluqueroId
                            && !l.IsDeleted
                            && l.FechaDesde < fechaHasta
                            && l.FechaHasta > fechaDesde, cancellationToken);
        }

        public async Task AddAsync(Liquidacion liquidacion, CancellationToken cancellationToken = default)
        {
            liquidacion.CreatedAt = DateTime.UtcNow;
            await _context.Set<Liquidacion>().AddAsync(liquidacion, cancellationToken);
        }

        public void Update(Liquidacion liquidacion)
        {
            liquidacion.UpdatedAt = DateTime.UtcNow;
            _context.Set<Liquidacion>().Update(liquidacion);
        }
    }
}