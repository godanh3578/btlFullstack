#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderApi.Controllers;
using OrderApi.Data;
using OrderApi.DTOs.Orders;
using OrderApi.Models;
using OrderApi.Services;
using Xunit;

namespace OrderApi.Tests.Tests
{
    public class OrdersControllerTests
    {
        private static void SetStaffUser(OrdersController controller, string role = "Sales", string username = "sales01")
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, username),
                new("role", role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, "role");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
        }

        private static (OrderDbContext context, OrdersController controller) CreateController()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase("OrdersTestDb_" + Guid.NewGuid())
                .Options;

            var context = new OrderDbContext(options);
            context.Customers.Add(new Customers
            {
                CustomerId = 1,
                CustomerCode = "KH001",
                FullName = "John",
                Phone = "0123456789",
                Email = "john@example.com",
                Address = "Somewhere"
            });
            context.ProductStockCaches.Add(new ProductStockCache
            {
                ProductId = 1,
                ProductCode = "SP001",
                ProductName = "Product 1",
                SellingPrice = 50,
                QuantityAvailable = 100,
                StockStatus = StockStatus.InStock
            });
            context.SaveChanges();

            var outbox = new OutboxService(context);
            var orderService = new OrderService(context, outbox, NullLogger<OrderService>.Instance);
            var controller = new OrdersController(orderService);
            return (context, controller);
        }

        [Fact]
        public async Task Create_Get_Update_Cancel_Delete_Order()
        {
            var (_, controller) = CreateController();
            SetStaffUser(controller);

            var dto = new CreateOrderDto
            {
                CustomerId = 1,
                CreatedByUserId = 1,
                DiscountAmount = 0,
                Items = new List<CreateOrderDetailDto>
                {
                    new() { ProductId = 1, ProductCode = "SP001", ProductName = "P1", Quantity = 2, UnitPrice = 50 }
                }
            };

            var createResult = await controller.Create(dto);
            var created = Assert.IsType<OrderDto>((createResult as OkObjectResult)!.Value);
            Assert.Equal(100, created.TotalAmount);

            var getByCustomer = await controller.GetAll(null, 1);
            var customerList = Assert.IsType<List<OrderDto>>((getByCustomer as OkObjectResult)!.Value);
            Assert.Single(customerList);

            var getAllStaff = await controller.GetAll(null, null);
            var staffList = Assert.IsType<List<OrderDto>>((getAllStaff as OkObjectResult)!.Value);
            Assert.Single(staffList);

            var getById = await controller.GetById(created.OrderId);
            var fetched = Assert.IsType<OrderDto>((getById as OkObjectResult)!.Value);
            Assert.Equal(created.OrderId, fetched.OrderId);

            var statusResult = await controller.UpdateStatus(created.OrderId, new UpdateOrderStatusRequest { Status = "Confirmed" });
            var updated = Assert.IsType<OrderDto>((statusResult as OkObjectResult)!.Value);
            Assert.Equal("Confirmed", updated.OrderStatus);

            var cancelResult = await controller.Cancel(created.OrderId);
            Assert.IsType<OkObjectResult>(cancelResult);

            var deleteResult = await controller.Delete(created.OrderId);
            Assert.IsType<OkResult>(deleteResult);

            var afterDelete = await controller.GetById(created.OrderId);
            Assert.IsType<NotFoundResult>(afterDelete);
        }

        [Fact]
        public async Task GetAll_WithoutAuth_ReturnsUnauthorized_WhenNoCustomerId()
        {
            var (_, controller) = CreateController();

            var result = await controller.GetAll(null, null);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CreateOrder_WithPercentDiscount_CalculatesDiscountAmount()
        {
            var (_, controller) = CreateController();
            SetStaffUser(controller);

            var dto = new CreateOrderDto
            {
                CustomerId = 1,
                CreatedByUserId = 1,
                DiscountType = "Percent",
                DiscountValue = 10,
                Items = new List<CreateOrderDetailDto>
                {
                    new() { ProductId = 1, ProductCode = "SP001", ProductName = "P1", Quantity = 2, UnitPrice = 50 }
                }
            };

            var createResult = await controller.Create(dto);
            var created = Assert.IsType<OrderDto>((createResult as OkObjectResult)!.Value);

            Assert.Equal(100, created.TotalAmount);
            Assert.Equal("Percent", created.DiscountType);
            Assert.Equal(10, created.DiscountValue);
            Assert.Equal(10, created.DiscountAmount);
            Assert.Equal(90, created.FinalAmount);
        }

        [Fact]
        public async Task CreateOrder_WithProductClient_DeductsInventoryAndConfirmsOrder()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase("OrdersProductClientDb_" + Guid.NewGuid())
                .Options;

            using var context = new OrderDbContext(options);
            context.Customers.Add(new Customers
            {
                CustomerId = 1,
                CustomerCode = "KH001",
                FullName = "John",
                Phone = "0123456789"
            });
            await context.SaveChangesAsync();

            var productClient = new FakeProductCatalogClient();
            var outbox = new OutboxService(context);
            var service = new OrderService(context, outbox, NullLogger<OrderService>.Instance, productClient);

            var created = await service.CreateOrderAsync(new CreateOrderDto
            {
                CustomerId = 1,
                CreatedByUserId = 1,
                Items = new List<CreateOrderDetailDto>
                {
                    new() { ProductId = 9, Quantity = 2 }
                }
            });

            Assert.True(productClient.DeductCalled);
            Assert.Equal("Confirmed", created.OrderStatus);
            Assert.Equal(100, created.TotalAmount);
        }

        [Fact]
        public async Task GetAll_WithStatusDateAndPhoneFilters_ReturnsMatchingOrders()
        {
            var (context, controller) = CreateController();
            SetStaffUser(controller);

            context.Orders.AddRange(
                new Order
                {
                    OrderCode = "ORD-FILTER-001",
                    CustomerId = 1,
                    CreatedByUserId = 1,
                    OrderDate = new DateTime(2026, 6, 15, 10, 0, 0),
                    OrderStatus = OrderStatus.Confirmed
                },
                new Order
                {
                    OrderCode = "ORD-FILTER-002",
                    CustomerId = 1,
                    CreatedByUserId = 1,
                    OrderDate = new DateTime(2026, 6, 16, 10, 0, 0),
                    OrderStatus = OrderStatus.Completed
                });
            await context.SaveChangesAsync();

            var result = await controller.GetAll(
                search: "0123456789",
                customerId: null,
                status: "Confirmed",
                fromDate: new DateTime(2026, 6, 15),
                toDate: new DateTime(2026, 6, 15));

            var orders = Assert.IsType<List<OrderDto>>((result as OkObjectResult)!.Value);
            Assert.Single(orders);
            Assert.Equal("ORD-FILTER-001", orders[0].OrderCode);
        }

        private sealed class FakeProductCatalogClient : IProductCatalogClient
        {
            public bool DeductCalled { get; private set; }

            public Task<ProductCatalogItem?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<ProductCatalogItem?>(CreateProduct(productId));
            }

            public Task<IReadOnlyList<ProductCatalogItem>> GetProductsAsync(IEnumerable<int> productIds, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyList<ProductCatalogItem>>(productIds.Select(CreateProduct).ToList());
            }

            public Task<InventoryCheckResult> CheckInventoryAsync(IEnumerable<ProductInventoryRequest> items, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(InventoryCheckResult.Available());
            }

            public Task<bool> DeductInventoryAsync(IEnumerable<ProductInventoryRequest> items, CancellationToken cancellationToken = default)
            {
                DeductCalled = true;
                return Task.FromResult(true);
            }

            public Task<bool> RestoreInventoryAsync(IEnumerable<ProductInventoryRequest> items, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(true);
            }

            private static ProductCatalogItem CreateProduct(int productId) => new()
            {
                ProductId = productId,
                ProductCode = $"SP{productId:D3}",
                ProductName = $"Product {productId}",
                SellingPrice = 50,
                QuantityAvailable = 10,
                StockStatus = "InStock"
            };
        }
    }
}
