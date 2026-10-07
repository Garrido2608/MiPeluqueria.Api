using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ClienteService(
            IClienteRepository repository,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ApiResponse<PagedResult<ClienteResponseDto>>> ObtenerTodosAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string? search = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var query = _repository.GetAllQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(term) ||
                    c.Apellido.ToLower().Contains(term) ||
                    c.Telefono.Contains(term) ||
                    (c.Email != null && c.Email.ToLower().Contains(term)));
            }

            int totalRecords = await query.CountAsync();

            var clientesPagina = await query
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<ClienteResponseDto>>(clientesPagina);

            var mesActual = DateTime.UtcNow.Month;
            foreach (var c in dtos)
            {
                c.EsCumpleaneroDelMes = (c.FechaNacimiento.Month == mesActual);
            }

            var pagedResult = new PagedResult<ClienteResponseDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ApiResponse<PagedResult<ClienteResponseDto>>.Exito(pagedResult, "Clientes obtenidos con éxito.");
        }

        public async Task<ApiResponse<ClienteResponseDto>> ObtenerPorIdAsync(int id)
        {
            var cliente = await _repository.GetByIdAsync(id);
            if (cliente == null)
                return ApiResponse<ClienteResponseDto>.Falla($"No se encontró el cliente con ID {id}.");

            var dto = _mapper.Map<ClienteResponseDto>(cliente);
            dto.EsCumpleaneroDelMes = (dto.FechaNacimiento.Month == DateTime.UtcNow.Month);

            return ApiResponse<ClienteResponseDto>.Exito(dto, "Cliente obtenido con éxito.");
        }

        public async Task<ApiResponse<ClienteResponseDto>> CrearClienteAsync(ClienteRequestDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Email) && await _repository.ExistsEmailAsync(dto.Email))
                return ApiResponse<ClienteResponseDto>.Falla("Ya existe un cliente registrado con ese correo electrónico.");

            var cliente = _mapper.Map<Cliente>(dto);

            await _repository.AddAsync(cliente);
            await _unitOfWork.SaveChangesAsync();

            var creado = await _repository.GetByIdAsync(cliente.Id);
            var response = _mapper.Map<ClienteResponseDto>(creado ?? cliente);

            return ApiResponse<ClienteResponseDto>.Exito(response, "Cliente registrado correctamente.");
        }

        public async Task<ApiResponse<ClienteResponseDto>> ActualizarClienteAsync(int id, ClienteRequestDto dto)
        {
            var clienteExistente = await _repository.GetByIdAsync(id);
            if (clienteExistente == null)
                return ApiResponse<ClienteResponseDto>.Falla($"No se encontró el cliente con ID {id}.");

            if (!string.IsNullOrWhiteSpace(dto.Email) &&
                !string.Equals(dto.Email, clienteExistente.Email, StringComparison.OrdinalIgnoreCase) &&
                await _repository.ExistsEmailAsync(dto.Email))
            {
                return ApiResponse<ClienteResponseDto>.Falla("Ya existe otro cliente registrado con ese correo electrónico.");
            }

            _mapper.Map(dto, clienteExistente);

            await _repository.UpdateAsync(clienteExistente);
            await _unitOfWork.SaveChangesAsync();

            var actualizado = await _repository.GetByIdAsync(id);
            var response = _mapper.Map<ClienteResponseDto>(actualizado ?? clienteExistente);

            return ApiResponse<ClienteResponseDto>.Exito(response, "Cliente actualizado correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarClienteAsync(int id)
        {
            var existe = await _repository.GetByIdAsync(id);
            if (existe == null)
                return ApiResponse<bool>.Falla($"No se encontró el cliente con ID {id}.");

            await _repository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Cliente dado de baja exitosamente (Soft Delete).");
        }
    }
}