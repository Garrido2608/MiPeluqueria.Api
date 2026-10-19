using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Integrations.Email;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly ITenantProvider _tenantProvider;

        public AuthService(
            IUnitOfWork uow,
            IConfiguration config,
            IEmailService emailService,
            ITenantProvider tenantProvider)
        {
            _uow = uow;
            _config = config;
            _emailService = emailService;
            _tenantProvider = tenantProvider;
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(
            LoginRequestDto dto,
            string ip = "127.0.0.1",
            string dispositivo = "WebClient")
        {
            var identificador = dto.UsernameOrEmail.Trim();
            var usuario = await _uow.Usuarios.GetByUsernameOrEmailAsync(identificador);

            if (usuario == null)
                return ApiResponse<AuthResponseDto>.Falla("Usuario o contraseña incorrectos.");

            if (!usuario.Activo)
                return ApiResponse<AuthResponseDto>.Falla("La cuenta de usuario se encuentra desactivada.");

            if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
            {
                await RegistrarIntentoSesionAsync(usuario.Id, ip, dispositivo, false, MotivoFalloSesionEnum.CuentaBloqueada);
                return ApiResponse<AuthResponseDto>.Falla($"La cuenta se encuentra bloqueada temporalmente hasta {usuario.BloqueadoHasta.Value:HH:mm:ss} UTC.");
            }

            bool passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);
            if (!passwordValida)
            {
                usuario.IntentosFallidos += 1;
                if (usuario.IntentosFallidos >= 5)
                {
                    usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(15);
                }

                _uow.Usuarios.Update(usuario);
                await RegistrarIntentoSesionAsync(usuario.Id, ip, dispositivo, false, MotivoFalloSesionEnum.CredencialesIncorrectas);

                return ApiResponse<AuthResponseDto>.Falla("Usuario o contraseña incorrectos.");
            }

            // Login Exitoso: restablecer contador de fallos y registrar sesión
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            _uow.Usuarios.Update(usuario);

            await RegistrarIntentoSesionAsync(usuario.Id, ip, dispositivo, true, MotivoFalloSesionEnum.Ninguno);

            int tenantId = _tenantProvider.CurrentTenantId > 0 ? _tenantProvider.CurrentTenantId : 1;
            int sucursalId = _tenantProvider.CurrentSucursalId ?? 1;

            var jwtKey = _config["Jwt:Key"] ?? "ClaveSecretaSuperSeguraMiPeluqueriaSaaS2026!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var rolNombre = usuario.Rol?.Nombre ?? "Cliente";
            var expiracionJwt = DateTime.UtcNow.AddHours(8);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim("Username", usuario.Username),
                new Claim(ClaimTypes.Role, rolNombre),
                new Claim("TenantId", tenantId.ToString()),
                new Claim("SucursalId", sucursalId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expiracionJwt,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // Generar y persistir RefreshToken en base de datos
            var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var refreshTokenEntity = new RefreshToken
            {
                UsuarioId = usuario.Id,
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ip,
                CreatedByUserId = usuario.Id
            };

            await _uow.Usuarios.AddRefreshTokenAsync(refreshTokenEntity);
            await _uow.SaveChangesAsync();

            var response = new AuthResponseDto
            {
                UsuarioId = usuario.Id,
                Username = usuario.Username,
                Email = usuario.Email,
                Rol = rolNombre,
                Token = tokenString,
                RefreshToken = refreshTokenString,
                Expiracion = expiracionJwt,
                TenantId = tenantId,
                SucursalId = sucursalId
            };

            return ApiResponse<AuthResponseDto>.Exito(response, "Inicio de sesión exitoso.");
        }

        public async Task<ApiResponse<UsuarioResponseDto>> RegistrarUsuarioAsync(RegistroUsuarioDto dto)
        {
            var emailNorm = dto.Email.Trim().ToLower();
            var userNorm = dto.Username.Trim();

            bool existe = await _uow.Usuarios.ExistsUsernameOrEmailAsync(userNorm, emailNorm);
            if (existe)
                return ApiResponse<UsuarioResponseDto>.Falla("El nombre de usuario o correo electrónico ya se encuentra registrado.");

            string codigoOtp = Random.Shared.Next(100000, 999999).ToString();

            var nuevoUsuario = new Usuario
            {
                Username = userNorm,
                Email = emailNorm,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RolId = dto.RolId > 0 ? dto.RolId : 4,
                IntentosFallidos = 0,
                EmailVerificado = false,
                CodigoVerificacion = codigoOtp,
                FechaExpiracionCodigo = DateTime.UtcNow.AddMinutes(15),
                AceptaTerminos = dto.AceptaTerminos,
                FechaAceptacionTerminos = DateTime.UtcNow,
                Activo = true,
                ClienteId = dto.ClienteId,
                PeluqueroId = dto.PeluqueroId
            };

            await _uow.Usuarios.AddAsync(nuevoUsuario);
            await _uow.SaveChangesAsync();

            await _emailService.EnviarCodigoOtpAsync(nuevoUsuario.Email, nuevoUsuario.Username, codigoOtp, "Verificación de Cuenta");

            var creado = await _uow.Usuarios.GetByIdWithRolAsync(nuevoUsuario.Id);

            return ApiResponse<UsuarioResponseDto>.Exito(
                MapToUsuarioDto(creado ?? nuevoUsuario),
                "Usuario registrado correctamente. Se envió un código de 6 dígitos a su correo electrónico.");
        }

        public async Task<ApiResponse<bool>> VerificarEmailAsync(VerificarEmailDto dto)
        {
            var usuario = await _uow.Usuarios.GetByEmailAsync(dto.Email);
            if (usuario == null)
                return ApiResponse<bool>.Falla("No existe un usuario registrado con ese correo electrónico.");

            if (string.IsNullOrWhiteSpace(usuario.CodigoVerificacion) || usuario.CodigoVerificacion != dto.CodigoVerificacion.Trim())
                return ApiResponse<bool>.Falla("El código de verificación ingresado es inválido.");

            if (usuario.FechaExpiracionCodigo.HasValue && usuario.FechaExpiracionCodigo.Value < DateTime.UtcNow)
                return ApiResponse<bool>.Falla("El código de verificación ha expirado.");

            usuario.EmailVerificado = true;
            usuario.CodigoVerificacion = null;
            usuario.FechaExpiracionCodigo = null;

            _uow.Usuarios.Update(usuario);
            await _uow.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Correo electrónico verificado exitosamente.");
        }

        public async Task<ApiResponse<bool>> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto)
        {
            var usuario = await _uow.Usuarios.GetByIdWithRolAsync(usuarioId);
            if (usuario == null)
                return ApiResponse<bool>.Falla("El usuario no existe.");

            if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
                return ApiResponse<bool>.Falla("La contraseña actual ingresada es incorrecta.");

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            _uow.Usuarios.Update(usuario);
            await _uow.Usuarios.RevocarRefreshTokensUsuarioAsync(usuario.Id, "Cambio de contraseña por el usuario");
            await _uow.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Contraseña actualizada exitosamente.");
        }

        public async Task<ApiResponse<bool>> SolicitarRecuperacionPasswordAsync(OlvidePasswordDto dto)
        {
            var usuario = await _uow.Usuarios.GetByEmailAsync(dto.Email);
            if (usuario == null)
                return ApiResponse<bool>.Exito(true, "Si el correo existe en nuestro sistema, recibirá un código de recuperación en instantes.");

            string codigoOtp = Random.Shared.Next(100000, 999999).ToString();
            usuario.CodigoVerificacion = codigoOtp;
            usuario.FechaExpiracionCodigo = DateTime.UtcNow.AddMinutes(15);

            _uow.Usuarios.Update(usuario);
            await _uow.SaveChangesAsync();

            await _emailService.EnviarCodigoOtpAsync(usuario.Email, usuario.Username, codigoOtp, "Recuperación de Contraseña");

            return ApiResponse<bool>.Exito(true, "Se ha enviado un código OTP de 6 dígitos a su correo electrónico.");
        }

        public async Task<ApiResponse<bool>> RestablecerPasswordConOtpAsync(RestablecerPasswordDto dto)
        {
            var usuario = await _uow.Usuarios.GetByEmailAsync(dto.Email);
            if (usuario == null)
                return ApiResponse<bool>.Falla("Datos de recuperación inválidos.");

            if (string.IsNullOrWhiteSpace(usuario.CodigoVerificacion) || usuario.CodigoVerificacion != dto.CodigoOtp.Trim())
                return ApiResponse<bool>.Falla("El código OTP ingresado no es válido.");

            if (usuario.FechaExpiracionCodigo.HasValue && usuario.FechaExpiracionCodigo.Value < DateTime.UtcNow)
                return ApiResponse<bool>.Falla("El código OTP ha expirado. Solicite uno nuevo.");

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            usuario.CodigoVerificacion = null;
            usuario.FechaExpiracionCodigo = null;

            _uow.Usuarios.Update(usuario);
            await _uow.Usuarios.RevocarRefreshTokensUsuarioAsync(usuario.Id, "Restablecimiento de contraseña vía OTP");
            await _uow.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Su contraseña ha sido restablecida exitosamente. Ya puede iniciar sesión.");
        }

        public async Task<ApiResponse<IEnumerable<UsuarioResponseDto>>> ObtenerUsuariosAsync()
        {
            var usuarios = await _uow.Usuarios.GetAllWithRolesAsync();
            return ApiResponse<IEnumerable<UsuarioResponseDto>>.Exito(usuarios.Select(MapToUsuarioDto));
        }

        public async Task<ApiResponse<UsuarioResponseDto>> ActualizarEstadoUsuarioAsync(int id, ActualizarEstadoUsuarioDto dto)
        {
            var usuario = await _uow.Usuarios.GetByIdWithRolAsync(id);
            if (usuario == null)
                return ApiResponse<UsuarioResponseDto>.Falla("El usuario solicitado no existe.");

            if (dto.RolId.HasValue && dto.RolId.Value > 0)
                usuario.RolId = dto.RolId.Value;

            if (dto.Activo.HasValue)
            {
                usuario.Activo = dto.Activo.Value;
                if (!dto.Activo.Value)
                {
                    await _uow.Usuarios.RevocarRefreshTokensUsuarioAsync(usuario.Id, "Usuario desactivado por administración");
                }
            }

            if (dto.DesbloquearCuenta)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
            }

            _uow.Usuarios.Update(usuario);
            await _uow.SaveChangesAsync();

            var actualizado = await _uow.Usuarios.GetByIdWithRolAsync(id);
            return ApiResponse<UsuarioResponseDto>.Exito(MapToUsuarioDto(actualizado ?? usuario), "Usuario actualizado correctamente.");
        }

        public async Task<ApiResponse<IEnumerable<HistorialSesionResponseDto>>> ObtenerHistorialSesionesAsync(int? usuarioId = null, int top = 100)
        {
            var registros = await _uow.Usuarios.GetHistorialSesionesAsync(usuarioId, top);

            var dtos = registros.Select(h => new HistorialSesionResponseDto
            {
                Id = h.Id,
                UsuarioId = h.UsuarioId,
                Username = h.Usuario?.Username ?? $"Usuario #{h.UsuarioId}",
                FechaHora = h.FechaIntento,
                IpOrigen = h.DireccionIP,
                Dispositivo = h.Dispositivo,
                Exitoso = h.Exitoso,
                MotivoFallo = h.MotivoFallo?.ToString()
            }).ToList();

            return ApiResponse<IEnumerable<HistorialSesionResponseDto>>.Exito(dtos);
        }

        private async Task RegistrarIntentoSesionAsync(
            int usuarioId,
            string ip,
            string dispositivo,
            bool exitoso,
            MotivoFalloSesionEnum motivo)
        {
            var sesion = new HistorialSesion
            {
                UsuarioId = usuarioId,
                DireccionIP = ip,
                Dispositivo = dispositivo,
                Exitoso = exitoso,
                MotivoFallo = motivo,
                FechaIntento = DateTime.UtcNow
            };

            await _uow.Usuarios.RegistrarHistorialSesionAsync(sesion);
            await _uow.SaveChangesAsync();
        }

        private static UsuarioResponseDto MapToUsuarioDto(Usuario u)
        {
            return new UsuarioResponseDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                RolId = u.RolId,
                RolNombre = u.Rol?.Nombre ?? string.Empty,
                EmailVerificado = u.EmailVerificado,
                Activo = u.Activo && !u.IsDeleted,
                IntentosFallidos = u.IntentosFallidos,
                BloqueadoHasta = u.BloqueadoHasta,
                ClienteId = u.ClienteId,
                PeluqueroId = u.PeluqueroId
            };
        }
    }
}