namespace MiPeluqueria.Api.Models.Common
{
    public abstract class AuditableTenantEntity : AuditableEntity, ITenantEntity
    {
        public int TenantId { get; set; } = 1;
        public int? SucursalId { get; set; } = 1;
    }
}
