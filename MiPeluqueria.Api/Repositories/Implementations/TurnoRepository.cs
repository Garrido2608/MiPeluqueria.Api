using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class TurnoRepository : GenericRepository<Turno>, ITurnoRepository
    {
        public TurnoRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Turno>> GetTurnosPeluqueroEnFechaAsync(int peluqueroId, DateTime fecha, CancellationToken ct = default)
        {
            var inicioDia = fecha.Date;
            var finDia = fecha.Date.AddDays(1);

            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.MotivoCancelacion)
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .Where(t => t.PeluqueroId == peluqueroId
                         && t.FechaHoraInicio >= inicioDia
                         && t.FechaHoraInicio < finDia
                         && t.EstadoId != (int)EstadoTurnoEnum.Cancelado
                         && t.EstadoId != (int)EstadoTurnoEnum.NoAsistio)
                .OrderBy(t => t.FechaHoraInicio)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<Turno>> GetTurnosActivosEnRangoAsync(
            int peluqueroId,
            DateTime fechaInicio,
            DateTime fechaFin,
            CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Where(t => t.PeluqueroId == peluqueroId
                         && t.EstadoId != (int)EstadoTurnoEnum.Cancelado
                         && t.EstadoId != (int)EstadoTurnoEnum.NoAsistio
                         && t.FechaHoraInicio < fechaFin
                         && t.FechaHoraFin > fechaInicio)
                .OrderBy(t => t.FechaHoraInicio)
                .ToListAsync(ct);
        }

        public async Task<bool> ExisteSolapamientoAsync(
            int peluqueroId,
            DateTime inicio,
            DateTime fin,
            int? turnoIdExcluir = null,
            CancellationToken ct = default)
        {
            return await _dbSet
                .AnyAsync(t => t.PeluqueroId == peluqueroId
                            && t.EstadoId != (int)EstadoTurnoEnum.Cancelado
                            && t.EstadoId != (int)EstadoTurnoEnum.NoAsistio
                            && (turnoIdExcluir == null || t.Id != turnoIdExcluir)
                            && t.FechaHoraInicio < fin
                            && t.FechaHoraFin > inicio, ct);
        }

        public async Task<Turno?> GetByIdConDetallesAsync(int id, bool tracking = true, CancellationToken ct = default)
        {
            var query = tracking ? _dbSet.AsQueryable() : _dbSet.AsNoTracking();
            return await query
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.MotivoCancelacion)
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .Include(t => t.HistorialEstados)
                .FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public async Task<(IReadOnlyList<Turno> Items, int TotalRecords)> GetPagedAsync(
            PaginationFilterDto filter,
            DateTime? fecha = null,
            int? peluqueroId = null,
            int? estadoId = null,
            int? clienteId = null,
            CancellationToken ct = default)
        {
            int pageNumber = Math.Max(1, filter.PageNumber);
            int pageSize = Math.Clamp(filter.PageSize, 1, 100);

            var baseQuery = _dbSet.AsNoTracking().AsQueryable();

            if (fecha.HasValue)
            {
                var inicioDia = fecha.Value.Date;
                var finDia = inicioDia.AddDays(1).AddTicks(-1);
                baseQuery = baseQuery.Where(t => t.FechaHoraInicio >= inicioDia && t.FechaHoraInicio <= finDia);
            }

            if (filter.FechaDesde.HasValue)
                baseQuery = baseQuery.Where(t => t.FechaHoraInicio >= filter.FechaDesde.Value);

            if (filter.FechaHasta.HasValue)
                baseQuery = baseQuery.Where(t => t.FechaHoraInicio <= filter.FechaHasta.Value);

            if (peluqueroId.HasValue)
                baseQuery = baseQuery.Where(t => t.PeluqueroId == peluqueroId.Value);

            if (estadoId.HasValue)
                baseQuery = baseQuery.Where(t => t.EstadoId == estadoId.Value);

            if (clienteId.HasValue)
                baseQuery = baseQuery.Where(t => t.ClienteId == clienteId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim();
                baseQuery = baseQuery.Where(t =>
                    (t.Cliente != null && (t.Cliente.Nombre.Contains(term) || t.Cliente.Apellido.Contains(term) || t.Cliente.Telefono.Contains(term))) ||
                    (t.Peluquero != null && (t.Peluquero.Nombre.Contains(term) || t.Peluquero.Apellido.Contains(term))) ||
                    (t.Observaciones != null && t.Observaciones.Contains(term)));
            }

            var orderedIdsQuery = filter.SortDescending
                ? baseQuery.OrderByDescending(t => t.FechaHoraInicio).ThenByDescending(t => t.Id)
                : baseQuery.OrderBy(t => t.FechaHoraInicio).ThenBy(t => t.Id);

            var pageIds = await orderedIdsQuery
                .Select(t => t.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            int totalRecords = (pageNumber == 1 && pageIds.Count < pageSize)
                ? pageIds.Count
                : await baseQuery.CountAsync(ct);

            if (pageIds.Count == 0)
            {
                return (Array.Empty<Turno>(), totalRecords);
            }

            var itemsUnordered = await _dbSet
                .AsNoTracking()
                .AsSingleQuery()
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.MotivoCancelacion)
                .Include(t => t.TurnoServicios)
                    .ThenInclude(ts => ts.Servicio)
                .Where(t => pageIds.Contains(t.Id))
                .ToListAsync(ct);

            var byId = itemsUnordered.ToDictionary(t => t.Id);
            var items = new List<Turno>(pageIds.Count);
            foreach (var id in pageIds)
            {
                if (byId.TryGetValue(id, out var turno))
                {
                    items.Add(turno);
                }
            }

            return (items, totalRecords);
        }

        public async Task<IReadOnlyList<Turno>> GetByClienteIdAsync(int clienteId, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.MotivoCancelacion)
                .Include(t => t.TurnoServicios)
                    .ThenInclude(ts => ts.Servicio)
                .Where(t => t.ClienteId == clienteId)
                .OrderByDescending(t => t.FechaHoraInicio)
                .ToListAsync(ct);
        }

        public async Task AddTurnoServicioAsync(TurnoServicio turnoServicio, CancellationToken ct = default)
        {
            turnoServicio.CreatedAt = DateTime.UtcNow;
            await _context.TurnoServicios.AddAsync(turnoServicio, ct);
        }

        public async Task AddTurnoServiciosRangeAsync(IEnumerable<TurnoServicio> turnoServicios, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var list = turnoServicios.ToList();
            foreach (var ts in list)
            {
                ts.CreatedAt = now;
            }

            if (list.Count > 0)
                await _context.TurnoServicios.AddRangeAsync(list, ct);
        }

        public async Task ReemplazarTurnoServiciosAsync(int turnoId, IEnumerable<TurnoServicio> nuevosServicios, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var existentes = await _context.TurnoServicios
                .Where(ts => ts.TurnoId == turnoId)
                .ToListAsync(ct);

            foreach (var ex in existentes)
            {
                ex.IsDeleted = true;
                ex.DeletedAt = now;
                ex.UpdatedAt = now;
            }

            if (nuevosServicios != null)
            {
                foreach (var item in nuevosServicios)
                {
                    item.TurnoId = turnoId;
                    item.CreatedAt = now;
                    await _context.TurnoServicios.AddAsync(item, ct);
                }
            }
        }

        public async Task AddHistorialEstadoAsync(HistorialEstadoTurno historial, CancellationToken ct = default)
        {
            historial.CreatedAt = DateTime.UtcNow;
            await _context.HistorialEstadoTurnos.AddAsync(historial, ct);
        }

        public async Task<IReadOnlyList<HistorialEstadoTurno>> GetHistorialEstadosByTurnoIdAsync(int turnoId, CancellationToken ct = default)
        {
            return await _context.HistorialEstadoTurnos
                .AsNoTracking()
                .Include(h => h.EstadoNuevo)
                .Where(h => h.TurnoId == turnoId)
                .OrderBy(h => h.FechaCambio)
                .ThenBy(h => h.Id)
                .ToListAsync(ct);
        }

        public async Task<bool> ExisteConsumoGabinetePorTurnoAsync(int turnoId, CancellationToken ct = default)
        {
            string marcadorEspacio = $"Turno #{turnoId} ";
            string marcadorParentesis = $"Turno #{turnoId})";
            string marcadorExacto = $"Turno #{turnoId}";

            return await _context.MovimientosStock
                .AnyAsync(m => m.TipoMovimiento == TipoMovimientoStockEnum.ConsumoInternoSalon
                            && m.Motivo != null
                            && (m.Motivo.Contains(marcadorEspacio)
                                || m.Motivo.Contains(marcadorParentesis)
                                || m.Motivo.EndsWith(marcadorExacto)), ct);
        }

        public Task UpdateAsync(Turno turno, CancellationToken ct = default)
        {
            Update(turno);
            return Task.CompletedTask;
        }
    }
}
