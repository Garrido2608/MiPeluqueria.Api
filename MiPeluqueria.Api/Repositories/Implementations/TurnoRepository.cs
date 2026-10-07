using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class TurnoRepository : ITurnoRepository
    {
        private readonly ApplicationDbContext _context;

        public TurnoRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Turno>> GetTurnosPeluqueroEnFechaAsync(int peluqueroId, DateTime fecha)
        {
            var inicioDia = fecha.Date;
            var finDia = fecha.Date.AddDays(1);

            return await _context.Turnos
                .AsNoTracking() // Vital para no saturar la memoria en consultas de listas
                .Include(t => t.Cliente)
                .Include(t => t.Estado)
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .Where(t => t.PeluqueroId == peluqueroId
                         && t.FechaHoraInicio >= inicioDia
                         && t.FechaHoraInicio < finDia
                         && t.EstadoId != 5) // 5 = Cancelado
                .OrderBy(t => t.FechaHoraInicio)
                .ToListAsync();
        }

        public async Task<bool> ExisteSolapamientoAsync(int peluqueroId, DateTime inicio, DateTime fin, int? turnoIdExcluir = null)
        {
            return await _context.Turnos
                .AnyAsync(t => t.PeluqueroId == peluqueroId
                            && t.EstadoId != 5 // 5 = Cancelado
                            && (turnoIdExcluir == null || t.Id != turnoIdExcluir)
                            && t.FechaHoraInicio < fin
                            && t.FechaHoraFin > inicio);
        }

        public async Task<Turno?> GetByIdConDetallesAsync(int id)
        {
            return await _context.Turnos
                .Include(t => t.Cliente)
                .Include(t => t.Peluquero)
                .Include(t => t.Estado)
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .Include(t => t.HistorialEstados)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(Turno turno)
        {
            await _context.Turnos.AddAsync(turno);
        }

        public void Update(Turno turno)
        {
            _context.Turnos.Update(turno);
        }

        public async Task AddHistorialEstadoAsync(HistorialEstadoTurno historial)
        {
            await _context.HistorialEstadoTurnos.AddAsync(historial);
        }
    }
}