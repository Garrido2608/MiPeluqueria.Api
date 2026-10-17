using Microsoft.Extensions.Caching.Memory;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.Exceptions;
using MiPeluqueria.Api.Models.Common;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class ConfiguracionSalonService : IConfiguracionSalonService
    {
        private const string CacheKey = "SaaS_ConfiguracionSalon_Current";
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;

        public ConfiguracionSalonService(IUnitOfWork unitOfWork, IMemoryCache memoryCache)
        {
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
        }

        public async Task<ConfiguracionSalonDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default)
        {
            if (_memoryCache.TryGetValue(CacheKey, out ConfiguracionSalonDto? cachedDto) && cachedDto != null)
                return cachedDto;

            var entidad = await _unitOfWork.ConfiguracionSalon.GetConfiguracionActualAsync(cancellationToken);

            if (entidad == null)
            {
                entidad = new ConfiguracionSalon();
                await _unitOfWork.ConfiguracionSalon.AddAsync(entidad, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var dto = MapToDto(entidad);
            _memoryCache.Set(CacheKey, dto, TimeSpan.FromMinutes(30));
            return dto;
        }

        public async Task<ConfiguracionSalonDto> ActualizarConfiguracionAsync(ConfiguracionSalonDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.IntervaloTurnosMinutos < 5 || dto.IntervaloTurnosMinutos > 180)
                throw new BusinessRuleException("El intervalo de turnos debe estar entre 5 y 180 minutos.");

            if (dto.PorcentajeSenaReserva < 0 || dto.PorcentajeSenaReserva > 100)
                throw new BusinessRuleException("El porcentaje de seña debe estar entre 0% y 100%.");

            var entidad = await _unitOfWork.ConfiguracionSalon.GetConfiguracionActualAsync(cancellationToken);
            var esNuevo = false;

            if (entidad == null)
            {
                entidad = new ConfiguracionSalon();
                esNuevo = true;
            }

            entidad.NombreSalon = dto.NombreSalon.Trim();
            entidad.RazonSocial = dto.RazonSocial.Trim();
            entidad.Cuit = dto.Cuit.Trim();
            entidad.Direccion = dto.Direccion.Trim();
            entidad.Telefono = dto.Telefono.Trim();
            entidad.EmailContacto = dto.EmailContacto.Trim();
            entidad.MonedaIso = string.IsNullOrWhiteSpace(dto.MonedaIso) ? "ARS" : dto.MonedaIso.Trim().ToUpper();
            entidad.TimeZoneId = string.IsNullOrWhiteSpace(dto.TimeZoneId) ? "America/Argentina/Cordoba" : dto.TimeZoneId.Trim();
            entidad.IntervaloTurnosMinutos = dto.IntervaloTurnosMinutos;
            entidad.MinutosToleranciaEspera = dto.MinutosToleranciaEspera;
            entidad.HorasAnticipacionCancelacion = dto.HorasAnticipacionCancelacion;
            entidad.PorcentajeSenaReserva = dto.PorcentajeSenaReserva;
            entidad.NotificacionesWhatsAppActivas = dto.NotificacionesWhatsAppActivas;
            entidad.NotificacionesEmailActivas = dto.NotificacionesEmailActivas;

            if (esNuevo)
                await _unitOfWork.ConfiguracionSalon.AddAsync(entidad, cancellationToken);
            else
                _unitOfWork.ConfiguracionSalon.Update(entidad);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var updatedDto = MapToDto(entidad);
            _memoryCache.Set(CacheKey, updatedDto, TimeSpan.FromMinutes(30));

            return updatedDto;
        }

        private static ConfiguracionSalonDto MapToDto(ConfiguracionSalon entidad) => new()
        {
            NombreSalon = entidad.NombreSalon,
            RazonSocial = entidad.RazonSocial,
            Cuit = entidad.Cuit,
            Direccion = entidad.Direccion,
            Telefono = entidad.Telefono,
            EmailContacto = entidad.EmailContacto,
            MonedaIso = entidad.MonedaIso,
            TimeZoneId = entidad.TimeZoneId,
            IntervaloTurnosMinutos = entidad.IntervaloTurnosMinutos,
            MinutosToleranciaEspera = entidad.MinutosToleranciaEspera,
            HorasAnticipacionCancelacion = entidad.HorasAnticipacionCancelacion,
            PorcentajeSenaReserva = entidad.PorcentajeSenaReserva,
            NotificacionesWhatsAppActivas = entidad.NotificacionesWhatsAppActivas,
            NotificacionesEmailActivas = entidad.NotificacionesEmailActivas
        };
    }
}