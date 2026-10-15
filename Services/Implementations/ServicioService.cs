using System.Collections.Generic;
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
    public class ServicioService : IServicioService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ServicioService(IUnitOfWork unitOfWork, ApplicationDbContext context, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _mapper = mapper;
        }

        public async Task<ApiResponse<IEnumerable<ServicioDto>>> GetAllAsync()
        {
            var servicios = await _context.Servicios
                .AsNoTracking()
                .ToListAsync();

            var dtos = _mapper.Map<IEnumerable<ServicioDto>>(servicios);
            return ApiResponse<IEnumerable<ServicioDto>>.Exito(dtos);
        }

        public async Task<ApiResponse<ServicioDto>> GetByIdAsync(int id)
        {
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
            if (servicio == null)
                return ApiResponse<ServicioDto>.Falla("El servicio solicitado no existe.");

            return ApiResponse<ServicioDto>.Exito(_mapper.Map<ServicioDto>(servicio));
        }

        public async Task<ApiResponse<ServicioDto>> CreateAsync(CrearServicioDto dto)
        {
            var servicio = _mapper.Map<Servicio>(dto);
            servicio.Activo = true;

            await _context.Servicios.AddAsync(servicio);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ServicioDto>.Exito(_mapper.Map<ServicioDto>(servicio), "Servicio creado exitosamente.");
        }

        public async Task<ApiResponse<ServicioDto>> UpdateAsync(int id, CrearServicioDto dto)
        {
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
            if (servicio == null)
                return ApiResponse<ServicioDto>.Falla("El servicio solicitado no existe.");

            _mapper.Map(dto, servicio);
            _context.Servicios.Update(servicio);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<ServicioDto>.Exito(_mapper.Map<ServicioDto>(servicio), "Servicio actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var servicio = await _context.Servicios.FirstOrDefaultAsync(s => s.Id == id);
            if (servicio == null)
                return ApiResponse<bool>.Falla("El servicio solicitado no existe.");

            servicio.IsDeleted = true;
            servicio.Activo = false;

            _context.Servicios.Update(servicio);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Servicio dado de baja correctamente.");
        }
    }
}