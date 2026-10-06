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
                .AsNoTracking()
                .Include(t => t.Cliente)
                .Include(t => t.Estado)
                .Include(t => t.TurnoServicios).ThenInclude(ts => ts.Servicio)
                .Where(t => t.PeluqueroId == peluqueroId
                         && t.FechaHoraInicio >= inicioDia
                         && t.FechaHoraInicio < finDia
                         && t.EstadoId != (int)EstadoTurnoEnum.Cancelado)
                .OrderBy(t => t.FechaHoraInicio)
                .ToListAsync();
        }

        public async Task<bool> ExisteSolapamientoAsync(int peluqueroId, DateTime inicio, DateTime fin, int? turnoIdExcluir = null)
        {
            // Fórmula universal de solapamiento de intervalos de tiempo
            return await _context.Turnos
                .AnyAsync(t => t.PeluqueroId == peluqueroId
                            && t.EstadoId != (int)EstadoTurnoEnum.Cancelado
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

        public async Task<Turno> AddAsync(Turno turno)
        {
            _context.Turnos.Add(turno);
            await _context.SaveChangesAsync();
            return turno;
        }

        public async Task UpdateAsync(Turno turno)
        {
            _context.Turnos.Update(turno);
            await _context.SaveChangesAsync();
        }

        public async Task AddHistorialEstadoAsync(HistorialEstadoTurno historial)
        {
            _context.HistorialEstadoTurnos.Add(historial);
            await _context.SaveChangesAsync();
        }
    }
}