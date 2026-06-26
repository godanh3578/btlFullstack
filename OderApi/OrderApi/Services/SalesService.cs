using OrderApi.Data;
using OrderApi.DTOs.Sales;
using OrderApi.DTOs.Orders;
using OrderApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Collections.Concurrent;

namespace OrderApi.Services
{
    // Shared in-memory map: N2 OrderId → N3 OrderId (GUID string)
    internal static class N3OrderTracker
    {
        private static readonly ConcurrentDictionary<int, string> _map = new();
        internal static void Register(int n2OrderId, string n3OrderId) => _map[n2OrderId] = n3OrderId;
        internal static bool TryGetAndRemove(int n2OrderId, out string n3OrderId) => _map.TryRemove(n2OrderId, out n3OrderId!);
    }

    public class SalesService : ISalesService
    {
        private readonly OrderDbContext _dbContext;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IOutboxService _outboxService;
        private readonly IProductCatalogClient _productCatalogClient;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SalesService> _logger;

        public SalesService(
            OrderDbContext dbContext,
            IOrderService orderService,
            IPaymentService paymentService,
            IOutboxService outboxService,
            IProductCatalogClient productCatalogClient,
            IHttpClientFactory httpClientFactory,
            ILogger<SalesService> logger)
        {
            _dbContext = dbContext;
            _orderService = orderService;
            _paymentService = paymentService;
            _outboxService = outboxService;
            _productCatalogClient = productCatalogClient;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<CalculateTotalResponseDto> CalculateTotalAsync(CalculateTotalDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Cần ít nhất một sản phẩm.");

            if (dto.DiscountAmount < 0)
                throw new InvalidOperationException("Chiết khấu không được nhỏ hơn 0.");

            if (dto.DiscountValue < 0)
                throw new InvalidOperationException("Gia tri chiet khau khong duoc nho hon 0.");

            decimal totalAmount = 0;

            foreach (var item in dto.Items)
            {
                var unitPrice = item.UnitPrice;
                if (unitPrice <= 0)
                {
                    var product = await _productCatalogClient.GetProductAsync(item.ProductId)
                        ?? throw new InvalidOperationException($"Product {item.ProductId} price/stock info not found");
                    unitPrice = product.SellingPrice;
                }

                if (unitPrice <= 0)
                {
                    var stock = await _dbContext.ProductStockCaches
                        .FirstOrDefaultAsync(p => p.ProductId == item.ProductId)
                        ?? throw new InvalidOperationException($"Không có giá/tồn kho cho sản phẩm {item.ProductId}.");
                    unitPrice = stock.SellingPrice;
                }

                totalAmount += item.Quantity * unitPrice;
            }

            var discountType = OrderService.NormalizeDiscountType(dto.DiscountType);
            var discountAmount = OrderService.CalculateDiscountAmount(
                totalAmount,
                discountType,
                dto.DiscountValue,
                dto.DiscountAmount);

            if (discountAmount > totalAmount)
                throw new InvalidOperationException("Chiết khấu không được lớn hơn tổng tiền.");

            return new CalculateTotalResponseDto
            {
                TotalAmount = totalAmount,
                DiscountAmount = discountAmount,
                DiscountType = discountType,
                DiscountValue = dto.DiscountValue > 0 ? dto.DiscountValue : dto.DiscountAmount,
                FinalAmount = totalAmount - discountAmount
            };
        }

        public async Task<CheckoutResponseDto> CheckoutAsync(CheckoutDto dto, string createdBy)
        {
            if (!string.IsNullOrWhiteSpace(dto.IdempotencyKey))
            {
                var existingOrder = await _dbContext.Orders.FirstOrDefaultAsync(o => o.IdempotencyKey == dto.IdempotencyKey);
                if (existingOrder != null)
                {
                    return new CheckoutResponseDto
                    {
                        OrderId = existingOrder.OrderId,
                        OrderCode = existingOrder.OrderCode,
                        TotalAmount = existingOrder.TotalAmount,
                        DiscountAmount = existingOrder.DiscountAmount,
                        DiscountType = existingOrder.DiscountType,
                        DiscountValue = existingOrder.DiscountValue,
                        FinalAmount = existingOrder.FinalAmount,
                        PaidAmount = existingOrder.PaidAmount,
                        DebtAmount = existingOrder.DebtAmount,
                        PaymentStatus = existingOrder.PaymentStatus.ToString(),
                        OrderStatus = existingOrder.OrderStatus.ToString()
                    };
                }
            }

            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Một đơn hàng phải có ít nhất một sản phẩm.");

            if (dto.DiscountAmount < 0)
                throw new InvalidOperationException("Chiết khấu không được nhỏ hơn 0.");

            if (dto.DiscountValue < 0)
                throw new InvalidOperationException("Gia tri chiet khau khong duoc nho hon 0.");

            if (dto.PaidAmount < 0)
                throw new InvalidOperationException("Số tiền thanh toán không được nhỏ hơn 0.");

            var inventoryRequests = dto.Items
                .Select(i => new ProductInventoryRequest
                {
                    ProductId = i.ProductId,
                    ExternalProductId = i.ExternalProductId,
                    Quantity = i.Quantity
                })
                .ToList();
            var inventoryCheck = await _productCatalogClient.CheckInventoryAsync(inventoryRequests);
            if (!inventoryCheck.IsAvailable)
                throw new InvalidOperationException(BuildInsufficientStockMessage(inventoryCheck));

            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            if (!_dbContext.Database.IsInMemory())
                transaction = await _dbContext.Database.BeginTransactionAsync();

            var customer = await _dbContext.Customers.FindAsync(dto.CustomerId)
                ?? throw new KeyNotFoundException($"Customer {dto.CustomerId} not found");

            var items = new List<CreateOrderDetailDto>();
            var categoryByProductId = new Dictionary<int, string?>();
            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException("Số lượng sản phẩm phải lớn hơn 0.");

                var catalogProduct = await _productCatalogClient.GetProductAsync(item.ProductId, item.ExternalProductId)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} stock info not found");

                if (catalogProduct.QuantityAvailable < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");

                var cachedStock = await _dbContext.ProductStockCaches
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);

                if (cachedStock == null)
                {
                    cachedStock = new ProductStockCache
                    {
                        ProductId = catalogProduct.ProductId
                    };
                    _dbContext.ProductStockCaches.Add(cachedStock);
                }

                cachedStock.ProductCode = string.IsNullOrWhiteSpace(catalogProduct.ProductCode)
                    ? $"SP{catalogProduct.ProductId:D3}"
                    : catalogProduct.ProductCode;
                cachedStock.ProductName = string.IsNullOrWhiteSpace(catalogProduct.ProductName)
                    ? $"Product {catalogProduct.ProductId}"
                    : catalogProduct.ProductName;
                cachedStock.CategoryName = catalogProduct.CategoryName;
                cachedStock.SellingPrice = catalogProduct.SellingPrice;
                cachedStock.QuantityAvailable = catalogProduct.QuantityAvailable;
                cachedStock.IsDeleted = false;

                if (cachedStock.QuantityAvailable < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");

                if (cachedStock.QuantityAvailable <= 0)
                    cachedStock.StockStatus = StockStatus.OutOfStock;
                else if (cachedStock.QuantityAvailable <= 5)
                {
                    cachedStock.StockStatus = StockStatus.LowStock;
                }
                else
                {
                    cachedStock.StockStatus = StockStatus.InStock;
                }

                cachedStock.LastUpdatedAt = DateTime.UtcNow;
                categoryByProductId[item.ProductId] = cachedStock.CategoryName;

                items.Add(new CreateOrderDetailDto
                {
                    ProductId = item.ProductId,
                    ProductCode = cachedStock.ProductCode,
                    ProductName = cachedStock.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = cachedStock.SellingPrice,
                    DiscountAmount = 0
                });
            }

            var createOrderDto = new CreateOrderDto
            {
                IdempotencyKey = dto.IdempotencyKey,
                CustomerId = dto.CustomerId,
                CustomerName = customer.FullName,
                CustomerPhone = customer.Phone,
                CustomerAddress = customer.Address,
                Items = items,
                DiscountAmount = dto.DiscountAmount,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                PaymentMethod = dto.PaymentMethod,
                CreatedByUserId = ResolveCreatedByUserId(createdBy)
            };

            // Create without duplicate outbox — we'll enqueue after payment/debt updates
            var orderEntity = await CreateOrderWithoutOutboxAsync(createOrderDto);

            var deducted = await _productCatalogClient.DeductInventoryAsync(inventoryRequests);
            if (!deducted)
            {
                orderEntity.OrderStatus = OrderStatus.Cancelled;
                orderEntity.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                if (transaction != null)
                    await transaction.CommitAsync();
                if (transaction != null)
                    await transaction.DisposeAsync();

                return new CheckoutResponseDto
                {
                    OrderId = orderEntity.OrderId,
                    OrderCode = orderEntity.OrderCode,
                    TotalAmount = orderEntity.TotalAmount,
                    DiscountAmount = orderEntity.DiscountAmount,
                    DiscountType = orderEntity.DiscountType,
                    DiscountValue = orderEntity.DiscountValue,
                    FinalAmount = orderEntity.FinalAmount,
                    PaidAmount = orderEntity.PaidAmount,
                    DebtAmount = orderEntity.DebtAmount,
                    PaymentStatus = orderEntity.PaymentStatus.ToString(),
                    OrderStatus = orderEntity.OrderStatus.ToString()
                };
            }

            var isWalletPayment = Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var parsedPaymentMethod)
                && parsedPaymentMethod == PaymentMethod.Wallet;

            if (dto.PaidAmount > 0)
            {
                if (dto.PaidAmount > orderEntity.FinalAmount - orderEntity.PaidAmount)
                    throw new InvalidOperationException("Số tiền thanh toán không được vượt quá số tiền còn lại.");

                if (isWalletPayment)
                {
                    if (customer.WalletBalance < dto.PaidAmount)
                        throw new InvalidOperationException("Số dư ví không đủ để thanh toán.");

                    customer.WalletBalance -= dto.PaidAmount;
                }

                await _paymentService.RecordPaymentAsync(orderEntity.OrderId, new DTOs.Payments.CreatePaymentDto
                {
                    OrderId = orderEntity.OrderId,
                    PaymentMethod = dto.PaymentMethod,
                    Amount = dto.PaidAmount,
                    Note = "Payment at checkout"
                });

                orderEntity = await _dbContext.Orders.FindAsync(orderEntity.OrderId)
                    ?? orderEntity;
            }

            if (orderEntity.PaidAmount < orderEntity.FinalAmount)
            {
                var debtAmount = orderEntity.FinalAmount - orderEntity.PaidAmount;
                var debt = new Debt
                {
                    CustomerId = dto.CustomerId,
                    OrderId = orderEntity.OrderId,
                    DebtAmount = debtAmount,
                    DebtStatus = orderEntity.PaidAmount > 0 ? DebtStatus.Partial : DebtStatus.Unpaid,
                    DueDate = DateTime.UtcNow.AddDays(30)
                };
                _dbContext.Debts.Add(debt);

                orderEntity.DebtAmount = debtAmount;
                orderEntity.OrderStatus = OrderStatus.Pending;
                orderEntity.PaymentStatus = orderEntity.PaidAmount > 0 ? PaymentStatus.Partial : PaymentStatus.Unpaid;

                customer.CurrentDebt += debtAmount;
            }
            else
            {
                orderEntity.OrderStatus = OrderStatus.Completed;
                orderEntity.PaymentStatus = PaymentStatus.Paid;
                orderEntity.DebtAmount = 0;
            }

            customer.TotalSpent += orderEntity.FinalAmount;
            customer.UpdatedAt = DateTime.UtcNow;
            orderEntity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await _outboxService.EnqueueOrderCreatedAsync(orderEntity.OrderId);

            if (transaction != null)
                await transaction.CommitAsync();
            if (transaction != null)
                await transaction.DisposeAsync();

            // Fire-and-forget: push order to N3 (UserReport). Do not fail checkout if N3 is down.
            var n3CustomerId = dto.CustomerId.ToString();
            var n3CustomerName = customer.FullName;
            var n3Email = customer.Email;
            var n3Phone = customer.Phone;
            var n3Address = customer.Address;
            var n3Discount = orderEntity.DiscountAmount;
            var n3PaymentMethod = dto.PaymentMethod;
            var n3PaidAmount = orderEntity.PaidAmount;
            var n3Items = orderEntity.Items.Select(i => new
            {
                ProductId = i.ProductId.ToString(),
                ProductName = i.ProductName,
                CategoryName = categoryByProductId.TryGetValue(i.ProductId, out var cat) ? cat : null,
                Quantity = (decimal)i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();
            var n2OrderId = orderEntity.OrderId;
            var httpFactory = _httpClientFactory;
            _ = Task.Run(async () =>
            {
                try
                {
                    var http = httpFactory.CreateClient("UserReport");
                    var payload = new
                    {
                        CustomerId = n3CustomerId,
                        CustomerName = n3CustomerName,
                        Email = n3Email,
                        Phone = n3Phone,
                        Address = n3Address,
                        DiscountAmount = n3Discount,
                        PaymentMethod = n3PaymentMethod,
                        PaidAmount = n3PaidAmount,
                        Items = n3Items
                    };
                    var res = await http.PostAsJsonAsync("/api/Orders", payload);
                    if (res.IsSuccessStatusCode)
                    {
                        var body = await res.Content.ReadFromJsonAsync<N3OrderCreatedDto>();
                        if (!string.IsNullOrEmpty(body?.OrderId))
                            N3OrderTracker.Register(n2OrderId, body.OrderId);
                    }
                }
                catch { /* N3 not available, ignore */ }
            });

            _logger.LogInformation("Checkout completed: {OrderCode}", orderEntity.OrderCode);

            return new CheckoutResponseDto
            {
                OrderId = orderEntity.OrderId,
                OrderCode = orderEntity.OrderCode,
                TotalAmount = orderEntity.TotalAmount,
                DiscountAmount = orderEntity.DiscountAmount,
                DiscountType = orderEntity.DiscountType,
                DiscountValue = orderEntity.DiscountValue,
                FinalAmount = orderEntity.FinalAmount,
                PaidAmount = orderEntity.PaidAmount,
                DebtAmount = orderEntity.DebtAmount,
                PaymentStatus = orderEntity.PaymentStatus.ToString(),
                OrderStatus = orderEntity.OrderStatus.ToString()
            };
        }

        private async Task<Order> CreateOrderWithoutOutboxAsync(CreateOrderDto dto)
        {
            var orderCode = $"ORD{(await _dbContext.Orders.IgnoreQueryFilters().CountAsync() + 1):D6}";
            var order = new Order
            {
                OrderCode = orderCode,
                IdempotencyKey = dto.IdempotencyKey,
                CustomerId = dto.CustomerId,
                CreatedByUserId = dto.CreatedByUserId,
                OrderDate = DateTime.UtcNow,
                DiscountType = OrderService.NormalizeDiscountType(dto.DiscountType),
                DiscountValue = dto.DiscountValue,
                OrderStatus = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Unpaid,
                PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "Cash" : dto.PaymentMethod,
            };

            decimal totalAmount = 0;
            foreach (var itemDto in dto.Items)
            {
                var detail = new OrderDetail
                {
                    ProductId = itemDto.ProductId,
                    ProductCode = itemDto.ProductCode,
                    ProductName = itemDto.ProductName,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    DiscountAmount = itemDto.DiscountAmount,
                    SubTotal = (itemDto.Quantity * itemDto.UnitPrice) - itemDto.DiscountAmount
                };
                order.Items.Add(detail);
                totalAmount += detail.SubTotal;
            }

            var discountAmount = OrderService.CalculateDiscountAmount(
                totalAmount,
                order.DiscountType,
                dto.DiscountValue,
                dto.DiscountAmount);

            if (discountAmount > totalAmount)
                throw new InvalidOperationException("Chiet khau khong duoc lon hon tong tien.");

            order.TotalAmount = totalAmount;
            order.DiscountValue = dto.DiscountValue > 0 ? dto.DiscountValue : dto.DiscountAmount;
            order.DiscountAmount = discountAmount;
            order.FinalAmount = totalAmount - discountAmount;

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();
            return order;
        }

        private static int ResolveCreatedByUserId(string? createdBy)
        {
            return int.TryParse(createdBy, out var userId) && userId > 0 ? userId : 1;
        }

        private static string BuildInsufficientStockMessage(InventoryCheckResult check)
        {
            if (check.Shortages.Count == 0)
                return "InsufficientStock";

            var details = string.Join(", ", check.Shortages.Select(s =>
                $"ProductId={s.ProductId}, requested={s.RequestedQuantity}, available={s.AvailableQuantity}"));
            return $"InsufficientStock: {details}";
        }
    }

    internal record N3OrderCreatedDto(string? OrderId);
}
