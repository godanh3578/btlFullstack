using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public enum StockStatus
    {
        InStock,
        OutOfStock,
        LowStock
    }

    public class ProductStockCache
    {
        public int ProductStockCacheId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = "";

        [StringLength(100)]
        public string CategoryName { get; set; } = "";

        [Range(0, double.MaxValue)]
        public decimal SellingPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int QuantityAvailable { get; set; }

        public StockStatus StockStatus { get; set; } = StockStatus.OutOfStock;

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;
    }
}
