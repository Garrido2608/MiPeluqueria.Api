namespace MiPeluqueria.Api.Models.Common
{
    public interface ITenantEntity
    {
        int TenantId { get; set; }
        int? SucursalId { get; set; }
    }
}