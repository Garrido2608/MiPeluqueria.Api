using MiPeluqueria.Api.Models.Clientes;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IFichaTecnicaRepository
    {
        Task<FichaTecnica?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FichaTecnica>> GetByClienteIdAsync(int clienteId, CancellationToken cancellationToken = default);
        Task AddAsync(FichaTecnica fichaTecnica, CancellationToken cancellationToken = default);
        void Update(FichaTecnica fichaTecnica);
        void Delete(FichaTecnica fichaTecnica);
    }
}