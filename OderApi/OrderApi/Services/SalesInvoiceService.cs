using Microsoft.EntityFrameworkCore;
using OrderApi.Data;
using OrderApi.DTOs.SalesInvoices;
using OrderApi.Models;

namespace OrderApi.Services
{
    public class SalesInvoiceService : ISalesInvoiceService
    {
        private readonly OrderDbContext _context;

        public SalesInvoiceService(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<SalesInvoiceDto?> GetByIdAsync(int invoiceId)
        {
            var inv = await _context.SalesInvoices
                .Include(i => i.Order)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

            return inv == null ? null : ToDto(inv);
        }

        public async Task<SalesInvoiceDto?> GetByOrderIdAsync(int orderId)
        {
            var inv = await _context.SalesInvoices
                .Include(i => i.Order)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.OrderId == orderId);

            return inv == null ? null : ToDto(inv);
        }

        public async Task<List<SalesInvoiceDto>> GetAllAsync(int? customerId = null, string? status = null)
        {
            var query = _context.SalesInvoices
                .Include(i => i.Order)
                .Include(i => i.Customer)
                .AsQueryable();

            if (customerId.HasValue)
                query = query.Where(i => i.CustomerId == customerId.Value);

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<PaymentStatus>(status, true, out var ps))
            {
                query = query.Where(i => i.PaymentStatus == ps);
            }

            var list = await query.OrderByDescending(i => i.IssuedDate).ToListAsync();
            return list.Select(ToDto).ToList();
        }

        public async Task<SalesInvoiceDto> CreateOrGetAsync(CreateSalesInvoiceDto dto)
        {
            var existing = await _context.SalesInvoices
                .Include(i => i.Order)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.OrderId == dto.OrderId);

            if (existing != null)
                return ToDto(existing);

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId)
                ?? throw new KeyNotFoundException($"Không tìm thấy đơn hàng #{dto.OrderId}.");

            var customer = await _context.Customers.FindAsync(dto.CustomerId)
                ?? throw new KeyNotFoundException($"Không tìm thấy khách hàng #{dto.CustomerId}.");

            var invoiceCode = $"INV{DateTime.UtcNow:yyyyMMddHHmmss}";

            var invoice = new SalesInvoice
            {
                InvoiceCode = invoiceCode,
                OrderId = dto.OrderId,
                CustomerId = dto.CustomerId,
                IssuedDate = DateTime.UtcNow,
                TotalAmount = order.TotalAmount,
                DiscountAmount = order.DiscountAmount,
                FinalAmount = order.FinalAmount,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = DateTime.UtcNow,
            };

            _context.SalesInvoices.Add(invoice);
            await _context.SaveChangesAsync();

            invoice.Order = order;
            invoice.Customer = customer;

            return ToDto(invoice);
        }

        private static SalesInvoiceDto ToDto(SalesInvoice i) => new()
        {
            InvoiceId = i.InvoiceId,
            InvoiceCode = i.InvoiceCode,
            OrderId = i.OrderId,
            OrderCode = i.Order?.OrderCode ?? "",
            CustomerId = i.CustomerId,
            CustomerName = i.Customer?.FullName ?? "",
            CustomerPhone = i.Customer?.Phone ?? "",
            CustomerAddress = i.Customer?.Address ?? "",
            IssuedDate = i.IssuedDate,
            TotalAmount = i.TotalAmount,
            DiscountAmount = i.DiscountAmount,
            FinalAmount = i.FinalAmount,
            PaymentStatus = i.PaymentStatus.ToString(),
            CreatedAt = i.CreatedAt,
        };
    }
}
