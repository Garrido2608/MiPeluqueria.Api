using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Clientes;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IFichaTecnicaRepository : IGenericRepository<FichaTecnica>
    {
        Task<FichaTecnica?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FichaTecnica>> GetByClienteIdAsync(int clienteId, CancellationToken cancellationToken = default);
    }
}
