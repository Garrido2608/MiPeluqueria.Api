using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public ProveedorService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<PagedResult<ProveedorDto>>> GetPagedAsync(
            PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            var effectiveFilter = filter ?? new PaginationFilterDto();
            var (items, totalRecords) = await _unitOfWork.Proveedores.GetPagedAsync(effectiveFilter, ct);

            var ids = items.Select(p => p.Id).ToList();
            var conteosPorProveedor = ids.Count > 0
                ? await _unitOfWork.Proveedores.GetCantidadProductosPorProveedorAsync(ids, ct)
                : new Dictionary<int, int>();

            var dtos = items
                .Select(p => MapToDto(p, conteosPorProveedor.TryGetValue(p.Id, out int cant) ? cant : 0))
                .ToList();

            var paged = new PagedResult<ProveedorDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = effectiveFilter.PageNumber,
                PageSize = effectiveFilter.PageSize
            };

            return ApiResponse<PagedResult<ProveedorDto>>.Ok(paged);
        }

        public async Task<ApiResponse<IEnumerable<ProveedorDto>>> GetAllAsync(CancellationToken ct = default)
        {
            var proveedores = await _unitOfWork.Proveedores.GetAllAsync(ct);

            // Eliminado bucle N+1 de CountAsync ([BUG-06]): 1 sola consulta GROUP BY en SQL Server
            var ids = proveedores.Select(p => p.Id).ToList();
            var conteosPorProveedor = ids.Count > 0
                ? await _unitOfWork.Proveedores.GetCantidadProductosPorProveedorAsync(ids, ct)
                : new Dictionary<int, int>();

            var dtos = proveedores
                .Select(p => MapToDto(p, conteosPorProveedor.TryGetValue(p.Id, out int cant) ? cant : 0))
                .ToList();

            return ApiResponse<IEnumerable<ProveedorDto>>.Ok(dtos);
        }

        public async Task<ApiResponse<ProveedorDto>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(id, ct);
            if (proveedor == null)
                return ApiResponse<ProveedorDto>.Fail("El proveedor solicitado no existe.");

            int cant = await _unitOfWork.Proveedores.CountProductosAsociadosAsync(id, ct);
            return ApiResponse<ProveedorDto>.Ok(MapToDto(proveedor, cant));
        }

        public async Task<ApiResponse<ProveedorDto>> CreateAsync(CrearProveedorDto dto, CancellationToken ct = default)
        {
            if (await _unitOfWork.Proveedores.ExistsByRazonSocialAsync(dto.RazonSocial, null, ct))
                return ApiResponse<ProveedorDto>.Fail("Ya existe un proveedor registrado con esa razón social.");

            if (!string.IsNullOrWhiteSpace(dto.Cuit) &&
                await _unitOfWork.Proveedores.ExistsByCuitAsync(dto.Cuit, null, ct))
            {
                return ApiResponse<ProveedorDto>.Fail("Ya existe un proveedor registrado con ese CUIT.");
            }

            var proveedor = new Proveedor
            {
                RazonSocial = dto.RazonSocial.Trim(),
                Cuit = string.IsNullOrWhiteSpace(dto.Cuit) ? null : dto.Cuit.Trim(),
                Contacto = string.IsNullOrWhiteSpace(dto.Contacto) ? null : dto.Contacto.Trim(),
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant()
            };

            await _unitOfWork.Proveedores.AddAsync(proveedor, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<ProveedorDto>.Ok(MapToDto(proveedor, 0), "Proveedor registrado exitosamente.");
        }

        public async Task<ApiResponse<ProveedorDto>> UpdateAsync(
            int id,
            ActualizarProveedorDto dto,
            CancellationToken ct = default)
        {
            var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(id, ct);
            if (proveedor == null)
                return ApiResponse<ProveedorDto>.Fail("El proveedor solicitado no existe.");

            if (await _unitOfWork.Proveedores.ExistsByRazonSocialAsync(dto.RazonSocial, id, ct))
                return ApiResponse<ProveedorDto>.Fail("Ya existe otro proveedor registrado con esa razón social.");

            if (!string.IsNullOrWhiteSpace(dto.Cuit) &&
                await _unitOfWork.Proveedores.ExistsByCuitAsync(dto.Cuit, id, ct))
            {
                return ApiResponse<ProveedorDto>.Fail("Ya existe otro proveedor registrado con ese CUIT.");
            }

            proveedor.RazonSocial = dto.RazonSocial.Trim();
            proveedor.Cuit = string.IsNullOrWhiteSpace(dto.Cuit) ? null : dto.Cuit.Trim();
            proveedor.Contacto = string.IsNullOrWhiteSpace(dto.Contacto) ? null : dto.Contacto.Trim();
            proveedor.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
            proveedor.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();

            _unitOfWork.Proveedores.Update(proveedor);
            await _unitOfWork.SaveChangesAsync(ct);

            int cant = await _unitOfWork.Proveedores.CountProductosAsociadosAsync(id, ct);
            return ApiResponse<ProveedorDto>.Ok(MapToDto(proveedor, cant), "Proveedor actualizado correctamente.");
        }

        public Task<ApiResponse<ProveedorDto>> UpdateAsync(int id, CrearProveedorDto dto)
        {
            return UpdateAsync(id, new ActualizarProveedorDto
            {
                RazonSocial = dto.RazonSocial,
                Cuit = dto.Cuit,
                Contacto = dto.Contacto,
                Telefono = dto.Telefono,
                Email = dto.Email
            });
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken ct = default)
        {
            var proveedor = await _unitOfWork.Proveedores.GetByIdAsync(id, ct);
            if (proveedor == null)
                return ApiResponse<bool>.Fail("El proveedor solicitado no existe.");

            if (await _unitOfWork.Proveedores.TieneProductosAsociadosAsync(id, ct))
                return ApiResponse<bool>.Fail("No se puede eliminar un proveedor que tiene productos activos asociados.");

            _unitOfWork.Proveedores.Delete(proveedor);
            await _unitOfWork.SaveChangesAsync(ct);

            return ApiResponse<bool>.Ok(true, "Proveedor dado de baja exitosamente.");
        }

        private static ProveedorDto MapToDto(Proveedor p, int cantidadProductos) => new()
        {
            Id = p.Id,
            RazonSocial = p.RazonSocial,
            Cuit = p.Cuit,
            Contacto = p.Contacto,
            Telefono = p.Telefono,
            Email = p.Email,
            CantidadProductosAsociados = cantidadProductos
        };
    }
}
