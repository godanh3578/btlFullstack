namespace OrderApi.DTOs.Returns
{
    public class ReturnItemDto
    {
        public int ReturnDetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class ReturnDto
    {
        public int ReturnId { get; set; }
        public string ReturnCode { get; set; } = "";
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = "";
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public DateTime ReturnDate { get; set; }
        public decimal RefundAmount { get; set; }
        public string? Reason { get; set; }
        public string ReturnStatus { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<ReturnItemDto> Items { get; set; } = new();
    }
}
