using MiPeluqueria.Api.DTOs.Seguridad;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IUsuarioService
    {
        Task<IReadOnlyList<UsuarioResponseDto>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
        Task<UsuarioResponseDto> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
        Task<UsuarioResponseDto> ActualizarEstadoAsync(int id, ActualizarEstadoUsuarioDto dto, CancellationToken cancellationToken = default);
        Task<UsuarioResponseDto> DesbloquearUsuarioAsync(int id, CancellationToken cancellationToken = default);
        Task<UsuarioResponseDto> CambiarRolAsync(int id, int nuevoRolId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HistorialSesionResponseDto>> ObtenerHistorialSesionesAsync(int? usuarioId = null, int top = 100, CancellationToken cancellationToken = default);
    }
}