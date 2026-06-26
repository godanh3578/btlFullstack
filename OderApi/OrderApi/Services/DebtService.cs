using OrderApi.Data;
using OrderApi.DTOs.Debts;
using OrderApi.Models;
using Microsoft.EntityFrameworkCore;

namespace OrderApi.Services
{
    public class DebtService : IDebtService
    {
        private readonly OrderDbContext _dbContext;
        private readonly ILogger<DebtService> _logger;

        public DebtService(OrderDbContext dbContext, ILogger<DebtService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<DebtDto?> GetDebtByIdAsync(int debtId)
        {
            var debt = await _dbContext.Debts.FindAsync(debtId);
            if (debt == null)
                return null;

            return MapToDto(debt);
        }

        public async Task<CustomerDebtsDto> GetDebtsByCustomerIdAsync(int customerId)
        {
            var customer = await _dbContext.Customers.FindAsync(customerId);
            if (customer == null)
                throw new KeyNotFoundException($"Customer {customerId} not found");

            var debts = await _dbContext.Debts
                .Where(d => d.CustomerId == customerId)
                .ToListAsync();

            return new CustomerDebtsDto
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.FullName,
                Debts = debts.Select(MapToDto).ToList()
            };
        }

        public async Task<List<DebtDto>> GetAllDebtsAsync()
        {
            var debts = await _dbContext.Debts
                .Include(d => d.Customer)
                .ToListAsync();
            return debts.Select(MapToDto).ToList();
        }

        public async Task<DebtDto> PayDebtAsync(int debtId, CreateDebtPaymentDto dto)
        {
            var debt = await _dbContext.Debts
                .Include(d => d.Customer)
                .Include(d => d.Order)
                .FirstOrDefaultAsync(d => d.DebtId == debtId);
            if (debt == null)
                throw new KeyNotFoundException($"Debt {debtId} not found");

            if (dto.Amount <= 0)
                throw new InvalidOperationException("Số tiền trả nợ phải lớn hơn 0.");

            var remainingBefore = GetRemainingAmount(debt);
            if (dto.Amount > remainingBefore)
                throw new InvalidOperationException("Số tiền trả không được lớn hơn số tiền còn nợ.");

            debt.PaidAmount += dto.Amount;

            if (debt.PaidAmount >= debt.DebtAmount)
            {
                debt.DebtStatus = DebtStatus.Paid;
                debt.PaidAmount = debt.DebtAmount;
            }
            else if (debt.PaidAmount > 0)
            {
                debt.DebtStatus = DebtStatus.Partial;
            }

            if (debt.DueDate.HasValue && DateTime.UtcNow > debt.DueDate && debt.DebtStatus != DebtStatus.Paid)
            {
                debt.DebtStatus = DebtStatus.Overdue;
            }

            var order = debt.Order ?? await _dbContext.Orders.FindAsync(debt.OrderId);
            if (order != null)
            {
                order.PaidAmount = Math.Min(order.FinalAmount, order.PaidAmount + dto.Amount);
                order.DebtAmount = Math.Max(0, order.FinalAmount - order.PaidAmount);
                order.PaymentStatus = order.DebtAmount <= 0 ? PaymentStatus.Paid : PaymentStatus.Partial;
                order.OrderStatus = order.DebtAmount <= 0 ? OrderStatus.Completed : OrderStatus.Confirmed;
                order.UpdatedAt = DateTime.UtcNow;
            }

            var customer = debt.Customer ?? await _dbContext.Customers.FindAsync(debt.CustomerId);
            if (customer != null)
            {
                customer.CurrentDebt = Math.Max(0, customer.CurrentDebt - dto.Amount);
                customer.UpdatedAt = DateTime.UtcNow;
            }

            var payment = new Payment
            {
                OrderId = debt.OrderId,
                PaymentCode = $"PAY{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                Amount = dto.Amount,
                PaymentDate = DateTime.UtcNow,
                PaymentStatus = GetRemainingAmount(debt) <= 0 ? PaymentStatus.Paid : PaymentStatus.Partial,
                Note = string.IsNullOrWhiteSpace(dto.Note) ? "Debt payment" : dto.Note
            };

            if (Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var method))
                payment.PaymentMethod = method;

            _dbContext.Payments.Add(payment);
            debt.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Debt {debtId} payment recorded: {dto.Amount}");

            return MapToDto(debt);
        }

        public async Task<DebtDto> UpdateDebtStatusAsync(int debtId, UpdateDebtStatusDto dto)
        {
            var debt = await _dbContext.Debts.FindAsync(debtId);
            if (debt == null)
                throw new KeyNotFoundException($"Debt {debtId} not found");

            if (Enum.TryParse<DebtStatus>(dto.DebtStatus, out var status))
            {
                debt.DebtStatus = status;
                debt.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Debt {debtId} status updated to {dto.DebtStatus}");
            }

            return MapToDto(debt);
        }

        public async Task<List<DebtReportDto>> GetDebtReportAsync()
        {
            var debts = await _dbContext.Debts
                .Include(d => d.Customer)
                .GroupBy(d => d.CustomerId)
                .Select(g => new DebtReportDto
                {
                    CustomerId = g.Key,
                    CustomerName = g.FirstOrDefault()!.Customer!.FullName,
                    TotalDebt = g.Sum(d => d.DebtAmount),
                    TotalPaid = g.Sum(d => d.PaidAmount),
                    TotalUnpaidOrders = g.Count(d => d.DebtAmount - d.PaidAmount > 0)
                })
                .ToListAsync();

            return debts;
        }

        private DebtDto MapToDto(Debt debt)
        {
            return new DebtDto
            {
                DebtId = debt.DebtId,
                CustomerId = debt.CustomerId,
                CustomerName = debt.Customer?.FullName,
                OrderId = debt.OrderId,
                DebtAmount = debt.DebtAmount,
                PaidAmount = debt.PaidAmount,
                RemainingAmount = GetRemainingAmount(debt),
                DueDate = debt.DueDate,
                DebtStatus = debt.DebtStatus.ToString(),
                CreatedAt = debt.CreatedAt,
                UpdatedAt = debt.UpdatedAt
            };
        }

        private static decimal GetRemainingAmount(Debt debt)
        {
            return Math.Max(0, debt.DebtAmount - debt.PaidAmount);
        }
    }
}
