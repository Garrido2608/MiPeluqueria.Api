using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface IServicioRepository : IGenericRepository<Servicio>
    {
        Task<IReadOnlyList<Servicio>> GetAllWithCategoriaAsync(bool soloActivos = false, CancellationToken ct = default);
        Task<Servicio?> GetByIdWithCategoriaAsync(int id, bool tracking = false, CancellationToken ct = default);
        Task<IReadOnlyList<Servicio>> GetActivosByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
        Task AddHistorialPrecioAsync(HistorialPrecioServicio historial, CancellationToken ct = default);
        Task<IReadOnlyList<HistorialPrecioServicio>> GetHistorialPreciosAsync(int servicioId, CancellationToken ct = default);
        Task<bool> TieneTurnosFuturosActivosAsync(int servicioId, DateTime desdeUtc, CancellationToken ct = default);
        Task UpdateAsync(Servicio servicio, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }
}
