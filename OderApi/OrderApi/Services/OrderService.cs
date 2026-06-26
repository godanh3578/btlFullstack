using OrderApi.Data;
using OrderApi.DTOs.Orders;
using OrderApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;

namespace OrderApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderDbContext _dbContext;
        private readonly IOutboxService _outboxService;
        private readonly ILogger<OrderService> _logger;
        private readonly IProductCatalogClient? _productCatalogClient;
        private readonly IHttpClientFactory? _httpClientFactory;

        public OrderService(
            OrderDbContext dbContext,
            IOutboxService outboxService,
            ILogger<OrderService> logger,
            IProductCatalogClient? productCatalogClient = null,
            IHttpClientFactory? httpClientFactory = null)
        {
            _dbContext = dbContext;
            _outboxService = outboxService;
            _logger = logger;
            _productCatalogClient = productCatalogClient;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int orderId)
        {
            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            return order == null ? null : MapToDto(order);
        }

        public async Task<OrderDto?> GetOrderByCodeAsync(string orderCode)
        {
            var upper = orderCode.Trim().ToUpperInvariant();
            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderCode.ToUpper() == upper);

            return order == null ? null : MapToDto(order);
        }

        public async Task<OrderDto?> LookupOrderAsync(string orderCode, string phone)
        {
            var normalizedCode = orderCode.Trim().ToUpperInvariant();
            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderCode.ToUpper() == normalizedCode);

            if (order?.Customer == null || order.Customer.Phone != phone.Trim())
                return null;

            return MapToDto(order);
        }

        public async Task<List<OrderDto>> LookupByPhoneAsync(string phone)
        {
            var normalizedPhone = phone.Trim();
            var orders = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .Where(o => o.Customer != null && o.Customer.Phone == normalizedPhone)
                .OrderByDescending(o => o.OrderDate)
                .Take(50)
                .ToListAsync();

            return orders.Select(MapToDto).ToList();
        }

        public async Task<List<OrderDto>> GetAllOrdersAsync(
            string? search = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            return await QueryOrdersAsync(search, null, status, fromDate, toDate);
        }

        public async Task<List<OrderDto>> GetOrdersByCustomerIdAsync(
            int customerId,
            string? search = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            return await QueryOrdersAsync(search, customerId, status, fromDate, toDate);
        }

        private async Task<List<OrderDto>> QueryOrdersAsync(
            string? search,
            int? customerId,
            string? status,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var query = _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .AsQueryable();

            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<OrderStatus>(status.Trim(), true, out var parsedStatus))
            {
                query = query.Where(o => o.OrderStatus == parsedStatus);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(o => o.OrderDate >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(o => o.OrderDate < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(o =>
                    o.OrderCode.Contains(term) ||
                    (o.Customer != null && o.Customer.FullName.Contains(term)) ||
                    (o.Customer != null && o.Customer.Phone.Contains(term)) ||
                    o.OrderDate.ToString().Contains(term));
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Một đơn hàng phải có ít nhất một sản phẩm.");

            // Xử lý khách lẻ (walk-in từ KhoPro) — CustomerId = 0 hoặc không truyền
            if (dto.CustomerId <= 0)
            {
                var guestPhone = string.IsNullOrWhiteSpace(dto.CustomerPhone) ? "0000000000" : dto.CustomerPhone.Trim();
                var guestName  = string.IsNullOrWhiteSpace(dto.CustomerName)  ? "Khách lẻ"  : dto.CustomerName.Trim();
                var guestAddress = string.IsNullOrWhiteSpace(dto.CustomerAddress) ? "" : dto.CustomerAddress.Trim();
                var guest = await _dbContext.Customers
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Phone == guestPhone);
                if (guest == null)
                {
                    guest = new Models.Customers
                    {
                        CustomerCode = $"KL{guestPhone.Substring(Math.Max(0, guestPhone.Length - 6))}",
                        FullName = guestName,
                        Phone = guestPhone,
                        Email = "",
                        Address = guestAddress,
                        Status = CustomerStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };
                    _dbContext.Customers.Add(guest);
                    await _dbContext.SaveChangesAsync();
                }
                else if (!string.IsNullOrWhiteSpace(guestAddress) && string.IsNullOrWhiteSpace(guest.Address))
                {
                    guest.Address = guestAddress;
                }
                dto.CustomerId = guest.CustomerId;
            }

            if (dto.DiscountAmount < 0)
                throw new InvalidOperationException("Chiết khấu không được nhỏ hơn 0.");

            if (dto.DiscountValue < 0)
                throw new InvalidOperationException("Gia tri chiet khau khong duoc nho hon 0.");

            var inventoryRequests = dto.Items
                .Select(i => new ProductInventoryRequest { ProductId = i.ProductId, Quantity = i.Quantity })
                .ToList();

            var catalogProducts = _productCatalogClient == null
                ? new List<ProductCatalogItem>()
                : (await _productCatalogClient.GetProductsAsync(dto.Items.Select(i => i.ProductId))).ToList();

            if (_productCatalogClient != null)
            {
                var inventoryCheck = await _productCatalogClient.CheckInventoryAsync(inventoryRequests);
                if (!inventoryCheck.IsAvailable)
                    throw new InvalidOperationException(BuildInsufficientStockMessage(inventoryCheck));
            }

            foreach (var itemDto in dto.Items)
            {
                if (itemDto.Quantity <= 0)
                    throw new InvalidOperationException("Số lượng sản phẩm phải lớn hơn 0.");

                if (itemDto.DiscountAmount < 0)
                    throw new InvalidOperationException("Chiết khấu sản phẩm không được nhỏ hơn 0.");

                var catalogProduct = catalogProducts.FirstOrDefault(p => p.ProductId == itemDto.ProductId);
                if (catalogProduct != null)
                {
                    itemDto.ProductCode = string.IsNullOrWhiteSpace(itemDto.ProductCode) ? catalogProduct.ProductCode : itemDto.ProductCode;
                    itemDto.ProductName = string.IsNullOrWhiteSpace(itemDto.ProductName) ? catalogProduct.ProductName : itemDto.ProductName;
                    itemDto.UnitPrice = itemDto.UnitPrice > 0 ? itemDto.UnitPrice : catalogProduct.SellingPrice;
                }

                var stock = await _dbContext.ProductStockCaches
                    .FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);

                if (_productCatalogClient == null && stock == null)
                    throw new InvalidOperationException($"Không có dữ liệu tồn kho cho sản phẩm {itemDto.ProductId}.");

                if (_productCatalogClient == null && stock!.QuantityAvailable < itemDto.Quantity)
                    throw new InvalidOperationException($"Sản phẩm {itemDto.ProductId} không đủ tồn kho.");
            }

            var orderCode = await GenerateOrderCodeAsync();
            var order = new Order
            {
                OrderCode = orderCode,
                CustomerId = dto.CustomerId,
                CreatedByUserId = dto.CreatedByUserId,
                OrderDate = DateTime.UtcNow,
                DiscountType = NormalizeDiscountType(dto.DiscountType),
                DiscountValue = dto.DiscountValue,
                OrderStatus = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Unpaid,
            };

            decimal totalAmount = 0;
            foreach (var itemDto in dto.Items)
            {
                var stock = await _dbContext.ProductStockCaches
                    .FirstOrDefaultAsync(p => p.ProductId == itemDto.ProductId);
                var catalogProduct = catalogProducts.FirstOrDefault(p => p.ProductId == itemDto.ProductId);

                var unitPrice = itemDto.UnitPrice > 0 ? itemDto.UnitPrice : stock?.SellingPrice ?? catalogProduct?.SellingPrice ?? 0;
                var detail = new OrderDetail
                {
                    ProductId = itemDto.ProductId,
                    ProductCode = string.IsNullOrEmpty(itemDto.ProductCode) ? stock?.ProductCode ?? catalogProduct?.ProductCode ?? "" : itemDto.ProductCode,
                    ProductName = string.IsNullOrEmpty(itemDto.ProductName) ? stock?.ProductName ?? catalogProduct?.ProductName ?? "" : itemDto.ProductName,
                    ProductImage = null, // Can map from catalog if needed later
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    DiscountAmount = itemDto.DiscountAmount,
                    SubTotal = (itemDto.Quantity * unitPrice) - itemDto.DiscountAmount
                };
                order.Items.Add(detail);
                totalAmount += detail.SubTotal;
            }

            var discountAmount = CalculateDiscountAmount(totalAmount, order.DiscountType, dto.DiscountValue, dto.DiscountAmount);

            if (discountAmount > totalAmount)
                throw new InvalidOperationException("Chiết khấu không được lớn hơn tổng tiền.");

            order.TotalAmount = totalAmount;
            order.DiscountValue = dto.DiscountValue > 0 ? dto.DiscountValue : dto.DiscountAmount;
            order.DiscountAmount = discountAmount;
            order.FinalAmount = totalAmount - discountAmount;

            // Ghi nhận thanh toán từ KhoPro (bán tại quầy)
            var paidAmount = Math.Min(dto.PaidAmount, order.FinalAmount);
            if (paidAmount > 0)
            {
                order.PaidAmount = paidAmount;
                order.PaymentStatus = paidAmount >= order.FinalAmount ? PaymentStatus.Paid : PaymentStatus.Partial;
                order.OrderStatus = paidAmount >= order.FinalAmount ? OrderStatus.Completed : OrderStatus.Confirmed;
            }

            _dbContext.Orders.Add(order);
            await _dbContext.SaveChangesAsync();

            if (_productCatalogClient != null)
            {
                var deducted = await _productCatalogClient.DeductInventoryAsync(inventoryRequests);
                order.OrderStatus = deducted ? OrderStatus.Confirmed : OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                if (!deducted)
                    return MapToDto(order);
            }

            await _outboxService.EnqueueOrderCreatedAsync(order.OrderId);

            // Fire-and-forget: push to N3 (UserReport) for KhoPro orders
            if (_httpClientFactory != null)
            {
                var n2OrderId = order.OrderId;
                var customer = await _dbContext.Customers.FindAsync(order.CustomerId);
                var n3CustomerId = order.CustomerId.ToString();
                var n3CustomerName = customer?.FullName ?? dto.CustomerName ?? "Khách lẻ";
                var n3Email = customer?.Email ?? "";
                var n3Phone = customer?.Phone ?? dto.CustomerPhone ?? "";
                var n3Address = customer?.Address ?? "";
                var n3Discount = order.DiscountAmount;
                var n3PaymentMethod = dto.PaymentMethod;
                var n3PaidAmount = order.PaidAmount;
                var n3Items = order.Items.Select(i => new
                {
                    ProductId = i.ProductId.ToString(),
                    ProductName = i.ProductName,
                    CategoryName = (string?)null,
                    Quantity = (decimal)i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList();
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
            }

            _logger.LogInformation("Order created: {OrderCode}", orderCode);
            return MapToDto(order);
        }

        public async Task<OrderDto> UpdateOrderStatusAsync(int orderId, string status, string? approvedBy = null)
        {
            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderId == orderId)
                ?? throw new KeyNotFoundException($"Order {orderId} not found");

            if (order.OrderStatus == OrderStatus.Completed)
                throw new InvalidOperationException("Đơn đã thanh toán, không được sửa chi tiết sản phẩm.");

            if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            {
                // Khi KhoPro xác nhận đơn web (Confirmed/Completed) mà khách chưa trả tiền
                // → coi như khách vừa thanh toán cash khi nhận hàng
                if ((orderStatus == OrderStatus.Confirmed || orderStatus == OrderStatus.Completed)
                    && order.PaymentStatus == PaymentStatus.Unpaid
                    && order.FinalAmount > 0)
                {
                    var debtToClear = order.DebtAmount;

                    order.OrderStatus = OrderStatus.Completed;
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.PaidAmount = order.FinalAmount;
                    order.DebtAmount = 0;

                    // Xóa nợ
                    if (debtToClear > 0)
                    {
                        var debt = await _dbContext.Debts
                            .FirstOrDefaultAsync(d => d.OrderId == orderId && d.DebtStatus != DebtStatus.Paid);
                        if (debt != null)
                        {
                            debt.PaidAmount = debt.DebtAmount;
                            debt.RemainingAmount = 0;
                            debt.DebtStatus = DebtStatus.Paid;
                        }

                        if (order.Customer != null)
                            order.Customer.CurrentDebt = Math.Max(0, order.Customer.CurrentDebt - debtToClear);
                    }
                }
                else
                {
                    order.OrderStatus = orderStatus;
                }

                order.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrWhiteSpace(approvedBy) && order.ApprovedBy == null)
                {
                    order.ApprovedBy = approvedBy;
                    order.ApprovedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
            }

            return MapToDto(order);
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Debt)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                return false;

            if (order.OrderStatus == OrderStatus.Completed)
                throw new InvalidOperationException("Không thể hủy đơn đã thanh toán đủ.");

            if (order.OrderStatus == OrderStatus.Cancelled)
            {
                if (order.StockRestoredAt == null)
                {
                    await RestoreStockForCancelledOrderAsync(order);
                    await _dbContext.SaveChangesAsync();
                }

                return true;
            }

            if (order.StockRestoredAt == null)
                await RestoreStockForCancelledOrderAsync(order);

            if (order.Customer != null && order.DebtAmount > 0)
            {
                order.Customer.CurrentDebt = Math.Max(0, order.Customer.CurrentDebt - order.DebtAmount);
                order.Customer.UpdatedAt = DateTime.UtcNow;
            }

            RefundWalletPayments(order);

            if (order.Debt != null)
            {
                order.Debt.PaidAmount = order.Debt.DebtAmount;
                order.Debt.DebtStatus = DebtStatus.Paid;
                order.Debt.UpdatedAt = DateTime.UtcNow;
            }

            order.DebtAmount = 0;
            order.OrderStatus = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            NotifyN3Cancel(orderId);
            return true;
        }

        public async Task<bool> CancelOrderForCustomerAsync(int orderId, string phone)
        {
            var normalizedPhone = (phone ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalizedPhone))
                return false;

            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Debt)
                .Include(o => o.Items)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null || order.Customer == null)
                return false;

            if (!string.Equals(order.Customer.Phone?.Trim(), normalizedPhone, StringComparison.OrdinalIgnoreCase))
                return false;

            if (order.OrderStatus == OrderStatus.Completed)
                throw new InvalidOperationException("Không thể hủy đơn đã thanh toán đủ.");

            if (order.OrderStatus == OrderStatus.Cancelled)
            {
                if (order.StockRestoredAt == null)
                {
                    await RestoreStockForCancelledOrderAsync(order);
                    await _dbContext.SaveChangesAsync();
                }

                return true;
            }

            if (order.StockRestoredAt == null)
                await RestoreStockForCancelledOrderAsync(order);

            if (order.Customer != null && order.DebtAmount > 0)
            {
                order.Customer.CurrentDebt = Math.Max(0, order.Customer.CurrentDebt - order.DebtAmount);
                order.Customer.UpdatedAt = DateTime.UtcNow;
            }

            RefundWalletPayments(order);

            if (order.Debt != null)
            {
                order.Debt.PaidAmount = order.Debt.DebtAmount;
                order.Debt.DebtStatus = DebtStatus.Paid;
                order.Debt.UpdatedAt = DateTime.UtcNow;
            }

            order.DebtAmount = 0;
            order.OrderStatus = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            NotifyN3Cancel(orderId);
            return true;
        }

        private void NotifyN3Cancel(int n2OrderId)
        {
            if (_httpClientFactory == null) return;
            if (!N3OrderTracker.TryGetAndRemove(n2OrderId, out var n3Id)) return;
            var factory = _httpClientFactory;
            _ = Task.Run(async () =>
            {
                try { await factory.CreateClient("UserReport").DeleteAsync($"/api/Orders/{n3Id}"); }
                catch { }
            });
        }

        private static void RefundWalletPayments(Order order)
        {
            if (order.Customer == null || order.Payments.Count == 0)
                return;

            var walletPaid = order.Payments
                .Where(payment => payment.PaymentMethod == PaymentMethod.Wallet && payment.Amount > 0)
                .Sum(payment => payment.Amount);

            if (walletPaid <= 0)
                return;

            order.Customer.WalletBalance += walletPaid;
            order.Customer.UpdatedAt = DateTime.UtcNow;
        }

        private async Task RestoreStockForCancelledOrderAsync(Order order)
        {
            if (_productCatalogClient != null)
            {
                var restored = await _productCatalogClient.RestoreInventoryAsync(order.Items.Select(i =>
                    new ProductInventoryRequest { ProductId = i.ProductId, Quantity = i.Quantity }));
                if (restored)
                {
                    order.StockRestoredAt = DateTime.UtcNow;
                    return;
                }
            }

            foreach (var item in order.Items)
            {
                var stock = await _dbContext.ProductStockCaches
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(p => p.ProductId == item.ProductId);

                if (stock == null)
                {
                    stock = new ProductStockCache
                    {
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        SellingPrice = item.UnitPrice
                    };
                    _dbContext.ProductStockCaches.Add(stock);
                }

                stock.QuantityAvailable += item.Quantity;
                stock.StockStatus = stock.QuantityAvailable <= 0
                    ? StockStatus.OutOfStock
                    : stock.QuantityAvailable <= 5
                        ? StockStatus.LowStock
                        : StockStatus.InStock;
                stock.IsDeleted = false;
                stock.LastUpdatedAt = DateTime.UtcNow;
            }

            order.StockRestoredAt = DateTime.UtcNow;
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null)
                return false;

            _dbContext.Orders.Remove(order);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private async Task<string> GenerateOrderCodeAsync()
        {
            var count = await _dbContext.Orders.CountAsync();
            return $"ORD{(count + 1):D6}";
        }

        private static OrderDto MapToDto(Order order)
        {
            var latestPayment = order.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

            return new OrderDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.FullName,
                CustomerPhone = order.Customer?.Phone,
                CustomerEmail = order.Customer?.Email,
                CustomerAddress = order.Customer?.Address,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                DiscountAmount = order.DiscountAmount,
                DiscountType = order.DiscountType,
                DiscountValue = order.DiscountValue,
                FinalAmount = order.FinalAmount,
                PaidAmount = order.PaidAmount,
                DebtAmount = order.DebtAmount,
                PaymentStatus = order.PaymentStatus.ToString(),
                PaymentMethod = order.PaymentMethod ?? latestPayment?.PaymentMethod.ToString(),
                OrderStatus = order.OrderStatus.ToString(),
                CreatedByUserId = order.CreatedByUserId,
                CreatedBy = order.CreatedByUserId.ToString(),
                Source = order.Customer?.Phone == "0000000000" ? "KhoPro" : "Web",
                ApprovedBy = order.ApprovedBy,
                ApprovedAt = order.ApprovedAt,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,

                Items = order.Items.Select(i => new OrderDetailDto
                {
                    OrderDetailId = i.OrderDetailId,
                    ProductId = i.ProductId,
                    ProductCode = i.ProductCode,
                    ProductName = i.ProductName,
                    ProductImage = i.ProductImage,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    DiscountAmount = i.DiscountAmount,
                    SubTotal = i.SubTotal
                }).ToList()
            };
        }

        internal static string NormalizeDiscountType(string? discountType)
        {
            return string.Equals(discountType, "Percent", StringComparison.OrdinalIgnoreCase)
                ? "Percent"
                : "Fixed";
        }

        internal static decimal CalculateDiscountAmount(
            decimal totalAmount,
            string discountType,
            decimal discountValue,
            decimal legacyDiscountAmount)
        {
            var value = discountValue > 0 ? discountValue : legacyDiscountAmount;
            if (value <= 0)
                return 0;

            if (string.Equals(discountType, "Percent", StringComparison.OrdinalIgnoreCase))
            {
                if (value > 100)
                    throw new InvalidOperationException("Chiet khau phan tram khong duoc lon hon 100.");

                return Math.Round(totalAmount * value / 100m, 2, MidpointRounding.AwayFromZero);
            }

            return value;
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
}
