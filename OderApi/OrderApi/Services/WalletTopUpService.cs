using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.DTOs.WalletTopUps;
using OrderApi.Models;

namespace OrderApi.Services
{
    public class WalletTopUpService : IWalletTopUpService
    {
        private readonly OrderDbContext _dbContext;
        private readonly ILogger<WalletTopUpService> _logger;

        public WalletTopUpService(OrderDbContext dbContext, ILogger<WalletTopUpService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<WalletTopUpRequestDto>> GetAllAsync(string? status = null)
        {
            var query = _dbContext.WalletTopUpRequests
                .Include(r => r.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<WalletTopUpStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }

            var requests = await query
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(MapToDto).ToList();
        }

        public async Task<List<WalletTopUpRequestDto>> GetByCustomerIdAsync(int customerId)
        {
            var requests = await _dbContext.WalletTopUpRequests
                .Include(r => r.Customer)
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return requests.Select(MapToDto).ToList();
        }

        public async Task<WalletTopUpRequestDto?> GetByIdAsync(int id)
        {
            var request = await _dbContext.WalletTopUpRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.WalletTopUpRequestId == id);

            return request == null ? null : MapToDto(request);
        }

        public async Task<WalletTopUpRequestDto> CreateAsync(CreateWalletTopUpRequestDto dto)
        {
            if (dto.Amount <= 0)
                throw new InvalidOperationException("So tien nap vi phai lon hon 0.");

            var customer = await _dbContext.Customers.FindAsync(dto.CustomerId);
            if (customer == null)
                throw new KeyNotFoundException($"Customer {dto.CustomerId} not found");

            var request = new WalletTopUpRequest
            {
                RequestCode = $"TOPUP{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                CustomerId = dto.CustomerId,
                Amount = dto.Amount,
                PaymentMethod = NormalizePaymentMethod(dto.PaymentMethod),
                Note = dto.Note ?? "",
                Status = WalletTopUpStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            _dbContext.WalletTopUpRequests.Add(request);
            await _dbContext.SaveChangesAsync();

            request.Customer = customer;
            _logger.LogInformation("Wallet top-up requested: {RequestCode}", request.RequestCode);
            return MapToDto(request);
        }

        public async Task<WalletTopUpRequestDto> ApproveAsync(int id, ReviewWalletTopUpRequestDto dto)
        {
            var request = await _dbContext.WalletTopUpRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.WalletTopUpRequestId == id);

            if (request == null)
                throw new KeyNotFoundException($"Wallet top-up request {id} not found");

            if (request.Status != WalletTopUpStatus.Pending)
                throw new InvalidOperationException("Yeu cau nap vi da duoc xu ly.");

            if (request.Customer == null)
                throw new KeyNotFoundException($"Customer {request.CustomerId} not found");

            request.Customer.WalletBalance += request.Amount;
            request.Customer.UpdatedAt = DateTime.UtcNow;
            request.Status = WalletTopUpStatus.Approved;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedBy = string.IsNullOrWhiteSpace(dto.ReviewedBy) ? "Staff" : dto.ReviewedBy.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Note))
                request.Note = dto.Note.Trim();

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Wallet top-up approved: {RequestCode}", request.RequestCode);
            return MapToDto(request);
        }

        public async Task<WalletTopUpRequestDto> RejectAsync(int id, ReviewWalletTopUpRequestDto dto)
        {
            var request = await _dbContext.WalletTopUpRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.WalletTopUpRequestId == id);

            if (request == null)
                throw new KeyNotFoundException($"Wallet top-up request {id} not found");

            if (request.Status != WalletTopUpStatus.Pending)
                throw new InvalidOperationException("Yeu cau nap vi da duoc xu ly.");

            request.Status = WalletTopUpStatus.Rejected;
            request.ReviewedAt = DateTime.UtcNow;
            request.ReviewedBy = string.IsNullOrWhiteSpace(dto.ReviewedBy) ? "Staff" : dto.ReviewedBy.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Note))
                request.Note = dto.Note.Trim();

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Wallet top-up rejected: {RequestCode}", request.RequestCode);
            return MapToDto(request);
        }

        private static WalletTopUpRequestDto MapToDto(WalletTopUpRequest request)
        {
            return new WalletTopUpRequestDto
            {
                WalletTopUpRequestId = request.WalletTopUpRequestId,
                RequestCode = request.RequestCode,
                CustomerId = request.CustomerId,
                CustomerName = request.Customer?.FullName ?? "",
                CustomerPhone = request.Customer?.Phone ?? "",
                Amount = request.Amount,
                PaymentMethod = request.PaymentMethod,
                Status = request.Status.ToString(),
                Note = request.Note,
                RequestedAt = request.RequestedAt,
                ReviewedAt = request.ReviewedAt,
                ReviewedBy = request.ReviewedBy,
                CustomerWalletBalance = request.Customer?.WalletBalance
            };
        }

        private static string NormalizePaymentMethod(string? paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return "BankTransfer";

            return paymentMethod.Trim().Equals("BankTransfer", StringComparison.OrdinalIgnoreCase)
                ? "BankTransfer"
                : "BankTransfer";
        }
    }
}
