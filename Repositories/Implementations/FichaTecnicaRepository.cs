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
    public class FichaTecnicaRepository : GenericRepository<FichaTecnica>, IFichaTecnicaRepository
    {
        public FichaTecnicaRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<FichaTecnica?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dbSet
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
            return await _dbSet
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Peluquero)
                .Include(f => f.Turno)
                .Where(f => f.ClienteId == clienteId)
                .OrderByDescending(f => f.FechaRegistro)
                .ThenByDescending(f => f.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
