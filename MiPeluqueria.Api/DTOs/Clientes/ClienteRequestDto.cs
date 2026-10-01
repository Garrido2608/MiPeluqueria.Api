using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
namespace MiPeluqueria.Api.DTOs.Clientes
{
    public class ClienteRequestDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener  entre 2 y 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;


        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2 y 100 caracteres")]
        public string Apellido { get; set; } = string.Empty;


        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(30)]
        public string Telefono { get; set; } = string.Empty;


        [EmailAddress(ErrorMessage = "El email no tiene un formato válido")]
        public string? Email { get; set; }



        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria para el programa de fidelización y promociones")]
        public DateTime FechaNacimiento { get; set; }


        public int? TipoCabelloId { get; set; }
        public int? TipoRostroId { get; set; }
    }
}