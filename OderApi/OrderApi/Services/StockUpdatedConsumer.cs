using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Events;
using OrderApi.Models;

namespace OrderApi.Services
{
    public sealed class StockUpdatedConsumer : IConsumer<StockUpdatedEvent>
    {
        private readonly OrderDbContext _db;
        private readonly ILogger<StockUpdatedConsumer> _logger;

        public StockUpdatedConsumer(OrderDbContext db, ILogger<StockUpdatedConsumer> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<StockUpdatedEvent> context)
        {
            var data = context.Message;
            var cache = await _db.ProductStockCaches
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ProductId == data.ProductId, context.CancellationToken);

            var stockStatus = Enum.TryParse<StockStatus>(data.StockStatus, true, out var parsed)
                ? parsed
                : data.QuantityAvailable > 0 ? StockStatus.InStock : StockStatus.OutOfStock;

            if (cache == null)
            {
                cache = new ProductStockCache { ProductId = data.ProductId };
                _db.ProductStockCaches.Add(cache);
            }

            cache.ProductCode = data.ProductCode;
            cache.ProductName = data.ProductName;
            cache.CategoryName = data.CategoryName;
            cache.SellingPrice = data.SellingPrice;
            cache.QuantityAvailable = data.QuantityAvailable;
            cache.StockStatus = stockStatus;
            cache.IsDeleted = false;
            cache.LastUpdatedAt = data.UpdatedAt == default ? DateTime.UtcNow : data.UpdatedAt;

            await _db.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation(
                "[MassTransit] stock.updated: ProductId={ProductId} Qty={Quantity}",
                data.ProductId,
                data.QuantityAvailable);
        }
    }
}
