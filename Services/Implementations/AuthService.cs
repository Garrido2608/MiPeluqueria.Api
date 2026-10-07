using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Models.Seguridad;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _config;

        public AuthService(
            ApplicationDbContext context,
            IUnitOfWork unitOfWork,
            IConfiguration config)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _config = config;
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(
            LoginRequestDto dto,
            string? ipAddress = null,
            string? userAgent = null)
        {
            var ip = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress;
            var dispositivo = string.IsNullOrWhiteSpace(userAgent) ? "Cliente HTTP" : userAgent;

            // 1. Buscar usuario por Username o Email (usando la propiedad real dto.UsernameOrEmail)
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u =>
                    u.Username == dto.UsernameOrEmail ||
                    u.Email == dto.UsernameOrEmail);

            if (usuario == null)
            {
                return ApiResponse<AuthResponseDto>.Falla("Usuario o contraseña incorrectos.");
            }

            // 2. Verificar si la cuenta está dada de baja
            if (!usuario.Activo)
            {
                await RegistrarIntentoSesionAsync(usuario.Id, ip, dispositivo, false, MotivoFalloSesionEnum.CuentaInactiva);
                return ApiResponse<AuthResponseDto>.Falla("La cuenta de usuario se encuentra desactivada.");
            }

            // 3. Verificar bloqueo temporal por fuerza bruta
            if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
            {
                await RegistrarIntentoSesionAsync(usuario.Id, ip, dispositivo, false, MotivoFalloSesionEnum.CuentaBloqueada);
                var minutosRestantes = Math.Ceiling((usuario.BloqueadoHasta.Value - DateTime.UtcNow).TotalMinutes);
                return ApiResponse<AuthResponseDto>.Falla(
                    $"Cuenta bloqueada preventivamente por múltiples intentos fallidos. Intente nuevamente en {minutosRestantes} minutos.");
            }

            // 4. Verificar contraseña con BCrypt
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

            // 5. Login Exitoso: restablecer contador de fallos y registrar sesión exitosa
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            _context.Usuarios.Update(usuario);

            await RegistrarIntentoSesionAsync(usuario.Id, ip, dispositivo, true, MotivoFalloSesionEnum.Ninguno);

            // 6. Emitir Token JWT firmado con HMAC-SHA256
            var jwtKey = _config["Jwt:Key"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var rolNombre = usuario.Rol?.Nombre ?? "Cliente";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim("Username", usuario.Username),
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
                Username = usuario.Username,
                Email = usuario.Email,
                Rol = rolNombre,
                Token = tokenString
            };

            return ApiResponse<AuthResponseDto>.Exito(response, "Autenticación exitosa.");
        }

        private async Task RegistrarIntentoSesionAsync(
            int usuarioId,
            string ip,
            string dispositivo,
            bool exitoso,
            MotivoFalloSesionEnum motivo)
        {
            await _context.HistorialSesiones.AddAsync(new HistorialSesion
            {
                UsuarioId = usuarioId,
                DireccionIP = ip.Length > 50 ? ip.Substring(0, 50) : ip,
                Dispositivo = dispositivo.Length > 500 ? dispositivo.Substring(0, 500) : dispositivo,
                Exitoso = exitoso,
                MotivoFallo = motivo,
                FechaIntento = DateTime.UtcNow
            });

            await _unitOfWork.SaveChangesAsync();
        }
    }
}