using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class PeluqueroService : IPeluqueroService
    {
        private readonly IPeluqueroRepository _peluqueroRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PeluqueroService(
            IPeluqueroRepository peluqueroRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _peluqueroRepo = peluqueroRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<PeluqueroDto>>> GetAllAsync()
        {
            var peluqueros = await _peluqueroRepo.GetAllAsync();
            var dtos = _mapper.Map<List<PeluqueroDto>>(peluqueros);
            return ApiResponse<List<PeluqueroDto>>.Exito(dtos, "Peluqueros obtenidos correctamente.");
        }

        public async Task<ApiResponse<PeluqueroDto>> GetByIdAsync(int id)
        {
            var peluquero = await _peluqueroRepo.GetByIdAsync(id);
            if (peluquero == null)
                return ApiResponse<PeluqueroDto>.Falla($"No se encontró el profesional con ID {id}.");

            var dto = _mapper.Map<PeluqueroDto>(peluquero);
            return ApiResponse<PeluqueroDto>.Exito(dto);
        }

        public async Task<ApiResponse<PeluqueroDto>> CreateAsync(CrearPeluqueroDto dto)
        {
            if (await _peluqueroRepo.ExistsEmailAsync(dto.Email))
            {
                return ApiResponse<PeluqueroDto>.Falla("Ya existe un profesional registrado con este correo.");
            }

            var peluquero = _mapper.Map<Peluquero>(dto);

            // 1. Vincular Especialidades en la tabla intermedia N:M
            if (dto.EspecialidadesIds != null && dto.EspecialidadesIds.Any())
            {
                foreach (var espId in dto.EspecialidadesIds.Distinct())
                {
                    peluquero.PeluqueroEspecialidades.Add(new PeluqueroEspecialidad
                    {
                        EspecialidadId = espId
                    });
                }
            }

            // 2. Inicializar jornada laboral operativa base (Lunes a Sábado 09:00 a 20:00 hs)
            var diasLaborables = new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
            };

            foreach (var dia in diasLaborables)
            {
                peluquero.HorariosLaborales.Add(new HorarioLaboral
                {
                    DiaSemana = dia,
                    HoraInicio = new TimeSpan(9, 0, 0),
                    HoraFin = new TimeSpan(20, 0, 0)
                });
            }

            await _peluqueroRepo.AddAsync(peluquero);
            await _unitOfWork.SaveChangesAsync();

            var creado = await _peluqueroRepo.GetByIdAsync(peluquero.Id);
            var responseDto = _mapper.Map<PeluqueroDto>(creado ?? peluquero);

            return ApiResponse<PeluqueroDto>.Exito(responseDto, "Profesional registrado exitosamente con sus especialidades y jornada laboral.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var peluquero = await _peluqueroRepo.GetByIdAsync(id);
            if (peluquero == null)
                return ApiResponse<bool>.Falla($"No se encontró el profesional con ID {id}.");

            await _peluqueroRepo.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Profesional dado de baja lógicamente.");
        }
    }
}