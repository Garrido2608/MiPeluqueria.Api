using System;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;
        private readonly ITenantProvider _tenantProvider;

        public ConfiguracionSalonService(
            IUnitOfWork unitOfWork,
            IMemoryCache memoryCache,
            ITenantProvider tenantProvider)
        {
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
            _tenantProvider = tenantProvider;
        }

        private string GetCacheKey()
        {
            int tenantId = _tenantProvider.CurrentTenantId;
            int sucursalId = _tenantProvider.CurrentSucursalId ?? 1;
            return $"SaaS_Config_T{tenantId}_S{sucursalId}";
        }

        public async Task<ConfiguracionSalonDto> ObtenerConfiguracionAsync(CancellationToken cancellationToken = default)
        {
            var cacheKey = GetCacheKey();
            if (_memoryCache.TryGetValue(cacheKey, out ConfiguracionSalonDto? cachedDto) && cachedDto != null)
                return cachedDto;

            var entidad = await _unitOfWork.ConfiguracionSalon.GetConfiguracionActualAsync(cancellationToken);

            if (entidad == null)
            {
                entidad = new ConfiguracionSalon
                {
                    TenantId = _tenantProvider.CurrentTenantId,
                    SucursalId = _tenantProvider.CurrentSucursalId ?? 1
                };
                await _unitOfWork.ConfiguracionSalon.AddAsync(entidad, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var dto = MapToDto(entidad);
            _memoryCache.Set(cacheKey, dto, TimeSpan.FromMinutes(30));
            return dto;
        }

        public async Task<ConfiguracionSalonDto> ActualizarConfiguracionAsync(
            ConfiguracionSalonDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto.IntervaloTurnosMinutos < 5 || dto.IntervaloTurnosMinutos > 180)
                throw new BusinessRuleException("El intervalo de turnos debe estar entre 5 y 180 minutos.");

            if (dto.PorcentajeSenaReserva < 0 || dto.PorcentajeSenaReserva > 100)
                throw new BusinessRuleException("El porcentaje de seña debe estar entre 0% y 100%.");

            var entidad = await _unitOfWork.ConfiguracionSalon.GetConfiguracionActualAsync(cancellationToken);
            var esNuevo = false;

            if (entidad == null)
            {
                entidad = new ConfiguracionSalon
                {
                    TenantId = _tenantProvider.CurrentTenantId,
                    SucursalId = _tenantProvider.CurrentSucursalId ?? 1
                };
                esNuevo = true;
            }

            var nombreComercial = !string.IsNullOrWhiteSpace(dto.NombreComercial)
                ? dto.NombreComercial.Trim()
                : dto.NombreSalon.Trim();
            var nombreSalon = !string.IsNullOrWhiteSpace(dto.NombreSalon)
                ? dto.NombreSalon.Trim()
                : nombreComercial;
            var monedaIso = !string.IsNullOrWhiteSpace(dto.MonedaIso)
                ? dto.MonedaIso.Trim().ToUpperInvariant()
                : (!string.IsNullOrWhiteSpace(dto.Moneda) ? dto.Moneda.Trim().ToUpperInvariant() : "ARS");

            entidad.NombreSalon = nombreSalon;
            entidad.NombreComercial = nombreComercial;
            entidad.RazonSocial = dto.RazonSocial?.Trim() ?? string.Empty;
            entidad.Cuit = dto.Cuit?.Trim() ?? string.Empty;
            entidad.Direccion = dto.Direccion?.Trim() ?? string.Empty;
            entidad.Telefono = dto.Telefono?.Trim() ?? string.Empty;
            entidad.EmailContacto = dto.EmailContacto?.Trim() ?? string.Empty;
            entidad.MonedaIso = monedaIso;
            entidad.TimeZoneId = string.IsNullOrWhiteSpace(dto.TimeZoneId) ? "America/Argentina/Cordoba" : dto.TimeZoneId.Trim();
            entidad.HorarioAtencionTexto = dto.HorarioAtencionTexto?.Trim() ?? "Lunes a Sábados de 09:00 a 20:00 hs";
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

            var cacheKey = GetCacheKey();
            _memoryCache.Remove(cacheKey);
            var updatedDto = MapToDto(entidad);
            _memoryCache.Set(cacheKey, updatedDto, TimeSpan.FromMinutes(30));

            return updatedDto;
        }

        public async Task<ConfiguracionSalonDto> ActualizarConfiguracionAsync(
            ActualizarConfiguracionSalonDto dto,
            CancellationToken cancellationToken = default)
        {
            return await ActualizarConfiguracionAsync(new ConfiguracionSalonDto
            {
                NombreSalon = dto.NombreSalon,
                NombreComercial = dto.NombreComercial,
                RazonSocial = dto.RazonSocial,
                Cuit = dto.Cuit,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                EmailContacto = dto.EmailContacto,
                MonedaIso = dto.MonedaIso,
                Moneda = dto.Moneda,
                TimeZoneId = dto.TimeZoneId,
                HorarioAtencionTexto = dto.HorarioAtencionTexto,
                IntervaloTurnosMinutos = dto.IntervaloTurnosMinutos,
                MinutosToleranciaEspera = dto.MinutosToleranciaEspera,
                HorasAnticipacionCancelacion = dto.HorasAnticipacionCancelacion,
                PorcentajeSenaReserva = dto.PorcentajeSenaReserva,
                NotificacionesWhatsAppActivas = dto.NotificacionesWhatsAppActivas,
                NotificacionesEmailActivas = dto.NotificacionesEmailActivas
            }, cancellationToken);
        }

        private static ConfiguracionSalonDto MapToDto(ConfiguracionSalon entidad) => new()
        {
            Id = entidad.Id,
            NombreSalon = entidad.NombreSalon,
            NombreComercial = string.IsNullOrWhiteSpace(entidad.NombreComercial) ? entidad.NombreSalon : entidad.NombreComercial,
            RazonSocial = entidad.RazonSocial,
            Cuit = entidad.Cuit,
            Direccion = entidad.Direccion,
            Telefono = entidad.Telefono,
            EmailContacto = entidad.EmailContacto,
            MonedaIso = entidad.MonedaIso,
            Moneda = entidad.MonedaIso,
            TimeZoneId = entidad.TimeZoneId,
            HorarioAtencionTexto = entidad.HorarioAtencionTexto,
            IntervaloTurnosMinutos = entidad.IntervaloTurnosMinutos,
            MinutosToleranciaEspera = entidad.MinutosToleranciaEspera,
            HorasAnticipacionCancelacion = entidad.HorasAnticipacionCancelacion,
            PorcentajeSenaReserva = entidad.PorcentajeSenaReserva,
            NotificacionesWhatsAppActivas = entidad.NotificacionesWhatsAppActivas,
            NotificacionesEmailActivas = entidad.NotificacionesEmailActivas
        };
    }
}
