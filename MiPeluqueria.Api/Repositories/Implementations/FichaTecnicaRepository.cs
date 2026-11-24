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
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Peluquero)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<FichaTecnica>> GetByClienteIdAsync(int clienteId, CancellationToken cancellationToken = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Peluquero)
                .Where(f => f.ClienteId == clienteId)
                .OrderByDescending(f => f.FechaRegistro)
                .ThenByDescending(f => f.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
