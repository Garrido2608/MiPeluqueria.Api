using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Services.Implementations
{
    public class TenantProvider : ITenantProvider
    {
        public int CurrentTenantId { get; private set; } = 1; // Default Tenant 1 para retrocompatibilidad
        public int? CurrentSucursalId { get; private set; } = 1;

        public void SetTenantContext(int tenantId, int? sucursalId = null)
        {
            if (tenantId > 0)
                CurrentTenantId = tenantId;

            if (sucursalId.HasValue && sucursalId.Value > 0)
                CurrentSucursalId = sucursalId.Value;
        }
    }
}