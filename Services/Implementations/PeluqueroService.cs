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
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class PeluqueroService : IPeluqueroService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public PeluqueroService(IUnitOfWork unitOfWork, ApplicationDbContext context, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<PeluqueroDto>>> GetAllAsync()
        {
            var peluqueros = await _context.Peluqueros
                .Include(p => p.PeluqueroEspecialidades)
                .ThenInclude(pe => pe.Especialidad)
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<PeluqueroDto>>(peluqueros);
            return ApiResponse<IEnumerable<PeluqueroDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<PeluqueroDto>> GetByIdAsync(int id)
        {
            var peluquero = await _context.Peluqueros
                .Include(p => p.PeluqueroEspecialidades)
                .ThenInclude(pe => pe.Especialidad)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (peluquero == null)
                return ApiResponse<PeluqueroDto>.Falla("El peluquero solicitado no existe.");

            return ApiResponse<PeluqueroDto>.Exito(_mapper.Map<PeluqueroDto>(peluquero));
        }

        public async Task<ApiResponse<PeluqueroDto>> CreateAsync(CrearPeluqueroDto dto)
        {
            var peluquero = _mapper.Map<Peluquero>(dto);
            await _context.Peluqueros.AddAsync(peluquero);
            await _unitOfWork.SaveChangesAsync();

            var especialidadesIds = GetEspecialidadesIdsFromDto(dto);
            if (especialidadesIds.Any())
            {
                foreach (var espId in especialidadesIds.Distinct())
                {
                    await _context.PeluqueroEspecialidades.AddAsync(new PeluqueroEspecialidad
                    {
                        PeluqueroId = peluquero.Id,
                        EspecialidadId = espId
                    });
                }
                await _unitOfWork.SaveChangesAsync();
            }

            return await GetByIdAsync(peluquero.Id);
        }

        public async Task<ApiResponse<PeluqueroDto>> UpdateAsync(int id, CrearPeluqueroDto dto)
        {
            var peluquero = await _context.Peluqueros
                .Include(p => p.PeluqueroEspecialidades)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (peluquero == null)
                return ApiResponse<PeluqueroDto>.Falla("El peluquero solicitado no existe.");

            _mapper.Map(dto, peluquero);

            var especialidadesIds = GetEspecialidadesIdsFromDto(dto);
            if (especialidadesIds.Any())
            {
                _context.PeluqueroEspecialidades.RemoveRange(peluquero.PeluqueroEspecialidades);
                foreach (var espId in especialidadesIds.Distinct())
                {
                    await _context.PeluqueroEspecialidades.AddAsync(new PeluqueroEspecialidad
                    {
                        PeluqueroId = peluquero.Id,
                        EspecialidadId = espId
                    });
                }
            }

            _context.Peluqueros.Update(peluquero);
            await _unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var peluquero = await _context.Peluqueros.FirstOrDefaultAsync(p => p.Id == id);
            if (peluquero == null)
                return ApiResponse<bool>.Falla("El peluquero solicitado no existe.");

            peluquero.IsDeleted = true;
            SetOptionalProp(peluquero, "Activo", false);

            _context.Peluqueros.Update(peluquero);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Peluquero dado de baja correctamente.");
        }

        public async Task<ApiResponse<IEnumerable<HorarioLaboralDto>>> ObtenerHorariosAsync(int peluqueroId)
        {
            var horarios = await _context.HorariosLaborales
                .Where(h => h.PeluqueroId == peluqueroId)
                .AsNoTracking()
                .ToListAsync();

            var dtos = horarios.Select(h => new HorarioLaboralDto
            {
                Id = h.Id,
                PeluqueroId = h.PeluqueroId,
                DiaSemana = Convert.ToInt32(GetOptionalProp(h, new[] { "DiaSemana", "Dia" }) ?? 1),
                HoraInicio = GetTimeSpanProp(h, new[] { "HoraInicio", "HoraDesde", "HoraApertura" }) ?? new TimeSpan(9, 0, 0),
                HoraFin = GetTimeSpanProp(h, new[] { "HoraFin", "HoraHasta", "HoraCierre" }) ?? new TimeSpan(18, 0, 0),
                HoraInicioDescanso = GetTimeSpanProp(h, new[] { "HoraInicioDescanso", "DescansoInicio" }),
                HoraFinDescanso = GetTimeSpanProp(h, new[] { "HoraFinDescanso", "DescansoFin" }),
                Activo = (bool?)(GetOptionalProp(h, new[] { "Activo", "EsLaborable" })) ?? true
            })
            .OrderBy(h => h.DiaSemana)
            .ToList();

            return ApiResponse<IEnumerable<HorarioLaboralDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<bool>> ConfigurarHorariosAsync(int peluqueroId, GuardarHorariosPeluqueroDto dto)
        {
            var peluquero = await _context.Peluqueros.FirstOrDefaultAsync(p => p.Id == peluqueroId);
            if (peluquero == null)
                return ApiResponse<bool>.Falla("El peluquero especificado no existe.");

            var existentes = await _context.HorariosLaborales
                .Where(h => h.PeluqueroId == peluqueroId)
                .ToListAsync();

            if (existentes.Any())
                _context.HorariosLaborales.RemoveRange(existentes);

            foreach (var item in dto.Horarios)
            {
                var nuevo = new HorarioLaboral
                {
                    PeluqueroId = peluqueroId
                };

                SetDiaProp(nuevo, new[] { "DiaSemana", "Dia" }, item.DiaSemana);
                SetOptionalProp(nuevo, "HoraInicio", item.HoraInicio);
                SetOptionalProp(nuevo, "HoraDesde", item.HoraInicio);
                SetOptionalProp(nuevo, "HoraFin", item.HoraFin);
                SetOptionalProp(nuevo, "HoraHasta", item.HoraFin);
                if (item.HoraInicioDescanso.HasValue)
                    SetOptionalProp(nuevo, "HoraInicioDescanso", item.HoraInicioDescanso.Value);
                if (item.HoraFinDescanso.HasValue)
                    SetOptionalProp(nuevo, "HoraFinDescanso", item.HoraFinDescanso.Value);
                SetOptionalProp(nuevo, "Activo", item.Activo);

                await _context.HorariosLaborales.AddAsync(nuevo);
            }

            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<bool>.Exito(true, "Horarios laborales actualizados correctamente.");
        }

        private static List<int> GetEspecialidadesIdsFromDto(CrearPeluqueroDto dto)
        {
            var prop = dto.GetType().GetProperty("EspecialidadesIds") ?? dto.GetType().GetProperty("Especialidades");
            if (prop != null && prop.GetValue(dto) is IEnumerable<int> ids)
                return ids.ToList();
            return new List<int>();
        }

        private static object? GetOptionalProp(object target, string[] names)
        {
            var type = target.GetType();
            foreach (var name in names)
            {
                var prop = type.GetProperty(name);
                if (prop != null && prop.CanRead)
                    return prop.GetValue(target);
            }
            return null;
        }

        private static TimeSpan? GetTimeSpanProp(object target, string[] names)
        {
            var val = GetOptionalProp(target, names);
            if (val is TimeSpan ts) return ts;
            if (val is TimeOnly to) return to.ToTimeSpan();
            return null;
        }

        private static void SetOptionalProp(object target, string name, object value)
        {
            var prop = target.GetType().GetProperty(name);
            if (prop != null && prop.CanWrite)
            {
                try { prop.SetValue(target, value); } catch { }
            }
        }

        private static void SetDiaProp(object target, string[] names, int dia)
        {
            var type = target.GetType();
            foreach (var name in names)
            {
                var prop = type.GetProperty(name);
                if (prop != null && prop.CanWrite)
                {
                    if (prop.PropertyType.IsEnum)
                        prop.SetValue(target, Enum.ToObject(prop.PropertyType, dia));
                    else
                        prop.SetValue(target, Convert.ChangeType(dia, prop.PropertyType));
                    return;
                }
            }
        }
    }
}