namespace MiPeluqueria.Api.DTOs.Common
{
    public class PaginationFilterDto
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 15;

        public int PageNumber { get; set; } = 1;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : (value < 1 ? 15 : value);
        }

        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = true;
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}