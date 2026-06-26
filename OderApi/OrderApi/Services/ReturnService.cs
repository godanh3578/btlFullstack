using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.DTOs.Returns;
using OrderApi.Models;

namespace OrderApi.Services
{
    public class ReturnService : IReturnService
    {
        private readonly OrderDbContext _context;

        public ReturnService(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<ReturnDto?> GetByIdAsync(int returnId)
        {
            var r = await _context.Returns
                .Include(x => x.Order)
                .Include(x => x.Customer)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.ReturnId == returnId);

            return r == null ? null : ToDto(r);
        }

        public async Task<List<ReturnDto>> GetAllAsync(string? search = null, string? status = null)
        {
            var query = _context.Returns
                .Include(x => x.Order)
                .Include(x => x.Customer)
                .Include(x => x.Items)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(r =>
                    r.ReturnCode.Contains(term) ||
                    (r.Order != null && r.Order.OrderCode.Contains(term)) ||
                    (r.Customer != null && r.Customer.FullName.Contains(term)) ||
                    (r.Customer != null && r.Customer.Phone.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ReturnStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(r => r.ReturnStatus == parsedStatus);
            }

            var list = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<List<ReturnDto>> GetByOrderIdAsync(int orderId)
        {
            var list = await _context.Returns
                .Include(x => x.Order)
                .Include(x => x.Customer)
                .Include(x => x.Items)
                .Where(r => r.OrderId == orderId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return list.Select(ToDto).ToList();
        }

        public async Task<List<ReturnDto>> GetByCustomerIdAsync(int customerId)
        {
            var list = await _context.Returns
                .Include(x => x.Order)
                .Include(x => x.Customer)
                .Include(x => x.Items)
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return list.Select(ToDto).ToList();
        }

        public async Task<ReturnDto> CreateAsync(CreateReturnDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId)
                ?? throw new KeyNotFoundException($"Không tìm thấy đơn hàng #{dto.OrderId}.");

            var customer = await _context.Customers.FindAsync(dto.CustomerId)
                ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng #{dto.CustomerId}.");

            var existingPending = await _context.Returns
                .AnyAsync(r => r.OrderId == dto.OrderId && r.ReturnStatus == ReturnStatus.Pending);
            if (existingPending)
                throw new InvalidOperationException("Đơn hàng này đã có yêu cầu hoàn hàng đang chờ xử lý.");

            var returnCode = $"RT{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}";

            var ret = new Return
            {
                ReturnCode = returnCode,
                OrderId = dto.OrderId,
                CustomerId = dto.CustomerId,
                ReturnDate = DateTime.UtcNow,
                RefundAmount = dto.RefundAmount,
                Reason = dto.Reason,
                ReturnStatus = ReturnStatus.Pending,
                CreatedBy = dto.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                Items = dto.Items.Select(i => new ReturnDetail
                {
                    ProductId = i.ProductId,
                    ProductCode = i.ProductCode,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    SubTotal = i.UnitPrice * i.Quantity,
                }).ToList()
            };

            _context.Returns.Add(ret);
            await _context.SaveChangesAsync();

            await _context.Entry(ret).Reference(r => r.Order).LoadAsync();
            await _context.Entry(ret).Reference(r => r.Customer).LoadAsync();

            return ToDto(ret);
        }

        public async Task<ReturnDto> UpdateStatusAsync(int returnId, string status)
        {
            var ret = await _context.Returns
                .Include(r => r.Order)
                .Include(r => r.Customer)
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.ReturnId == returnId)
                ?? throw new KeyNotFoundException($"Không tìm thấy phiếu hoàn hàng #{returnId}.");

            if (!Enum.TryParse<ReturnStatus>(status, true, out var newStatus))
                throw new InvalidOperationException($"Trạng thái '{status}' không hợp lệ.");

            ret.ReturnStatus = newStatus;
            await _context.SaveChangesAsync();

            return ToDto(ret);
        }

        private static ReturnDto ToDto(Return r) => new()
        {
            ReturnId = r.ReturnId,
            ReturnCode = r.ReturnCode,
            OrderId = r.OrderId,
            OrderCode = r.Order?.OrderCode ?? "",
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.FullName ?? "",
            CustomerPhone = r.Customer?.Phone ?? "",
            ReturnDate = r.ReturnDate,
            RefundAmount = r.RefundAmount,
            Reason = r.Reason,
            ReturnStatus = r.ReturnStatus.ToString(),
            CreatedBy = r.CreatedBy,
            CreatedAt = r.CreatedAt,
            Items = r.Items.Select(i => new ReturnItemDto
            {
                ReturnDetailId = i.ReturnDetailId,
                ProductId = i.ProductId,
                ProductCode = i.ProductCode,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
            }).ToList()
        };
    }
}
