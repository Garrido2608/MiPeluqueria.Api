using System;
using System.Collections.Generic;

namespace MiPeluqueria.Api.DTOs.Common
{
    public class PagedResult<T>
    {
        // Los datos reales que estamos devolviendo (ej. la lista de clientes)
        public List<T> Data { get; set; } = new List<T>();

        // Cantidad total de registros en la base de datos (sin paginar)
        public int TotalRecords { get; set; }

        // En qué página estamos parados actualmente
        public int PageNumber { get; set; }

        // Cuántos registros entran por página
        public int PageSize { get; set; }

        // --- PROPIEDADES DINÁMICAS ---

        // Calcula automáticamente cuántas páginas totales hay
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        // Te dice si hay una página anterior
        public bool HasPreviousPage => PageNumber > 1;

        // Te dice si hay una página siguiente
        public bool HasNextPage => PageNumber < TotalPages;
    }
}