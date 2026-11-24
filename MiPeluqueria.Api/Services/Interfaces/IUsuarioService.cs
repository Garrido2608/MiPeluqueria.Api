using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<ApiResponse<IReadOnlyList<UsuarioResponseDto>>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
        Task<ApiResponse<UsuarioResponseDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<ApiResponse<UsuarioResponseDto>> CrearUsuarioStaffAsync(CrearUsuarioStaffDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<UsuarioResponseDto>> ActualizarEstadoAsync(int id, ActualizarEstadoUsuarioDto dto, CancellationToken cancellationToken = default);
        Task<ApiResponse<UsuarioResponseDto>> DesbloquearUsuarioAsync(int id, CancellationToken cancellationToken = default);
        Task<ApiResponse<UsuarioResponseDto>> CambiarRolAsync(int id, int nuevoRolId, CancellationToken cancellationToken = default);
        Task<ApiResponse<PagedResult<HistorialSesionResponseDto>>> ObtenerHistorialSesionesAsync(
            PaginationFilterDto filter,
            int? usuarioId = null,
            CancellationToken cancellationToken = default);
    }
}
