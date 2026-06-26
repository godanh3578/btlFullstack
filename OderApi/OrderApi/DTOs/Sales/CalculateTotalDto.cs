namespace OrderApi.DTOs.Sales
{
    public class CalculateTotalDto
    {
        public List<SalesItemDto> Items { get; set; } = new();
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; } = "Fixed";
        public decimal DiscountValue { get; set; }
    }

    public class SalesItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CalculateTotalResponseDto
    {
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; } = "";
        public decimal DiscountValue { get; set; }
        public decimal FinalAmount { get; set; }
    }
}
