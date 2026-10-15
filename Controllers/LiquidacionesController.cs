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
    public class LiquidacionesController : ControllerBase
    {
        private readonly ILiquidacionService _liquidacionService;

        public LiquidacionesController(ILiquidacionService liquidacionService)
        {
            _liquidacionService = liquidacionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? peluqueroId = null, [FromQuery] DateTime? desde = null, [FromQuery] DateTime? hasta = null)
        {
            var res = await _liquidacionService.ObtenerTodasAsync(peluqueroId, desde, hasta);
            return Ok(res);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _liquidacionService.ObtenerPorIdAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpGet("peluquero/{peluqueroId}")]
        public async Task<IActionResult> GetPorPeluquero(int peluqueroId)
        {
            var res = await _liquidacionService.ObtenerPorPeluqueroAsync(peluqueroId);
            return Ok(res);
        }

        [HttpPost("generar")]
        [Authorize]
        public async Task<IActionResult> Generar([FromBody] GenerarLiquidacionDto dto)
        {
            var res = await _liquidacionService.LiquidarPeriodoAsync(dto);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }

        [HttpPatch("{id}/pagar")]
        [HttpPost("{id}/pagar")]
        [Authorize]
        public async Task<IActionResult> Pagar(int id, [FromQuery] int? sesionCajaId = null)
        {
            var res = await _liquidacionService.RegistrarPagoLiquidacionAsync(id, sesionCajaId);
            if (!res.Success) return BadRequest(res);
            return Ok(res);
        }
    }
}