using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Turnos;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface ITurnoRepository
    {
        Task<List<Turno>> GetTurnosPeluqueroEnFechaAsync(int peluqueroId, DateTime fecha);
        Task<bool> ExisteSolapamientoAsync(int peluqueroId, DateTime inicio, DateTime fin, int? turnoIdExcluir = null);
        Task<Turno?> GetByIdConDetallesAsync(int id);
        Task AddAsync(Turno turno);
        void Update(Turno turno);
        Task AddHistorialEstadoAsync(HistorialEstadoTurno historial);
    }
}