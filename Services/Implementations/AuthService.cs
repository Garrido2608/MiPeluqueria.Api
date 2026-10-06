using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.Seguridad;
using MiPeluqueria.Api.Services.Interfaces;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public AuthService(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto)
        {
            // 1. Buscar al usuario por Email o Username
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == dto.UsernameOrEmail || u.Username == dto.UsernameOrEmail);

            if (usuario == null)
            {
                return ApiResponse<AuthResponseDto>.Falla("Usuario o contraseña incorrectos.");
            }

            if (!usuario.Activo)
            {
                return ApiResponse<AuthResponseDto>.Falla("El usuario se encuentra inactivo.");
            }

            // 2. Verificar la contraseña encriptada con BCrypt
            bool passwordValida = BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash);

            if (!passwordValida)
            {
                return ApiResponse<AuthResponseDto>.Falla("Usuario o contraseña incorrectos.");
            }

            // 3. Generar el Token JWT
            var jwtKey = _config["Jwt:Key"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim("Username", usuario.Username),
                new Claim(ClaimTypes.Role, usuario.Rol.Nombre) // El Rol viaja encriptado en el token
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8), // El token dura 8 horas
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            // 4. Armar la respuesta final
            var response = new AuthResponseDto
            {
                UsuarioId = usuario.Id,
                Username = usuario.Username,
                Email = usuario.Email,
                Rol = usuario.Rol.Nombre,
                Token = tokenString
            };

            return ApiResponse<AuthResponseDto>.Exito(response, "Login exitoso.");
        }
    }
}