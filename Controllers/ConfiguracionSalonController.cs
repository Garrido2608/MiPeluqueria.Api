// filepath: Controllers/ConfiguracionSalonController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MiPeluqueria.Api.DTOs.Common;

namespace MiPeluqueria.Api.Controllers
{
    public class ConfiguracionSalonDto
    {
        public int Id { get; set; } = 1;
        public string NombreComercial { get; set; } = "Mi Peluquería & Barbería SaaS";
        public string NombreSalon { get; set; } = "Mi Peluquería & Barbería SaaS";
        public string RazonSocial { get; set; } = "Estudio Capilar S.R.L.";
        public string Cuit { get; set; } = "30-71548963-9";
        public string Direccion { get; set; } = "Av. Colón 1250, Córdoba, Argentina";
        public string Telefono { get; set; } = "+54 9 351 555-0199";
        public string EmailContacto { get; set; } = "contacto@mipeluqueria.com.ar";
        public string Moneda { get; set; } = "ARS";
        public string MonedaIso { get; set; } = "ARS";
        public string TimeZoneId { get; set; } = "America/Argentina/Cordoba";
        public int IntervaloTurnosMinutos { get; set; } = 30;
        public int MinutosToleranciaEspera { get; set; } = 15;
        public int HorasAnticipacionCancelacion { get; set; } = 24;
        public decimal PorcentajeSenaReserva { get; set; } = 0m;
        public bool NotificacionesWhatsAppActivas { get; set; } = true;
        public bool NotificacionesEmailActivas { get; set; } = true;
        public string HorarioAtencionTexto { get; set; } = "Lunes a Sábados de 09:00 a 20:00 hs";
    }

    [Route("api/configuracion-salon")]
    [ApiController]
    public class ConfiguracionSalonController : ControllerBase
    {
        private static ConfiguracionSalonDto _configActual = new();

        public ConfiguracionSalonController(IConfiguration configuration)
        {
            var nombre = configuration["SalonConfig:NombreComercial"];
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                _configActual.NombreComercial = nombre;
                _configActual.NombreSalon = nombre;
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ObtenerConfiguracion()
        {
            return Ok(ApiResponse<ConfiguracionSalonDto>.Exito(
                _configActual,
                "Configuración comercial del salón obten