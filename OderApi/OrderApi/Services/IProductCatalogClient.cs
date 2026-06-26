namespace OrderApi.Services
{
    public interface IProductCatalogClient
    {
        Task<ProductCatalogItem?> GetProductAsync(int productId, string? externalProductId = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProductCatalogItem>> GetProductsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default);
        Task<InventoryCheckResult> CheckInventoryAsync(IEnumerable<ProductInventoryRequest> items, CancellationToken cancellationToken = default);
        Task<bool> DeductInventoryAsync(IEnumerable<ProductInventoryRequest> items, CancellationToken cancellationToken = default);
        Task<bool> RestoreInventoryAsync(IEnumerable<ProductInventoryRequest> items, CancellationToken cancellationToken = default);
    }

    public sealed class ProductCatalogItem
    {
        public int ProductId { get; set; }
        public string ExternalProductId { get; set; } = "";
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public decimal SellingPrice { get; set; }
        public int QuantityAvailable { get; set; }
        public string StockStatus { get; set; } = "";
    }

    public sealed class ProductInventoryRequest
    {
        public int ProductId { get; set; }
        public string? ExternalProductId { get; set; }
        public int Quantity { get; set; }
    }

    public sealed class InventoryCheckResult
    {
        public bool IsAvailable { get; set; }
        public List<InventoryShortageItem> Shortages { get; set; } = new();

        public static InventoryCheckResult Available() => new() { IsAvailable = true };
    }

    public sealed class InventoryShortageItem
    {
        public int ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
    }
}
