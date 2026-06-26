using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Models;

namespace OrderApi.Services
{
    public sealed class ProductCatalogClient : IProductCatalogClient
    {
        private readonly HttpClient _httpClient;
        private readonly OrderDbContext _dbContext;
        private readonly IConfiguration _config;
        private readonly ILogger<ProductCatalogClient> _logger;

        public ProductCatalogClient(
            HttpClient httpClient,
            OrderDbContext dbContext,
            IConfiguration config,
            ILogger<ProductCatalogClient> logger)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _config = config;
            _logger = logger;
        }

        public async Task<ProductCatalogItem?> GetProductAsync(
            int productId,
            string? externalProductId = null,
            CancellationToken cancellationToken = default)
        {
            if (_config.GetValue<bool>("ProductIntegration:UseGatewayLookup"))
            {
                var allProducts = await SyncFromGatewayAsync(cancellationToken);
                var product = !string.IsNullOrWhiteSpace(externalProductId)
                    ? allProducts.FirstOrDefault(p => string.Equals(p.ExternalProductId, externalProductId, StringComparison.OrdinalIgnoreCase))
                    : null;
                product ??= allProducts.FirstOrDefault(p => p.ProductId == productId);
                if (product != null)
                {
                    product.ProductId = productId;
                    return product;
                }

                if (!_config.GetValue("ProductIntegration:AllowCacheFallback", true))
                    return null;
            }

            return await GetFromCacheAsync(productId, cancellationToken);
        }

        public async Task<IReadOnlyList<ProductCatalogItem>> GetProductsAsync(
            IEnumerable<int> productIds,
            CancellationToken cancellationToken = default)
        {
            var ids = productIds.Distinct().ToList();
            if (ids.Count == 0)
                return Array.Empty<ProductCatalogItem>();

            if (_config.GetValue<bool>("ProductIntegration:UseGatewayLookup") && _httpClient.BaseAddress != null)
            {
                var allProducts = await SyncFromGatewayAsync(cancellationToken);
                var products = allProducts.Where(p => ids.Contains(p.ProductId)).ToList();
                if (products.Count > 0)
                    return products;
            }

            var results = new List<ProductCatalogItem>();
            foreach (var id in ids)
            {
                var product = await GetFromCacheAsync(id, cancellationToken);
                if (product != null)
                    results.Add(product);
            }

            return results;
        }

        private async Task<List<ProductCatalogItem>> SyncFromGatewayAsync(CancellationToken cancellationToken)
        {
            if (_httpClient.BaseAddress == null)
                return new List<ProductCatalogItem>();

            var path = "/api/products";
            try
            {
                using var response = await _httpClient.GetAsync(path, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return new List<ProductCatalogItem>();

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                
                var source = document.RootElement;
                if (source.ValueKind == JsonValueKind.Object)
                {
                    if (TryGetProperty(source, "data", out var data)) source = data;
                    else if (TryGetProperty(source, "items", out var items)) source = items;
                    else if (TryGetProperty(source, "products", out var products)) source = products;
                }

                if (source.ValueKind != JsonValueKind.Array)
                    return new List<ProductCatalogItem>();

                var results = new List<ProductCatalogItem>();
                int index = 1;

                foreach (var item in source.EnumerateArray())
                {
                    var catalogItem = new ProductCatalogItem
                    {
                        ProductId = index,
                        ExternalProductId = GetString(item, "id", "productId", "ProductID", "Id"),
                        ProductCode = $"SP{index:D3}",
                        ProductName = GetString(item, "productName", "name", "ProductName", "Name"),
                        CategoryName = GetString(item, "categoryName", "category", "CategoryName", "Category"),
                        SellingPrice = GetDecimal(item, "sellingPrice", "price", "Price", "SellingPrice"),
                        QuantityAvailable = GetInt(item, 0, "quantityAvailable", "quality", "stock", "availableStock", "QuantityAvailable", "Quantity"),
                        StockStatus = GetString(item, "stockStatus", "status", "StockStatus", "Status")
                    };

                    if (string.IsNullOrWhiteSpace(catalogItem.StockStatus))
                    {
                        catalogItem.StockStatus = catalogItem.QuantityAvailable <= 0 ? "OutOfStock" : "InStock";
                    }

                    results.Add(catalogItem);

                    var cache = await _dbContext.ProductStockCaches
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.ProductId == index, cancellationToken);

                    if (cache == null)
                    {
                        cache = new ProductStockCache { ProductId = index };
                        _dbContext.ProductStockCaches.Add(cache);
                    }

                    cache.ProductCode = catalogItem.ProductCode;
                    cache.ProductName = catalogItem.ProductName;
                    cache.CategoryName = catalogItem.CategoryName;
                    cache.SellingPrice = catalogItem.SellingPrice;
                    cache.QuantityAvailable = catalogItem.QuantityAvailable;
                    
                    if (Enum.TryParse<StockStatus>(catalogItem.StockStatus, true, out var parsedStatus))
                        cache.StockStatus = parsedStatus;
                    else
                        cache.StockStatus = catalogItem.QuantityAvailable > 0 ? StockStatus.InStock : StockStatus.OutOfStock;
                        
                    cache.IsDeleted = false;
                    cache.LastUpdatedAt = DateTime.UtcNow;

                    index++;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot sync products from Product API");
                return new List<ProductCatalogItem>();
            }
        }

        public async Task<InventoryCheckResult> CheckInventoryAsync(
            IEnumerable<ProductInventoryRequest> items,
            CancellationToken cancellationToken = default)
        {
            var requests = NormalizeInventoryRequests(items);
            if (requests.Count == 0)
                return InventoryCheckResult.Available();

            if (_config.GetValue<bool>("ProductIntegration:UseGatewayLookup") && _httpClient.BaseAddress != null)
            {
                var path = _config["ProductIntegration:InventoryCheckPath"] ?? "/api/inventory/check";
                var result = await TryPostInventoryAsync(path, requests, cancellationToken);
                if (result.HasValue)
                    return result.Value
                        ? InventoryCheckResult.Available()
                        : new InventoryCheckResult { IsAvailable = false };
            }

            return await CheckCacheInventoryAsync(requests, cancellationToken);
        }

        public async Task<bool> DeductInventoryAsync(
            IEnumerable<ProductInventoryRequest> items,
            CancellationToken cancellationToken = default)
        {
            return await PostOrApplyCacheInventoryAsync(
                _config["ProductIntegration:InventoryDeductPath"] ?? "/api/inventory/deduct",
                NormalizeInventoryRequests(items),
                deduct: true,
                cancellationToken);
        }

        public async Task<bool> RestoreInventoryAsync(
            IEnumerable<ProductInventoryRequest> items,
            CancellationToken cancellationToken = default)
        {
            return await PostOrApplyCacheInventoryAsync(
                _config["ProductIntegration:InventoryRestorePath"] ?? "/api/inventory/restore",
                NormalizeInventoryRequests(items),
                deduct: false,
                cancellationToken);
        }

        private async Task<ProductCatalogItem?> GetFromCacheAsync(int productId, CancellationToken cancellationToken)
        {
            var stock = await _dbContext.ProductStockCaches
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.ProductId == productId, cancellationToken);

            if (stock == null)
                return null;

            return new ProductCatalogItem
            {
                ProductId = stock.ProductId,
                ProductCode = stock.ProductCode,
                ProductName = stock.ProductName,
                CategoryName = stock.CategoryName,
                SellingPrice = stock.SellingPrice,
                QuantityAvailable = stock.QuantityAvailable,
                StockStatus = stock.StockStatus.ToString()
            };
        }

        private async Task<bool> PostOrApplyCacheInventoryAsync(
            string path,
            List<ProductInventoryRequest> requests,
            bool deduct,
            CancellationToken cancellationToken)
        {
            if (requests.Count == 0)
                return true;

            if (_config.GetValue<bool>("ProductIntegration:UseGatewayLookup") && _httpClient.BaseAddress != null)
            {
                var result = await TryPostInventoryAsync(path, requests, cancellationToken);
                if (result.HasValue)
                    return result.Value;
            }

            if (!_config.GetValue("ProductIntegration:AllowCacheFallback", true))
                return false;

            return await ApplyCacheInventoryAsync(requests, deduct, cancellationToken);
        }

        private async Task<bool?> TryPostInventoryAsync(
            string path,
            List<ProductInventoryRequest> requests,
            CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(path, new { items = requests }, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Cannot call inventory endpoint {Path}", path);
                return null;
            }
        }

        private async Task<InventoryCheckResult> CheckCacheInventoryAsync(
            List<ProductInventoryRequest> requests,
            CancellationToken cancellationToken)
        {
            var result = InventoryCheckResult.Available();
            foreach (var request in requests)
            {
                var stock = await _dbContext.ProductStockCaches
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.ProductId == request.ProductId, cancellationToken);

                var available = stock?.QuantityAvailable ?? 0;
                if (available < request.Quantity)
                {
                    result.IsAvailable = false;
                    result.Shortages.Add(new InventoryShortageItem
                    {
                        ProductId = request.ProductId,
                        RequestedQuantity = request.Quantity,
                        AvailableQuantity = available
                    });
                }
            }

            return result;
        }

        private async Task<bool> ApplyCacheInventoryAsync(
            List<ProductInventoryRequest> requests,
            bool deduct,
            CancellationToken cancellationToken)
        {
            if (deduct)
            {
                var check = await CheckCacheInventoryAsync(requests, cancellationToken);
                if (!check.IsAvailable)
                    return false;
            }

            foreach (var request in requests)
            {
                var stock = await _dbContext.ProductStockCaches
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.ProductId == request.ProductId, cancellationToken);
                if (stock == null)
                    continue;

                stock.QuantityAvailable = deduct
                    ? Math.Max(0, stock.QuantityAvailable - request.Quantity)
                    : stock.QuantityAvailable + request.Quantity;
                stock.StockStatus = stock.QuantityAvailable <= 0
                    ? StockStatus.OutOfStock
                    : stock.QuantityAvailable <= 5
                        ? StockStatus.LowStock
                        : StockStatus.InStock;
                stock.IsDeleted = false;
                stock.LastUpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static List<ProductInventoryRequest> NormalizeInventoryRequests(IEnumerable<ProductInventoryRequest> items)
        {
            return items
                .Where(i => i.Quantity > 0)
                .GroupBy(i => new { i.ProductId, ExternalProductId = i.ExternalProductId ?? "" })
                .Select(g => new ProductInventoryRequest
                {
                    ProductId = g.Key.ProductId,
                    ExternalProductId = string.IsNullOrWhiteSpace(g.Key.ExternalProductId) ? null : g.Key.ExternalProductId,
                    Quantity = g.Sum(i => i.Quantity)
                })
                .ToList();
        }

        private static ProductCatalogItem MapGatewayProduct(JsonElement element, int requestedProductId)
        {
            var productId = GetInt(element, requestedProductId, "productId", "id", "ProductID", "Id");
            var productCode = GetString(element, "productCode", "code", "ProductCode", "Code");

            return new ProductCatalogItem
            {
                ProductId = productId,
                ExternalProductId = GetString(element, "externalProductId", "id", "productId", "ProductID", "Id"),
                ProductCode = string.IsNullOrWhiteSpace(productCode) || IsGuidLike(productCode)
                    ? $"SP{productId:D3}"
                    : productCode,
                ProductName = GetString(element, "productName", "name", "ProductName", "Name"),
                CategoryName = GetString(element, "categoryName", "category", "CategoryName", "Category"),
                SellingPrice = GetDecimal(element, "sellingPrice", "price", "Price", "SellingPrice"),
                QuantityAvailable = GetInt(element, 0, "quantityAvailable", "quality", "stock", "availableStock", "QuantityAvailable", "Quantity"),
                StockStatus = GetString(element, "stockStatus", "status", "StockStatus", "Status")
            };
        }

        private static List<ProductCatalogItem> MapProductCollection(JsonElement element, List<int> requestedIds)
        {
            var source = element;
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (TryGetProperty(element, "data", out var data))
                    source = data;
                else if (TryGetProperty(element, "items", out var items))
                    source = items;
                else if (TryGetProperty(element, "products", out var products))
                    source = products;
            }

            if (source.ValueKind != JsonValueKind.Array)
                return new List<ProductCatalogItem>();

            var results = new List<ProductCatalogItem>();
            foreach (var item in source.EnumerateArray())
            {
                var productId = GetInt(item, 0, "productId", "id", "ProductID", "Id");
                results.Add(MapGatewayProduct(item, productId == 0 ? requestedIds.FirstOrDefault() : productId));
            }

            return results;
        }

        private static bool IsGuidLike(string value)
        {
            var text = value.Trim();
            return text.Length is 32 or 36 && Guid.TryParse(text, out _);
        }

        private static string GetString(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (TryGetProperty(element, name, out var property))
                    return property.ValueKind == JsonValueKind.String ? property.GetString() ?? "" : property.ToString();
            }

            return "";
        }

        private static int GetInt(JsonElement element, int fallback, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetProperty(element, name, out var property))
                    continue;

                if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
                    return value;

                if (property.ValueKind == JsonValueKind.String && int.TryParse(property.GetString(), out value))
                    return value;
            }

            return fallback;
        }

        private static decimal GetDecimal(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (!TryGetProperty(element, name, out var property))
                    continue;

                if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var value))
                    return value;

                if (property.ValueKind == JsonValueKind.String && decimal.TryParse(property.GetString(), out value))
                    return value;
            }

            return 0;
        }

        private static bool TryGetProperty(JsonElement element, string name, out JsonElement property)
        {
            foreach (var item in element.EnumerateObject())
            {
                if (string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    property = item.Value;
                    return true;
                }
            }

            property = default;
            return false;
        }
    }
}
