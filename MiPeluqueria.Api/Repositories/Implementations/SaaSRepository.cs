using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.Models.SaaS;
using MiPeluqueria.Api.Repositories.Interfaces;

namespace MiPeluqueria.Api.Repositories.Implementations
{
    public class SaaSRepository : GenericRepository<Sucursal>, ISaaSRepository
    {
        public SaaSRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Tenant?> GetTenantByIdAsync(int tenantId, CancellationToken ct = default)
        {
            return await _context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        }

        public async Task<Tenant?> GetTenantBySubdominioAsync(string subdominioSlug, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(subdominioSlug))
                return null;

            string normalized = subdominioSlug.Trim().ToLowerInvariant();
            return await _context.Tenants
                .AsNoTracking()
                .Include(t => t.Sucursales)
                .FirstOrDefaultAsync(t => t.SubdominioSlug.ToLower() == normalized && t.Activo, ct);
        }

        public async Task<IReadOnlyList<Sucursal>> GetSucursalesByTenantAsync(int tenantId, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .OrderByDescending(s => s.EsCasaMatriz)
                .ThenBy(s => s.Id)
                .ToListAsync(ct);
        }

        public async Task<Sucursal?> GetSucursalByIdAsync(int sucursalId, int? tenantId = null, CancellationToken ct = default)
        {
            var query = _dbSet.AsQueryable();
            if (tenantId.HasValue)
                query = query.Where(s => s.TenantId == tenantId.Value);

            return await query.FirstOrDefaultAsync(s => s.Id == sucursalId, ct);
        }

        public async Task<int> CountSucursalesActivasByTenantAsync(int tenantId, CancellationToken ct = default)
        {
            return await _dbSet
                .CountAsync(s => s.TenantId == tenantId && s.Activa, ct);
        }

        public async Task<bool> EsSucursalValidaYActivaAsync(int tenantId, int sucursalId, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .AnyAsync(s => s.Id == sucursalId && s.TenantId == tenantId && s.Activa && !s.IsDeleted, ct);
        }

        public async Task AddSucursalAsync(Sucursal sucursal, CancellationToken ct = default)
        {
            await AddAsync(sucursal, ct);
        }

        public void UpdateSucursal(Sucursal sucursal)
        {
            Update(sucursal);
        }

        public async Task<IReadOnlyList<PlanSuscripcion>> GetPlanesActivosAsync(CancellationToken ct = default)
        {
            return await _context.PlanesSuscripcion
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.PrecioMensualUsd)
                .ToListAsync(ct);
        }

        public async Task<PlanSuscripcion?> GetPlanByIdAsync(int planId, CancellationToken ct = default)
        {
            return await _context.PlanesSuscripcion
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == planId && p.Activo, ct);
        }

        public async Task<SuscripcionTenant?> GetSuscripcionActualByTenantAsync(
            int tenantId,
            bool tracking = false,
            CancellationToken ct = default)
        {
            var query = tracking
                ? _context.SuscripcionesTenant.AsQueryable()
                : _context.SuscripcionesTenant.AsNoTracking();

            return await query
                .Include(s => s.Tenant)
                .Include(s => s.PlanSuscripcion)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        }

        public async Task AddSuscripcionTenantAsync(SuscripcionTenant suscripcion, CancellationToken ct = default)
        {
            suscripcion.CreatedAt = DateTime.UtcNow;
            await _context.SuscripcionesTenant.AddAsync(suscripcion, ct);
        }

        public void UpdateSuscripcionTenant(SuscripcionTenant suscripcion)
        {
            suscripcion.UpdatedAt = DateTime.UtcNow;
            _context.SuscripcionesTenant.Update(suscripcion);
        }
    }
}
