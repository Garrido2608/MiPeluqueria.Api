using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class FichaTecnicaService : IFichaTecnicaService
    {
        private readonly IUnitOfWork _uow;

        public FichaTecnicaService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ApiResponse<FichaTecnicaResponseDto>> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        {
            var ficha = await _uow.FichasTecnicas.GetByIdWithDetailsAsync(id, ct);
            if (ficha == null)
                return ApiResponse<FichaTecnicaResponseDto>.Fail($"No se encontró la ficha técnica con ID {id}.");

            return ApiResponse<FichaTecnicaResponseDto>.Ok(MapToDto(ficha));
        }

        public async Task<ApiResponse<IEnumerable<FichaTecnicaResponseDto>>> ObtenerHistorialClienteAsync(int clienteId)
        {
            var fichas = await _uow.FichasTecnicas.GetByClienteIdAsync(clienteId);
            var response = fichas.Select(MapToDto).ToList();
            return ApiResponse<IEnumerable<FichaTecnicaResponseDto>>.Ok(response);
        }

        public async Task<ApiResponse<FichaTecnicaResponseDto>> CrearFichaAsync(CrearFichaTecnicaDto dto)
        {
            var cliente = await _uow.Clientes.GetByIdAsync(dto.ClienteId);
            if (cliente == null)
                return ApiResponse<FichaTecnicaResponseDto>.Fail("El cliente especificado no existe.");

            var peluquero = await _uow.Peluqueros.GetByIdAsync(dto.PeluqueroId);
            if (peluquero == null)
                return ApiResponse<FichaTecnicaResponseDto>.Fail("El profesional especificado no existe.");

            var ficha = new FichaTecnica
            {
                ClienteId = dto.ClienteId,
                PeluqueroId = dto.PeluqueroId,
                TurnoId = dto.TurnoId,
                FechaRegistro = DateTime.UtcNow,
                FormulaAplicada = dto.FormulaAplicada.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
                NotasSensibilidadAlergias = string.IsNullOrWhiteSpace(dto.NotasSensibilidadAlergias) ? null : dto.NotasSensibilidadAlergias.Trim(),
                ProductosUtilizados = string.IsNullOrWhiteSpace(dto.ProductosUtilizados) ? null : dto.ProductosUtilizados.Trim(),
                FotoResultadoUrl = string.IsNullOrWhiteSpace(dto.FotoResultadoUrl) ? null : dto.FotoResultadoUrl.Trim()
            };

            await _uow.FichasTecnicas.AddAsync(ficha);
            await _uow.SaveChangesAsync();

            var creada = await _uow.FichasTecnicas.GetByIdWithDetailsAsync(ficha.Id) ?? ficha;
            creada.Cliente ??= cliente;
            creada.Peluquero ??= peluquero;

            return ApiResponse<FichaTecnicaResponseDto>.Ok(MapToDto(creada), "Ficha técnica registrada correctamente.");
        }

        public async Task<ApiResponse<FichaTecnicaResponseDto>> ActualizarFichaAsync(
            int id,
            ActualizarFichaTecnicaDto dto,
            CancellationToken ct = default)
        {
            var ficha = await _uow.FichasTecnicas.GetByIdWithDetailsAsync(id, ct);
            if (ficha == null)
                return ApiResponse<FichaTecnicaResponseDto>.Fail("La ficha técnica solicitada no existe.");

            if (ficha.PeluqueroId != dto.PeluqueroId)
            {
                var peluquero = await _uow.Peluqueros.GetByIdAsync(dto.PeluqueroId, ct);
                if (peluquero == null)
                    return ApiResponse<FichaTecnicaResponseDto>.Fail("El profesional especificado no existe.");
                ficha.Peluquero = peluquero;
            }

            ficha.PeluqueroId = dto.PeluqueroId;
            ficha.TurnoId = dto.TurnoId;
            ficha.FormulaAplicada = dto.FormulaAplicada.Trim();
            ficha.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();
            ficha.NotasSensibilidadAlergias = string.IsNullOrWhiteSpace(dto.NotasSensibilidadAlergias) ? null : dto.NotasSensibilidadAlergias.Trim();
            ficha.ProductosUtilizados = string.IsNullOrWhiteSpace(dto.ProductosUtilizados) ? null : dto.ProductosUtilizados.Trim();
            ficha.FotoResultadoUrl = string.IsNullOrWhiteSpace(dto.FotoResultadoUrl) ? null : dto.FotoResultadoUrl.Trim();

            _uow.FichasTecnicas.Update(ficha);
            await _uow.SaveChangesAsync(ct);

            var actualizada = await _uow.FichasTecnicas.GetByIdWithDetailsAsync(id, ct) ?? ficha;
            return ApiResponse<FichaTecnicaResponseDto>.Ok(MapToDto(actualizada), "Ficha técnica actualizada correctamente.");
        }

        public async Task<ApiResponse<FichaTecnicaResponseDto>> ActualizarFichaAsync(int id, CrearFichaTecnicaDto dto)
        {
            return await ActualizarFichaAsync(id, new ActualizarFichaTecnicaDto
            {
                PeluqueroId = dto.PeluqueroId,
                TurnoId = dto.TurnoId,
                FormulaAplicada = dto.FormulaAplicada,
                Observaciones = dto.Observaciones,
                NotasSensibilidadAlergias = dto.NotasSensibilidadAlergias,
                ProductosUtilizados = dto.ProductosUtilizados,
                FotoResultadoUrl = dto.FotoResultadoUrl
            });
        }

        public async Task<ApiResponse<bool>> EliminarFichaAsync(int id)
        {
            var ficha = await _uow.FichasTecnicas.GetByIdAsync(id);
            if (ficha == null)
                return ApiResponse<bool>.Fail("La ficha técnica solicitada no existe.");

            _uow.FichasTecnicas.Delete(ficha);
            await _uow.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Ficha técnica eliminada correctamente.");
        }

        private static FichaTecnicaResponseDto MapToDto(FichaTecnica f) => new()
        {
            Id = f.Id,
            ClienteId = f.ClienteId,
            NombreCliente = f.Cliente != null ? $"{f.Cliente.Nombre} {f.Cliente.Apellido}".Trim() : string.Empty,
            PeluqueroId = f.PeluqueroId,
            NombrePeluquero = f.Peluquero != null ? $"{f.Peluquero.Nombre} {f.Peluquero.Apellido}".Trim() : "No asignado",
            TurnoId = f.TurnoId,
            FechaRegistro = f.FechaRegistro,
            FormulaAplicada = f.FormulaAplicada,
            Observaciones = f.Observaciones,
            NotasSensibilidadAlergias = f.NotasSensibilidadAlergias,
            ProductosUtilizados = f.ProductosUtilizados,
            FotoResultadoUrl = f.FotoResultadoUrl
        };
    }
}
