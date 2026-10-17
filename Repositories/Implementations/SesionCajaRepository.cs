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
    public class SesionCajaRepository : ISesionCajaRepository
    {
        private readonly ApplicationDbContext _context;

        public SesionCajaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SesionCaja?> GetCajaAbiertaActualAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<SesionCaja>()
                .Include(s => s.MovimientosCaja)
                .Include(s => s.Ventas)
                    .ThenInclude(v => v.Pagos)
                        .ThenInclude(p => p.MedioPago)
                .FirstOrDefaultAsync(s => s.EstadoAbierta && s.FechaCierre == null && !s.IsDeleted, cancellationToken);
        }

        public async Task<SesionCaja?> GetByIdWithMovimientosYVentasAsync(int sesionCajaId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<SesionCaja>()
                .Include(s => s.MovimientosCaja)
                .Include(s => s.Ventas)
                    .ThenInclude(v => v.Pagos)
                        .ThenInclude(p => p.MedioPago)
                .FirstOrDefaultAsync(s => s.Id == sesionCajaId, cancellationToken);
        }

        public async Task<IReadOnlyList<SesionCaja>> GetHistorialCajasAsync(DateTime? desde = null, DateTime? hasta = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<SesionCaja>()
                .AsNoTracking()
                .Include(s => s.MovimientosCaja)
                .Where(s => !s.IsDeleted);

            if (desde.HasValue)
                query = query.Where(s => s.FechaApertura >= desde.Value);

            if (hasta.HasValue)
                query = query.Where(s => s.FechaApertura <= hasta.Value);

            return await query
                .OrderByDescending(s => s.FechaApertura)
                .ToListAsync(cancellationToken);
        }

        public async Task AbrirSesionCajaAsync(SesionCaja sesionCaja, CancellationToken cancellationToken = default)
        {
            sesionCaja.CreatedAt = DateTime.UtcNow;
            await _context.Set<SesionCaja>().AddAsync(sesionCaja, cancellationToken);
        }

        public void ActualizarSesionCaja(SesionCaja sesionCaja)
        {
            sesionCaja.UpdatedAt = DateTime.UtcNow;
            _context.Set<SesionCaja>().Update(sesionCaja);
        }

        public async Task RegistrarMovimientoCajaAsync(MovimientoCaja movimiento, CancellationToken cancellationToken = default)
        {
            movimiento.CreatedAt = DateTime.UtcNow;
            await _context.Set<MovimientoCaja>().AddAsync(movimiento, cancellationToken);
        }

        public async Task<IReadOnlyList<MovimientoCaja>> GetMovimientosBySesionIdAsync(int sesionCajaId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<MovimientoCaja>()
                .AsNoTracking()
                .Where(m => m.SesionCajaId == sesionCajaId)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync(cancellationToken);
        }
    }
}