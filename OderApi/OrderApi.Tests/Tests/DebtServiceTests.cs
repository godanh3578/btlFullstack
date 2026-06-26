using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderApi.Data;
using OrderApi.DTOs.Debts;
using OrderApi.Models;
using OrderApi.Services;
using Xunit;

namespace OrderApi.Tests.Tests
{
    public class DebtServiceTests
    {
        private static OrderDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase("DebtServiceTestDb_" + Guid.NewGuid())
                .Options;

            var context = new OrderDbContext(options);
            context.Customers.Add(new Customers
            {
                CustomerId = 1,
                CustomerCode = "KH001",
                FullName = "Alice",
                Phone = "0900000000",
                CurrentDebt = 300
            });
            context.Orders.Add(new Order
            {
                OrderId = 1,
                OrderCode = "ORD001",
                CustomerId = 1,
                CreatedByUserId = 1,
                TotalAmount = 500,
                FinalAmount = 500,
                PaidAmount = 200,
                DebtAmount = 300,
                PaymentStatus = PaymentStatus.Partial,
                OrderStatus = OrderStatus.Confirmed
            });
            context.Debts.Add(new Debt
            {
                DebtId = 1,
                CustomerId = 1,
                OrderId = 1,
                DebtAmount = 300,
                PaidAmount = 0,
                DebtStatus = DebtStatus.Unpaid,
                DueDate = DateTime.UtcNow.AddDays(30)
            });
            context.SaveChanges();

            return context;
        }

        [Fact]
        public async Task PayDebt_UpdatesDebtOrderCustomerAndCreatesPayment()
        {
            using var context = CreateContext();
            var service = new DebtService(context, NullLogger<DebtService>.Instance);

            var result = await service.PayDebtAsync(1, new CreateDebtPaymentDto
            {
                Amount = 300,
                PaymentMethod = "Cash",
                Note = "Final payment"
            });

            var order = await context.Orders.FindAsync(1);
            var customer = await context.Customers.FindAsync(1);
            var payment = await context.Payments.SingleAsync();

            Assert.Equal("Paid", result.DebtStatus);
            Assert.Equal(0, result.RemainingAmount);
            Assert.Equal(500, order!.PaidAmount);
            Assert.Equal(0, order.DebtAmount);
            Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
            Assert.Equal(OrderStatus.Completed, order.OrderStatus);
            Assert.Equal(0, customer!.CurrentDebt);
            Assert.Equal(300, payment.Amount);
            Assert.Equal(PaymentStatus.Paid, payment.PaymentStatus);
        }

        [Fact]
        public async Task PayDebt_RejectsOverpayment()
        {
            using var context = CreateContext();
            var service = new DebtService(context, NullLogger<DebtService>.Instance);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.PayDebtAsync(1, new CreateDebtPaymentDto { Amount = 301 }));
        }

        [Fact]
        public async Task GetPaymentsByCustomerId_ReturnsCustomerPaymentHistory()
        {
            using var context = CreateContext();
            context.Payments.Add(new Payment
            {
                OrderId = 1,
                PaymentCode = "PAY001",
                Amount = 100,
                PaymentDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var service = new PaymentService(context, NullLogger<PaymentService>.Instance);

            var payments = await service.GetPaymentsByCustomerIdAsync(1);

            Assert.Single(payments);
            Assert.Equal(100, payments[0].Amount);
        }
    }
}
