using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class PeluqueroRepository : IPeluqueroRepository
    {
        private readonly ApplicationDbContext _context;

        public PeluqueroRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Peluquero>> GetAllAsync()
        {
            // Usamos Include para traer las especialidades junto con el peluquero
            return await _context.Peluqueros
                .Include(p => p.PeluqueroEspecialidades)
                    .ThenInclude(pe => pe.Especialidad)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Peluquero?> GetByIdAsync(int id)
        {
            // Traemos al peluquero con sus especialidades y sus horarios laborales
            return await _context.Peluqueros
                .Include(p => p.PeluqueroEspecialidades)
                    .ThenInclude(pe => pe.Especialidad)
                .Include(p => p.HorariosLaborales)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Peluquero> AddAsync(Peluquero peluquero)
        {
            _context.Peluqueros.Add(peluquero);
            return peluquero;
        }

        public async Task UpdateAsync(Peluquero peluquero)
        {
            _context.Peluqueros.Update(peluquero);
        }

        public async Task DeleteAsync(int id)
        {
            var peluquero = await _context.Peluqueros.FindAsync(id);
            if (peluquero != null)
            {
                _context.Peluqueros.Remove(peluquero);
            }
        }

        public async Task<bool> ExistsEmailAsync(string email)
        {
            return await _context.Peluqueros.AnyAsync(p => p.Email == email);
        }
    }
}