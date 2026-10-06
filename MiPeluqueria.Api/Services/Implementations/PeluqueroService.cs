using System.Collections.Generic;
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

        // Inyectamos el Repositorio, el Unit Of Work y el Mapper
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
                return ApiResponse<PeluqueroDto>.Falla($"No se encontró el profesional con ID {id}");

            var dto = _mapper.Map<PeluqueroDto>(peluquero);
            return ApiResponse<PeluqueroDto>.Exito(dto);
        }

        public async Task<ApiResponse<PeluqueroDto>> CreateAsync(CrearPeluqueroDto dto)
        {
            // 1. Regla de negocio en el Service: No pueden existir dos peluqueros con el mismo email
            if (await _peluqueroRepo.ExistsEmailAsync(dto.Email))
            {
                return ApiResponse<PeluqueroDto>.Falla("Ya existe un profesional registrado con este correo.");
            }

            // 2. Mapeamos el DTO que viene del Frontend hacia la Entidad de la Base de Datos
            var peluquero = _mapper.Map<Peluquero>(dto);

            // 3. El Repositorio "prepara" la inserción en memoria
            await _peluqueroRepo.AddAsync(peluquero);

            // 4. ACÁ BRILLA EL UNIT OF WORK: Confirmamos la transacción físicamente en SQL Server
            await _unitOfWork.SaveChangesAsync();

            // 5. Devolvemos el profesional ya creado con su nuevo ID generado por la BD
            var responseDto = _mapper.Map<PeluqueroDto>(peluquero);
            return ApiResponse<PeluqueroDto>.Exito(responseDto, "Profesional registrado exitosamente.");
        }
    }
}