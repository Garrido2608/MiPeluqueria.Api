using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class FichaTecnicaRepository : IFichaTecnicaRepository
    {
        private readonly ApplicationDbContext _context;

        public FichaTecnicaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FichaTecnica?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FichaTecnica>()
                .Include(f => f.Cliente)
                    .ThenInclude(c => c!.TipoCabello)
                .Include(f => f.Cliente)
                    .ThenInclude(c => c!.TipoRostro)
                .Include(f => f.Peluquero)
                .Include(f => f.Turno)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<FichaTecnica>> GetByClienteIdAsync(int clienteId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<FichaTecnica>()
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Peluquero)
                .Include(f => f.Turno)
                .Where(f => f.ClienteId == clienteId)
                .OrderByDescending(f => f.Fecha)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(FichaTecnica fichaTecnica, CancellationToken cancellationToken = default)
        {
            fichaTecnica.CreatedAt = DateTime.UtcNow;
            await _context.Set<FichaTecnica>().AddAsync(fichaTecnica, cancellationToken);
        }

        public void Update(FichaTecnica fichaTecnica)
        {
            fichaTecnica.UpdatedAt = DateTime.UtcNow;
            _context.Set<FichaTecnica>().Update(fichaTecnica);
        }

        public void Delete(FichaTecnica fichaTecnica)
        {
            fichaTecnica.IsDeleted = true;
            fichaTecnica.DeletedAt = DateTime.UtcNow;
            fichaTecnica.UpdatedAt = DateTime.UtcNow;
            _context.Set<FichaTecnica>().Update(fichaTecnica);
        }
    }
}