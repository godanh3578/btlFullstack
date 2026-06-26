namespace OrderApi.DTOs.Sales
{
    public class CheckoutDto
    {
        public string? IdempotencyKey { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; } = "Fixed";
        public decimal DiscountValue { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public decimal PaidAmount { get; set; }
        public List<CheckoutItemDto> Items { get; set; } = new();
    }

    public class CheckoutItemDto
    {
        public int ProductId { get; set; }
        public string? ExternalProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CheckoutResponseDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; } = "";
        public decimal DiscountValue { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DebtAmount { get; set; }
        public string PaymentStatus { get; set; } = "";
        public string OrderStatus { get; set; } = "";
    }
}
