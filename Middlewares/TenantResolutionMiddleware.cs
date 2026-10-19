using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Middlewares
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
        {
            int tenantId = 1;
            int? sucursalId = null;

            var tenantClaim = context.User.FindFirst("TenantId")?.Value;
            if (!string.IsNullOrWhiteSpace(tenantClaim) && int.TryParse(tenantClaim, out int parsedClaimTenant))
            {
                tenantId = parsedClaimTenant;
            }
            else if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenant) &&
                     int.TryParse(headerTenant.ToString(), out int parsedHeaderTenant))
            {
                tenantId = parsedHeaderTenant;
            }

            var sucursalClaim = context.User.FindFirst("SucursalId")?.Value;
            if (!string.IsNullOrWhiteSpace(sucursalClaim) && int.TryParse(sucursalClaim, out int parsedClaimSucursal))
            {
                sucursalId = parsedClaimSucursal;
            }
            else if (context.Request.Headers.TryGetValue("X-Sucursal-Id", out var headerSucursal) &&
                     int.TryParse(headerSucursal.ToString(), out int parsedHeaderSucursal))
            {
                sucursalId = parsedHeaderSucursal;
            }

            tenantProvider.SetTenantContext(tenantId, sucursalId);

            await _next(context);
        }
    }
}