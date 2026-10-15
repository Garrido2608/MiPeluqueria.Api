using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Ventas;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ICajaVentaService
    {
        Task<ApiResponse<int>> AbrirCajaAsync(AperturaCajaDto dto);
        Task<ApiResponse<int>> RegistrarVentaAsync(CrearVentaDto dto, int usuarioId);
        Task<ApiResponse<bool>> RegistrarEgresoCajaAsync(int sesionCajaId, decimal monto, string concepto, string tipo = "EgresoMenor");
        Task<ApiResponse<decimal>> CerrarCajaAsync(CierreCajaDto dto);
    }
}