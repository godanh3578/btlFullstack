using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public enum WalletTopUpStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class WalletTopUpRequest
    {
        public int WalletTopUpRequestId { get; set; }

        [Required]
        [StringLength(50)]
        public string RequestCode { get; set; } = "";

        public int CustomerId { get; set; }
        public Customers? Customer { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "BankTransfer";

        public WalletTopUpStatus Status { get; set; } = WalletTopUpStatus.Pending;

        [StringLength(500)]
        public string Note { get; set; } = "";

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        [StringLength(100)]
        public string ReviewedBy { get; set; } = "";
    }
}
