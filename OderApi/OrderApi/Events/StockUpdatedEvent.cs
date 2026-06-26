namespace OrderApi.Events
{
    public class StockUpdatedEvent
    {
        public string EventName { get; set; } = "stock.updated";
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public decimal SellingPrice { get; set; }
        public int QuantityAvailable { get; set; }
        public string StockStatus { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
    }
}
