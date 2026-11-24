using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.SaaS;
using MiPeluqueria.Api.Models.SaaS;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class SaaSService : ISaaSService
    {
        private const int MaxSucursalesAntiAbuso = 20;
        private readonly IUnitOfWork _uow;
        private readonly ITenantProvider _tenantProvider;
        private readonly IMemoryCache _cache;

        public SaaSService(
            IUnitOfWork uow,
            ITenantProvider tenantProvider,
            IMemoryCache cache)
        {
            _uow = uow;
            _tenantProvider = tenantProvider;
            _cache = cache;
        }

        public async Task<ApiResponse<IReadOnlyList<SucursalResponseDto>>> ObtenerSucursalesAsync(CancellationToken ct = default)
        {
            int tenantId = _tenantProvider.CurrentTenantId;
            var sucursales = await _uow.SaaS.GetSucursalesByTenantAsync(tenantId, ct);

            var dtos = sucursales.Select(MapSucursalDto).ToList();
            return ApiResponse<IReadOnlyList<SucursalResponseDto>>.Exito(dtos, "Sucursales obtenidas con éxito.");
        }

        public async Task<ApiResponse<SucursalResponseDto>> CrearSucursalAsync(CrearSucursalDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                return ApiResponse<SucursalResponseDto>.Falla("El nombre de la sucursal es obligatorio.");

            int tenantId = _tenantProvider.CurrentTenantId;
            int activas = await _uow.SaaS.CountSucursalesActivasByTenantAsync(tenantId, ct);
            if (activas >= MaxSucursalesAntiAbuso)
            {
                return ApiResponse<SucursalResponseDto>.Falla(
                    $"Se ha alcanzado el tope técnico anti-abuso de {MaxSucursalesAntiAbuso} sucursales activas para este salón.");
            }

            var nueva = new Sucursal
            {
                TenantId = tenantId,
                Nombre = dto.Nombre.Trim(),
                Direccion = dto.Direccion?.Trim() ?? string.Empty,
                Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                TimeZoneId = string.IsNullOrWhiteSpace(dto.TimeZoneId) ? "America/Argentina/Cordoba" : dto.TimeZoneId.Trim(),
                MonedaIso = string.IsNullOrWhiteSpace(dto.MonedaIso) ? "ARS" : dto.MonedaIso.Trim().ToUpperInvariant(),
                EsCasaMatriz = dto.EsCasaMatriz,
                Activa = true
            };

            await _uow.SaaS.AddSucursalAsync(nueva, ct);
            await _uow.SaveChangesAsync(ct);

            _cache.Remove($"SaaS_Valid_T{tenantId}_S{nueva.Id}");

            return ApiResponse<SucursalResponseDto>.Exito(MapSucursalDto(nueva), "Sucursal creada exitosamente.");
        }

        public async Task<ApiResponse<IReadOnlyList<PlanSuscripcionResponseDto>>> ObtenerPlanesActivosAsync(CancellationToken ct = default)
        {
            var planes = await _uow.SaaS.GetPlanesActivosAsync(ct);
            var dtos = planes.Select(p => new PlanSuscripcionResponseDto
            {
                Id = p.Id,
                Nombre = p.Nombre,
                PrecioMensualUsd = p.PrecioMensualUsd,
                MaxSucursales = p.MaxSucursales,
                MaxPeluqueros = p.MaxPeluqueros,
                IncluyeWhatsAppRecordatorios = p.IncluyeWhatsAppRecordatorios,
                IncluyePortalReservasOnline = p.IncluyePortalReservasOnline,
                Activo = p.Activo
            }).ToList();

            return ApiResponse<IReadOnlyList<PlanSuscripcionResponseDto>>.Exito(dtos, "Planes de suscripción obtenidos con éxito.");
        }

        public async Task<ApiResponse<SuscripcionEstadoResponseDto>> ObtenerMiSuscripcionAsync(CancellationToken ct = default)
        {
            int tenantId = _tenantProvider.CurrentTenantId;
            var sub = await _uow.SaaS.GetSuscripcionActualByTenantAsync(tenantId, false, ct);

            if (sub == null)
                return ApiResponse<SuscripcionEstadoResponseDto>.Falla("No se encontró suscripción activa para este salón.");

            return ApiResponse<SuscripcionEstadoResponseDto>.Exito(MapSuscripcionEstadoDto(sub));
        }

        public async Task<ApiResponse<SuscripcionEstadoResponseDto>> CambiarPlanAsync(
            CambiarPlanSuscripcionDto dto,
            CancellationToken ct = default)
        {
            var plan = await _uow.SaaS.GetPlanByIdAsync(dto.NuevoPlanSuscripcionId, ct);
            if (plan == null || !plan.Activo)
                return ApiResponse<SuscripcionEstadoResponseDto>.Falla("El plan de suscripción seleccionado no existe o no se encuentra disponible.");

            int tenantId = _tenantProvider.CurrentTenantId;
            var sub = await _uow.SaaS.GetSuscripcionActualByTenantAsync(tenantId, true, ct);

            if (sub == null)
            {
                sub = new SuscripcionTenant
                {
                    TenantId = tenantId,
                    PlanSuscripcionId = plan.Id,
                    FechaInicio = DateTime.UtcNow,
                    FechaProximoVencimiento = DateTime.UtcNow.AddDays(30),
                    Estado = EstadoSuscripcionEnum.Activa
                };
                await _uow.SaaS.AddSuscripcionTenantAsync(sub, ct);
            }
            else
            {
                sub.PlanSuscripcionId = plan.Id;
                sub.Estado = EstadoSuscripcionEnum.Activa;
                if (sub.FechaProximoVencimiento < DateTime.UtcNow)
                {
                    sub.FechaProximoVencimiento = DateTime.UtcNow.AddDays(30);
                }
                _uow.SaaS.UpdateSuscripcionTenant(sub);
            }

            await _uow.SaveChangesAsync(ct);

            var actualizada = await _uow.SaaS.GetSuscripcionActualByTenantAsync(tenantId, false, ct) ?? sub;
            actualizada.PlanSuscripcion ??= plan;

            return ApiResponse<SuscripcionEstadoResponseDto>.Exito(
                MapSuscripcionEstadoDto(actualizada),
                $"Plan actualizado a {plan.Nombre} correctamente.");
        }

        private static SucursalResponseDto MapSucursalDto(Sucursal s) => new()
        {
            Id = s.Id,
            TenantId = s.TenantId,
            Nombre = s.Nombre,
            Direccion = s.Direccion,
            Telefono = s.Telefono,
            TimeZoneId = s.TimeZoneId,
            MonedaIso = s.MonedaIso,
            EsCasaMatriz = s.EsCasaMatriz,
            Activa = s.Activa
        };

        private static SuscripcionEstadoResponseDto MapSuscripcionEstadoDto(SuscripcionTenant sub) => new()
        {
            TenantId = sub.TenantId,
            NombreComercial = sub.Tenant?.NombreComercial ?? "Mi Peluquería",
            PlanActual = sub.PlanSuscripcion?.Nombre ?? "Professional",
            PrecioMensualUsd = sub.PlanSuscripcion?.PrecioMensualUsd ?? 29.99m,
            Estado = sub.Estado,
            EstadoNombre = sub.Estado.ToString(),
            FechaProximoVencimiento = sub.FechaProximoVencimiento,
            MaxSucursalesPermitidas = sub.PlanSuscripcion?.MaxSucursales ?? 1,
            MaxPeluquerosPermitidos = sub.PlanSuscripcion?.MaxPeluqueros ?? 10
        };
    }
}
