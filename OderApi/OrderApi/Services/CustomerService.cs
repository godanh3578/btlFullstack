using OrderApi.Data;
using OrderApi.DTOs.Customers;
using OrderApi.Models;
using Microsoft.EntityFrameworkCore;

namespace OrderApi.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly OrderDbContext _dbContext;
        private readonly ILogger<CustomerService> _logger;
        private static readonly HashSet<string> AllowedMembershipTiers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Thường",
            "Bạc",
            "Vàng",
            "Bạch Kim",
            "Kim Cương"
        };

        public CustomerService(OrderDbContext dbContext, ILogger<CustomerService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<CustomerDto> GetCustomerProfileAsync(int customerId)
    {
        // Tìm customer trong Database bao gồm cả trường Ngày sinh (DateTime?)
        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    
         if (customer == null)
        throw new KeyNotFoundException($"Không tìm thấy khách hàng với ID: {customerId}");

    // Trả về dữ liệu đã được map qua hàm MapToDto cực kỳ an toàn
        if (customer.Status != CustomerStatus.Active)
            throw new InvalidOperationException("Tai khoan khach hang da bi khoa hoac ngung hoat dong.");

        return MapToDto(customer);
    }

        public async Task<CustomerDto?> GetCustomerByCodeAsync(string customerCode)
        {
            var customer = await _dbContext.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerCode == customerCode);

            if (customer == null)
                return null;

            return MapToDto(customer);
        }

        public async Task<CustomerDto?> GetCustomerByPhoneAsync(string phone)
        {
            var normalizedPhone = NormalizePhone(phone);
            var customer = await _dbContext.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Phone == normalizedPhone);

            if (customer == null)
                return null;

            return MapToDto(customer);
        }

        public async Task<CustomerDto?> GetCustomerByEmailAsync(string email)
        {
            var normalizedEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalizedEmail))
                return null;

            var customer = await _dbContext.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Email.ToLower() == normalizedEmail);

            if (customer == null)
                return null;

            return MapToDto(customer);
        }

        public async Task<List<CustomerDto>> GetAllCustomersAsync()
        {
            var customers = await _dbContext.Customers
                .Include(c => c.Orders)
                .ToListAsync();

            return customers.Select(MapToDto).ToList();
        }

        public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto)
        {
            dto.FullName = dto.FullName.Trim();
            dto.Phone = NormalizePhone(dto.Phone);
            dto.Email = NormalizeEmail(dto.Email);
            dto.Address = dto.Address.Trim();

            if (string.IsNullOrWhiteSpace(dto.CustomerCode))
            {
                var count = await _dbContext.Customers.IgnoreQueryFilters().CountAsync();
                dto.CustomerCode = $"KH{(count + 1):D6}";
            }

            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                var normalizedPhoneExists = await _dbContext.Customers
                    .IgnoreQueryFilters()
                    .AnyAsync(c => c.Phone == dto.Phone);
                if (normalizedPhoneExists)
                    throw new InvalidOperationException("DUPLICATE_PHONE: So dien thoai nay da duoc dang ky.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var normalizedEmailExists = await _dbContext.Customers
                    .IgnoreQueryFilters()
                    .AnyAsync(c => c.Email.ToLower() == dto.Email);
                if (normalizedEmailExists)
                    throw new InvalidOperationException("DUPLICATE_EMAIL: Email nay da duoc dang ky.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Phone))
            {
                var phoneExists = await _dbContext.Customers
                    .AnyAsync(c => c.Phone == dto.Phone.Trim());
                if (phoneExists)
                    throw new InvalidOperationException("Số điện thoại đã được đăng ký.");
            }

            var existing = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.CustomerCode == dto.CustomerCode);

            if (existing != null)
                throw new InvalidOperationException($"Customer code {dto.CustomerCode} already exists");

            var customer = new Customers
            {
                CustomerCode = dto.CustomerCode,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                MembershipTier = "Thường",
                Status = CustomerStatus.Active
            };

            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Customer created: {dto.CustomerCode}");

            return MapToDto(customer);
        }

        public async Task<CustomerDto> UpdateCustomerAsync(int customerId, UpdateCustomerDto dto)
        {
            var customer = await _dbContext.Customers.FindAsync(customerId);
            if (customer == null)
                throw new KeyNotFoundException($"Customer {customerId} not found");

            customer.FullName = dto.FullName;
            customer.Phone = dto.Phone;
            customer.Email = dto.Email;
            customer.Address = dto.Address;

            var membershipTier = NormalizeMembershipTier(dto.MembershipTier);
            if (!string.IsNullOrWhiteSpace(membershipTier))
                customer.MembershipTier = membershipTier;

            if (Enum.TryParse<CustomerStatus>(dto.Status, out var status))
                customer.Status = status;

            customer.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Customer {customerId} updated");

            return MapToDto(customer);
        }

      
     
   public async Task<CustomerDto> UpdateCustomerProfileAsync(int customerId, UpdateCustomerProfileDto dto)
{
    Console.WriteLine($"DateOfBirth received: {dto.DateOfBirth}");
    var customer = await _dbContext.Customers.FindAsync(customerId);
    if (customer == null)
        throw new KeyNotFoundException($"Customer {customerId} not found");

    var newPhone = (dto.Phone ?? string.Empty).Trim();
    if (string.IsNullOrWhiteSpace(newPhone))
        throw new InvalidOperationException("Số điện thoại không được để trống.");

    if (!string.Equals(customer.Phone, newPhone, StringComparison.Ordinal))
    {
        var phoneExists = await _dbContext.Customers
            .AnyAsync(c => c.CustomerId != customerId && c.Phone == newPhone);

        if (phoneExists)
            throw new InvalidOperationException("Số điện thoại đã được đăng ký.");
    }

    customer.FullName = dto.FullName;
    customer.Phone = newPhone;
    customer.Email = dto.Email;
    customer.Address = dto.Address;
    customer.Gender = dto.Gender ?? string.Empty;

    // Ép kiểu chuỗi string thô từ DTO sang DateTime? cho Entity
    if (!string.IsNullOrWhiteSpace(dto.DateOfBirth))
        {
        if (DateTime.TryParse(dto.DateOfBirth, out DateTime parsedDate))
        {
            customer.DateOfBirth = DateTime.TryParse(dto.DateOfBirth, out var dob) ? dob : null;
        }
        else
        {
            customer.DateOfBirth = null; 
        }
    }
    else
    {
        customer.DateOfBirth = null; 
    }

    customer.UpdatedAt = DateTime.UtcNow;
    
    // Lưu các thay đổi vào cơ sở dữ liệu
    await _dbContext.SaveChangesAsync();

    // Trả về kết quả map thông qua hàm MapToDto
    return MapToDto(customer);
    }
           public async Task<bool> DeleteCustomerAsync(int customerId)
        {
            var customer = await _dbContext.Customers.FindAsync(customerId);
            if (customer == null)
                return false;

            var hasOrders = await _dbContext.Orders
                .IgnoreQueryFilters()
                .AnyAsync(o => o.CustomerId == customerId);
            if (hasOrders)
                throw new InvalidOperationException("Khong the xoa khach hang da co don hang.");

            var hasDebt = customer.CurrentDebt > 0
                || await _dbContext.Debts.AnyAsync(d =>
                    d.CustomerId == customerId &&
                    d.DebtStatus != DebtStatus.Paid &&
                    d.DebtAmount > d.PaidAmount);
            if (hasDebt)
                throw new InvalidOperationException("Khong the xoa khach hang con cong no.");

            _dbContext.Customers.Remove(customer);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Customer {customerId} deleted");
            return true;
        }

        public async Task<CustomerPurchaseHistoryDto> GetPurchaseHistoryAsync(int customerId)
        {
            var customer = await _dbContext.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
                throw new KeyNotFoundException($"Customer {customerId} not found");

            var orders = customer.Orders
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new PurchaseHistoryItemDto
                {
                    OrderCode = o.OrderCode,
                    OrderDate = o.OrderDate,
                    FinalAmount = o.FinalAmount,
                    PaymentStatus = o.PaymentStatus.ToString()
                }).ToList();

            return new CustomerPurchaseHistoryDto
            {
                CustomerId = customer.CustomerId,
                CustomerName = customer.FullName,
                TotalSpent = customer.TotalSpent,
                CurrentDebt = customer.CurrentDebt,
                Orders = orders
            };
        }

private CustomerDto MapToDto(Customers customer)
{
    return new CustomerDto
    {
        CustomerId = customer.CustomerId,
        CustomerCode = customer.CustomerCode,
        FullName = customer.FullName,
        Phone = customer.Phone,
        Email = customer.Email,
        Address = customer.Address,
        Gender = customer.Gender,
        AvatarUrl = customer.AvatarUrl,
        // 🛠️ GIẢI PHÁP AN TOÀN: Kiểm tra kỹ lưỡng trước khi bóc tách DateOnly
        DateOfBirth = (customer.DateOfBirth != null && customer.DateOfBirth != DateTime.MinValue)
            ? DateOnly.FromDateTime(customer.DateOfBirth.Value)
            : null,
            
        TotalSpent = customer.TotalSpent,
        MembershipTier = customer.MembershipTier,
        WalletBalance = customer.WalletBalance,
        CurrentDebt = customer.CurrentDebt,
        Status = customer.Status.ToString() ?? "Active",
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt
    };
}

        private static string NormalizeMembershipTier(string? tier)
        {
            if (string.IsNullOrWhiteSpace(tier))
                return "";

            var normalized = tier.Trim();
            var codeTier = normalized.ToUpperInvariant() switch
            {
                "DEFAULT" => "Thường",
                "SILVER" => "Bạc",
                "GOLD" => "Vàng",
                "PLATINUM" => "Bạch Kim",
                "DIAMOND" => "Kim Cương",
                _ => normalized
            };
            return AllowedMembershipTiers.FirstOrDefault(t => string.Equals(t, codeTier, StringComparison.OrdinalIgnoreCase)) ?? "";
        }

        private static string NormalizeEmail(string? email)
        {
            return (email ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static string NormalizePhone(string? phone)
        {
            return (phone ?? string.Empty).Trim();
        }

        public async Task<CustomerDto?> GetCustomerByIdAsync(int customerId)
        {
            var customer = await _dbContext.Customers.FindAsync(customerId);
            return customer == null ? null : MapToDto(customer);
        }
    }
}
