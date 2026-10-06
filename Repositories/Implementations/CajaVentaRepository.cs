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
            _context.SesionesCaja.Add(sesion);
            await _context.SaveChangesAsync();
            return sesion;
        }

        public async Task CerrarCajaAsync(SesionCaja sesion)
        {
            _context.SesionesCaja.Update(sesion);
            await _context.SaveChangesAsync();
        }

        public async Task RegistrarMovimientoCajaAsync(MovimientoCaja movimiento)
        {
            _context.MovimientosCaja.Add(movimiento);
            await _context.SaveChangesAsync();
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
            _context.Ventas.Add(venta);
            await _context.SaveChangesAsync();
            return venta;
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