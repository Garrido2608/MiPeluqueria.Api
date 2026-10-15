using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class BloqueoHorarioService : IBloqueoHorarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public BloqueoHorarioService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<object>>> ObtenerPorPeluqueroAsync(int peluqueroId)
        {
            var bloqueos = await _context.BloqueosHorario
                .Where(b => b.PeluqueroId == peluqueroId)
                .OrderByDescending(b => b.Id)
                .AsNoTracking()
                .ToListAsync();

            var resultado = bloqueos.Select(b => (object)b).ToList();
            return ApiResponse<IEnumerable<object>>.Exito(resultado);
        }

        public async Task<ApiResponse<int>> CrearBloqueoAsync(BloqueoHorarioDto dto)
        {
            var peluquero = await _context.Peluqueros.FindAsync(dto.PeluqueroId);
            if (peluquero == null)
                return ApiResponse<int>.Falla("El peluquero especificado no existe.");

            var bloqueo = _mapper.Map<BloqueoHorario>(dto);
            await _context.BloqueosHorario.AddAsync(bloqueo);
            await _context.SaveChangesAsync();

            return ApiResponse<int>.Exito(bloqueo.Id, "Bloqueo horario registrado correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarBloqueoAsync(int id)
        {
            var bloqueo = await _context.BloqueosHorario.FindAsync(id);
            if (bloqueo == null)
                return ApiResponse<bool>.Falla("El bloqueo horario especificado no existe.");

            _context.BloqueosHorario.Remove(bloqueo);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Bloqueo horario eliminado correctamente.");
        }
    }
}