using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MiPeluqueria.Api.Models.SaaS;

namespace MiPeluqueria.Api.Repositories.Interfaces
{
    public interface ISaaSRepository : IGenericRepository<Sucursal>
    {
        Task<Tenant?> GetTenantByIdAsync(int tenantId, CancellationToken ct = default);
        Task<Tenant?> GetTenantBySubdominioAsync(string subdominioSlug, CancellationToken ct = default);

        Task<IReadOnlyList<Sucursal>> GetSucursalesByTenantAsync(int tenantId, CancellationToken ct = default);
        Task<Sucursal?> GetSucursalByIdAsync(int sucursalId, int? tenantId = null, CancellationToken ct = default);
        Task<int> CountSucursalesActivasByTenantAsync(int tenantId, CancellationToken ct = default);
        Task<bool> EsSucursalValidaYActivaAsync(int tenantId, int sucursalId, CancellationToken ct = default);
        Task AddSucursalAsync(Sucursal sucursal, CancellationToken ct = default);
        void UpdateSucursal(Sucursal sucursal);

        Task<IReadOnlyList<PlanSuscripcion>> GetPlanesActivosAsync(CancellationToken ct = default);
        Task<PlanSuscripcion?> GetPlanByIdAsync(int planId, CancellationToken ct = default);

        Task<SuscripcionTenant?> GetSuscripcionActualByTenantAsync(int tenantId, bool tracking = false, CancellationToken ct = default);
        Task AddSuscripcionTenantAsync(SuscripcionTenant suscripcion, CancellationToken ct = default);
        void UpdateSuscripcionTenant(SuscripcionTenant suscripcion);
    }
}
