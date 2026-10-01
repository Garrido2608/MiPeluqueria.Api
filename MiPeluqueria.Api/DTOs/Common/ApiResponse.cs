using System.Collections.Generic;
namespace MiPeluqueria.Api.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public static ApiResponse<T> Exito(T data, string mensaje = "") =>
        new ApiResponse<T> { Success = true, Message = mensaje, Data = data };

        public static ApiResponse<T> Falla(string mensaje, List<string>? errores =
        null) => new ApiResponse<T>
        { Success = false, Message = mensaje, Errors = errores};
    }
}
//para q todo se devuelva en json y respuestas para usar en las otras ent.