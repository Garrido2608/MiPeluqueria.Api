using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using MiPeluqueria.Api.Exceptions;
using MiPeluqueria.Api.Repositories.Interfaces;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Middlewares
{
    public class TenantResolutionMiddleware
    {
        private const string MensajeSucursalInvalida =
            "La sucursal indicada no pertenece al salón actual o se encuentra inactiva.";

        private static readonly TimeSpan DuracionCacheValidacion = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan TopeAbsolutoCacheValidacion = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan DuracionCacheNegativa = TimeSpan.FromMinutes(1);

        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        public TenantResolutionMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITenantProvider tenantProvider,
            IUnitOfWork unitOfWork)
        {
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            int tenantId = 1;
            int sucursalId = 1;

            bool isAuthenticated = context.User.Identity?.IsAuthenticated == true;

            if (isAuthenticated)
            {
                var tenantClaim = context.User.Claims
                    .FirstOrDefault(c => string.Equals(c.Type, "TenantId", StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                if (!string.IsNullOrWhiteSpace(tenantClaim))
                {
                    if (!int.TryParse(tenantClaim.Trim(), out tenantId) || tenantId <= 0)
                        throw new ForbiddenAccessException(MensajeSucursalInvalida);
                }

                var sucursalClaim = context.User.Claims
                    .FirstOrDefault(c => string.Equals(c.Type, "SucursalId", StringComparison.OrdinalIgnoreCase))
                    ?.Value;

                if (!string.IsNullOrWhiteSpace(sucursalClaim))
                {
                    if (!int.TryParse(sucursalClaim.Trim(), out sucursalId) || sucursalId <= 0)
                        throw new ForbiddenAccessException(MensajeSucursalInvalida);
                }
                else if (TryReadHeaderOrQuery(context, "X-Sucursal-Id", "sucursalId", out string rawSucursal))
                {
                    if (!int.TryParse(rawSucursal, out sucursalId) || sucursalId <= 0)
                        throw new ForbiddenAccessException(MensajeSucursalInvalida);
                }
            }
            else
            {
                if (TryReadHeaderOrQuery(context, "X-Tenant-Id", "tenantId", out string rawTenant))
                {
                    if (!int.TryParse(rawTenant, out tenantId) || tenantId <= 0)
                        throw new ForbiddenAccessException(MensajeSucursalInvalida);
                }
                else if (context.Request.Query.TryGetValue("subdominioSlug", out var slugValues) &&
                         !string.IsNullOrWhiteSpace(slugValues.ToString()))
                {
                    string slug = slugValues.ToString().Trim().ToLowerInvariant();
                    var tenant = await unitOfWork.SaaS.GetTenantBySubdominioAsync(slug, context.RequestAborted)
                        ?? throw new ForbiddenAccessException(MensajeSucursalInvalida);

                    tenantId = tenant.Id;
                    sucursalId = tenant.Sucursales
                        .Where(s => s.Activa && !s.IsDeleted)
                        .OrderByDescending(s => s.EsCasaMatriz)
                        .ThenBy(s => s.Id)
                        .Select(s => s.Id)
                        .FirstOrDefault(1);
                }

                if (TryReadHeaderOrQuery(context, "X-Sucursal-Id", "sucursalId", out string rawSucursal))
                {
                    if (!int.TryParse(rawSucursal, out sucursalId) || sucursalId <= 0)
                        throw new ForbiddenAccessException(MensajeSucursalInvalida);
                }
            }

            string cacheKey = $"SaaS_Valid_T{tenantId}_S{sucursalId}";
            if (!_cache.TryGetValue(cacheKey, out bool esValida))
            {
                esValida = await unitOfWork.SaaS.EsSucursalValidaYActivaAsync(
                    tenantId,
                    sucursalId,
                    context.RequestAborted);

                var opcionesCache = esValida
                    ? new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = DuracionCacheValidacion,
                        // Tope absoluto: evita que el tráfico continuo mantenga viva para siempre
                        // la validación de una sucursal que luego fue desactivada.
                        AbsoluteExpirationRelativeToNow = TopeAbsolutoCacheValidacion
                    }
                    : new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = DuracionCacheNegativa
                    };

                _cache.Set(cacheKey, esValida, opcionesCache);
            }

            if (!esValida)
            {
                throw new ForbiddenAccessException(MensajeSucursalInvalida);
            }

            tenantProvider.SetTenantContext(tenantId, sucursalId);

            await _next(context);
        }

        private static bool TryReadHeaderOrQuery(
            HttpContext context,
            string headerName,
            string queryName,
            out string value)
        {
            if (context.Request.Headers.TryGetValue(headerName, out var headerValues) &&
                !string.IsNullOrWhiteSpace(headerValues.ToString()))
            {
                value = headerValues.ToString().Trim();
                return true;
            }

            if (context.Request.Query.TryGetValue(queryName, out var queryValues) &&
                !string.IsNullOrWhiteSpace(queryValues.ToString()))
            {
                value = queryValues.ToString().Trim();
                return true;
            }

            value = string.Empty;
            return false;
        }
    }
}