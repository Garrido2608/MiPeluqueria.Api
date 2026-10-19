using System;
using System.ComponentModel.DataAnnotations;
using MiPeluqueria.Api.Models.Common;

namespace MiPeluqueria.Api.Models.Seguridad
{
    public class RefreshToken : AuditableEntity
    {
        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [StringLength(256)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        [StringLength(50)]
        public string? CreatedByIp { get; set; }

        public DateTime? RevokedAt { get; set; }

        [StringLength(50)]
        public string? RevokedByIp { get; set; }

        [StringLength(256)]
        public string? ReplacedByToken { get; set; }

        [StringLength(200)]
        public string? ReasonRevoked { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt != null;
        public bool IsActive => !IsRevoked && !IsExpired && !IsDeleted;

        public Usuario? Usuario { get; set; }
    }
}