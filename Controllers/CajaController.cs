using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CajaController : ControllerBase
    {
        private readonly ICajaService _cajaService;

        public CajaController(ICajaService cajaService)
        {
            _cajaService = cajaService;
        }

        [HttpGet("estado-actual")]
        public async Task<IActionResult> GetEstadoActual()
        {
            var res = await _cajaService.ObtenerEstadoCajaActualAsync();
            return Ok(res);
        }

        [HttpGet("historial")]
        [Authorize]
        public async Task<IActionResult> GetHistorial([FromQuery] DateTime? fechaDesde = null, [FromQuery] DateTime? fechaHasta = null)
        {
            var res = await _cajaService.ObtenerHistorialCajasAsync(fechaDesde, fechaHasta);
            return Ok(res);
        }

        [HttpPost("abrir")]
        [Authorize]
        public async Task<IActionResult> Abrir([FromBody] AperturaCajaDto dto)
        {
            var res = await _cajaService.AbrirCajaAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("egreso")]
        [Authorize]
        public async Task<IActionResult> RegistrarEgreso([FromBody] RegistrarMovimientoCajaDto dto)
        {
            var res = await _cajaService.RegistrarEgresoCajaAsync(dto.SesionCajaId, dto.Monto, dto.Concepto, dto.Tipo ?? "EgresoMenor");
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("ingreso")]
        [Authorize]
        public async Task<IActionResult> RegistrarIngreso([FromBody] RegistrarMovimientoCajaDto dto)
        {
            var res = await _cajaService.RegistrarIngresoCajaAsync(dto.SesionCajaId, dto.Monto, dto.Concepto, dto.Tipo ?? "IngresoManual");
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPost("cerrar")]
        [Authorize]
        public async Task<IActionResult> Cerrar([FromBody] CierreCajaDto dto)
        {
            var res = await _cajaService.CerrarCajaAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}