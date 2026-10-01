using System.Threading.Tasks;
using MiPeluqueria.Api.Models.Ventas;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface ISesionCajaRepository
    {
        Task<SesionCaja?> GetCajaAbiertaActualAsync();
        Task<SesionCaja> AbrirCajaAsync(SesionCaja sesion);
        Task CerrarCajaAsync(SesionCaja sesion);
        Task RegistrarMovimientoCajaAsync(MovimientoCaja movimiento);
    }

    public interface IVentaRepository
    {
        Task<Venta> RegistrarVentaAsync(Venta venta);
        Task<Venta?> GetVentaByIdAsync(int id);
    }
}