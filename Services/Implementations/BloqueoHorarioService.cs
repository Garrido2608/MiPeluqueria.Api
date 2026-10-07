using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class BloqueoHorarioService : IBloqueoHorarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITurnoRepository _turnoRepo;
        private readonly IUnitOfWork _unitOfWork;

        public BloqueoHorarioService(
            ApplicationDbContext context,
            ITurnoRepository turnoRepo,
            IUnitOfWork unitOfWork)
        {
            _context = context;
            _turnoRepo = turnoRepo;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<int>> CrearBloqueoAsync(BloqueoHorarioDto dto)
        {
            bool peluqueroExiste = await _context.Peluqueros.AnyAsync(p => p.Id == dto.PeluqueroId && p.Activo);
            if (!peluqueroExiste)
                return ApiResponse<int>.Falla("El profesional especificado no existe o está inactivo.");

            // Verificar que no haya turnos activos ya otorgados en el rango que se pretende bloquear
            bool tieneTurnosActivos = await _turnoRepo.ExisteSolapamientoAsync(dto.PeluqueroId, dto.FechaInicio, dto.FechaFin);
            if (tieneTurnosActivos)
                return ApiResponse<int>.Falla("El profesional posee turnos activos en ese rango horario. Reprograme o cancele dichos turnos antes de bloquear la agenda.");

            var bloqueo = new BloqueoHorario
            {
                PeluqueroId = dto.PeluqueroId,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                Motivo = dto.Motivo
            };

            await _context.BloqueosHorario.AddAsync(bloqueo);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<int>.Exito(bloqueo.Id, "Bloqueo horario registrado correctamente.");
        }

        public async Task<ApiResponse<List<BloqueoHorario>>> ObtenerPorPeluqueroAsync(int peluqueroId)
        {
            var bloqueos = await _context.BloqueosHorario
                .AsNoTracking()
                .Where(b => b.PeluqueroId == peluqueroId)
                .OrderByDescending(b => b.FechaInicio)
                .ToListAsync();

            return ApiResponse<List<BloqueoHorario>>.Exito(bloqueos, "Bloqueos horarios obtenidos.");
        }

        public async Task<ApiResponse<bool>> EliminarBloqueoAsync(int bloqueoId)
        {
            var bloqueo = await _context.BloqueosHorario.FindAsync(bloqueoId);
            if (bloqueo == null)
                return ApiResponse<bool>.Falla("El bloqueo horario indicado no existe.");

            _context.BloqueosHorario.Remove(bloqueo);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Bloqueo horario eliminado (agenda liberada).");
        }
    }
}