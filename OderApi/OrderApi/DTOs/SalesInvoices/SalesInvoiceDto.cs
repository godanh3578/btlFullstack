namespace OrderApi.DTOs.SalesInvoices
{
    public class SalesInvoiceDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = "";
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = "";
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerAddress { get; set; } = "";
        public DateTime IssuedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentStatus { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSalesInvoiceDto
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
    }
}
