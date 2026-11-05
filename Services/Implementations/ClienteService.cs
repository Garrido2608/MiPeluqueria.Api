using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ClienteService : IClienteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ClienteService(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PagedResult<ClienteResponseDto>>> ObtenerTodosAsync(
            int pageNumber = 1,
            int pageSize = 15,
            string? search = null)
        {
            var filter = new PaginationFilterDto
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search
            };
            return await ObtenerTodosAsync(filter);
        }

        public async Task<ApiResponse<PagedResult<ClienteResponseDto>>> ObtenerTodosAsync(
            PaginationFilterDto filter,
            CancellationToken ct = default)
        {
            var (items, totalRecords) = await _unitOfWork.Clientes.GetPagedAsync(filter, ct);
            var dtos = _mapper.Map<List<ClienteResponseDto>>(items);

            var mesActual = DateTime.UtcNow.Month;
            foreach (var c in dtos)
            {
                c.EsCumpleaneroDelMes = (c.FechaNacimiento.Month == mesActual);
            }

            var pagedResult = new PagedResult<ClienteResponseDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<ClienteResponseDto>>.Exito(pagedResult, "Clientes obtenidos con éxito.");
        }

        public async Task<ApiResponse<ClienteResponseDto>> ObtenerPorIdAsync(int id)
        {
            var cliente = await _unitOfWork.Clientes.GetByIdWithDetailsAsync(id);
            if (cliente == null)
                return ApiResponse<ClienteResponseDto>.Falla($"No se encontró el cliente con ID {id}.");

            var dto = _mapper.Map<ClienteResponseDto>(cliente);
            dto.EsCumpleaneroDelMes = (dto.FechaNacimiento.Month == DateTime.UtcNow.Month);

            return ApiResponse<ClienteResponseDto>.Exito(dto, "Cliente obtenido con éxito.");
        }

        public async Task<ApiResponse<ClienteResponseDto>> CrearClienteAsync(ClienteRequestDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Email) && await _unitOfWork.Clientes.ExistsEmailAsync(dto.Email.Trim()))
                return ApiResponse<ClienteResponseDto>.Falla("Ya existe un cliente registrado con ese correo electrónico en este salón.");

            var cliente = _mapper.Map<Cliente>(dto);
            cliente.Nombre = dto.Nombre.Trim();
            cliente.Apellido = dto.Apellido.Trim();
            cliente.Telefono = dto.Telefono.Trim();
            cliente.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();
            cliente.FechaRegistro = DateTime.UtcNow;
            cliente.Activo = true;

            await _unitOfWork.Clientes.AddAsync(cliente);
            await _unitOfWork.SaveChangesAsync();

            var creado = await _unitOfWork.Clientes.GetByIdWithDetailsAsync(cliente.Id);
            var response = _mapper.Map<ClienteResponseDto>(creado ?? cliente);

            return ApiResponse<ClienteResponseDto>.Exito(response, "Cliente registrado correctamente.");
        }

        public async Task<ApiResponse<ClienteResponseDto>> ActualizarClienteAsync(int id, ClienteRequestDto dto)
        {
            var clienteExistente = await _unitOfWork.Clientes.GetByIdAsync(id);
            if (clienteExistente == null)
                return ApiResponse<ClienteResponseDto>.Falla($"No se encontró el cliente con ID {id}.");

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                await _unitOfWork.Clientes.ExistsEmailAsync(dto.Email.Trim(), excludeId: id))
            {
                return ApiResponse<ClienteResponseDto>.Falla("Ya existe otro cliente registrado con ese correo electrónico en este salón.");
            }

            clienteExistente.Nombre = dto.Nombre.Trim();
            clienteExistente.Apellido = dto.Apellido.Trim();
            clienteExistente.Telefono = dto.Telefono.Trim();
            clienteExistente.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLowerInvariant();
            clienteExistente.FechaNacimiento = dto.FechaNacimiento;
            clienteExistente.TipoCabelloId = dto.TipoCabelloId;
            clienteExistente.TipoRostroId = dto.TipoRostroId;

            await _unitOfWork.Clientes.UpdateAsync(clienteExistente);
            await _unitOfWork.SaveChangesAsync();

            var actualizado = await _unitOfWork.Clientes.GetByIdWithDetailsAsync(id);
            var response = _mapper.Map<ClienteResponseDto>(actualizado ?? clienteExistente);

            return ApiResponse<ClienteResponseDto>.Exito(response, "Cliente actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarClienteAsync(int id)
        {
            var existe = await _unitOfWork.Clientes.GetByIdAsync(id);
            if (existe == null)
                return ApiResponse<bool>.Falla($"No se encontró el cliente con ID {id}.");

            await _unitOfWork.Clientes.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Cliente dado de baja exitosamente (Soft Delete).");
        }
    }
}
