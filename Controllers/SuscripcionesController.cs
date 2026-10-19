using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiPeluqueria.Api.Data.Context;
using MiPeluqueria.Api.DTOs.Common;
using MiPeluqueria.Api.DTOs.SaaS;
using MiPeluqueria.Api.Models.SaaS;
using MiPeluqueria.Api.Services.Interfaces;

[Route("api/suscripciones")]
[ApiController]
[Authorize]
public class SuscripcionesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public SuscripcionesController(ApplicationDbContext context, ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    [HttpGet("planes")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlanes(CancellationToken ct = default)
    {
        var planes = await _context.Set<PlanSuscripcion>()
            .AsNoTracking()
            .Where(p => p.Activo)
            .ToListAsync(ct);

        return Ok(ApiResponse<IEnumerable<PlanSuscripcion>>.Exito(planes));
    }

    [HttpGet("mi-suscripcion")]
    public async Task<IActionResult> GetMiSuscripcion(CancellationToken ct = default)
    {
        int tenantId = _tenantProvider.CurrentTenantId;
        var sub = await _context.Set<SuscripcionTenant>()
            .Include(s => s.Tenant)
            .Include(s => s.PlanSuscripcion)
            .AsNoTracking()
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        if (sub == null)
            return NotFound(ApiResponse<SuscripcionEstadoResponseDto>.Falla("No se encontró suscripción activa para este salón."));

        return Ok(ApiResponse<SuscripcionEstadoResponseDto>.Exito(new SuscripcionEstadoResponseDto
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
        }));
    }

    [HttpPost("cambiar-plan")]
    public async Task<IActionResult> CambiarPlan([FromBody] CambiarPlanSuscripcionDto dto, CancellationToken ct = default)
    {
        int tenantId = _tenantProvider.CurrentTenantId;
        var plan = await _context.Set<PlanSuscripcion>().FindAsync(new object[] { dto.NuevoPlanSuscripcionId }, ct);
        if (plan == null)
            return BadRequest(ApiResponse<bool>.Falla("El plan seleccionado no existe."));

        var sub = await _context.Set<SuscripcionTenant>()
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

        if (sub == null)
        {
            sub = new SuscripcionTenant
            {
                TenantId = tenantId,
                PlanSuscripcionId = dto.NuevoPlanSuscripcionId,
                FechaInicio = DateTime.UtcNow,
                FechaProximoVencimiento = DateTime.UtcNow.AddDays(30),
                Estado = EstadoSuscripcionEnum.Activa
            };
            await _context.Set<SuscripcionTenant>().AddAsync(sub, ct);
        }
        else
        {
            sub.PlanSuscripcionId = dto.NuevoPlanSuscripcionId;
            sub.Estado = EstadoSuscripcionEnum.Activa;
            _context.Set<SuscripcionTenant>().Update(sub);
        }

        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.Exito(true, $"Plan actualizado a {plan.Nombre} correctamente."));
    }
}
