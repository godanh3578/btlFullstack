using OrderApi.Data;
using OrderApi.DTOs.Payments;
using OrderApi.Models;
using Microsoft.EntityFrameworkCore;

namespace OrderApi.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly OrderDbContext _dbContext;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(OrderDbContext dbContext, ILogger<PaymentService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<PaymentDto>> GetAllPaymentsAsync()
        {
            var payments = await _dbContext.Payments
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(int paymentId)
        {
            var payment = await _dbContext.Payments.FindAsync(paymentId);
            if (payment == null)
                return null;

            return MapToDto(payment);
        }

        public async Task<List<PaymentDto>> GetPaymentsByOrderIdAsync(int orderId)
        {
            var payments = await _dbContext.Payments
                .Where(p => p.OrderId == orderId)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<List<PaymentDto>> GetPaymentsByCustomerIdAsync(int customerId)
        {
            var payments = await _dbContext.Payments
                .Include(p => p.Order)
                .Where(p => p.Order != null && p.Order.CustomerId == customerId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            return payments.Select(MapToDto).ToList();
        }

        public async Task<PaymentDto> RecordPaymentAsync(int orderId, CreatePaymentDto dto)
        {
            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null)
                throw new KeyNotFoundException($"Order {orderId} not found");

            if (dto.Amount <= 0)
                throw new InvalidOperationException("So tien thanh toan phai lon hon 0.");

            var remaining = order.FinalAmount - order.PaidAmount;
            if (dto.Amount > remaining)
                throw new InvalidOperationException("So tien thanh toan khong duoc vuot qua so tien con lai.");

            var paymentCode = $"PAY{DateTime.UtcNow:yyyyMMddHHmmssfff}";

            var payment = new Payment
            {
                OrderId = orderId,
                PaymentCode = paymentCode,
                Amount = dto.Amount,
                PaymentDate = DateTime.UtcNow,
                Note = dto.Note,
            };

            if (Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var method))
                payment.PaymentMethod = method;

            order.PaidAmount += dto.Amount;
            order.DebtAmount = Math.Max(0, order.FinalAmount - order.PaidAmount);

            if (order.DebtAmount <= 0)
            {
                payment.PaymentStatus = PaymentStatus.Paid;
                order.PaymentStatus = PaymentStatus.Paid;
                order.OrderStatus = OrderStatus.Completed;
            }
            else
            {
                payment.PaymentStatus = PaymentStatus.Partial;
                order.PaymentStatus = PaymentStatus.Partial;
                order.OrderStatus = OrderStatus.Confirmed;
            }

            var debt = await _dbContext.Debts
                .FirstOrDefaultAsync(d => d.OrderId == orderId && d.DebtStatus != DebtStatus.Paid);
            if (debt != null)
            {
                debt.PaidAmount = Math.Min(debt.DebtAmount, debt.PaidAmount + dto.Amount);
                debt.DebtStatus = debt.PaidAmount >= debt.DebtAmount ? DebtStatus.Paid : DebtStatus.Partial;
                debt.UpdatedAt = DateTime.UtcNow;
            }

            if (order.Customer != null)
            {
                order.Customer.CurrentDebt = Math.Max(0, order.Customer.CurrentDebt - dto.Amount);
                order.Customer.UpdatedAt = DateTime.UtcNow;
            }

            order.UpdatedAt = DateTime.UtcNow;

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Payment recorded: {PaymentCode} for order {OrderId}", paymentCode, orderId);

            return MapToDto(payment);
        }

        private static PaymentDto MapToDto(Payment payment)
        {
            return new PaymentDto
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                PaymentCode = payment.PaymentCode,
                PaymentMethod = payment.PaymentMethod.ToString(),
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                PaymentStatus = payment.PaymentStatus.ToString(),
                Note = payment.Note,
                CreatedAt = payment.CreatedAt
            };
        }
    }
}
