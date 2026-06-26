using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddSingleton<ProductInventoryStore>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddHostedService<ReservationExpiryService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    var tokens = context.RequestServices.GetRequiredService<JwtTokenService>();
    var user = GetCurrentUser(context.Request, tokens);
    if (user is not null && !CanAccessProductInventoryApi(context.Request, user))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { message = "Ban khong co quyen truy cap chuc nang nay." });
        return;
    }

    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    service = "KhoPro Machine 1 - Product & Inventory Service",
    status = "running",
    api = "/api"
}));

app.MapGet("/api/products", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null
        ? Results.Ok(store.GetProductsForPublicCatalog())
        : Results.Ok(store.GetProducts(user));
});

app.MapGet("/api/products/{id}", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    var product = user is null
        ? store.GetPublicProduct(id)
        : store.GetProduct(user, id);
    return product is null ? Results.NotFound(new { message = "Khong tim thay san pham." }) : Results.Ok(product);
});

app.MapPost("/api/products/batch", (OrderProductBatchRequest payload, ProductInventoryStore store) =>
{
    var products = store.GetOrderProducts(payload.ProductIds);
    return Results.Ok(new { products });
});

app.MapPost("/api/inventory/check", (OrderInventoryRequest payload, ProductInventoryStore store) =>
{
    var result = store.CheckOrderInventory(payload.Items);
    return result.IsAvailable
        ? Results.Ok(new { isAvailable = true })
        : Results.BadRequest(new { isAvailable = false, shortages = result.Shortages });
});

app.MapPost("/api/inventory/deduct", (OrderInventoryRequest payload, ProductInventoryStore store) =>
{
    var result = store.ApplyOrderInventory(payload.Items, deduct: true);
    return result.IsAvailable
        ? Results.Ok(new { success = true })
        : Results.BadRequest(new { success = false, shortages = result.Shortages });
});

app.MapPost("/api/inventory/restore", (OrderInventoryRequest payload, ProductInventoryStore store) =>
{
    store.ApplyOrderInventory(payload.Items, deduct: false);
    return Results.Ok(new { success = true });
});

app.MapPost("/api/products", (ProductRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var product = store.CreateProduct(user.Id, payload);
    return Results.Ok(product);
});

app.MapPut("/api/products/{id}", (string id, ProductRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var product = store.UpdateProduct(user.Id, id, payload);
    return product is null ? Results.NotFound(new { message = "Khong tim thay san pham." }) : Results.Ok(product);
});

app.MapDelete("/api/products/{id}", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    return store.DeleteProduct(user.Id, id)
        ? Results.Ok(new { message = "Da xoa san pham." })
        : Results.NotFound(new { message = "Khong tim thay san pham." });
});

app.MapPost("/api/products/{id}/stock/adjust", (string id, StockAdjustmentRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var movement = store.CreateMovementRequest(user, id, payload);
    return movement is null ? Results.NotFound(new { message = "Khong tim thay san pham." }) : Results.Ok(movement);
});

app.MapPost("/api/products/{id}/stock/sell", (string id, SellStockRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var product = store.SellStock(id, payload.Quantity, payload.Note ?? $"Ban hang boi {user.Email}");
    return product is null ? Results.NotFound(new { message = "Khong tim thay san pham hoac khong du ton kho." }) : Results.Ok(product);
});

app.MapGet("/api/inventory/reservations", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null ? Results.Unauthorized() : Results.Ok(store.GetReservations(user.Id));
});

app.MapPost("/api/inventory/reservations", (InventoryReservationRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var result = store.CreateReservation(user.Id, payload);
    return result.Reservation is null
        ? Results.BadRequest(new { message = result.Message })
        : Results.Ok(result.Reservation);
});

app.MapPost("/api/inventory/reservations/{id}/confirm", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var reservation = store.ConfirmReservation(user.Id, id);
    return reservation is null
        ? Results.BadRequest(new { message = "Phieu giu hang khong ton tai, da het han hoac da duoc xu ly." })
        : Results.Ok(reservation);
});

app.MapPost("/api/inventory/reservations/{id}/cancel", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var reservation = store.CancelReservation(user.Id, id, "cancelled");
    return reservation is null
        ? Results.BadRequest(new { message = "Phieu giu hang khong ton tai hoac da duoc xu ly." })
        : Results.Ok(reservation);
});

app.MapGet("/api/inventory/summary", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null ? Results.Unauthorized() : Results.Ok(store.GetInventorySummary(user));
});

app.MapGet("/api/inventory/low-stock", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null ? Results.Unauthorized() : Results.Ok(store.GetLowStockProducts(user));
});

app.MapGet("/api/inventory/movements", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null ? Results.Unauthorized() : Results.Ok(store.GetMovements(user));
});

app.MapPost("/api/inventory/movements", (InventoryMovementRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var movement = store.CreateMovementRequest(user, payload);
    return movement is null ? Results.NotFound(new { message = "Khong tim thay san pham." }) : Results.Ok(movement);
});

app.MapPut("/api/inventory/movements/{id}", (string id, InventoryMovementRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var movement = store.UpdateMovementRequest(user, id, payload);
    return movement is null ? Results.NotFound(new { message = "Khong tim thay phieu hoac phieu da duoc xu ly." }) : Results.Ok(movement);
});

app.MapPost("/api/inventory/movements/{id}/approve", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var movement = store.ApproveMovement(user, id);
    return movement is null ? Results.BadRequest(new { message = "Khong tim thay phieu hoac phieu khong the duyet." }) : Results.Ok(movement);
});

app.MapPost("/api/inventory/movements/{id}/cancel", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var movement = store.CancelMovement(user, id);
    return movement is null ? Results.BadRequest(new { message = "Khong tim thay phieu hoac phieu khong the huy." }) : Results.Ok(movement);
});

// Categories (self-referencing parent-child)
app.MapGet("/api/categories", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null
        ? Results.Ok(store.GetPublicCategories())
        : Results.Ok(store.GetCategories(user));
});

app.MapPost("/api/categories", (CategoryRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();
    var cat = store.CreateCategory(user.Id, payload);
    return Results.Ok(cat);
});

app.MapPut("/api/categories/{id}", (string id, CategoryRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();
    var updated = store.UpdateCategory(user.Id, id, payload);
    return updated is null ? Results.NotFound(new { message = "Khong tim thay danh muc." }) : Results.Ok(updated);
});

app.MapDelete("/api/categories/{id}", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();
    return store.DeleteCategory(user.Id, id)
        ? Results.Ok(new { message = "Da xoa danh muc." })
        : Results.NotFound(new { message = "Khong tim thay danh muc." });
});

// Inventory receipts (incoming stock) and confirmation
app.MapGet("/api/inventory/receipts", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null ? Results.Unauthorized() : Results.Ok(store.GetReceipts(user));
});

app.MapPost("/api/inventory/receipts", (InventoryReceiptRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();
    var receipt = store.CreateReceipt(user, payload);
    return Results.Ok(receipt);
});

app.MapPut("/api/inventory/receipts/{id}", (string id, InventoryReceiptRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var receipt = store.UpdateReceipt(user, id, payload);
    return receipt is null ? Results.NotFound(new { message = "Khong tim thay phieu hoac phieu da duoc xu ly." }) : Results.Ok(receipt);
});

app.MapPost("/api/inventory/receipts/{id}/approve", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();
    var approved = store.ApproveReceipt(user, id);
    return approved is null ? Results.BadRequest(new { message = "Khong tim thay phieu hoac phieu khong the duyet." }) : Results.Ok(approved);
});

app.MapPost("/api/inventory/receipts/{id}/cancel", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();
    var cancelled = store.CancelReceipt(user, id);
    return cancelled is null ? Results.BadRequest(new { message = "Khong tim thay phieu hoac phieu khong the huy." }) : Results.Ok(cancelled);
});

app.MapPost("/api/inventory/receipts/{id}/confirm", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();
    var approved = store.ApproveReceipt(user, id);
    return approved is null ? Results.BadRequest(new { message = "Khong tim thay phieu hoac phieu khong the duyet." }) : Results.Ok(approved);
});

app.MapGet("/api/suppliers", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null ? Results.Unauthorized() : Results.Ok(store.GetSuppliers(user));
});

app.MapPost("/api/suppliers", (SupplierRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null ? Results.Unauthorized() : Results.Ok(store.CreateSupplier(user.Id, payload));
});

app.MapPut("/api/suppliers/{id}", (string id, SupplierRequest payload, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    var supplier = store.UpdateSupplier(user.Id, id, payload);
    return supplier is null ? Results.NotFound(new { message = "Khong tim thay nha cung cap." }) : Results.Ok(supplier);
});

app.MapDelete("/api/suppliers/{id}", (string id, HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    if (user is null) return Results.Unauthorized();

    return store.DeleteSupplier(user.Id, id)
        ? Results.Ok(new { message = "Da xoa nha cung cap." })
        : Results.NotFound(new { message = "Khong tim thay nha cung cap." });
});

app.MapGet("/api/events", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null ? Results.Unauthorized() : Results.Ok(store.GetEvents(user.Id));
});

app.MapGet("/api/events/stock-updated", (HttpRequest request, ProductInventoryStore store, JwtTokenService tokens) =>
{
    var user = GetCurrentUser(request, tokens);
    return user is null ? Results.Unauthorized() : Results.Ok(store.GetEvents(user.Id, "stock.updated"));
});

app.Run();

static CurrentUser? GetCurrentUser(HttpRequest request, JwtTokenService tokens)
{
    var authHeader = request.Headers.Authorization.ToString();
    if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        return null;
    }

    return tokens.ValidateToken(authHeader["Bearer ".Length..].Trim());
}

static bool CanAccessProductInventoryApi(HttpRequest request, CurrentUser user)
{
    var method = request.Method;
    var path = request.Path.Value ?? "";

    if (path.StartsWith("/api/inventory/reservations", StringComparison.OrdinalIgnoreCase))
    {
        return HasAnyRole(user, "admin-user", "Admin", "admin", "Sales", "Warehouse");
    }

    if (method == HttpMethods.Post
        && path.StartsWith("/api/products", StringComparison.OrdinalIgnoreCase)
        && path.EndsWith("/stock/sell", StringComparison.OrdinalIgnoreCase))
    {
        return HasAnyRole(user, "admin-user", "Admin", "admin", "Sales", "Warehouse", "user");
    }

    if (method == HttpMethods.Get && (path.StartsWith("/api/products", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/categories", StringComparison.OrdinalIgnoreCase)))
    {
        return HasAnyRole(user, "admin-user", "Admin", "admin", "Sales", "Warehouse", "user");
    }

    if (method == HttpMethods.Get && (path.StartsWith("/api/inventory", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/suppliers", StringComparison.OrdinalIgnoreCase)))
    {
        return HasAnyRole(user, "admin-user", "Admin", "admin", "Sales", "Warehouse");
    }

    if (path.StartsWith("/api/products", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/categories", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/suppliers", StringComparison.OrdinalIgnoreCase))
    {
        return HasAnyRole(user, "admin-user", "Admin", "admin", "Sales", "Warehouse");
    }

    if (path.StartsWith("/api/inventory", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/api/events", StringComparison.OrdinalIgnoreCase))
    {
        return HasAnyRole(user, "admin-user", "Admin", "admin", "Warehouse");
    }

    return HasAnyRole(user, "admin-user", "Admin", "admin");
}

static bool HasAnyRole(CurrentUser user, params string[] roles)
{
    return roles.Any(role => role.Equals(user.Role, StringComparison.OrdinalIgnoreCase));
}

public sealed class ProductInventoryStore
{
    private readonly object _lock = new();
    private readonly string _productsPath;
    private readonly string _movementsPath;
    private readonly string _suppliersPath;
    private readonly string _categoriesPath;
    private readonly string _receiptsPath;
    private readonly string _eventsPath;
    private readonly string _reservationsPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public ProductInventoryStore(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _productsPath = Path.Combine(dataDir, "products.json");
        _movementsPath = Path.Combine(dataDir, "inventory-movements.json");
        _suppliersPath = Path.Combine(dataDir, "suppliers.json");
        _categoriesPath = Path.Combine(dataDir, "categories.json");
        _receiptsPath = Path.Combine(dataDir, "inventory-receipts.json");
        _eventsPath = Path.Combine(dataDir, "events.json");
        _reservationsPath = Path.Combine(dataDir, "inventory-reservations.json");

        EnsureFile<ProductItem>(_productsPath);
        EnsureFile<InventoryMovement>(_movementsPath);
        EnsureFile<Supplier>(_suppliersPath);
        EnsureFile<Category>(_categoriesPath);
        EnsureFile<InventoryReceipt>(_receiptsPath);
        EnsureFile<EventRecord>(_eventsPath);
        EnsureFile<InventoryReservation>(_reservationsPath);
    }

    public List<ProductItem> GetProducts(string ownerId)
    {
        lock (_lock)
        {
            var categories = Read<Category>(_categoriesPath);
            var products = Read<ProductItem>(_productsPath)
                .OrderByDescending(product => product.CreatedAt)
                .ToList();

            if (EnsureProductSkus(products, categories))
            {
                Write(_productsPath, products);
            }

            return products;
        }
    }

    public List<ProductItem> GetProducts(CurrentUser user)
    {
        lock (_lock)
        {
            var categories = Read<Category>(_categoriesPath);
            var allProducts = Read<ProductItem>(_productsPath)
                .OrderByDescending(product => product.CreatedAt)
                .ToList();

            if (EnsureProductSkus(allProducts, categories))
            {
                Write(_productsPath, allProducts);
            }

            return allProducts;
        }
    }

    public List<ProductItem> GetProductsForPublicCatalog()
    {
        lock (_lock)
        {
            var categories = Read<Category>(_categoriesPath);
            var products = Read<ProductItem>(_productsPath)
                .Where(product => !string.Equals(product.Status, "Ngung ban", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(product => product.CreatedAt)
                .ToList();

            if (EnsureProductSkus(products, categories))
            {
                Write(_productsPath, products);
            }

            return products;
        }
    }

    public ProductItem? GetProduct(string ownerId, string id)
    {
        lock (_lock)
        {
            return Read<ProductItem>(_productsPath).FirstOrDefault(product => product.Id == id);
        }
    }

    public ProductItem? GetProduct(CurrentUser user, string id)
    {
        lock (_lock)
        {
            return Read<ProductItem>(_productsPath)
                .FirstOrDefault(product => product.Id == id);
        }
    }

    public ProductItem? GetPublicProduct(string id)
    {
        lock (_lock)
        {
            return Read<ProductItem>(_productsPath)
                .FirstOrDefault(product => product.Id == id && !string.Equals(product.Status, "Ngung ban", StringComparison.OrdinalIgnoreCase));
        }
    }

    public List<OrderProductDto> GetOrderProducts(IEnumerable<int> productIds)
    {
        lock (_lock)
        {
            var products = Read<ProductItem>(_productsPath)
                .OrderByDescending(product => product.CreatedAt)
                .ToList();
            return productIds
                .Distinct()
                .Select(id => MapForOrder(ResolveOrderProduct(products, id), id))
                .Where(product => product is not null)
                .Select(product => product!)
                .ToList();
        }
    }

    public OrderInventoryResult CheckOrderInventory(IEnumerable<OrderInventoryItem> items)
    {
        lock (_lock)
        {
            var products = Read<ProductItem>(_productsPath)
                .OrderByDescending(product => product.CreatedAt)
                .ToList();
            var shortages = new List<OrderInventoryShortage>();

            foreach (var item in items.Where(item => item.Quantity > 0))
            {
                var product = ResolveOrderProduct(products, item.ProductId, item.ExternalProductId);
                var available = product is null ? 0 : (int)Math.Floor(ParseDecimal(product.Stock));
                if (available < item.Quantity)
                {
                    shortages.Add(new OrderInventoryShortage(item.ProductId, item.Quantity, available));
                }
            }

            return new OrderInventoryResult(shortages.Count == 0, shortages);
        }
    }

    public OrderInventoryResult ApplyOrderInventory(IEnumerable<OrderInventoryItem> items, bool deduct)
    {
        lock (_lock)
        {
            var products = Read<ProductItem>(_productsPath)
                .OrderByDescending(product => product.CreatedAt)
                .ToList();
            var requests = items.Where(item => item.Quantity > 0).ToList();

            if (deduct)
            {
                var check = CheckOrderInventory(requests);
                if (!check.IsAvailable)
                    return check;
            }

            foreach (var item in requests)
            {
                var product = ResolveOrderProduct(products, item.ProductId, item.ExternalProductId);
                if (product is null)
                    continue;

                var current = ParseDecimal(product.Stock);
                var next = deduct ? Math.Max(0, current - item.Quantity) : current + item.Quantity;
                product.Stock = next.ToString("0.##");
                product.UpdatedAt = DateTimeOffset.UtcNow;

                CreateMovementLocked(
                    product.OwnerId,
                    new InventoryMovementRequest(
                        product.Id,
                        product.Name,
                        deduct ? "sale" : "restore",
                        item.Quantity,
                        deduct ? "Order service deduct" : "Order service restore"));

                PublishEventLocked(product.OwnerId, "stock.updated", new { product.Id, product.Name, stock = product.Stock });
            }

            Write(_productsPath, products);
            return new OrderInventoryResult(true, []);
        }
    }

    private static ProductItem? ResolveOrderProduct(List<ProductItem> products, int productId, string? externalProductId = null)
    {
        if (!string.IsNullOrWhiteSpace(externalProductId))
        {
            var externalId = externalProductId.Trim();
            var externalProduct = products.FirstOrDefault(product =>
                product.Id.Equals(externalId, StringComparison.OrdinalIgnoreCase)
                || product.Sku.Equals(externalId, StringComparison.OrdinalIgnoreCase));
            if (externalProduct is not null)
                return externalProduct;
        }

        if (productId <= 0)
            return null;

        var idText = productId.ToString();
        return products.FirstOrDefault(product => product.Id == idText || product.Sku == idText)
            ?? products.ElementAtOrDefault(productId - 1);
    }

    private static OrderProductDto? MapForOrder(ProductItem? product, int requestedProductId)
    {
        if (product is null)
            return null;

        var sellingPrice = ParseDecimal(product.Price);
        var quantityAvailable = (int)Math.Floor(ParseDecimal(product.Stock));

        return new OrderProductDto(
            requestedProductId,
            string.IsNullOrWhiteSpace(product.Sku) ? product.Id : product.Sku,
            product.Name,
            "",
            sellingPrice,
            quantityAvailable,
            quantityAvailable <= 0 ? "OutOfStock" : quantityAvailable <= ParseDecimal(product.MinimumStock) ? "LowStock" : "InStock");
    }

    public ProductItem CreateProduct(string ownerId, ProductRequest payload)
    {
        lock (_lock)
        {
            var products = Read<ProductItem>(_productsPath);
            var categories = Read<Category>(_categoriesPath);
            var now = DateTimeOffset.UtcNow;
            var product = new ProductItem
            {
                Id = Guid.NewGuid().ToString("N"),
                OwnerId = ownerId,
                Name = payload.Name?.Trim() ?? "",
                Description = payload.Description?.Trim() ?? "",
                Price = payload.Price?.Trim() ?? "",
                Cost = payload.Cost?.Trim() ?? "",
                Image = payload.Image?.Trim() ?? "",
                Sku = payload.Sku?.Trim() ?? "",
                Stock = NormalizeNumber(payload.Stock),
                CategoryId = payload.CategoryId?.Trim() ?? "",
                MinimumStock = NormalizeNumber(payload.MinimumStock),
                Status = string.IsNullOrWhiteSpace(payload.Status) ? "Dang ban" : payload.Status.Trim(),
                CreatedAt = now
            };

            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                product.Sku = GenerateProductSku(product, products.Append(product).ToList(), categories);
            }

            products.Add(product);
            Write(_productsPath, products);
            return product;
        }
    }

    public ProductItem? UpdateProduct(string ownerId, string id, ProductRequest payload)
    {
        lock (_lock)
        {
            var products = Read<ProductItem>(_productsPath);
            var categories = Read<Category>(_categoriesPath);
            var product = products.FirstOrDefault(item => item.Id == id);
            if (product is null) return null;

            product.Name = payload.Name?.Trim() ?? product.Name;
            product.Description = payload.Description?.Trim() ?? product.Description;
            product.Price = payload.Price?.Trim() ?? product.Price;
            product.Cost = payload.Cost?.Trim() ?? product.Cost;
            product.Image = payload.Image?.Trim() ?? product.Image;
            product.Sku = payload.Sku?.Trim() ?? product.Sku;
            product.Stock = NormalizeNumber(payload.Stock ?? product.Stock);
            product.CategoryId = payload.CategoryId?.Trim() ?? product.CategoryId;
            product.MinimumStock = NormalizeNumber(payload.MinimumStock ?? product.MinimumStock);
            product.Status = payload.Status?.Trim() ?? product.Status;
            product.UpdatedAt = DateTimeOffset.UtcNow;

            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                product.Sku = GenerateProductSku(product, products, categories);
            }

            Write(_productsPath, products);
            return product;
        }
    }

    public bool DeleteProduct(string ownerId, string id)
    {
        lock (_lock)
        {
            var products = Read<ProductItem>(_productsPath);
            var removed = products.RemoveAll(product => product.Id == id) > 0;
            if (removed)
            {
                Write(_productsPath, products);
            }

            return removed;
        }
    }

    public ProductItem? AdjustStock(string ownerId, string id, StockAdjustmentRequest payload)
    {
        lock (_lock)
        {
            var products = Read<ProductItem>(_productsPath);
            var product = products.FirstOrDefault(item => item.Id == id);
            if (product is null) return null;

            var currentStock = ParseDecimal(product.Stock);
            var quantity = payload.Quantity;
            var nextStock = payload.Type?.Trim().ToLowerInvariant() switch
            {
                "out" or "export" or "sale" => Math.Max(0, currentStock - quantity),
                "set" => Math.Max(0, quantity),
                _ => Math.Max(0, currentStock + quantity)
            };

            product.Stock = nextStock.ToString("0.##");
            product.UpdatedAt = DateTimeOffset.UtcNow;
            Write(_productsPath, products);

            CreateMovementLocked(ownerId, new InventoryMovementRequest(
                product.Id,
                product.Name,
                payload.Type ?? "in",
                quantity,
                payload.Note));

            // publish stock.updated event
            PublishEventLocked(ownerId, "stock.updated", new { product.Id, product.Name, stock = product.Stock });

            return product;
        }
    }

    public ProductItem? SellStock(string id, decimal quantity, string note)
    {
        lock (_lock)
        {
            var products = Read<ProductItem>(_productsPath);
            var product = products.FirstOrDefault(item => item.Id == id);
            if (product is null) return null;

            var currentStock = ParseDecimal(product.Stock);
            if (quantity <= 0 || quantity > currentStock) return null;

            var ownerId = product.OwnerId;
            var nextStock = currentStock - quantity;
            product.Stock = nextStock.ToString("0.##");
            product.UpdatedAt = DateTimeOffset.UtcNow;
            Write(_productsPath, products);

            CreateMovementLocked(ownerId, new InventoryMovementRequest(
                product.Id,
                product.Name,
                "out",
                quantity,
                note));

            PublishEventLocked(ownerId, "stock.updated", new { product.Id, product.Name, stock = product.Stock });

            return product;
        }
    }

    public List<InventoryReservation> GetReservations(string ownerId)
    {
        ExpireReservations();
        lock (_lock)
        {
            return Read<InventoryReservation>(_reservationsPath)
                .Where(item => item.OwnerId == ownerId)
                .OrderByDescending(item => item.CreatedAt)
                .ToList();
        }
    }

    public ReservationResult CreateReservation(string ownerId, InventoryReservationRequest payload)
    {
        lock (_lock)
        {
            if (payload.Lines is null || payload.Lines.Count == 0)
            {
                return new ReservationResult(null, "Don COD chua co san pham.");
            }

            var products = Read<ProductItem>(_productsPath);
            var normalizedLines = payload.Lines
                .Where(line => !string.IsNullOrWhiteSpace(line.ProductId) && line.Quantity > 0)
                .GroupBy(line => line.ProductId)
                .Select(group => new ReservationLine(group.Key, group.Sum(line => line.Quantity)))
                .ToList();

            if (normalizedLines.Count == 0)
            {
                return new ReservationResult(null, "So luong giu hang khong hop le.");
            }

            foreach (var line in normalizedLines)
            {
                var product = products.FirstOrDefault(item => item.Id == line.ProductId);
                if (product is null || line.Quantity > ParseDecimal(product.Stock))
                {
                    return new ReservationResult(null, $"San pham {product?.Name ?? line.ProductId} khong du ton kho.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var reservation = new InventoryReservation
            {
                Id = Guid.NewGuid().ToString("N"),
                OwnerId = ownerId,
                OrderId = payload.OrderId?.Trim() ?? "",
                CustomerName = payload.CustomerName?.Trim() ?? "",
                Lines = normalizedLines,
                Status = "pending",
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(10)
            };

            foreach (var line in normalizedLines)
            {
                var product = products.First(item => item.Id == line.ProductId);
                product.Stock = (ParseDecimal(product.Stock) - line.Quantity).ToString("0.##");
                product.UpdatedAt = now;
                CreateMovementLocked(ownerId, new InventoryMovementRequest(
                    product.Id,
                    product.Name,
                    "reserve",
                    line.Quantity,
                    $"Giu hang COD {reservation.OrderId} den {reservation.ExpiresAt:O}"));
            }

            var reservations = Read<InventoryReservation>(_reservationsPath);
            reservations.Add(reservation);
            Write(_productsPath, products);
            Write(_reservationsPath, reservations);
            PublishEventLocked(ownerId, "inventory.reserved", new { reservation.Id, reservation.OrderId, reservation.ExpiresAt });
            return new ReservationResult(reservation, "");
        }
    }

    public InventoryReservation? ConfirmReservation(string ownerId, string id)
    {
        lock (_lock)
        {
            var reservations = Read<InventoryReservation>(_reservationsPath);
            var reservation = reservations.FirstOrDefault(item => item.Id == id && item.OwnerId == ownerId);
            if (reservation is null || reservation.Status != "pending" || reservation.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            reservation.Status = "confirmed";
            reservation.ConfirmedAt = DateTimeOffset.UtcNow;
            Write(_reservationsPath, reservations);
            PublishEventLocked(ownerId, "inventory.reservation.confirmed", new { reservation.Id, reservation.OrderId });
            return reservation;
        }
    }

    public InventoryReservation? CancelReservation(string ownerId, string id, string status)
    {
        lock (_lock)
        {
            var reservations = Read<InventoryReservation>(_reservationsPath);
            var reservation = reservations.FirstOrDefault(item => item.Id == id && item.OwnerId == ownerId);
            if (reservation is null || reservation.Status != "pending") return null;

            RestoreReservationLocked(reservation, status);
            Write(_reservationsPath, reservations);
            return reservation;
        }
    }

    public int ExpireReservations()
    {
        lock (_lock)
        {
            var reservations = Read<InventoryReservation>(_reservationsPath);
            var expired = reservations
                .Where(item => item.Status == "pending" && item.ExpiresAt <= DateTimeOffset.UtcNow)
                .ToList();
            if (expired.Count == 0) return 0;

            foreach (var reservation in expired)
            {
                RestoreReservationLocked(reservation, "expired");
            }

            Write(_reservationsPath, reservations);
            return expired.Count;
        }
    }

    private void RestoreReservationLocked(InventoryReservation reservation, string status)
    {
        var products = Read<ProductItem>(_productsPath);
        var now = DateTimeOffset.UtcNow;
        foreach (var line in reservation.Lines)
        {
            var product = products.FirstOrDefault(item => item.Id == line.ProductId);
            if (product is null) continue;
            product.Stock = (ParseDecimal(product.Stock) + line.Quantity).ToString("0.##");
            product.UpdatedAt = now;
            CreateMovementLocked(reservation.OwnerId, new InventoryMovementRequest(
                product.Id,
                product.Name,
                "release",
                line.Quantity,
                $"Hoan kho COD {reservation.OrderId} - {status}"));
        }

        reservation.Status = status;
        reservation.CancelledAt = now;
        Write(_productsPath, products);
        PublishEventLocked(reservation.OwnerId, "inventory.reservation.released", new { reservation.Id, reservation.OrderId, status });
    }

    public object GetInventorySummary(CurrentUser user)
    {
        var products = GetProducts(user);
        var totalStock = products.Sum(product => ParseDecimal(product.Stock));
        var lowStock = products.Count(product => ParseDecimal(product.Stock) <= ParseDecimal(product.MinimumStock));

        return new
        {
            totalProducts = products.Count,
            totalStock,
            lowStockProducts = lowStock,
            activeProducts = products.Count(product => product.Status != "Ngung ban")
        };
    }

    public List<ProductItem> GetLowStockProducts(CurrentUser user)
    {
        lock (_lock)
        {
            return Read<ProductItem>(_productsPath)
                .Where(product => ParseDecimal(product.Stock) < ParseDecimal(product.MinimumStock))
                .OrderBy(product => ParseDecimal(product.Stock))
                .ToList();
        }
    }

    public List<InventoryMovement> GetMovements(CurrentUser user)
    {
        lock (_lock)
        {
            var movements = Read<InventoryMovement>(_movementsPath);
            if (!CanReviewInventory(user))
            {
                movements = movements.Where(item => item.CreatedById == user.Id || item.OwnerId == user.Id).ToList();
            }

            return movements
                .OrderByDescending(item => item.CreatedAt)
                .ToList();
        }
    }

    public List<InventoryMovement> GetMovements(string ownerId)
    {
        lock (_lock)
        {
            var movements = Read<InventoryMovement>(_movementsPath);
            return movements
                .Where(item => item.OwnerId == ownerId)
                .OrderByDescending(item => item.CreatedAt)
                .ToList();
        }
    }

    public InventoryMovement? CreateMovementRequest(CurrentUser user, string productId, StockAdjustmentRequest payload)
    {
        lock (_lock)
        {
            var product = Read<ProductItem>(_productsPath)
                .FirstOrDefault(item => item.Id == productId);
            if (product is null) return null;

            var movement = CreateMovementLocked(
                product.OwnerId,
                new InventoryMovementRequest(
                    product.Id,
                    product.Name,
                    NormalizeMovementType(payload.Type),
                    Math.Max(0, payload.Quantity),
                    payload.Note),
                user,
                "pending");

            return movement;
        }
    }

    public InventoryMovement? CreateMovementRequest(CurrentUser user, InventoryMovementRequest payload)
    {
        lock (_lock)
        {
            var product = Read<ProductItem>(_productsPath)
                .FirstOrDefault(item => item.Id == payload.ProductId);
            if (product is null) return null;

            return CreateMovementLocked(
                product.OwnerId,
                payload with
                {
                    ProductName = product.Name,
                    Type = NormalizeMovementType(payload.Type),
                    Quantity = Math.Max(0, payload.Quantity)
                },
                user,
                "pending");
        }
    }

    public InventoryMovement? UpdateMovementRequest(CurrentUser user, string id, InventoryMovementRequest payload)
    {
        lock (_lock)
        {
            var movements = Read<InventoryMovement>(_movementsPath);
            var movement = CanReviewInventory(user)
                ? movements.FirstOrDefault(item => item.Id == id)
                : movements.FirstOrDefault(item =>
                    (item.CreatedById == user.Id || item.OwnerId == user.Id) && item.Id == id);
            if (movement is null || !movement.Status.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var product = Read<ProductItem>(_productsPath)
                .FirstOrDefault(item => item.Id == payload.ProductId);
            if (product is null) return null;

            movement.ProductId = product.Id;
            movement.ProductName = product.Name;
            movement.Type = NormalizeMovementType(payload.Type);
            movement.Quantity = Math.Max(0, payload.Quantity);
            movement.Note = payload.Note?.Trim() ?? "";
            Write(_movementsPath, movements);
            return movement;
        }
    }

    public InventoryMovement? ApproveMovement(CurrentUser user, string id)
    {
        if (!CanReviewInventory(user))
        {
            return null;
        }

        lock (_lock)
        {
            var movements = Read<InventoryMovement>(_movementsPath);
            var movement = CanReviewInventory(user)
                ? movements.FirstOrDefault(item => item.Id == id)
                : movements.FirstOrDefault(item => item.OwnerId == user.Id && item.Id == id);
            if (movement is null || !movement.Status.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var products = Read<ProductItem>(_productsPath);
            var product = products.FirstOrDefault(item => item.OwnerId == movement.OwnerId && item.Id == movement.ProductId);
            if (product is null) return null;

            var currentStock = ParseDecimal(product.Stock);
            var nextStock = ApplyMovementDelta(currentStock, movement.Type, movement.Quantity);
            if (nextStock < 0) return null;

            product.Stock = nextStock.ToString("0.##");
            product.UpdatedAt = DateTimeOffset.UtcNow;

            movement.Status = "approved";
            movement.ReviewedById = user.Id;
            movement.ReviewedByEmail = user.Email;
            movement.ReviewedByRole = user.Role;
            movement.ReviewedAt = DateTimeOffset.UtcNow;
            movement.ReviewNote = "Da duyet";

            Write(_productsPath, products);
            Write(_movementsPath, movements);
            PublishEventLocked(movement.OwnerId, "stock.updated", new { product.Id, product.Name, stock = product.Stock });
            return movement;
        }
    }

    public InventoryMovement? CancelMovement(CurrentUser user, string id)
    {
        if (!CanReviewInventory(user))
        {
            return null;
        }

        lock (_lock)
        {
            var movements = Read<InventoryMovement>(_movementsPath);
            var movement = CanReviewInventory(user)
                ? movements.FirstOrDefault(item => item.Id == id)
                : movements.FirstOrDefault(item => item.OwnerId == user.Id && item.Id == id);
            if (movement is null || !movement.Status.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            movement.Status = "cancelled";
            movement.ReviewedById = user.Id;
            movement.ReviewedByEmail = user.Email;
            movement.ReviewedByRole = user.Role;
            movement.ReviewedAt = DateTimeOffset.UtcNow;
            movement.ReviewNote = "Da huy";
            Write(_movementsPath, movements);
            return movement;
        }
    }

    public InventoryMovement? CreateMovement(string ownerId, InventoryMovementRequest payload)
    {
        lock (_lock)
        {
            var product = Read<ProductItem>(_productsPath)
                .FirstOrDefault(item => item.OwnerId == ownerId && item.Id == payload.ProductId);
            if (product is null) return null;

            return CreateMovementLocked(ownerId, payload with { ProductName = product.Name });
        }
    }

    public List<Supplier> GetSuppliers(string ownerId)
    {
        lock (_lock)
        {
            return Read<Supplier>(_suppliersPath)
                .Where(item => item.OwnerId == ownerId)
                .OrderByDescending(item => item.CreatedAt)
                .ToList();
        }
    }

    public List<Supplier> GetSuppliers(CurrentUser user)
    {
        lock (_lock)
        {
            return Read<Supplier>(_suppliersPath)
                .OrderByDescending(item => item.CreatedAt)
                .ToList();
        }
    }

    public Supplier CreateSupplier(string ownerId, SupplierRequest payload)
    {
        lock (_lock)
        {
            var suppliers = Read<Supplier>(_suppliersPath);
            var supplier = new Supplier
            {
                Id = Guid.NewGuid().ToString("N"),
                OwnerId = ownerId,
                Name = payload.Name?.Trim() ?? "",
                Phone = payload.Phone?.Trim() ?? "",
                Email = payload.Email?.Trim() ?? "",
                Address = payload.Address?.Trim() ?? "",
                Note = payload.Note?.Trim() ?? "",
                CreatedAt = DateTimeOffset.UtcNow
            };

            suppliers.Add(supplier);
            Write(_suppliersPath, suppliers);
            return supplier;
        }
    }

    public Supplier? UpdateSupplier(string ownerId, string id, SupplierRequest payload)
    {
        lock (_lock)
        {
            var suppliers = Read<Supplier>(_suppliersPath);
            var supplier = suppliers.FirstOrDefault(item => item.OwnerId == ownerId && item.Id == id);
            if (supplier is null) return null;

            supplier.Name = payload.Name?.Trim() ?? supplier.Name;
            supplier.Phone = payload.Phone?.Trim() ?? supplier.Phone;
            supplier.Email = payload.Email?.Trim() ?? supplier.Email;
            supplier.Address = payload.Address?.Trim() ?? supplier.Address;
            supplier.Note = payload.Note?.Trim() ?? supplier.Note;
            supplier.UpdatedAt = DateTimeOffset.UtcNow;
            Write(_suppliersPath, suppliers);
            return supplier;
        }
    }

    public bool DeleteSupplier(string ownerId, string id)
    {
        lock (_lock)
        {
            var suppliers = Read<Supplier>(_suppliersPath);
            var removed = suppliers.RemoveAll(item => item.OwnerId == ownerId && item.Id == id) > 0;
            if (removed)
            {
                Write(_suppliersPath, suppliers);
            }

            return removed;
        }
    }

    private InventoryMovement CreateMovementLocked(string ownerId, InventoryMovementRequest payload)
        => CreateMovementLocked(ownerId, payload, null, "approved", null, null, null);

    private InventoryMovement CreateMovementLocked(
        string ownerId,
        InventoryMovementRequest payload,
        CurrentUser? createdBy = null,
        string status = "approved",
        CurrentUser? reviewedBy = null,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? reviewedAt = null)
    {
        var movements = Read<InventoryMovement>(_movementsPath);
        var now = createdAt ?? DateTimeOffset.UtcNow;
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "approved" : status.Trim().ToLowerInvariant();
        var movement = new InventoryMovement
        {
            Id = Guid.NewGuid().ToString("N"),
            OwnerId = ownerId,
            ProductId = payload.ProductId,
            ProductName = payload.ProductName?.Trim() ?? "",
            Type = NormalizeMovementType(payload.Type),
            Quantity = payload.Quantity,
            Note = payload.Note?.Trim() ?? "",
            Status = normalizedStatus,
            CreatedById = createdBy?.Id ?? "system",
            CreatedByEmail = createdBy?.Email ?? "system",
            CreatedByRole = createdBy?.Role ?? "system",
            CreatedAt = now,
            ReviewedById = reviewedBy?.Id,
            ReviewedByEmail = reviewedBy?.Email,
            ReviewedByRole = reviewedBy?.Role,
            ReviewedAt = reviewedAt
        };

        movements.Add(movement);
        Write(_movementsPath, movements);
        return movement;
    }

    private static bool EnsureProductSkus(List<ProductItem> products, IReadOnlyCollection<Category> categories)
    {
        var changed = false;
        foreach (var product in products.OrderBy(product => product.CreatedAt).ThenBy(product => product.Id))
        {
            if (!string.IsNullOrWhiteSpace(product.Sku))
            {
                continue;
            }

            product.Sku = GenerateProductSku(product, products, categories);
            changed = true;
        }

        return changed;
    }

    private static string GenerateProductSku(ProductItem product, IReadOnlyCollection<ProductItem> products, IReadOnlyCollection<Category> categories)
    {
        var categoryName = categories.FirstOrDefault(category => category.Id == product.CategoryId)?.Name ?? "";
        var prefix = ResolveCategoryPrefix(categoryName);
        var nextNumber = products
            .Where(item => item.Id != product.Id)
            .Select(item => ExtractSkuNumber(item.Sku, prefix))
            .Where(number => number > 0)
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{nextNumber:0000}";
    }

    private static int ExtractSkuNumber(string? sku, string prefix)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return 0;
        }

        var normalizedSku = sku.Trim().ToUpperInvariant();
        var normalizedPrefix = prefix.Trim().ToUpperInvariant();
        if (!normalizedSku.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return int.TryParse(normalizedSku[normalizedPrefix.Length..], out var number) ? number : 0;
    }

    private static string ResolveCategoryPrefix(string? categoryName)
    {
        var normalized = NormalizeLookupText(categoryName);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "SP";
        }

        if (normalized.Contains("nuoc uong") || normalized.Contains("nuoc"))
        {
            return "NU";
        }

        if (normalized.Contains("hoa qua") || normalized.Contains("hoaqua"))
        {
            return "HQ";
        }

        var parts = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => part.Length > 0)
            .Select(part => char.ToUpperInvariant(part[0]))
            .Take(2)
            .ToArray();

        if (parts.Length == 2)
        {
            return new string(parts);
        }

        if (parts.Length == 1)
        {
            return $"{parts[0]}X";
        }

        var fallback = new string(normalized.Where(char.IsLetterOrDigit).Take(2).Select(char.ToUpperInvariant).ToArray());
        return fallback.Length == 2 ? fallback : "SP";
    }

    private static string NormalizeLookupText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    // Categories
    public List<Category> GetCategories(string ownerId)
    {
        lock (_lock)
        {
            return Read<Category>(_categoriesPath)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
        }
    }

    public List<Category> GetCategories(CurrentUser user)
    {
        lock (_lock)
        {
            return Read<Category>(_categoriesPath)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
        }
    }

    public List<Category> GetPublicCategories()
    {
        lock (_lock)
        {
            return Read<Category>(_categoriesPath)
                .OrderByDescending(c => c.CreatedAt)
                .ToList();
        }
    }

    public Category CreateCategory(string ownerId, CategoryRequest payload)
    {
        lock (_lock)
        {
            var cats = Read<Category>(_categoriesPath);
            var cat = new Category
            {
                Id = Guid.NewGuid().ToString("N"),
                OwnerId = ownerId,
                Name = payload.Name?.Trim() ?? "",
                ParentId = payload.ParentId?.Trim() ?? "",
                CreatedAt = DateTimeOffset.UtcNow
            };

            cats.Add(cat);
            Write(_categoriesPath, cats);
            return cat;
        }
    }

    public Category? UpdateCategory(string ownerId, string id, CategoryRequest payload)
    {
        lock (_lock)
        {
            var cats = Read<Category>(_categoriesPath);
            var cat = cats.FirstOrDefault(c => c.Id == id);
            if (cat is null) return null;
            cat.Name = payload.Name?.Trim() ?? cat.Name;
            cat.ParentId = payload.ParentId?.Trim() ?? cat.ParentId;
            Write(_categoriesPath, cats);
            return cat;
        }
    }

    public bool DeleteCategory(string ownerId, string id)
    {
        lock (_lock)
        {
            var cats = Read<Category>(_categoriesPath);
            var removed = cats.RemoveAll(c => c.Id == id) > 0;
            if (removed) Write(_categoriesPath, cats);
            return removed;
        }
    }

    // Receipts
    public List<InventoryReceipt> GetReceipts(CurrentUser user)
    {
        lock (_lock)
        {
            var receipts = Read<InventoryReceipt>(_receiptsPath);
            if (!CanReviewInventory(user))
            {
                receipts = receipts.Where(r => r.CreatedById == user.Id || r.OwnerId == user.Id).ToList();
            }

            return receipts
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }
    }

    public List<InventoryReceipt> GetReceipts(string ownerId)
    {
        lock (_lock)
        {
            var receipts = Read<InventoryReceipt>(_receiptsPath);
            return receipts
                .Where(r => r.OwnerId == ownerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
        }
    }

    public InventoryReceipt CreateReceipt(CurrentUser user, InventoryReceiptRequest payload)
    {
        lock (_lock)
    {
        var receipts = Read<InventoryReceipt>(_receiptsPath);
        var products = Read<ProductItem>(_productsPath);
        var normalizedLines = NormalizeReceiptLines(payload.Lines);
        var receiptOwnerId = normalizedLines
                .Select(line => products.FirstOrDefault(product => product.Id == line.ProductId)?.OwnerId)
                .FirstOrDefault(owner => !string.IsNullOrWhiteSpace(owner))
                ?? user.Id;
        var receipt = new InventoryReceipt
        {
            Id = Guid.NewGuid().ToString("N"),
            OwnerId = receiptOwnerId,
            SupplierId = payload.SupplierId?.Trim() ?? "",
            Lines = normalizedLines,
                Status = "pending",
                CreatedById = user.Id,
                CreatedByEmail = user.Email,
                CreatedByRole = user.Role,
                CreatedAt = DateTimeOffset.UtcNow,
                ReviewNote = payload.Note?.Trim() ?? ""
            };

            receipts.Add(receipt);
            Write(_receiptsPath, receipts);
            return receipt;
        }
    }

    public InventoryReceipt? UpdateReceipt(CurrentUser user, string id, InventoryReceiptRequest payload)
    {
        lock (_lock)
        {
            var receipts = Read<InventoryReceipt>(_receiptsPath);
            var receipt = CanReviewInventory(user)
                ? receipts.FirstOrDefault(r => r.Id == id)
                : receipts.FirstOrDefault(r => (r.CreatedById == user.Id || r.OwnerId == user.Id) && r.Id == id);
            if (receipt is null || !receipt.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)) return null;

            receipt.SupplierId = payload.SupplierId?.Trim() ?? receipt.SupplierId;
            receipt.Lines = NormalizeReceiptLines(payload.Lines);
            receipt.ReviewNote = payload.Note?.Trim() ?? receipt.ReviewNote;
            Write(_receiptsPath, receipts);
            return receipt;
        }
    }

    public InventoryReceipt? ApproveReceipt(CurrentUser user, string id)
    {
        if (!CanReviewInventory(user))
        {
            return null;
        }

        lock (_lock)
        {
            var receipts = Read<InventoryReceipt>(_receiptsPath);
            var receipt = CanReviewInventory(user)
                ? receipts.FirstOrDefault(r => r.Id == id)
                : receipts.FirstOrDefault(r => r.OwnerId == user.Id && r.Id == id);
            if (receipt is null || !receipt.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)) return null;

            var products = Read<ProductItem>(_productsPath);
            var resolvedOwnerId = receipt.OwnerId;

            foreach (var line in receipt.Lines)
            {
                var product = products.FirstOrDefault(p => p.OwnerId == receipt.OwnerId && p.Id == line.ProductId)
                    ?? products.FirstOrDefault(p => p.Id == line.ProductId);
                if (product is null) return null;

                resolvedOwnerId = string.IsNullOrWhiteSpace(resolvedOwnerId) ? product.OwnerId : resolvedOwnerId;
                var current = ParseDecimal(product.Stock);
                var next = Math.Max(0, current + line.Quantity);
                product.Stock = next.ToString("0.##");
                product.UpdatedAt = DateTimeOffset.UtcNow;

                CreateMovementLocked(
                    product.OwnerId,
                    new InventoryMovementRequest(product.Id, product.Name, "in", line.Quantity, line.Note ?? receipt.ReviewNote),
                    new CurrentUser(receipt.CreatedById, receipt.CreatedByEmail, receipt.CreatedByRole),
                    "approved",
                    user,
                    receipt.CreatedAt,
                    DateTimeOffset.UtcNow);
                PublishEventLocked(product.OwnerId, "stock.updated", new { product.Id, product.Name, stock = product.Stock });
            }

            Write(_productsPath, products);

            receipt.OwnerId = resolvedOwnerId;
            receipt.Status = "approved";
            receipt.ReviewedById = user.Id;
            receipt.ReviewedByEmail = user.Email;
            receipt.ReviewedByRole = user.Role;
            receipt.ReviewedAt = DateTimeOffset.UtcNow;
            receipt.ConfirmedAt = receipt.ReviewedAt;
            Write(_receiptsPath, receipts);
            return receipt;
        }
    }

    public InventoryReceipt? CancelReceipt(CurrentUser user, string id)
    {
        if (!CanReviewInventory(user))
        {
            return null;
        }

        lock (_lock)
        {
            var receipts = Read<InventoryReceipt>(_receiptsPath);
            var receipt = CanReviewInventory(user)
                ? receipts.FirstOrDefault(r => r.Id == id)
                : receipts.FirstOrDefault(r => r.OwnerId == user.Id && r.Id == id);
            if (receipt is null || !receipt.Status.Equals("pending", StringComparison.OrdinalIgnoreCase)) return null;

            receipt.Status = "cancelled";
            receipt.ReviewedById = user.Id;
            receipt.ReviewedByEmail = user.Email;
            receipt.ReviewedByRole = user.Role;
            receipt.ReviewedAt = DateTimeOffset.UtcNow;
            receipt.ReviewNote = "Da huy";
            Write(_receiptsPath, receipts);
            return receipt;
        }
    }

    public InventoryReceipt? ConfirmReceipt(string ownerId, string id)
    {
        lock (_lock)
        {
            var receipts = Read<InventoryReceipt>(_receiptsPath);
            var receipt = receipts.FirstOrDefault(r => r.OwnerId == ownerId && r.Id == id);
            if (receipt is null) return null;
            if (receipt.Status == "confirmed" || receipt.Status == "approved") return receipt;

            return null;
        }
    }

    private void PublishEventLocked(string ownerId, string type, object payload)
    {
        var events = Read<EventRecord>(_eventsPath);
        var ev = new EventRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            OwnerId = ownerId,
            Type = type,
            Payload = payload,
            CreatedAt = DateTimeOffset.UtcNow
        };

        events.Add(ev);
        Write(_eventsPath, events);
    }

    public List<EventRecord> GetEvents(string ownerId, string? type = null)
    {
        lock (_lock)
        {
            return Read<EventRecord>(_eventsPath)
                .Where(item => item.OwnerId == ownerId)
                .Where(item => string.IsNullOrWhiteSpace(type) || item.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.CreatedAt)
                .ToList();
        }
    }

    private void EnsureFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            Write(path, new List<T>());
        }
    }

    private List<T> Read<T>(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path)) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void Write<T>(string path, List<T> items)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(items, _jsonOptions));
    }

    private static string NormalizeNumber(string? value)
    {
        return Math.Max(0, ParseDecimal(value)).ToString("0.##");
    }

    private static decimal ParseDecimal(string? value)
    {
        if (decimal.TryParse(value, out var number))
        {
            return number;
        }

        var cleaned = new string((value ?? "").Where(character => char.IsDigit(character) || character == '.' || character == '-').ToArray());
        return decimal.TryParse(cleaned, out number) ? number : 0;
    }

    private static string NormalizeMovementType(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "out" or "export" or "sale" => "out",
            "in" or "import" or "receipt" => "in",
            "set" or "adjust" or "adjustment" => "set",
            "reserve" or "release" => normalized ?? "in",
            _ => "in"
        };
    }

    private static decimal ApplyMovementDelta(decimal currentStock, string? movementType, decimal quantity)
    {
        return NormalizeMovementType(movementType) switch
        {
            "out" => currentStock - quantity,
            "set" => quantity,
            _ => currentStock + quantity
        };
    }

    private static List<ReceiptLine> NormalizeReceiptLines(List<ReceiptLine>? lines)
    {
        return (lines ?? [])
            .Where(line => !string.IsNullOrWhiteSpace(line.ProductId) && line.Quantity > 0)
            .GroupBy(line => line.ProductId)
            .Select(group => new ReceiptLine(
                group.Key,
                group.Sum(line => line.Quantity),
                group.Select(line => line.Note).FirstOrDefault(note => !string.IsNullOrWhiteSpace(note))))
            .ToList();
    }

    private static bool CanReviewInventory(CurrentUser user)
    {
        return user.Role.Equals("admin-user", StringComparison.OrdinalIgnoreCase) ||
               user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
               user.Role.Equals("admin", StringComparison.OrdinalIgnoreCase);
    }

}

public sealed class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public CurrentUser? ValidateToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var expectedSignature = Sign($"{parts[0]}.{parts[1]}");
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedSignature),
                Encoding.UTF8.GetBytes(parts[2])))
        {
            return null;
        }

        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var payload = JsonDocument.Parse(payloadJson);
        var root = payload.RootElement;

        if (!root.TryGetProperty("exp", out var expElement) ||
            DateTimeOffset.FromUnixTimeSeconds(expElement.GetInt64()) <= DateTimeOffset.UtcNow ||
            !root.TryGetProperty("sub", out var subElement))
        {
            return null;
        }

        var id = subElement.GetString();
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var email = root.TryGetProperty("email", out var emailElement) ? emailElement.GetString() ?? "" : "";
        var role = root.TryGetProperty("role", out var roleElement) ? roleElement.GetString() ?? "user" : "user";
        return new CurrentUser(id, email, role);
    }

    private string Sign(string data)
    {
        var secret = _configuration["Jwt:Secret"] ?? "CHANGE_THIS_SECRET_ON_MACHINE_3_MINIMUM_32_CHARS";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var output = input.Replace('-', '+').Replace('_', '/');
        output = output.PadRight(output.Length + (4 - output.Length % 4) % 4, '=');
        return Convert.FromBase64String(output);
    }
}

public sealed record CurrentUser(string Id, string Email, string Role);

public sealed record ProductRequest(
    string? Name,
    string? Description,
    string? Price,
    string? Cost,
    string? Image,
    string? Sku,
    string? Stock,
    string? Status,
    string? CategoryId,
    string? MinimumStock);

public sealed record StockAdjustmentRequest(string? Type, decimal Quantity, string? Note);
public sealed record SellStockRequest(decimal Quantity, string? Note);
public sealed record OrderProductBatchRequest(List<int> ProductIds);
public sealed record OrderInventoryRequest(List<OrderInventoryItem> Items);
public sealed record OrderInventoryItem(int ProductId, int Quantity, string? ExternalProductId = null);
public sealed record OrderInventoryShortage(int ProductId, int RequestedQuantity, int AvailableQuantity);
public sealed record OrderInventoryResult(bool IsAvailable, List<OrderInventoryShortage> Shortages);
public sealed record OrderProductDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    string CategoryName,
    decimal SellingPrice,
    int QuantityAvailable,
    string StockStatus);

public sealed record InventoryMovementRequest(
    string ProductId,
    string? ProductName,
    string? Type,
    decimal Quantity,
    string? Note);

public sealed record SupplierRequest(string? Name, string? Phone, string? Email, string? Address, string? Note);

public sealed record CategoryRequest(string? Name, string? ParentId);

public sealed class Category
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string ParentId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed record ReceiptLine(string ProductId, decimal Quantity, string? Note);

public sealed record InventoryReceiptRequest(string SupplierId, List<ReceiptLine> Lines, string? Note = null);

public sealed record ReservationLine(string ProductId, decimal Quantity);

public sealed record InventoryReservationRequest(
    string? OrderId,
    string? CustomerName,
    List<ReservationLine> Lines);

public sealed record ReservationResult(InventoryReservation? Reservation, string Message);

public sealed class InventoryReservation
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string OrderId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public List<ReservationLine> Lines { get; set; } = new();
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

public sealed class InventoryReceipt
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string SupplierId { get; set; } = "";
    public List<ReceiptLine> Lines { get; set; } = new();
    public string Status { get; set; } = "approved";
    public string CreatedById { get; set; } = "";
    public string CreatedByEmail { get; set; } = "";
    public string CreatedByRole { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string? ReviewedById { get; set; }
    public string? ReviewedByEmail { get; set; }
    public string? ReviewedByRole { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}

public sealed class EventRecord
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Type { get; set; } = "";
    public object? Payload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ProductItem
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Price { get; set; } = "";
    public string Cost { get; set; } = "";
    public string Image { get; set; } = "";
    public string Sku { get; set; } = "";
    public string Stock { get; set; } = "0";
    public string CategoryId { get; set; } = "";
    public string MinimumStock { get; set; } = "0";
    public string Status { get; set; } = "Dang ban";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class InventoryMovement
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string Type { get; set; } = "in";
    public decimal Quantity { get; set; }
    public string Note { get; set; } = "";
    public string Status { get; set; } = "approved";
    public string CreatedById { get; set; } = "";
    public string CreatedByEmail { get; set; } = "";
    public string CreatedByRole { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string? ReviewedById { get; set; }
    public string? ReviewedByEmail { get; set; }
    public string? ReviewedByRole { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
}

public sealed class Supplier
{
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string Address { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class ReservationExpiryService(
    ProductInventoryStore store,
    ILogger<ReservationExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var count = store.ExpireReservations();
                if (count > 0)
                {
                    logger.LogInformation("Da hoan kho {Count} phieu COD het han.", count);
                }
            }
            catch (Exception error)
            {
                logger.LogError(error, "Khong the xu ly phieu COD het han.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
