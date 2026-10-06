using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IPeluqueroRepository
    {
        Task<List<Peluquero>> GetAllAsync();
        Task<Peluquero?> GetByIdAsync(int id);
        Task<Peluquero> AddAsync(Peluquero peluquero);
        Task UpdateAsync(Peluquero peluquero);
        Task DeleteAsync(int id);
        Task<bool> ExistsEmailAsync(string email);
    }
}