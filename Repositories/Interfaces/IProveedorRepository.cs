using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IProveedorRepository
    {
        Task<IEnumerable<Proveedor>> GetAllAsync();
        Task<Proveedor?> GetByIdAsync(int id);
        Task<bool> ExistsByRazonSocialAsync(string razonSocial, int? excludeId = null);
        Task AddAsync(Proveedor proveedor);
        void Update(Proveedor proveedor);
        void Delete(Proveedor proveedor);
    }
}