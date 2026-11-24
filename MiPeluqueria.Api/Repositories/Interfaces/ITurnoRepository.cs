using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface ITurnoRepository : IGenericRepository<Turno>
    {
        Task<List<Turno>> GetTurnosPeluqueroEnFechaAsync(int peluqueroId, DateTime fecha, CancellationToken ct = default);
        Task<IReadOnlyList<Turno>> GetTurnosActivosEnRangoAsync(int peluqueroId, DateTime fechaInicio, DateTime fechaFin, CancellationToken ct = default);
        Task<bool> ExisteSolapamientoAsync(int peluqueroId, DateTime inicio, DateTime fin, int? turnoIdExcluir = null, CancellationToken ct = default);
        Task<Turno?> GetByIdConDetallesAsync(int id, bool tracking = true, CancellationToken ct = default);
        Task<(IReadOnlyList<Turno> Items, int TotalRecords)> GetPagedAsync(PaginationFilterDto filter, DateTime? fecha = null, int? peluqueroId = null, int? estadoId = null, int? clienteId = null, CancellationToken ct = default);
        Task<IReadOnlyList<Turno>> GetByClienteIdAsync(int clienteId, CancellationToken ct = default);
        Task AddTurnoServicioAsync(TurnoServicio turnoServicio, CancellationToken ct = default);
        Task AddTurnoServiciosRangeAsync(IEnumerable<TurnoServicio> turnoServicios, CancellationToken ct = default);
        Task ReemplazarTurnoServiciosAsync(int turnoId, IEnumerable<TurnoServicio> nuevosServicios, CancellationToken ct = default);
        Task AddHistorialEstadoAsync(HistorialEstadoTurno historial, CancellationToken ct = default);
        Task<IReadOnlyList<HistorialEstadoTurno>> GetHistorialEstadosByTurnoIdAsync(int turnoId, CancellationToken ct = default);
        Task<bool> ExisteConsumoGabinetePorTurnoAsync(int turnoId, CancellationToken ct = default);
        Task UpdateAsync(Turno turno, CancellationToken ct = default);
    }
}
