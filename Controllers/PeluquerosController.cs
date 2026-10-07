using System.Threading.Tasks;
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
            var response = await _peluqueroService.GetAllAsync();
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _peluqueroService.GetByIdAsync(id);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CrearPeluqueroDto dto)
        {
            var response = await _peluqueroService.CreateAsync(dto);
            if (!response.Success) return BadRequest(response);
            return CreatedAtAction(nameof(GetById), new { id = response.Data!.Id }, response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _peluqueroService.DeleteAsync(id);
            if (!response.Success) return NotFound(response);
            return Ok(response);
        }
    }
}