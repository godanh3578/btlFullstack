#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using OrderApi.Data;
using OrderApi.DTOs.Sales;
using OrderApi.Models;
using OrderApi.Services;
using Xunit;

namespace OrderApi.Tests.Tests
{
    public class SalesServiceIntegrationTests
    {
        private static OrderDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase("SalesIntegrationDb_" + Guid.NewGuid())
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new OrderDbContext(options);
        }

        private static SalesService CreateService(OrderDbContext context, FakeProductCatalogClient productClient)
        {
            var outbox = new OutboxService(context);
            var orderService = new OrderService(context, outbox, NullLogger<OrderService>.Instance, productClient);
            var paymentService = new PaymentService(context, NullLogger<PaymentService>.Instance);

            return new SalesService(
                context,
                orderService,
                paymentService,
                outbox,
                productClient,
                NullLogger<SalesService>.Instance);
        }

        [Fact]
        public async Task Checkout_WithPartialPayment_CreatesOrderDebtPaymentAndOutboxEnvelope()
        {
            using var context = CreateContext();
            context.Customers.Add(new Customers
            {
                CustomerId = 1,
                CustomerCode = "KH001",
                FullName = "Alice",
                Phone = "0900000000"
            });
            await context.SaveChangesAsync();

            var productClient = new FakeProductCatalogClient();
            var service = CreateService(context, productClient);

            var result = await service.CheckoutAsync(new CheckoutDto
            {
                CustomerId = 1,
                DiscountType = "Percent",
                DiscountValue = 10,
                PaidAmount = 40,
                PaymentMethod = "Cash",
                Items = new List<CheckoutItemDto>
                {
                    new() { ProductId = 9, Quantity = 2 }
                }
            }, "sales01");

            var order = await context.Orders.Include(o => o.Payments).SingleAsync();
            var debt = await context.Debts.SingleAsync();
            var customer = await context.Customers.SingleAsync();
            var outbox = await context.OutboxMessages.SingleAsync();

            Assert.True(productClient.CheckCalled);
            Assert.True(productClient.DeductCalled);
            Assert.Equal(result.OrderId, order.OrderId);
            Assert.Equal(200, order.TotalAmount);
            Assert.Equal(20, order.DiscountAmount);
            Assert.Equal(180, order.FinalAmount);
            Assert.Equal(40, order.PaidAmount);
            Assert.Equal(140, order.DebtAmount);
            Assert.Equal(PaymentStatus.Partial, order.PaymentStatus);
            Assert.Equal(OrderStatus.Confirmed, order.OrderStatus);
            Assert.Equal(140, debt.DebtAmount);
            Assert.Equal(140, customer.CurrentDebt);
            Assert.Single(order.Payments);

            using var document = JsonDocument.Parse(outbox.Payload);
            Assert.Equal("order.created", document.RootElement.GetProperty("EventType").GetString());
            Assert.True(document.RootElement.TryGetProperty("EventId", out _));
            Assert.True(document.RootElement.TryGetProperty("Timestamp", out _));
            Assert.Equal(order.OrderId, document.RootElement.GetProperty("Data").GetProperty("OrderId").GetInt32());
        }

        [Fact]
        public async Task Checkout_WhenInventoryIsInsufficient_DoesNotCreateOrder()
        {
            using var context = CreateContext();
            context.Customers.Add(new Customers
            {
                CustomerId = 1,
                CustomerCode = "KH001",
                FullName = "Alice",
                Phone = "0900000000"
            });
            await context.SaveChangesAsync();

            var productClient = new FakeProductCatalogClient { AvailableQuantity = 1 };
            var service = CreateService(context, productClient);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.CheckoutAsync(new CheckoutDto
                {
                    CustomerId = 1,
                    Items = new List<CheckoutItemDto>
                    {
                        new() { ProductId = 9, Quantity = 2 }
                    }
                }, "sales01"));

            Assert.Empty(context.Orders);
            Assert.Empty(context.OutboxMessages);
            Assert.True(productClient.CheckCalled);
            Assert.False(productClient.DeductCalled);
        }

        private sealed class FakeProductCatalogClient : IProductCatalogClient
        {
            public int AvailableQuantity { get; set; } = 10;
            public bool CheckCalled { get; private set; }
            public bool DeductCalled { get; private set; }

            public Task<ProductCatalogItem?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<ProductCatalogItem?>(CreateProduct(productId));
            }

            public Task<IReadOnlyList<ProductCatalogItem>> GetProductsAsync(
                IEnumerable<int> productIds,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<ProductCatalogItem>>(productIds.Select(CreateProduct).ToList());
            }

            public Task<InventoryCheckResult> CheckInventoryAsync(
                IEnumerable<ProductInventoryRequest> items,
                CancellationToken cancellationToken = default)
            {
                CheckCalled = true;
                var shortages = items
                    .Where(i => i.Quantity > AvailableQuantity)
                    .Select(i => new InventoryShortageItem
                    {
                        ProductId = i.ProductId,
                        RequestedQuantity = i.Quantity,
                        AvailableQuantity = AvailableQuantity
                    })
                    .ToList();

                return Task.FromResult(new InventoryCheckResult
                {
                    IsAvailable = shortages.Count == 0,
                    Shortages = shortages
                });
            }

            public Task<bool> DeductInventoryAsync(
                IEnumerable<ProductInventoryRequest> items,
                CancellationToken cancellationToken = default)
            {
                DeductCalled = true;
                return Task.FromResult(true);
            }

            public Task<bool> RestoreInventoryAsync(
                IEnumerable<ProductInventoryRequest> items,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(true);
            }

            private ProductCatalogItem CreateProduct(int productId) => new()
            {
                ProductId = productId,
                ProductCode = $"SP{productId:D3}",
                ProductName = $"Product {productId}",
                CategoryName = "Test",
                SellingPrice = 100,
                QuantityAvailable = AvailableQuantity,
                StockStatus = AvailableQuantity > 0 ? "InStock" : "OutOfStock"
            };
        }
    }
}
