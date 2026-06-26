using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public enum DebtStatus
    {
        Unpaid,   // Chưa trả
        Partial,  // Đã trả một phần
        Paid,     // Đã trả đủ
        Overdue   // Quá hạn thanh toán
    }

    public class Debt
    {
        public int DebtId { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customers? Customer { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DebtAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; } = 0;

        public decimal RemainingAmount { get; set; }

        public DateTime? DueDate { get; set; }

        public DebtStatus DebtStatus { get; set; } = DebtStatus.Unpaid;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
