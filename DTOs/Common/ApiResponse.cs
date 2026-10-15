namespace MiPeluqueria.Api.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(T data, string message = "Operación exitosa.")
        {
            Success = true;
            Message = message;
            Data = data;
        }

        public ApiResponse(string message, List<string>? errors = null)
        {
            Success = false;
            Message = message;
            Errors = errors ?? new List<string> { message };
        }

        // Métodos usados por los módulos nuevos (Bloques A y B)
        public static ApiResponse<T> Ok(T data, string message = "Operación exitosa.")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string> { message }
            };
        }

        // Métodos originales en español usados por Caja, Liquidación, Dashboard, Peluquero, etc.
        public static ApiResponse<T> Exito(T data, string message = "Operación exitosa.")
            => Ok(data, message);

        public static ApiResponse<T> Falla(string message, List<string>? errors = null)
            => Fail(message, errors);

        public static ApiResponse<T> SuccessResponse(T data, string message = "Operación exitosa.")
            => Ok(data, message);

        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
            => Fail(message, errors);
    }
}

//para q todo se devuelva en json y respuestas para usar en las otras ent.