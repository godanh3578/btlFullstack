using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public enum OutboxMessageStatus
    {
        Pending,
        Processed,
        Failed
    }

    public class OutboxMessage
    {
        public int OutboxMessageId { get; set; }

        [Required]
        [StringLength(100)]
        public string EventName { get; set; } = "";

        [Required]
        public string Payload { get; set; } = "";

        public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

        [Range(0, int.MaxValue)]
        public int RetryCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }
}
