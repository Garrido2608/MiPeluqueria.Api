using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class CrearFichaTecnicaDto
    {
        public int ClienteId { get; set; }
        public int PeluqueroId { get; set; }
        public int? TurnoId { get; set; }
        public string? DetalleFormulaTintura { get; set; }
        public string? NotasSensibilidadAlergias { get; set; }
    }

    public class FichaTecnicaResponseDto
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public int PeluqueroId { get; set; }
        public string PeluqueroNombre { get; set; } = string.Empty;
        public int? TurnoId { get; set; }
        public DateTime Fecha { get; set; }
        public string? DetalleFormulaTintura { get; set; }
        public string? NotasSensibilidadAlergias { get; set; }
    }

    public interface IFichaTecnicaService
    {
        Task<ApiResponse<List<FichaTecnicaResponseDto>>> ObtenerPorClienteAsync(int clienteId);
        Task<ApiResponse<FichaTecnicaResponseDto>> CrearFichaAsync(CrearFichaTecnicaDto dto);
    }

    public class FichaTecnicaService : IFichaTecnicaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;

        public FichaTecnicaService(ApplicationDbContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<List<FichaTecnicaResponseDto>>> ObtenerPorClienteAsync(int clienteId)
        {
            var fichas = await _context.FichasTecnicas
                .AsNoTracking()
                .Include(f => f.Cliente)
                .Include(f => f.Peluquero)
                .Where(f => f.ClienteId == clienteId)
                .OrderByDescending(f => f.Fecha)
                .Select(f => new FichaTecnicaResponseDto
                {
                    Id = f.Id,
                    ClienteId = f.ClienteId,
                    ClienteNombre = f.Cliente != null ? (f.Cliente.Nombre + " " + f.Cliente.Apellido) : string.Empty,
                    PeluqueroId = f.PeluqueroId,
                    PeluqueroNombre = f.Peluquero != null ? (f.Peluquero.Nombre + " " + f.Peluquero.Apellido) : string.Empty,
                    TurnoId = f.TurnoId,
                    Fecha = f.Fecha,
                    DetalleFormulaTintura = f.DetalleFormulaTintura,
                    NotasSensibilidadAlergias = f.NotasSensibilidadAlergias
                })
                .ToListAsync();

            return ApiResponse<List<FichaTecnicaResponseDto>>.Exito(fichas, "Historial de fichas técnicas obtenido.");
        }

        public async Task<ApiResponse<FichaTecnicaResponseDto>> CrearFichaAsync(CrearFichaTecnicaDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
                return ApiResponse<FichaTecnicaResponseDto>.Falla("El cliente indicado no existe.");

            var peluquero = await _context.Peluqueros.FindAsync(dto.PeluqueroId);
            if (peluquero == null)
                return ApiResponse<FichaTecnicaResponseDto>.Falla("El profesional indicado no existe.");

            var ficha = new FichaTecnica
            {
                ClienteId = dto.ClienteId,
                PeluqueroId = dto.PeluqueroId,
                TurnoId = dto.TurnoId,
                Fecha = DateTime.UtcNow,
                DetalleFormulaTintura = dto.DetalleFormulaTintura,
                NotasSensibilidadAlergias = dto.NotasSensibilidadAlergias
            };

            await _context.FichasTecnicas.AddAsync(ficha);
            await _unitOfWork.SaveChangesAsync();

            return ApiResponse<FichaTecnicaResponseDto>.Exito(new FichaTecnicaResponseDto
            {
                Id = ficha.Id,
                ClienteId = ficha.ClienteId,
                ClienteNombre = $"{cliente.Nombre} {cliente.Apellido}".Trim(),
                PeluqueroId = ficha.PeluqueroId,
                PeluqueroNombre = $"{peluquero.Nombre} {peluquero.Apellido}".Trim(),
                TurnoId = ficha.TurnoId,
                Fecha = ficha.Fecha,
                DetalleFormulaTintura = ficha.DetalleFormulaTintura,
                NotasSensibilidadAlergias = ficha.NotasSensibilidadAlergias
            }, "Ficha técnica capilar registrada exitosamente.");
        }
    }
}