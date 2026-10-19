namespace MiPeluqueria.Api.Services.Interfaces
{
    public interface ITenantProvider
    {
        int CurrentTenantId { get; }
        int? CurrentSucursalId { get; }
        void SetTenantContext(int tenantId, int? sucursalId = null);
    }
}