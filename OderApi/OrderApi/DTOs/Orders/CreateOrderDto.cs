namespace OrderApi.DTOs.Orders
{
    public class CreateOrderDto
    {
        public string? IdempotencyKey { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public List<CreateOrderDetailDto> Items { get; set; } = new();
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; } = "Fixed";
        public decimal DiscountValue { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedBy { get; set; } = "";
        public string PaymentMethod { get; set; } = "Cash";
        public decimal PaidAmount { get; set; } = 0;
        public string? Source { get; set; }
    }

    public class CreateOrderDetailDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
    }
}
