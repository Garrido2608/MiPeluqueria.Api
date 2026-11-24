using System;

namespace MiPeluqueria.Api.Models.Common

{
    public interface ISoftDelete

    {
        bool IsDeleted { get; set; }

        DateTime? DeletedAt { get; set; }

    }

}
// esto es un desactivador; es vez de borrar que puede traer fallas al sql 
// lo desactivamos y lo dejamos enl la base de datos con una fecha de cuando lo ejecutamos
// mejor ejemplo un cliente se da de baja y una venta perderia el idcliente 