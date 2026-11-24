using System;

namespace MiPeluqueria.Api.DTOs.Common
{
    public class ClientErrorReportDto
    {
        public string Message { get; set; } = string.Empty;
        public string? Stack { get; set; }

        public string? StackTrace
        {
            get => Stack;
            set => Stack ??= value;
        }

        public string? Route { get; set; }

        public string? Url
        {
            get => Route;
            set => Route ??= value;
        }

        public string? ComponentStack { get; set; }
        public string? CorrelationId { get; set; }
        public string? UserAgent { get; set; }
        public string? TimestampUtc { get; set; }
        public string? Severity { get; set; }
    }
}
