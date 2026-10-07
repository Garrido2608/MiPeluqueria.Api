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
                return ApiResponse<ServicioDto>.Falla($"No se encontró el servicio con ID {id}.");

            var dto = _mapper.Map<ServicioDto>(servicio);
            return ApiResponse<ServicioDto>.Exito(dto);
        }

        public async Task<ApiResponse<ServicioDto>> CreateAsync(CrearServicioDto dto)
        {
            var servicio = _mapper.Map<Servicio>(dto);
            await _servicioRepo.AddAsync(servicio);
            await _unitOfWork.SaveChangesAsync();

            var creado = await _servicioRepo.GetByIdAsync(servicio.Id);
            var responseDto = _mapper.Map<ServicioDto>(creado ?? servicio);

            return ApiResponse<ServicioDto>.Exito(responseDto, "Servicio creado exitosamente en el catálogo.");
        }

        public async Task<ApiResponse<ServicioDto>> UpdateAsync(int id, CrearServicioDto dto)
        {
            var existente = await _servicioRepo.GetByIdAsync(id);
            if (existente == null)
                return ApiResponse<ServicioDto>.Falla($"No se encontró el servicio con ID {id}.");

            _mapper.Map(dto, existente);

            await _servicioRepo.UpdateAsync(existente);
            await _unitOfWork.SaveChangesAsync();

            var actualizado = await _servicioRepo.GetByIdAsync(id);
            return ApiResponse<ServicioDto>.Exito(_mapper.Map<ServicioDto>(actualizado!), "Servicio actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var existente = await _servicioRepo.GetByIdAsync(id);
            if (existente == null)
                return ApiResponse<bool>.Falla($"No se encontró el servicio con ID {id}.");

            await _servicioRepo.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Servicio dado de baja lógicamente.");
        }
    }
}