using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.SaaS
{
    public enum EstadoSuscripcionEnum
    {
        Trial = 1,
        Activa = 2,
        PagoPendiente = 3,
        Suspendida = 4,
        Cancelada = 5
    }
}