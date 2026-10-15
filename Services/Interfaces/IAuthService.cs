using System.Collections.Generic;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;

namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface IAuthService
    {
        Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto, string ip = "127.0.0.1", string dispositivo = "WebClient");
        Task<ApiResponse<UsuarioResponseDto>> RegistrarUsuarioAsync(RegistroUsuarioDto dto);
        Task<ApiResponse<bool>> VerificarEmailAsync(VerificarEmailDto dto);
        Task<ApiResponse<bool>> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto);
        Task<ApiResponse<bool>> SolicitarRecuperacionPasswordAsync(OlvidePasswordDto dto);
        Task<ApiResponse<bool>> RestablecerPasswordConOtpAsync(RestablecerPasswordDto dto);
        Task<ApiResponse<IEnumerable<UsuarioResponseDto>>> ObtenerUsuariosAsync();
        Task<ApiResponse<UsuarioResponseDto>> ActualizarEstadoUsuarioAsync(int id, ActualizarEstadoUsuarioDto dto);
        Task<ApiResponse<IEnumerable<HistorialSesionResponseDto>>> ObtenerHistorialSesionesAsync(int? usuarioId = null, int top = 100);
    }
}