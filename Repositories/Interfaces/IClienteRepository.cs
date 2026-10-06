using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Clientes;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IClienteRepository
    {
        Task<List<Cliente>> GetAllAsync();
        Task<Cliente?> GetByIdAsync(int id);
        Task<Cliente> AddAsync(Cliente cliente);
        Task UpdateAsync(Cliente cliente);
        Task DeleteAsync(int id);
        Task<bool> ExistsEmailAsync(string email);
    }
}