using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Stock;
using MiPeluqueria.Api.Models.Stock;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ProveedorService : IProveedorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;

        public ProveedorService(IUnitOfWork unitOfWork, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<ApiResponse<IEnumerable<ProveedorDto>>> GetAllAsync()
        {
            var proveedores = await _unitOfWork.Proveedores.GetAllAsync();
            var dtos = new List<ProveedorDto>();

            foreach (var p in proveedores)
            {
                var cant = await _context.Productos.CountAsync(prod => prod.ProveedorId == p.Id);
                dtos.Add(MapToDto(p, cant));
            }

            return ApiResponse<IEnumerable<ProveedorDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<ProveedorDto>> GetByIdAsync(int id)
        {
            var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(id);
            if (proveedor == null)
                return ApiResponse<ProveedorDto>.Fail("El proveedor solicitado no existe.");

            var cant = await _context.Productos.CountAsync(prod => prod.ProveedorId == proveedor.Id);
            return ApiResponse<ProveedorDto>.Ok(MapToDto(proveedor, cant));
        }

        public async Task<ApiResponse<ProveedorDto>> CreateAsync(CrearProveedorDto dto)
        {
            if (await _unitOfWork.Proveedores.ExistsByRazonSocialAsync(dto.RazonSocial))
                return ApiResponse<ProveedorDto>.Fail("Ya existe un proveedor registrado con esa razón social.");

            var proveedor = new Proveedor
            {
                RazonSocial = dto.RazonSocial.Trim(),
                Contacto = dto.Contacto?.Trim(),
                Telefono = dto.Telefono?.Trim(),
                Email = dto.Email?.Trim()
            };

            await _unitOfWork.Proveedores.AddAsync(proveedor);
            await _unitOfWork.CommitAsync();

            return ApiResponse<ProveedorDto>.Ok(MapToDto(proveedor, 0), "Proveedor registrado exitosamente.");
        }

        public async Task<ApiResponse<ProveedorDto>> UpdateAsync(int id, CrearProveedorDto dto)
        {
            var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(id);
            if (proveedor == null)
                return ApiResponse<ProveedorDto>.Fail("El proveedor solicitado no existe.");

            if (await _unitOfWork.Proveedores.ExistsByRazonSocialAsync(dto.RazonSocial, id))
                return ApiResponse<ProveedorDto>.Fail("Ya existe otro proveedor con esa razón social.");

            proveedor.RazonSocial = dto.RazonSocial.Trim();
            proveedor.Contacto = dto.Contacto?.Trim();
            proveedor.Telefono = dto.Telefono?.Trim();
            proveedor.Email = dto.Email?.Trim();

            _unitOfWork.Proveedores.Update(proveedor);
            await _unitOfWork.CommitAsync();

            var cant = await _context.Productos.CountAsync(prod => prod.ProveedorId == proveedor.Id);
            return ApiResponse<ProveedorDto>.Ok(MapToDto(proveedor, cant), "Proveedor actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(id);
            if (proveedor == null)
                return ApiResponse<bool>.Fail("El proveedor solicitado no existe.");

            var tieneProductos = await _context.Productos.AnyAsync(p => p.ProveedorId == id);
            if (tieneProductos)
                return ApiResponse<bool>.Fail("No se puede eliminar un proveedor que tiene productos asociados.");

            _unitOfWork.Proveedores.Delete(proveedor);
            await _unitOfWork.CommitAsync();

            return ApiResponse<bool>.Ok(true, "Proveedor dado de baja exitosamente.");
        }

        private static ProveedorDto MapToDto(Proveedor p, int cantidadProductos)
        {
            return new ProveedorDto
            {
                Id = p.Id,
                RazonSocial = p.RazonSocial,
                Contacto = p.Contacto,
                Telefono = p.Telefono,
                Email = p.Email,
                CantidadProductosAsociados = cantidadProductos
            };
        }
    }
}