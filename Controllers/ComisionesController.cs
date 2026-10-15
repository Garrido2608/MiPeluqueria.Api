using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Ventas;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ComisionesController : ControllerBase
    {
        private readonly IComisionConfigService _comisionService;

        public ComisionesController(IComisionConfigService comisionService)
        {
            _comisionService = comisionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _comisionService.ObtenerTodasAsync();
            return Ok(result);
        }

        [HttpGet("peluquero/{peluqueroId}")]
        public async Task<IActionResult> GetByPeluquero(int peluqueroId)
        {
            var result = await _comisionService.ObtenerPorPeluqueroAsync(peluqueroId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> SaveConfig([FromBody] GuardarComisionConfigDto dto)
        {
            var result = await _comisionService.GuardarConfiguracionAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UpdateConfig(int id, [FromBody] GuardarComisionConfigDto dto)
        {
            var result = await _comisionService.ActualizarConfiguracionAsync(id, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> DeleteConfig(int id)
        {
            var result = await _comisionService.EliminarConfiguracionAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}