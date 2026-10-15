namespace MiPeluqueria.Api.DTOs.Common
{
    public class CatalogoItemDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? ExtraInfo { get; set; }
    }

    public class CrearCatalogoItemDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class CatalogosMaestrosResponseDto
    {
        public IEnumerable<CatalogoItemDto> TiposCabello { get; set; } = new List<CatalogoItemDto>();
        public IEnumerable<CatalogoItemDto> TiposRostro { get; set; } = new List<CatalogoItemDto>();
        public IEnumerable<CatalogoItemDto> CategoriasServicio { get; set; } = new List<CatalogoItemDto>();
        public IEnumerable<CatalogoItemDto> Especialidades { get; set; } = new List<CatalogoItemDto>();
        public IEnumerable<CatalogoItemDto> EstadosTurno { get; set; } = new List<CatalogoItemDto>();
        public IEnumerable<CatalogoItemDto> MotivosCancelacion { get; set; } = new List<CatalogoItemDto>();
        public IEnumerable<CatalogoItemDto> CategoriasProducto { get; set; } = new List<CatalogoItemDto>();
        public IEnumerable<CatalogoItemDto> MediosPago { get; set; } = new List<CatalogoItemDto>();
        public IEnumerable<CatalogoItemDto> Roles { get; set; } = new List<CatalogoItemDto>();
    }
}