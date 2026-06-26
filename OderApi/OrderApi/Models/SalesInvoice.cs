using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public class SalesInvoice
    {
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceCode { get; set; } = "";

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        public int CustomerId { get; set; }
        public Customers? Customer { get; set; }

        public DateTime IssuedDate { get; set; } = DateTime.UtcNow;

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal FinalAmount { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
