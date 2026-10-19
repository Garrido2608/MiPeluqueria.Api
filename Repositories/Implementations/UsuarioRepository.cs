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
            return await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }

        public async Task<Usuario?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
        {
            var norm = usernameOrEmail.Trim().ToLower();
            return await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == norm || u.Email.ToLower() == norm, cancellationToken);
        }

        public async Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var norm = email.Trim().ToLower();
            return await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == norm, cancellationToken);
        }

        public async Task<IReadOnlyList<Usuario>> GetAllWithRolesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.Id)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsUsernameOrEmailAsync(string username, string email, int? excludeUserId = null, CancellationToken cancellationToken = default)
        {
            var userNorm = username.Trim().ToLower();
            var emailNorm = email.Trim().ToLower();

            var query = _context.Usuarios.AsQueryable();
            if (excludeUserId.HasValue)
                query = query.Where(u => u.Id != excludeUserId.Value);

            return await query.AnyAsync(u => u.Username.ToLower() == userNorm || u.Email.ToLower() == emailNorm, cancellationToken);
        }

        public async Task<Rol?> GetRolByIdAsync(int rolId, CancellationToken cancellationToken = default)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Id == rolId, cancellationToken);
        }

        public async Task<Rol?> GetRolByNombreAsync(string nombreRol, CancellationToken cancellationToken = default)
        {
            var norm = nombreRol.Trim().ToLower();
            return await _context.Roles.FirstOrDefaultAsync(r => r.Nombre.ToLower() == norm, cancellationToken);
        }

        public async Task AddAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            await _context.Usuarios.AddAsync(usuario, cancellationToken);
        }

        public void Update(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
        }

        public async Task RegistrarHistorialSesionAsync(HistorialSesion historial, CancellationToken cancellationToken = default)
        {
            await _context.HistorialSesiones.AddAsync(historial, cancellationToken);
        }

        public async Task<IReadOnlyList<HistorialSesion>> GetHistorialSesionesAsync(int? usuarioId = null, int top = 100, CancellationToken cancellationToken = default)
        {
            var query = _context.HistorialSesiones
                .Include(h => h.Usuario)
                .AsNoTracking()
                .AsQueryable();

            if (usuarioId.HasValue)
                query = query.Where(h => h.UsuarioId == usuarioId.Value);

            return await query
                .OrderByDescending(h => h.Id)
                .Take(top > 0 ? top : 100)
                .ToListAsync(cancellationToken);
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _context.RefreshTokens
                .Include(r => r.Usuario)
                    .ThenInclude(u => u!.Rol)
                .FirstOrDefaultAsync(r => r.Token == token, cancellationToken);
        }

        public async Task RevocarRefreshTokensUsuarioAsync(int usuarioId, string motivo, string? ip = null, CancellationToken cancellationToken = default)
        {
            var tokensActivos = await _context.RefreshTokens
                .Where(r => r.UsuarioId == usuarioId && r.RevokedAt == null && r.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var token in tokensActivos)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ip;
                token.ReasonRevoked = motivo;
            }
        }
    }
}