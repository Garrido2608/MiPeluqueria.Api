namespace MiPeluqueria.Api.DTOs.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public Dictionary<string, string[]>? FieldErrors { get; set; }
        public List<string>? Warnings { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(T data, string message = "Operación exitosa.", List<string>? warnings = null)
        {
            Success = true;
            Message = message;
            Data = data;
            Warnings = warnings;
        }

        public ApiResponse(string message, List<string>? errors = null, Dictionary<string, string[]>? fieldErrors = null)
        {
            Success = false;
            Message = message;
            Errors = errors ?? new List<string> { message };
            FieldErrors = fieldErrors;
        }

        public static ApiResponse<T> Ok(T data, string message = "Operación exitosa.", List<string>? warnings = null) => new()
        {
            Success = true,
            Message = message,
            Data = data,
            Warnings = warnings
        };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null, Dictionary<string, string[]>? fieldErrors = null) => new()
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string> { message },
            FieldErrors = fieldErrors
        };

        public static ApiResponse<T> Exito(T data, string message = "Operación exitosa.", List<string>? warnings = null)
            => Ok(data, message, warnings);

        public static ApiResponse<T> Falla(string message, List<string>? errors = null, Dictionary<string, string[]>? fieldErrors = null)
            => Fail(message, errors, fieldErrors);

        public static ApiResponse<T> SuccessResponse(T data, string message = "Operación exitosa.", List<string>? warnings = null)
            => Ok(data, message, warnings);

        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null, Dictionary<string, string[]>? fieldErrors = null)
            => Fail(message, errors, fieldErrors);
    }
}