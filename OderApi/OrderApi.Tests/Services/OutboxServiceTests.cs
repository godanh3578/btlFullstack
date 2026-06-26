using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.Models;
using OrderApi.Services;

namespace OrderApi.Tests.Services
{
    public class OutboxServiceTests
    {
        private OrderDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new OrderDbContext(options);
        }

        [Fact]
        public async Task EnqueueOrderCreatedAsync_ValidOrderId_AddsMessageToOutbox()
        {
            // Arrange
            var dbContext = GetDbContext("EnqueueOrder_Valid");
            dbContext.Customers.Add(new Customers
            {
                CustomerId = 1,
                CustomerCode = "KH001",
                FullName = "John",
                Phone = "123"
            });
            dbContext.Orders.Add(new Order
            {
                OrderId = 123,
                OrderCode = "ORD-001",
                CustomerId = 1,
                CreatedByUserId = 1,
                TotalAmount = 100,
                FinalAmount = 100,
                DebtAmount = 100
            });
            await dbContext.SaveChangesAsync();
            var service = new OutboxService(dbContext);

            // Act
            await service.EnqueueOrderCreatedAsync(123);

            // Assert
            var message = await dbContext.OutboxMessages.FirstOrDefaultAsync();
            Assert.NotNull(message);
            Assert.Equal("order.created", message.EventName);
            Assert.Contains("123", message.Payload);
            Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        }

        [Fact]
        public async Task EnqueueAsync_ValidPayload_AddsPendingMessage()
        {
            // Arrange
            var dbContext = GetDbContext("EnqueueAsync_Valid");
            var service = new OutboxService(dbContext);

            // Act
            await service.EnqueueAsync("test.event", new { Id = 1 });

            // Assert
            var message = await dbContext.OutboxMessages.FirstOrDefaultAsync();
            Assert.NotNull(message);
            Assert.Equal("test.event", message.EventName);
            Assert.Equal(OutboxMessageStatus.Pending, message.Status);
        }
    }
}
