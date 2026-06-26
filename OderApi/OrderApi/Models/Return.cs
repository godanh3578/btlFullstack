using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public enum ReturnStatus
    {
        Pending,
        Approved,
        Refunded,
        Rejected
    }

    public class Return
    {
        public int ReturnId { get; set; }

        [Required]
        [StringLength(50)]
        public string ReturnCode { get; set; } = "";

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customers?  Customer { get; set; }

        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

        [Range(0, double.MaxValue)]
        public decimal RefundAmount { get; set; } = 0;

        [StringLength(500)]
        public string? Reason { get; set; }

        public ReturnStatus ReturnStatus { get; set; } = ReturnStatus.Pending;

        [StringLength(100)]
        public string CreatedBy { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ReturnDetail> Items { get; set; } = new();
    }
}
