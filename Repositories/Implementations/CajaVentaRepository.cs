using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.Ventas;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class SesionCajaRepository : ISesionCajaRepository
    {
        private readonly ApplicationDbContext _context;

        public SesionCajaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SesionCaja?> GetCajaAbiertaActualAsync()
        {
            return await _context.SesionesCaja
                .Include(s => s.Ventas).ThenInclude(v => v.Pagos)
                .Include(s => s.MovimientosCaja)
                .FirstOrDefaultAsync(s => s.EstadoAbierta);
        }

        public async Task<SesionCaja> AbrirCajaAsync(SesionCaja sesion)
        {
            await _context.SesionesCaja.AddAsync(sesion);
            return sesion; // Sin SaveChangesAsync: lo confirma IUnitOfWork en CajaVentaService
        }

        public Task CerrarCajaAsync(SesionCaja sesion)
        {
            _context.SesionesCaja.Update(sesion);
            return Task.CompletedTask; // Sin SaveChangesAsync: lo confirma IUnitOfWork
        }

        public async Task RegistrarMovimientoCajaAsync(MovimientoCaja movimiento)
        {
            await _context.MovimientosCaja.AddAsync(movimiento); // Sin SaveChangesAsync
        }
    }

    public class VentaRepository : IVentaRepository
    {
        private readonly ApplicationDbContext _context;

        public VentaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Venta> RegistrarVentaAsync(Venta venta)
        {
            await _context.Ventas.AddAsync(venta);
            return venta; // Sin SaveChangesAsync: se confirma en CommitTransactionAsync()
        }

        public async Task<Venta?> GetVentaByIdAsync(int id)
        {
            return await _context.Ventas
                .Include(v => v.DetallesVenta)
                .Include(v => v.Pagos).ThenInclude(p => p.MedioPago)
                .FirstOrDefaultAsync(v => v.Id == id);
        }
    }
}