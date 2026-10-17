using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Exceptions;
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

        public async Task<IReadOnlyList<UsuarioResponseDto>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
        {
            var usuarios = await _unitOfWork.Usuarios.GetAllWithRolesAsync(cancellationToken);
            return usuarios.Select(MapToResponseDto).ToList();
        }

        public async Task<UsuarioResponseDto> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRolAsync(id, cancellationToken)
                ?? throw new NotFoundException($"No se encontró el usuario con ID {id}.");

            return MapToResponseDto(usuario);
        }

        public async Task<UsuarioResponseDto> ActualizarEstadoAsync(int id, ActualizarEstadoUsuarioDto dto, CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRolAsync(id, cancellationToken)
                ?? throw new NotFoundException($"No se encontró el usuario con ID {id}.");

            if (dto.RolId.HasValue)
            {
                var rol = await _unitOfWork.Usuarios.GetRolByIdAsync(dto.RolId.Value, cancellationToken)
                    ?? throw new NotFoundException($"No existe el rol con ID {dto.RolId.Value}.");
                usuario.RolId = rol.Id;
                usuario.Rol = rol;
            }

            if (dto.Activo.HasValue)
                usuario.Activo = dto.Activo.Value;

            if (dto.DesbloquearCuenta)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
                usuario.Activo = true;
            }

            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToResponseDto(usuario);
        }

        public async Task<UsuarioResponseDto> DesbloquearUsuarioAsync(int id, CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRolAsync(id, cancellationToken)
                ?? throw new NotFoundException($"No se encontró el usuario con ID {id}.");

            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            usuario.Activo = true;

            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToResponseDto(usuario);
        }

        public async Task<UsuarioResponseDto> CambiarRolAsync(int id, int nuevoRolId, CancellationToken cancellationToken = default)
        {
            var usuario = await _unitOfWork.Usuarios.GetByIdWithRolAsync(id, cancellationToken)
                ?? throw new NotFoundException($"No se encontró el usuario con ID {id}.");

            var rol = await _unitOfWork.Usuarios.GetRolByIdAsync(nuevoRolId, cancellationToken)
                ?? throw new NotFoundException($"No existe el rol con ID {nuevoRolId}.");

            usuario.RolId = rol.Id;
            usuario.Rol = rol;

            _unitOfWork.Usuarios.Update(usuario);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return MapToResponseDto(usuario);
        }

        public async Task<IReadOnlyList<HistorialSesionResponseDto>> ObtenerHistorialSesionesAsync(int? usuarioId = null, int top = 100, CancellationToken cancellationToken = default)
        {
            var sesiones = await _unitOfWork.Usuarios.GetHistorialSesionesAsync(usuarioId, top, cancellationToken);

            return sesiones.Select(h => new HistorialSesionResponseDto
            {
                Id = h.Id,
                UsuarioId = h.UsuarioId,
                Username = h.Usuario?.Username ?? "Desconocido",
                FechaHora = h.FechaIntento,
                IpOrigen = h.DireccionIP,
                Dispositivo = h.Dispositivo,
                Exitoso = h.Exitoso,
                MotivoFallo = h.MotivoFallo?.ToString()
            }).ToList();
        }

        private static UsuarioResponseDto MapToResponseDto(Usuario u) => new()
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            RolId = u.RolId,
            RolNombre = u.Rol?.Nombre ?? "Sin Rol",
            EmailVerificado = u.EmailVerificado,
            Activo = u.Activo,
            IntentosFallidos = u.IntentosFallidos,
            BloqueadoHasta = u.BloqueadoHasta,
            ClienteId = u.ClienteId,
            PeluqueroId = u.PeluqueroId
        };
    }
}