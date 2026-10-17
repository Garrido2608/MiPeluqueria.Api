using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ApplicationDbContext _context;

        public UsuarioRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> GetByIdWithRolAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Usuario>()
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<Usuario?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
        {
            var normalized = usernameOrEmail.Trim().ToLower();
            return await _context.Set<Usuario>()
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized || u.Email.ToLower() == normalized, cancellationToken);
        }

        public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalized = email.Trim().ToLower();
            return await _context.Set<Usuario>()
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);
        }

        public async Task<IReadOnlyList<Usuario>> GetAllWithRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Set<Usuario>()
                .AsNoTracking()
                .Include(u => u.Rol)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsUsernameOrEmailAsync(string username, string email, int? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            var normUser = username.Trim().ToLower();
            var normEmail = email.Trim().ToLower();

            var query = _context.Set<Usuario>().AsNoTracking()
                .Where(u => u.Username.ToLower() == normUser || u.Email.ToLower() == normEmail);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync(cancellationToken);
        }

        public async Task<Rol?> GetRolByIdAsync(int rolId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Rol>().FirstOrDefaultAsync(r => r.Id == rolId, cancellationToken);
        }

        public async Task<Rol?> GetRolByNombreAsync(string nombreRol, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Rol>()
                .FirstOrDefaultAsync(r => r.Nombre.ToLower() == nombreRol.Trim().ToLower(), cancellationToken);
        }

        public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            usuario.CreatedAt = DateTime.UtcNow;
            await _context.Set<Usuario>().AddAsync(usuario, cancellationToken);
        }

        public void Update(Usuario usuario)
        {
            usuario.UpdatedAt = DateTime.UtcNow;
            _context.Set<Usuario>().Update(usuario);
        }

        public async Task RegistrarHistorialSesionAsync(HistorialSesion historial, CancellationToken cancellationToken = default)
        {
            historial.CreatedAt = DateTime.UtcNow;
            await _context.Set<HistorialSesion>().AddAsync(historial, cancellationToken);
        }

        public async Task<IReadOnlyList<HistorialSesion>> GetHistorialSesionesAsync(int? usuarioId = null, int top = 100, CancellationToken cancellationToken = default)
        {
            var query = _context.Set<HistorialSesion>()
                .AsNoTracking()
                .Include(h => h.Usuario)
                .AsQueryable();

            if (usuarioId.HasValue)
                query = query.Where(h => h.UsuarioId == usuarioId.Value);

            return await query
                .OrderByDescending(h => h.FechaIntento)
                .Take(Math.Clamp(top, 1, 500))
                .ToListAsync(cancellationToken);
        }
    }
}