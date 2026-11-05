using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Seguridad;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IUsuarioRepository : IGenericRepository<Usuario>
    {
        Task<Usuario?> GetByIdWithRolAsync(int id, CancellationToken cancellationToken = default);
        Task<Usuario?> GetByUsernameOrEmailAsync(string usernameOrEmail, CancellationToken cancellationToken = default);
        Task<Usuario?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Usuario>> GetAllWithRolesAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsUsernameOrEmailAsync(string username, string email, int? excludeUserId = null, CancellationToken cancellationToken = default);
        Task<Rol?> GetRolByIdAsync(int rolId, CancellationToken cancellationToken = default);
        Task<Rol?> GetRolByNombreAsync(string nombreRol, CancellationToken cancellationToken = default);
        Task RegistrarHistorialSesionAsync(HistorialSesion historial, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HistorialSesion>> GetHistorialSesionesAsync(int? usuarioId = null, int top = 100, CancellationToken cancellationToken = default);
        Task<(IReadOnlyList<HistorialSesion> Items, int TotalRecords)> GetHistorialSesionesPagedAsync(PaginationFilterDto filter, int? usuarioId = null, CancellationToken ct = default);

        Task AddRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
        Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
        void UpdateRefreshToken(RefreshToken refreshToken);
        Task RevocarRefreshTokensUsuarioAsync(int usuarioId, string motivo, string? ip = null, CancellationToken cancellationToken = default);
    }
}
