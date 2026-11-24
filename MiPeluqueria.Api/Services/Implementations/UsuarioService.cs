using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsuarioService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ApiResponse<IReadOnlyList<UsuarioResponseDto>>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        {
            var usuarios = await _unitOfWork.Usuarios.GetAllWithRolesAsync(cancellationToken);
            var dtos = usuarios.Select(MapToDto).ToList();
            return ApiResponse<IReadOnlyList<UsuarioResponseDto>>.Exito(dtos, "Usuarios obtenidos con éxito.");
        }

        public async Task<ApiResponse<UsuarioResponseDto>> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRolAsync(id, cancellationToken);
            if (usuario == null)
                return ApiResponse<UsuarioResponseDto>.Falla($"No se encontró el usuario con ID {id}.");

            return ApiResponse<UsuarioResponseDto>.Exito(MapToDto(usuario));
        }

        public async Task<ApiResponse<UsuarioResponseDto>> CrearUsuarioStaffAsync(
            CrearUsuarioStaffDto dto,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return ApiResponse<UsuarioResponseDto>.Falla("El nombre de usuario, correo electrónico y contraseña son obligatorios.");

            var rol = await _unitOfWork.Usuarios.GetRolByIdAsync(dto.RolId, cancellationToken);
            if (rol == null)
                return ApiResponse<UsuarioResponseDto>.Falla("El rol especificado no existe en el catálogo maestro.");

            bool existeDuplicado = await _unitOfWork.Usuarios.ExistsUsernameOrEmailAsync(
                dto.Username.Trim(),
                dto.Email.Trim().ToLowerInvariant(),
                null,
                cancellationToken);
            if (existeDuplicado)
                return ApiResponse<UsuarioResponseDto>.Falla("Ya existe un usuario registrado con ese nombre de usuario o correo electrónico en este salón.");

            if (dto.PeluqueroId.HasValue)
            {
                var peluquero = await _unitOfWork.Peluqueros.GetByIdAsync(dto.PeluqueroId.Value, cancellationToken);
                if (peluquero == null)
                    return ApiResponse<UsuarioResponseDto>.Falla($"No se encontró el profesional con ID {dto.PeluqueroId.Value}.");
            }

            var nuevo = new Usuario
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RolId = dto.RolId,
                PeluqueroId = dto.PeluqueroId,
                EmailVerificado = true,
                AceptaTerminos = true,
                FechaAceptacionTerminos = DateTime.UtcNow,
                Activo = true
            };

            await _unitOfWork.Usuarios.AddAsync(nuevo, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var creado = await _unitOfWork.Usuarios.GetByIdWithRolAsync(nuevo.Id, cancellationToken) ?? nuevo;
            return ApiResponse<UsuarioResponseDto>.Exito(MapToDto(creado), "Usuario de personal creado correctamente.");
        }

        public async Task<ApiResponse<UsuarioResponseDto>> ActualizarEstadoAsync(
            int id,
            ActualizarEstadoUsuarioDto dto,
            CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRolAsync(id, cancellationToken);
            if (usuario == null)
                return ApiResponse<UsuarioResponseDto>.Falla($"No se encontró el usuario con ID {id}.");

            if (dto.Activo.HasValue)
            {
                usuario.Activo = dto.Activo.Value;
            }

            if (dto.RolId.HasValue)
            {
                var rol = await _unitOfWork.Usuarios.GetRolByIdAsync(dto.RolId.Value, cancellationToken);
                if (rol == null)
                    return ApiResponse<UsuarioResponseDto>.Falla("El rol especificado no existe en el catálogo maestro.");
                usuario.RolId = dto.RolId.Value;
            }

            if (dto.DesbloquearCuenta)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
            }

            if (dto.Activo == false)
            {
                await _unitOfWork.Usuarios.RevocarRefreshTokensUsuarioAsync(
                    usuario.Id,
                    "Cuenta desactivada por administración",
                    null,
                    cancellationToken);
            }

            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<UsuarioResponseDto>.Exito(MapToDto(usuario), "Estado del usuario actualizado correctamente.");
        }

        public async Task<ApiResponse<UsuarioResponseDto>> DesbloquearUsuarioAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRolAsync(id, cancellationToken);
            if (usuario == null)
                return ApiResponse<UsuarioResponseDto>.Falla($"No se encontró el usuario con ID {id}.");

            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            usuario.Activo = true;

            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return ApiResponse<UsuarioResponseDto>.Exito(MapToDto(usuario), "Cuenta de usuario desbloqueada correctamente.");
        }

        public async Task<ApiResponse<UsuarioResponseDto>> CambiarRolAsync(
            int id,
            int nuevoRolId,
            CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRolAsync(id, cancellationToken);
            if (usuario == null)
                return ApiResponse<UsuarioResponseDto>.Falla($"No se encontró el usuario con ID {id}.");

            var rol = await _unitOfWork.Usuarios.GetRolByIdAsync(nuevoRolId, cancellationToken);
            if (rol == null)
                return ApiResponse<UsuarioResponseDto>.Falla("El rol especificado no existe en el catálogo maestro.");

            usuario.RolId = nuevoRolId;
            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var actualizado = await _unitOfWork.Usuarios.GetByIdWithRolAsync(id, cancellationToken) ?? usuario;
            return ApiResponse<UsuarioResponseDto>.Exito(MapToDto(actualizado), "Rol del usuario actualizado correctamente.");
        }

        public async Task<ApiResponse<PagedResult<HistorialSesionResponseDto>>> ObtenerHistorialSesionesAsync(
            PaginationFilterDto filter,
            int? usuarioId = null,
            CancellationToken cancellationToken = default)
        {
            var (items, totalRecords) = await _unitOfWork.Usuarios.GetHistorialSesionesPagedAsync(filter, usuarioId, cancellationToken);

            var dtos = items.Select(s => new HistorialSesionResponseDto
            {
                Id = s.Id,
                UsuarioId = s.UsuarioId,
                Username = s.Usuario?.Username ?? "Desconocido",
                FechaHora = s.FechaIntento,
                IpOrigen = s.DireccionIP,
                Dispositivo = s.Dispositivo ?? "WebClient",
                Exitoso = s.Exitoso,
                MotivoFallo = s.MotivoFallo?.ToString()
            }).ToList();

            var result = new PagedResult<HistorialSesionResponseDto>
            {
                Data = dtos,
                TotalRecords = totalRecords,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return ApiResponse<PagedResult<HistorialSesionResponseDto>>.Exito(result, "Historial de sesiones obtenido con éxito.");
        }

        private static UsuarioResponseDto MapToDto(Usuario u) => new()
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            RolId = u.RolId,
            RolNombre = u.Rol?.Nombre ?? string.Empty,
            EmailVerificado = u.EmailVerificado,
            Activo = u.Activo,
            IntentosFallidos = u.IntentosFallidos,
            BloqueadoHasta = u.BloqueadoHasta,
            PeluqueroId = u.PeluqueroId,
            ClienteId = u.ClienteId
        };
    }
}
