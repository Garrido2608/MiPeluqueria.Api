using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Turnos;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class VentaRepository : IVentaRepository
    {
        private readonly ApplicationDbContext _context;

        public VentaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Venta?> GetByIdCompletaAsync(int ventaId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Venta>()
                .Include(v => v.Cliente)
                .Include(v => v.Turno)
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MedioPago)
                .FirstOrDefaultAsync(v => v.Id == ventaId, cancellationToken);
        }

        public async Task<IReadOnlyList<Venta>> GetBySesionCajaIdAsync(int sesionCajaId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Venta>()
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos)
                    .ThenInclude(p => p.MedioPago)
                .Where(v => v.SesionCajaId == sesionCajaId)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Venta>> GetVentasParaLiquidacionAsync(int peluqueroId, DateTime fechaDesde, DateTime fechaHasta, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Venta>()
                .AsNoTracking()
                .Include(v => v.DetallesVenta)
                .Include(v => v.Turno)
                .Where(v => !v.IsDeleted
                         && v.Fecha >= fechaDesde
                         && v.Fecha <= fechaHasta
                         && (v.DetallesVenta.Any(d => d.PeluqueroId == peluqueroId) || (v.Turno != null && v.Turno.PeluqueroId == peluqueroId)))
                .ToListAsync(cancellationToken);
        }

        public async Task<(string Nombre, decimal Precio)?> GetDatosServicioAsync(int servicioId, CancellationToken cancellationToken = default)
        {
            var servicio = await _context.Set<Servicio>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == servicioId && !s.IsDeleted, cancellationToken);

            if (servicio == null) return null;

            var entry = _context.Entry(servicio);
            string nombre = entry.Metadata.FindProperty("Nombre") != null
                ? entry.Property<string>("Nombre").CurrentValue ?? $"Servicio #{servicioId}"
                : $"Servicio #{servicioId}";

            var propPrecio = entry.Metadata.GetProperties()
                .FirstOrDefault(p => p.ClrType == typeof(decimal) &&
                    (p.Name.Contains("Precio", StringComparison.OrdinalIgnoreCase) ||
                     p.Name.Contains("Monto", StringComparison.OrdinalIgnoreCase) ||
                     p.Name.Contains("Valor", StringComparison.OrdinalIgnoreCase)));

            decimal precio = propPrecio != null
                ? (decimal)(entry.Property(propPrecio.Name).CurrentValue ?? 0m)
                : 0m;

            return (nombre, precio);
        }

        public async Task<string?> GetNombrePeluqueroAsync(int peluqueroId, CancellationToken cancellationToken = default)
        {
            var peluquero = await _context.Set<Peluquero>()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == peluqueroId && !p.IsDeleted, cancellationToken);

            if (peluquero == null) return null;

            var entry = _context.Entry(peluquero);
            var nombre = entry.Metadata.FindProperty("Nombre") != null ? entry.Property<string>("Nombre").CurrentValue : "";
            var apellido = entry.Metadata.FindProperty("Apellido") != null ? entry.Property<string>("Apellido").CurrentValue : "";
            return $"{nombre} {apellido}".Trim();
        }

        public async Task AddAsync(Venta venta, CancellationToken cancellationToken = default)
        {
            venta.CreatedAt = DateTime.UtcNow;
            await _context.Set<Venta>().AddAsync(venta, cancellationToken);
        }

        public void Update(Venta venta)
        {
            venta.UpdatedAt = DateTime.UtcNow;
            _context.Set<Venta>().Update(venta);
        }
    }
}