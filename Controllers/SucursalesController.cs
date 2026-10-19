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

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/sucursales")]
    [ApiController]
    [Authorize]
    public class SucursalesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public SucursalesController(ApplicationDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            int tenantId = _tenantProvider.CurrentTenantId;
            var sucursales = await _context.Set<Sucursal>()
                .AsNoTracking()
                .Where(s => s.TenantId == tenantId)
                .Select(s => new SucursalResponseDto
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
                })
                .ToListAsync(ct);

            return Ok(ApiResponse<IEnumerable<SucursalResponseDto>>.Exito(sucursales));
        }

        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] CrearSucursalDto dto, CancellationToken ct = default)
        {
            int tenantId = _tenantProvider.CurrentTenantId;

            var nueva = new Sucursal
            {
                TenantId = tenantId,
                Nombre = dto.Nombre,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                TimeZoneId = dto.TimeZoneId,
                MonedaIso = dto.MonedaIso,
                EsCasaMatriz = dto.EsCasaMatriz,
                Activa = true
            };

            await _context.Set<Sucursal>().AddAsync(nueva, ct);
            await _context.SaveChangesAsync(ct);

            return Ok(ApiResponse<int>.Exito(nueva.Id, "Sucursal creada exitosamente."));
        }
    }