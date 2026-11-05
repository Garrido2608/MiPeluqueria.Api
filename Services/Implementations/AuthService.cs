using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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

        public AuthService(
            IUnitOfWork uow,
            IConfiguration config,
            IEmailService emailService)
        {
            _uow = uow;
            _config = config;
            _emailService = emailService;
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(
            LoginRequestDto dto,
            string ip = "127.0.0.1",
            string dispositivo = "WebClient")
        {
            var user = await _uow.Usuarios.GetByUsernameOrEmailAsync(dto.UsernameOrEmail);
            if (user == null)
                return ApiResponse<AuthResponseDto>.Falla("Credenciales inválidas.");

            if (!user.Activo)
            {
                await RegistrarIntentoSesionAsync(user.Id, ip, dispositivo, false, MotivoFalloSesionEnum.CuentaInactiva);
                return ApiResponse<AuthResponseDto>.Falla("La cuenta se encuentra desactivada. Contacte al administrador.");
            }

            if (user.BloqueadoHasta.HasValue && user.BloqueadoHasta.Value > DateTime.UtcNow)
            {
                await RegistrarIntentoSesionAsync(user.Id, ip, dispositivo, false, MotivoFalloSesionEnum.CuentaBloqueada);
                return ApiResponse<AuthResponseDto>.Falla($"Cuenta bloqueada temporalmente hasta {user.BloqueadoHasta.Value:HH:mm} UTC.");
            }

            bool passwordOk = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!passwordOk)
            {
                user.IntentosFallidos++;
                if (user.IntentosFallidos >= 5)
                {
                    user.BloqueadoHasta = DateTime.UtcNow.AddMinutes(15);
                }

                _uow.Usuarios.Update(user);
                await RegistrarIntentoSesionAsync(user.Id, ip, dispositivo, false, MotivoFalloSesionEnum.CredencialesIncorrectas);

                return ApiResponse<AuthResponseDto>.Falla("Credenciales inválidas.");
            }

            if (!user.EmailVerificado)
            {
                await RegistrarIntentoSesionAsync(user.Id, ip, dispositivo, false, MotivoFalloSesionEnum.EmailNoVerificado);
                return ApiResponse<AuthResponseDto>.Falla("Debe verificar su correo electrónico antes de iniciar sesión.");
            }

            user.IntentosFallidos = 0;
            user.BloqueadoHasta = null;
            _uow.Usuarios.Update(user);
            await RegistrarIntentoSesionAsync(user.Id, ip, dispositivo, true, MotivoFalloSesionEnum.Ninguno);

            var (tokenString, expira) = GenerarJwtToken(user);
            var refreshTokenString = GenerarRefreshTokenSeguro();

            var refreshTokenEntity = new RefreshToken
            {
                TenantId = user.TenantId,
                SucursalId = user.SucursalId ?? 1,
                UsuarioId = user.Id,
                Token = refreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ip
            };

            await _uow.Usuarios.AddRefreshTokenAsync(refreshTokenEntity);
            await _uow.CommitAsync();

            return ApiResponse<AuthResponseDto>.Exito(new AuthResponseDto
            {
                UsuarioId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Rol = user.Rol?.Nombre ?? "Cliente",
                Token = tokenString,
                RefreshToken = refreshTokenString,
                Expiracion = expira,
                TenantId = user.TenantId,
                SucursalId = user.SucursalId ?? 1
            }, "Inicio de sesión exitoso.");
        }

        public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(
            RefreshTokenRequestDto dto,
            string ip = "127.0.0.1",
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.RefreshToken))
                return ApiResponse<AuthResponseDto>.Falla("El Refresh Token es obligatorio.");

            var storedToken = await _uow.Usuarios.GetRefreshTokenAsync(dto.RefreshToken.Trim(), ct);
            if (storedToken == null || !storedToken.IsActive || storedToken.Usuario == null)
                return ApiResponse<AuthResponseDto>.Falla("Refresh token inválido, expirado o revocado.");

            var user = storedToken.Usuario;
            if (!user.Activo || user.IsDeleted)
                return ApiResponse<AuthResponseDto>.Falla("La cuenta de usuario asociada se encuentra inactiva.");

            var newRefreshTokenString = GenerarRefreshTokenSeguro();

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.RevokedByIp = ip;
            storedToken.ReplacedByToken = newRefreshTokenString;
            storedToken.ReasonRevoked = "Rotación de Refresh Token";
            _uow.Usuarios.UpdateRefreshToken(storedToken);

            var newRefreshTokenEntity = new RefreshToken
            {
                TenantId = user.TenantId,
                SucursalId = user.SucursalId ?? 1,
                UsuarioId = user.Id,
                Token = newRefreshTokenString,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ip
            };

            await _uow.Usuarios.AddRefreshTokenAsync(newRefreshTokenEntity, ct);
            await _uow.SaveChangesAsync(ct);

            var (jwtString, expira) = GenerarJwtToken(user);

            return ApiResponse<AuthResponseDto>.Exito(new AuthResponseDto
            {
                UsuarioId = user.Id,
                Username = user.Username,
                Email = user.Email,
                Rol = user.Rol?.Nombre ?? "Cliente",
                Token = jwtString,
                RefreshToken = newRefreshTokenString,
                Expiracion = expira,
                TenantId = user.TenantId,
                SucursalId = user.SucursalId ?? 1
            }, "Token renovado exitosamente.");
        }

        public async Task<ApiResponse<UsuarioResponseDto>> RegistrarUsuarioAsync(RegistroUsuarioDto dto)
        {
            if (!dto.AceptaTerminos)
                return ApiResponse<UsuarioResponseDto>.Falla("Debe aceptar los términos y condiciones para registrarse.");

            var existente = await _uow.Usuarios.GetByUsernameOrEmailAsync(dto.Username)
                            ?? await _uow.Usuarios.GetByUsernameOrEmailAsync(dto.Email);

            if (existente != null)
                return ApiResponse<UsuarioResponseDto>.Falla("El nombre de usuario o correo electrónico ya está registrado.");

            string codigoOtp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

            var nuevo = new Usuario
            {
                Username = dto.Username.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RolId = 4, // Seguridad RBAC: El registro público asigna estrictamente RolId = 4 (Cliente)
                PeluqueroId = null,
                ClienteId = dto.ClienteId,
                AceptaTerminos = true,
                FechaAceptacionTerminos = DateTime.UtcNow,
                EmailVerificado = false,
                CodigoVerificacion = codigoOtp,
                FechaExpiracionCodigo = DateTime.UtcNow.AddMinutes(30),
                Activo = true
            };

            await _uow.Usuarios.AddAsync(nuevo);
            await _uow.CommitAsync();

            var warnings = new List<string>();
            try
            {
                bool enviado = await _emailService.EnviarCodigoOtpAsync(nuevo.Email, nuevo.Username, codigoOtp, "Verificación de Cuenta");
                if (!enviado)
                {
                    warnings.Add("El usuario fue creado exitosamente, pero no se pudo enviar el correo de verificación en este momento.");
                }
            }
            catch
            {
                warnings.Add("El usuario fue creado exitosamente, pero ocurrió una falla al contactar el servidor de correo electrónico.");
            }

            var usuarioCreado = await _uow.Usuarios.GetByIdWithRolAsync(nuevo.Id) ?? nuevo;
            return ApiResponse<UsuarioResponseDto>.Ok(
                MapUsuarioDto(usuarioCreado),
                "Usuario registrado. Revise su correo electrónico para activar su cuenta.",
                warnings.Count > 0 ? warnings : null);
        }

        public async Task<ApiResponse<bool>> VerificarEmailAsync(VerificarEmailDto dto)
        {
            var user = await _uow.Usuarios.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant());
            if (user == null)
                return ApiResponse<bool>.Falla("Usuario no encontrado.");

            if (user.EmailVerificado)
                return ApiResponse<bool>.Exito(true, "El correo electrónico ya había sido verificado.");

            if (user.CodigoVerificacion != dto.CodigoVerificacion.Trim() ||
                !user.FechaExpiracionCodigo.HasValue ||
                user.FechaExpiracionCodigo.Value < DateTime.UtcNow)
            {
                return ApiResponse<bool>.Falla("El código de verificación es inválido o ha expirado.");
            }

            user.EmailVerificado = true;
            user.CodigoVerificacion = null;
            user.FechaExpiracionCodigo = null;

            _uow.Usuarios.Update(user);
            await _uow.CommitAsync();

            return ApiResponse<bool>.Exito(true, "Correo electrónico verificado exitosamente.");
        }

        public async Task<ApiResponse<bool>> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto)
        {
            var user = await _uow.Usuarios.GetByIdWithRolAsync(usuarioId);
            if (user == null)
                return ApiResponse<bool>.Falla("Usuario no encontrado.");

            if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, user.PasswordHash))
                return ApiResponse<bool>.Falla("La contraseña actual es incorrecta.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            _uow.Usuarios.Update(user);
            await _uow.CommitAsync();

            return ApiResponse<bool>.Exito(true, "Contraseña actualizada correctamente.");
        }

        public async Task<ApiResponse<bool>> SolicitarRecuperacionPasswordAsync(OlvidePasswordDto dto)
        {
            var user = await _uow.Usuarios.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant());
            if (user == null)
            {
                return ApiResponse<bool>.Exito(true, "Si el correo existe, se ha enviado un código de recuperación.");
            }

            string otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            user.CodigoVerificacion = otp;
            user.FechaExpiracionCodigo = DateTime.UtcNow.AddMinutes(15);

            _uow.Usuarios.Update(user);
            await _uow.CommitAsync();

            var warnings = new List<string>();
            try
            {
                bool enviado = await _emailService.EnviarCodigoOtpAsync(user.Email, user.Username, otp, "Recuperación de Contraseña");
                if (!enviado)
                {
                    warnings.Add("El código fue generado, pero no se pudo despachar el correo electrónico en este momento.");
                }
            }
            catch
            {
                warnings.Add("El código fue generado, pero el servidor SMTP no se encuentra disponible en este momento.");
            }

            return ApiResponse<bool>.Ok(
                true,
                "Si el correo existe, se ha enviado un código de recuperación.",
                warnings.Count > 0 ? warnings : null);
        }

        public async Task<ApiResponse<bool>> RestablecerPasswordConOtpAsync(RestablecerPasswordDto dto)
        {
            var user = await _uow.Usuarios.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant());
            if (user == null)
                return ApiResponse<bool>.Falla("Solicitud inválida.");

            if (user.CodigoVerificacion != dto.CodigoOtp.Trim() ||
                !user.FechaExpiracionCodigo.HasValue ||
                user.FechaExpiracionCodigo.Value < DateTime.UtcNow)
            {
                return ApiResponse<bool>.Falla("El código OTP es inválido o ha expirado.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            user.CodigoVerificacion = null;
            user.FechaExpiracionCodigo = null;
            user.IntentosFallidos = 0;
            user.BloqueadoHasta = null;

            _uow.Usuarios.Update(user);
            await _uow.CommitAsync();

            return ApiResponse<bool>.Exito(true, "Contraseña restablecida con éxito.");
        }

        public async Task<ApiResponse<IEnumerable<UsuarioResponseDto>>> ObtenerUsuariosAsync()
        {
            var users = await _uow.Usuarios.GetAllWithRolesAsync();
            return ApiResponse<IEnumerable<UsuarioResponseDto>>.Exito(users.Select(MapUsuarioDto));
        }

        public async Task<ApiResponse<UsuarioResponseDto>> ActualizarEstadoUsuarioAsync(int id, ActualizarEstadoUsuarioDto dto)
        {
            var user = await _uow.Usuarios.GetByIdWithRolAsync(id);
            if (user == null)
                return ApiResponse<UsuarioResponseDto>.Falla("Usuario no encontrado.");

            if (dto.Activo.HasValue)
            {
                user.Activo = dto.Activo.Value;
            }

            if (dto.RolId.HasValue)
            {
                user.RolId = dto.RolId.Value;
            }

            if (dto.DesbloquearCuenta)
            {
                user.IntentosFallidos = 0;
                user.BloqueadoHasta = null;
            }

            if (dto.Activo == false)
            {
                await _uow.Usuarios.RevocarRefreshTokensUsuarioAsync(user.Id, "Cuenta desactivada por administración");
            }

            _uow.Usuarios.Update(user);
            await _uow.CommitAsync();

            return ApiResponse<UsuarioResponseDto>.Exito(MapUsuarioDto(user), "Estado del usuario actualizado.");
        }

        public async Task<ApiResponse<IEnumerable<HistorialSesionResponseDto>>> ObtenerHistorialSesionesAsync(int? usuarioId = null, int top = 100)
        {
            var logs = await _uow.Usuarios.GetHistorialSesionesAsync(usuarioId, top);
            var dtos = logs.Select(l => new HistorialSesionResponseDto
            {
                Id = l.Id,
                UsuarioId = l.UsuarioId,
                Username = l.Usuario?.Username ?? "Desconocido",
                FechaHora = l.FechaIntento,
                IpOrigen = l.DireccionIP,
                Dispositivo = l.Dispositivo ?? string.Empty,
                Exitoso = l.Exitoso,
                MotivoFallo = l.MotivoFallo?.ToString()
            });

            return ApiResponse<IEnumerable<HistorialSesionResponseDto>>.Exito(dtos);
        }

        private (string Token, DateTime ExpiraEn) GenerarJwtToken(Usuario user)
        {
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "ClaveSecretaSuperSeguraMiPeluqueriaSaaS2026!");
            var expira = DateTime.UtcNow.AddHours(8);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Rol?.Nombre ?? "Cliente"),
                new("TenantId", user.TenantId.ToString()),
                new("SucursalId", (user.SucursalId ?? 1).ToString())
            };

            if (user.PeluqueroId.HasValue)
                claims.Add(new Claim("PeluqueroId", user.PeluqueroId.Value.ToString()));

            if (user.ClienteId.HasValue)
                claims.Add(new Claim("ClienteId", user.ClienteId.Value.ToString()));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expira,
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), expira);
        }

        private static string GenerarRefreshTokenSeguro()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private async Task RegistrarIntentoSesionAsync(
            int usuarioId,
            string ip,
            string dispositivo,
            bool exitoso,
            MotivoFalloSesionEnum motivo)
        {
            await _uow.Usuarios.RegistrarHistorialSesionAsync(new HistorialSesion
            {
                UsuarioId = usuarioId,
                DireccionIP = ip,
                Dispositivo = dispositivo,
                Exitoso = exitoso,
                MotivoFallo = motivo,
                FechaIntento = DateTime.UtcNow
            });
            await _uow.CommitAsync();
        }

        private static UsuarioResponseDto MapUsuarioDto(Usuario u) => new()
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            RolId = u.RolId,
            RolNombre = u.Rol?.Nombre ?? "Cliente",
            EmailVerificado = u.EmailVerificado,
            Activo = u.Activo,
            IntentosFallidos = u.IntentosFallidos,
            BloqueadoHasta = u.BloqueadoHasta,
            PeluqueroId = u.PeluqueroId,
            ClienteId = u.ClienteId
        };
    }
}
