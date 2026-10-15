using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Integrations.Email;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthService(
            ApplicationDbContext context,
            IConfiguration config,
            IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(
            LoginRequestDto dto,
            string ip = "127.0.0.1",
            string dispositivo = "WebClient")
        {
            var identificador = dto.UsernameOrEmail.Trim();

            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Username == identificador || u.Email == identificador);

            if (usuario == null)
            {
                return ApiResponse<AuthResponseDto>.Falla("Usuario o contraseña incorrectos.");
            }

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

                _context.Usuarios.Update(usuario);
                await RegistrarIntentoSesionAsync(usuario.Id, ip, dispositivo, false, MotivoFalloSesionEnum.CredencialesIncorrectas);

                return ApiResponse<AuthResponseDto>.Falla("Usuario o contraseña incorrectos.");
            }

            // Login Exitoso: restablecer contador de fallos y registrar sesión
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            _context.Usuarios.Update(usuario);

            await RegistrarIntentoSesionAsync(usuario.Id, ip, dispositivo, true, MotivoFalloSesionEnum.Ninguno);

            var jwtKey = _config["Jwt:Key"] ?? "ClaveSecretaSuperSeguraMiPeluqueriaSaaS2026!";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var rolNombre = usuario.Rol?.Nombre ?? "Cliente";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
                new Claim("Username", usuario.Username ?? string.Empty),
                new Claim(ClaimTypes.Role, rolNombre)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var response = new AuthResponseDto
            {
                UsuarioId = usuario.Id,
                Username = usuario.Username ?? string.Empty,
                Email = usuario.Email ?? string.Empty
            };

            SetOptionalProp(response, "Token", tokenString);
            SetOptionalProp(response, "Rol", rolNombre);
            SetOptionalProp(response, "RolNombre", rolNombre);
            SetOptionalProp(response, "Expiracion", DateTime.UtcNow.AddHours(8));

            return ApiResponse<AuthResponseDto>.Exito(response, "Inicio de sesión exitoso.");
        }

        public async Task<ApiResponse<UsuarioResponseDto>> RegistrarUsuarioAsync(RegistroUsuarioDto dto)
        {
            var emailNorm = dto.Email.Trim().ToLower();
            var userNorm = dto.Username.Trim();

            bool existe = await _context.Usuarios
                .AnyAsync(u => u.Email.ToLower() == emailNorm || u.Username.ToLower() == userNorm.ToLower());

            if (existe)
                return ApiResponse<UsuarioResponseDto>.Falla("El nombre de usuario o correo electrónico ya se encuentra registrado.");

            string codigoOtp = Random.Shared.Next(100000, 999999).ToString();

            var nuevoUsuario = new Usuario
            {
                Username = userNorm,
                Email = emailNorm,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                RolId = dto.RolId > 0 ? dto.RolId : 5,
                IntentosFallidos = 0
            };

            SetOptionalProp(nuevoUsuario, "EmailVerificado", false);
            SetOptionalProp(nuevoUsuario, "CodigoVerificacion", codigoOtp);
            SetOptionalProp(nuevoUsuario, "FechaExpiracionCodigo", DateTime.UtcNow.AddMinutes(15));
            SetOptionalProp(nuevoUsuario, "AceptaTerminos", dto.AceptaTerminos);
            SetOptionalProp(nuevoUsuario, "FechaAceptacionTerminos", DateTime.UtcNow);
            SetOptionalProp(nuevoUsuario, "Activo", true);
            if (dto.ClienteId.HasValue) SetOptionalProp(nuevoUsuario, "ClienteId", dto.ClienteId.Value);
            if (dto.PeluqueroId.HasValue) SetOptionalProp(nuevoUsuario, "PeluqueroId", dto.PeluqueroId.Value);

            await _context.Usuarios.AddAsync(nuevoUsuario);
            await _context.SaveChangesAsync();

            await _emailService.EnviarCodigoOtpAsync(nuevoUsuario.Email, nuevoUsuario.Username, codigoOtp, "Verificación de Cuenta");

            var creado = await _context.Usuarios
                .Include(u => u.Rol)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == nuevoUsuario.Id);

            return ApiResponse<UsuarioResponseDto>.Exito(
                MapToUsuarioDto(creado ?? nuevoUsuario),
                "Usuario registrado correctamente. Se envió un código de 6 dígitos a su correo electrónico.");
        }

        public async Task<ApiResponse<bool>> VerificarEmailAsync(VerificarEmailDto dto)
        {
            var emailNorm = dto.Email.Trim().ToLower();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm);

            if (usuario == null)
                return ApiResponse<bool>.Falla("No existe un usuario registrado con ese correo electrónico.");

            var codigoGuardado = GetOptionalProp(usuario, new[] { "CodigoVerificacion", "CodigoOtp" })?.ToString();
            var expiracion = GetOptionalProp(usuario, new[] { "FechaExpiracionCodigo", "ExpiracionCodigo" }) as DateTime?;

            if (string.IsNullOrWhiteSpace(codigoGuardado) || codigoGuardado != dto.CodigoVerificacion.Trim())
                return ApiResponse<bool>.Falla("El código de verificación ingresado es inválido.");

            if (expiracion.HasValue && expiracion.Value < DateTime.UtcNow)
                return ApiResponse<bool>.Falla("El código de verificación ha expirado.");

            SetOptionalProp(usuario, "EmailVerificado", true);
            SetOptionalProp(usuario, "CodigoVerificacion", null);
            SetOptionalProp(usuario, "FechaExpiracionCodigo", null);

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Correo electrónico verificado exitosamente.");
        }

        public async Task<ApiResponse<bool>> CambiarPasswordAsync(int usuarioId, CambiarPasswordDto dto)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return ApiResponse<bool>.Falla("El usuario no existe.");

            if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
                return ApiResponse<bool>.Falla("La contraseña actual ingresada es incorrecta.");

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Contraseña actualizada exitosamente.");
        }

        public async Task<ApiResponse<bool>> SolicitarRecuperacionPasswordAsync(OlvidePasswordDto dto)
        {
            var emailNorm = dto.Email.Trim().ToLower();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm);

            if (usuario == null)
                return ApiResponse<bool>.Exito(true, "Si el correo existe en nuestro sistema, recibirá un código de recuperación en instantes.");

            string codigoOtp = Random.Shared.Next(100000, 999999).ToString();
            SetOptionalProp(usuario, "CodigoVerificacion", codigoOtp);
            SetOptionalProp(usuario, "FechaExpiracionCodigo", DateTime.UtcNow.AddMinutes(15));

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            await _emailService.EnviarCodigoOtpAsync(usuario.Email, usuario.Username, codigoOtp, "Recuperación de Contraseña");

            return ApiResponse<bool>.Exito(true, "Se ha enviado un código OTP de 6 dígitos a su correo electrónico.");
        }

        public async Task<ApiResponse<bool>> RestablecerPasswordConOtpAsync(RestablecerPasswordDto dto)
        {
            var emailNorm = dto.Email.Trim().ToLower();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == emailNorm);

            if (usuario == null)
                return ApiResponse<bool>.Falla("Datos de recuperación inválidos.");

            var codigoGuardado = GetOptionalProp(usuario, new[] { "CodigoVerificacion", "CodigoOtp" })?.ToString();
            var expiracion = GetOptionalProp(usuario, new[] { "FechaExpiracionCodigo", "ExpiracionCodigo" }) as DateTime?;

            if (string.IsNullOrWhiteSpace(codigoGuardado) || codigoGuardado != dto.CodigoOtp.Trim())
                return ApiResponse<bool>.Falla("El código OTP ingresado no es válido.");

            if (expiracion.HasValue && expiracion.Value < DateTime.UtcNow)
                return ApiResponse<bool>.Falla("El código OTP ha expirado. Solicite uno nuevo.");

            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NuevaPassword);
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            SetOptionalProp(usuario, "CodigoVerificacion", null);
            SetOptionalProp(usuario, "FechaExpiracionCodigo", null);

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.Exito(true, "Su contraseña ha sido restablecida exitosamente. Ya puede iniciar sesión.");
        }

        public async Task<ApiResponse<IEnumerable<UsuarioResponseDto>>> ObtenerUsuariosAsync()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .OrderBy(u => u.Id)
                .AsNoTracking()
                .ToListAsync();

            return ApiResponse<IEnumerable<UsuarioResponseDto>>.Exito(usuarios.Select(MapToUsuarioDto));
        }

        public async Task<ApiResponse<UsuarioResponseDto>> ActualizarEstadoUsuarioAsync(int id, ActualizarEstadoUsuarioDto dto)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                return ApiResponse<UsuarioResponseDto>.Falla("El usuario solicitado no existe.");

            if (dto.RolId.HasValue && dto.RolId.Value > 0)
                usuario.RolId = dto.RolId.Value;

            if (dto.Activo.HasValue)
                SetOptionalProp(usuario, "Activo", dto.Activo.Value);

            if (dto.DesbloquearCuenta)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
            }

            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();

            var actualizado = await _context.Usuarios
                .Include(u => u.Rol)
                .AsNoTracking()
                .FirstAsync(u => u.Id == id);

            return ApiResponse<UsuarioResponseDto>.Exito(MapToUsuarioDto(actualizado), "Usuario actualizado correctamente.");
        }

        public async Task<ApiResponse<IEnumerable<HistorialSesionResponseDto>>> ObtenerHistorialSesionesAsync(int? usuarioId = null, int top = 100)
        {
            var query = _context.HistorialSesiones
                .Include(h => h.Usuario)
                .AsNoTracking()
                .AsQueryable();

            if (usuarioId.HasValue)
                query = query.Where(h => h.UsuarioId == usuarioId.Value);

            var registros = await query
                .OrderByDescending(h => h.Id)
                .Take(top > 0 ? top : 100)
                .ToListAsync();

            var dtos = registros.Select(h => new HistorialSesionResponseDto
            {
                Id = h.Id,
                UsuarioId = h.UsuarioId,
                Username = h.Usuario?.Username ?? $"Usuario #{h.UsuarioId}",
                FechaHora = (GetOptionalProp(h, new[] { "FechaHora", "Fecha", "FechaIntento" }) as DateTime?) ?? DateTime.UtcNow,
                IpOrigen = GetOptionalProp(h, new[] { "IpOrigen", "DireccionIp", "Ip" })?.ToString(),
                Dispositivo = GetOptionalProp(h, new[] { "Dispositivo", "UserAgent", "Navegador" })?.ToString(),
                Exitoso = (GetOptionalProp(h, new[] { "Exitoso", "FueExitoso", "Exito" }) as bool?) ?? false,
                MotivoFallo = GetOptionalProp(h, new[] { "MotivoFallo", "Motivo" })?.ToString()
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
                UsuarioId = usuarioId
            };

            SetOptionalProp(sesion, "FechaHora", DateTime.UtcNow);
            SetOptionalProp(sesion, "Fecha", DateTime.UtcNow);
            SetOptionalProp(sesion, "IpOrigen", ip);
            SetOptionalProp(sesion, "Ip", ip);
            SetOptionalProp(sesion, "Dispositivo", dispositivo);
            SetOptionalProp(sesion, "UserAgent", dispositivo);
            SetOptionalProp(sesion, "Exitoso", exitoso);
            SetOptionalProp(sesion, "FueExitoso", exitoso);
            SetOptionalProp(sesion, "MotivoFallo", motivo);

            await _context.HistorialSesiones.AddAsync(sesion);
            await _context.SaveChangesAsync();
        }

        private static UsuarioResponseDto MapToUsuarioDto(Usuario u)
        {
            return new UsuarioResponseDto
            {
                Id = u.Id,
                Username = u.Username ?? string.Empty,
                Email = u.Email ?? string.Empty,
                RolId = u.RolId,
                RolNombre = u.Rol?.Nombre ?? string.Empty,
                EmailVerificado = (GetOptionalProp(u, new[] { "EmailVerificado" }) as bool?) ?? false,
                Activo = (GetOptionalProp(u, new[] { "Activo" }) as bool?) ?? !u.IsDeleted,
                IntentosFallidos = u.IntentosFallidos,
                BloqueadoHasta = u.BloqueadoHasta,
                ClienteId = GetOptionalProp(u, new[] { "ClienteId" }) as int?,
                PeluqueroId = GetOptionalProp(u, new[] { "PeluqueroId" }) as int?
            };
        }

        private static object? GetOptionalProp(object target, string[] names)
        {
            var type = target.GetType();
            foreach (var name in names)
            {
                var prop = type.GetProperty(name);
                if (prop != null && prop.CanRead)
                    return prop.GetValue(target);
            }
            return null;
        }

        private static void SetOptionalProp(object target, string name, object? value)
        {
            var prop = target.GetType().GetProperty(name);
            if (prop != null && prop.CanWrite)
            {
                try { prop.SetValue(target, value); } catch { }
            }
        }
    }
}