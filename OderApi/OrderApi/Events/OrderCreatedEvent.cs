namespace OrderApi.Events
{
    public class OrderCreatedEvent
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = "";
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; } = "";
        public decimal DiscountValue { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DebtAmount { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
        public string OrderStatus { get; set; } = "";
        public int CreatedByUserId { get; set; }
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<OrderCreatedEventItem> Items { get; set; } = new();
    }

    public class OrderCreatedEventItem
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }
}
