using AutoMapper;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ClienteService : IClienteService
    {
        private readonly IClienteRepository _repository;
        private readonly IMapper _mapper;

        public ClienteService(IClienteRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<List<ClienteResponseDto>>> ObtenerTodosAsync()
        {
            var clientes = await _repository.GetAllAsync();
            var dtos = _mapper.Map<List<ClienteResponseDto>>(clientes);

            var mesActual = DateTime.UtcNow.Month;
            foreach (var c in dtos)
            {
                c.EsCumpleaneroDelMes = (c.FechaNacimiento.Month == mesActual);
            }

            return ApiResponse<List<ClienteResponseDto>>.Exito(dtos, "Clientes obtenidos con éxito");
        }

        public async Task<ApiResponse<ClienteResponseDto>> ObtenerPorIdAsync(int id)
        {
            var cliente = await _repository.GetByIdAsync(id);
            if (cliente == null)
            {
                return ApiResponse<ClienteResponseDto>.Falla($"No se encontró el cliente con ID {id}");
            }

            var dto = _mapper.Map<ClienteResponseDto>(cliente);
            return ApiResponse<ClienteResponseDto>.Exito(dto, "Cliente obtenido con éxito");
        }

        public async Task<ApiResponse<ClienteResponseDto>> CrearClienteAsync(ClienteRequestDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Email) && await _repository.ExistsEmailAsync(dto.Email))
            {
                return ApiResponse<ClienteResponseDto>.Falla("Ya existe un cliente registrado con ese correo");
            }

            var cliente = _mapper.Map<Cliente>(dto);
            await _repository.AddAsync(cliente);

            var response = _mapper.Map<ClienteResponseDto>(cliente);
            return ApiResponse<ClienteResponseDto>.Exito(response, "Cliente registrado correctamente");
        }

        public async Task<ApiResponse<ClienteResponseDto>> ActualizarClienteAsync(int id, ClienteRequestDto dto)
        {
            var clienteExistente = await _repository.GetByIdAsync(id);
            if (clienteExistente == null)
            {
                return ApiResponse<ClienteResponseDto>.Falla($"No se encontró el cliente con ID {id}");
            }

            // Validar email duplicado SOLO si lo está cambiando por uno nuevo que ya le pertenece a otro
            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != clienteExistente.Email && await _repository.ExistsEmailAsync(dto.Email))
            {
                return ApiResponse<ClienteResponseDto>.Falla("Ya existe otro cliente registrado con ese correo");
            }

            // AutoMapper aplica los datos del DTO directamente sobre la entidad que ya trajimos de la BD
            _mapper.Map(dto, clienteExistente);

            await _repository.UpdateAsync(clienteExistente);

            var response = _mapper.Map<ClienteResponseDto>(clienteExistente);
            return ApiResponse<ClienteResponseDto>.Exito(response, "Cliente actualizado correctamente");
        }

        public async Task<ApiResponse<bool>> EliminarClienteAsync(int id)
        {
            var existe = await _repository.GetByIdAsync(id);
            if (existe == null)
            {
                return ApiResponse<bool>.Falla($"No se encontró el cliente con ID {id}");
            }

            await _repository.DeleteAsync(id);
            return ApiResponse<bool>.Exito(true, "Cliente dado de baja exitosamente (Soft Delete)");
        }
    }
}