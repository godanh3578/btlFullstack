using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Completed = 2,
        Debt = 3,
        Cancelled = 4,
        Shipping = 5,
        Processing = 6,
        Paid = Completed,
    }

    public enum PaymentStatus
    {
        Unpaid,
        Partial,
        Paid
    }

    public class Order
    {
        public int OrderId { get; set; }

        [Required]
        [StringLength(50)]
        public string OrderCode { get; set; } = "";

        [StringLength(100)]
        public string? IdempotencyKey { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customers? Customer { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0;

        [StringLength(10)]
        public string DiscountType { get; set; } = "Fixed";

        [Range(0, double.MaxValue)]
        public decimal DiscountValue { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal FinalAmount { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; } = 0;

        public decimal DebtAmount { get; set; } = 0;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StockRestoredAt { get; set; }

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        [Required]
        public List<OrderDetail> Items { get; set; } = new();

        public List<Payment> Payments { get; set; } = new();

        public Debt? Debt { get; set; }
    }
}
