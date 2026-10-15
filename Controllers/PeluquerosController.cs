using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiPeluqueria.Api.DTOs.Turnos;
using MiPeluqueria.Api.Services.Interfaces;

namespace MiPeluqueria.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PeluquerosController : ControllerBase
    {
        private readonly IPeluqueroService _peluqueroService;

        public PeluquerosController(IPeluqueroService peluqueroService)
        {
            _peluqueroService = peluqueroService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var res = await _peluqueroService.GetAllAsync();
            return Ok(res);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var res = await _peluqueroService.GetByIdAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CrearPeluqueroDto dto)
        {
            var res = await _peluqueroService.CreateAsync(dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] CrearPeluqueroDto dto)
        {
            var res = await _peluqueroService.UpdateAsync(id, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var res = await _peluqueroService.DeleteAsync(id);
            return res.Success ? Ok(res) : NotFound(res);
        }

        [HttpGet("{id}/horarios")]
        public async Task<IActionResult> GetHorarios(int id)
        {
            var res = await _peluqueroService.ObtenerHorariosAsync(id);
            return Ok(res);
        }

        [HttpPut("{id}/horarios")]
        [Authorize]
        public async Task<IActionResult> ConfigurarHorarios(int id, [FromBody] GuardarHorariosPeluqueroDto dto)
        {
            var res = await _peluqueroService.ConfigurarHorariosAsync(id, dto);
            return res.Success ? Ok(res) : BadRequest(res);
        }
    }
}