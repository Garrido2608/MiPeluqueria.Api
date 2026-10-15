using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Clientes;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Models.Clientes;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class FichaTecnicaService : IFichaTecnicaService
    {
        private readonly ApplicationDbContext _context;

        public FichaTecnicaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<FichaTecnicaResponseDto>> CrearFichaAsync(CrearFichaTecnicaDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
                return ApiResponse<FichaTecnicaResponseDto>.Fail("El cliente especificado no existe.");

            var ficha = new FichaTecnica
            {
                ClienteId = dto.ClienteId,
                TurnoId = dto.TurnoId,
                PeluqueroId = dto.PeluqueroId
            };

            SetOptionalProp(ficha, new[] { "FormulaAplicada", "FormulaColor", "Formula", "TrabajoRealizado" }, dto.FormulaAplicada);
            SetOptionalProp(ficha, new[] { "Observaciones", "Notas", "NotasTecnicas", "Descripcion", "Comentarios" }, dto.Observaciones);
            SetOptionalProp(ficha, new[] { "ProductosUtilizados", "Productos" }, dto.ProductosUtilizados);
            SetOptionalProp(ficha, new[] { "FotoResultadoUrl", "FotoUrl" }, dto.FotoResultadoUrl);
            SetOptionalDate(ficha, new[] { "FechaRegistro", "Fecha", "FechaCreacion" }, DateTime.UtcNow);

            _context.FichasTecnicas.Add(ficha);
            await _context.SaveChangesAsync();

            var peluqueroNombre = "No asignado";
            if (dto.PeluqueroId > 0)
            {
                var peluquero = await _context.Peluqueros.FindAsync(dto.PeluqueroId);
                if (peluquero != null)
                    peluqueroNombre = $"{peluquero.Nombre} {peluquero.Apellido}";
            }

            var response = MapToDto(ficha, $"{cliente.Nombre} {cliente.Apellido}", peluqueroNombre, dto);
            return ApiResponse<FichaTecnicaResponseDto>.Ok(response, "Ficha técnica registrada correctamente.");
        }

        public async Task<ApiResponse<IEnumerable<FichaTecnicaResponseDto>>> ObtenerHistorialClienteAsync(int clienteId)
        {
            var fichas = await _context.FichasTecnicas
                .Include(f => f.Cliente)
                .Include(f => f.Peluquero)
                .Where(f => f.ClienteId == clienteId && !f.IsDeleted)
                .OrderByDescending(f => f.Id)
                .AsNoTracking()
                .ToListAsync();

            var response = fichas.Select(f => MapToDto(
                f,
                f.Cliente != null ? $"{f.Cliente.Nombre} {f.Cliente.Apellido}" : string.Empty,
                f.Peluquero != null ? $"{f.Peluquero.Nombre} {f.Peluquero.Apellido}" : "No asignado",
                null
            )).ToList();

            return ApiResponse<IEnumerable<FichaTecnicaResponseDto>>.Ok(response);
        }

        public async Task<ApiResponse<FichaTecnicaResponseDto>> ActualizarFichaAsync(int id, CrearFichaTecnicaDto dto)
        {
            var ficha = await _context.FichasTecnicas
                .Include(f => f.Cliente)
                .Include(f => f.Peluquero)
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

            if (ficha == null)
                return ApiResponse<FichaTecnicaResponseDto>.Fail("La ficha técnica solicitada no existe.");

            ficha.TurnoId = dto.TurnoId;
            ficha.PeluqueroId = dto.PeluqueroId;

            SetOptionalProp(ficha, new[] { "FormulaAplicada", "FormulaColor", "Formula", "TrabajoRealizado" }, dto.FormulaAplicada);
            SetOptionalProp(ficha, new[] { "Observaciones", "Notas", "NotasTecnicas", "Descripcion", "Comentarios" }, dto.Observaciones);
            SetOptionalProp(ficha, new[] { "ProductosUtilizados", "Productos" }, dto.ProductosUtilizados);
            SetOptionalProp(ficha, new[] { "FotoResultadoUrl", "FotoUrl" }, dto.FotoResultadoUrl);

            await _context.SaveChangesAsync();

            var response = MapToDto(
                ficha,
                ficha.Cliente != null ? $"{ficha.Cliente.Nombre} {ficha.Cliente.Apellido}" : string.Empty,
                ficha.Peluquero != null ? $"{ficha.Peluquero.Nombre} {ficha.Peluquero.Apellido}" : "No asignado",
                dto
            );

            return ApiResponse<FichaTecnicaResponseDto>.Ok(response, "Ficha técnica actualizada correctamente.");
        }

        public async Task<ApiResponse<bool>> EliminarFichaAsync(int id)
        {
            var ficha = await _context.FichasTecnicas.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (ficha == null)
                return ApiResponse<bool>.Fail("La ficha técnica solicitada no existe.");

            ficha.IsDeleted = true;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Ok(true, "Ficha técnica eliminada correctamente.");
        }

        private static FichaTecnicaResponseDto MapToDto(FichaTecnica f, string nombreCliente, string nombrePeluquero, CrearFichaTecnicaDto? fallbackDto)
        {
            return new FichaTecnicaResponseDto
            {
                Id = f.Id,
                ClienteId = f.ClienteId,
                NombreCliente = nombreCliente,
                TurnoId = f.TurnoId,
                PeluqueroId = f.PeluqueroId,
                NombrePeluquero = nombrePeluquero,
                FechaRegistro = GetOptionalDate(f, new[] { "FechaRegistro", "Fecha", "FechaCreacion" }),
                FormulaAplicada = GetOptionalProp(f, new[] { "FormulaAplicada", "FormulaColor", "Formula", "TrabajoRealizado" }) ?? fallbackDto?.FormulaAplicada ?? string.Empty,
                Observaciones = GetOptionalProp(f, new[] { "Observaciones", "Notas", "NotasTecnicas", "Descripcion", "Comentarios" }) ?? fallbackDto?.Observaciones,
                ProductosUtilizados = GetOptionalProp(f, new[] { "ProductosUtilizados", "Productos" }) ?? fallbackDto?.ProductosUtilizados,
                FotoResultadoUrl = GetOptionalProp(f, new[] { "FotoResultadoUrl", "FotoUrl" }) ?? fallbackDto?.FotoResultadoUrl
            };
        }

        private static void SetOptionalProp(object target, string[] propNames, string? value)
        {
            var type = target.GetType();
            foreach (var name in propNames)
            {
                var prop = type.GetProperty(name);
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string))
                {
                    prop.SetValue(target, value ?? string.Empty);
                    return;
                }
            }
        }

        private static string? GetOptionalProp(object target, string[] propNames)
        {
            var type = target.GetType();
            foreach (var name in propNames)
            {
                var prop = type.GetProperty(name);
                if (prop != null && prop.CanRead)
                {
                    return prop.GetValue(target)?.ToString();
                }
            }
            return null;
        }

        private static void SetOptionalDate(object target, string[] propNames, DateTime value)
        {
            var type = target.GetType();
            foreach (var name in propNames)
            {
                var prop = type.GetProperty(name);
                if (prop != null && prop.CanWrite && prop.PropertyType == typeof(DateTime))
                {
                    prop.SetValue(target, value);
                    return;
                }
            }
        }

        private static DateTime GetOptionalDate(object target, string[] propNames)
        {
            var type = target.GetType();
            foreach (var name in propNames)
            {
                var prop = type.GetProperty(name);
                if (prop != null && prop.CanRead && prop.GetValue(target) is DateTime dt)
                {
                    return dt;
                }
            }
            return DateTime.UtcNow;
        }
    }
}