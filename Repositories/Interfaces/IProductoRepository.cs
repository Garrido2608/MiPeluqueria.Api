using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Stock;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IProductoRepository
    {
        Task<List<Producto>> GetAllAsync();
        Task<Producto?> GetByIdAsync(int id);
        Task<Producto> AddAsync(Producto producto);
        Task UpdateAsync(Producto producto);
        Task RegistrarMovimientoAsync(MovimientoStock movimiento);
    }
}