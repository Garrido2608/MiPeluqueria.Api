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
    public class ServicioService : IServicioService
    {
        private readonly IServicioRepository _servicioRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ServicioService(
            IServicioRepository servicioRepo,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _servicioRepo = servicioRepo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<ServicioDto>>> GetAllAsync()
        {
            var servicios = await _servicioRepo.GetAllAsync();
            var dtos = _mapper.Map<List<ServicioDto>>(servicios);
            return ApiResponse<List<ServicioDto>>.Exito(dtos, "Catálogo de servicios obtenido.");
        }

        public async Task<ApiResponse<ServicioDto>> GetByIdAsync(int id)
        {
            var servicio = await _servicioRepo.GetByIdAsync(id);
            if (servicio == null)
                return ApiResponse<ServicioDto>.Falla($"No se encontró el servicio con ID {id}");

            var dto = _mapper.Map<ServicioDto>(servicio);
            return ApiResponse<ServicioDto>.Exito(dto);
        }

        public async Task<ApiResponse<ServicioDto>> CreateAsync(CrearServicioDto dto)
        {
            // 1. Mapeamos el DTO de creación a la Entidad de dominio
            var servicio = _mapper.Map<Servicio>(dto);

            // 2. Preparamos en memoria RAM
            await _servicioRepo.AddAsync(servicio);

            // 3. Persistimos de forma atómica en SQL Server
            await _unitOfWork.SaveChangesAsync();

            // 4. Mapeamos de vuelta y respondemos
            var responseDto = _mapper.Map<ServicioDto>(servicio);
            return ApiResponse<ServicioDto>.Exito(responseDto, "Servicio creado exitosamente en el catálogo.");
        }
    }
}