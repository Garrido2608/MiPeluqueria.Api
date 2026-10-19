namespace MiPeluqueria.Api.DTOs.Common
{
    public class ConfiguracionSalonDto
    {
        public int Id { get; set; }
        public string NombreSalon { get; set; } = "Mi Peluquería SaaS";
        public string RazonSocial { get; set; } = string.Empty;
        public string Cuit { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string EmailContacto { get; set; } = string.Empty;
        public string MonedaIso { get; set; } = "ARS";
        public string TimeZoneId { get; set; } = "America/Argentina/Cordoba";
        public int IntervaloTurnosMinutos { get; set; } = 30;
        public int MinutosToleranciaEspera { get; set; } = 15;
        public int HorasAnticipacionCancelacion { get; set; } = 24;
        public decimal PorcentajeSenaReserva { get; set; } = 0m;
        public bool NotificacionesWhatsAppActivas { get; set; } = true;
        public bool NotificacionesEmailActivas { get; set; } = true;
    }

    public class ActualizarConfiguracionSalonDto
    {
        public string NombreSalon { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string Cuit { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string EmailContacto { get; set; } = string.Empty;
        public string MonedaIso { get; set; } = "ARS";
        public string TimeZoneId { get; set; } = "America/Argentina/Cordoba";
        public int IntervaloTurnosMinutos { get; set; } = 30;
        public int MinutosToleranciaEspera { get; set; } = 15;
        public int HorasAnticipacionCancelacion { get; set; } = 24;
        public decimal PorcentajeSenaReserva { get; set; } = 0m;
        public bool NotificacionesWhatsAppActivas { get; set; } = true;
        public bool NotificacionesEmailActivas { get; set; } = true;
    }
}