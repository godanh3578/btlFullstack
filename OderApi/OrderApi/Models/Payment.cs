using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public enum PaymentMethod
    {
        Cash,
        BankTransfer,
        Wallet,
        EWallet = Wallet,
        QR
    }

    public class Payment
    {
        public int PaymentId { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentCode { get; set; } = "";

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Paid;

        [StringLength(500)]
        public string Note { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
