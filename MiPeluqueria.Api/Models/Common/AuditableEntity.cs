using System;

using System.ComponentModel.DataAnnotations;

namespace MiPeluqueria.Api.Models.Common

{

    public abstract class AuditableEntity : ISoftDelete

    {

        [Key]

        public int Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

    }

}
//para q hereden todo esto y ya tiene para el softdelete 