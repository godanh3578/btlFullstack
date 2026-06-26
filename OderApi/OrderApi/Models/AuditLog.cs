using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public class AuditLog
    {
        public int AuditLogId { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string EntityId { get; set; } = "";

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        [StringLength(100)]
        public string PerformedBy { get; set; } = "";

        public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? IpAddress { get; set; }
    }
}
