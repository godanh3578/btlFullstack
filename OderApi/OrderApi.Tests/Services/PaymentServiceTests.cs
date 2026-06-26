using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderApi.Data;
using OrderApi.DTOs.Payments;
using OrderApi.Models;
using OrderApi.Services;

namespace OrderApi.Tests.Services
{
    public class PaymentServiceTests
    {
        private OrderDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new OrderDbContext(options);
        }

        [Fact]
        public async Task ProcessPayment_ValidAmount_UpdatesOrderAndCustomer()
        {
            // Arrange
            var dbContext = GetDbContext("ProcessPayment_Valid");
            var order = new Order { OrderId = 1, CustomerId = 1, FinalAmount = 100, DebtAmount = 100, OrderDate = DateTime.UtcNow };
            var customer = new Customers { CustomerId = 1, CustomerCode = "KH001", CurrentDebt = 100, FullName = "John", Phone = "123" };
            dbContext.Orders.Add(order);
            dbContext.Customers.Add(customer);
            await dbContext.SaveChangesAsync();

            var service = new PaymentService(dbContext, NullLogger<PaymentService>.Instance);

            // Act
            var result = await service.RecordPaymentAsync(1, new CreatePaymentDto
            {
                Amount = 50,
                PaymentMethod = "Cash",
                Note = "Partial payment"
            });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(50, result.Amount);
            
            var updatedOrder = await dbContext.Orders.FindAsync(1);
            Assert.Equal(50, updatedOrder!.PaidAmount);
            Assert.Equal(50, updatedOrder.DebtAmount);
            Assert.Equal(PaymentStatus.Partial, updatedOrder.PaymentStatus);

            var updatedCustomer = await dbContext.Customers.FindAsync(1);
            Assert.Equal(50, updatedCustomer!.CurrentDebt);
        }

        [Fact]
        public async Task ProcessPayment_AmountExceedsDebt_ThrowsException()
        {
            // Arrange
            var dbContext = GetDbContext("ProcessPayment_Exceeds");
            var order = new Order { OrderId = 1, CustomerId = 1, FinalAmount = 100, PaidAmount = 50, DebtAmount = 50, OrderDate = DateTime.UtcNow };
            dbContext.Orders.Add(order);
            dbContext.Customers.Add(new Customers { CustomerId = 1, CustomerCode = "KH001", FullName = "John", Phone = "123" });
            await dbContext.SaveChangesAsync();

            var service = new PaymentService(dbContext, NullLogger<PaymentService>.Instance);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordPaymentAsync(1, new CreatePaymentDto
            {
                Amount = 100,
                PaymentMethod = "Cash",
                Note = "Note"
            }));
        }
    }
}
