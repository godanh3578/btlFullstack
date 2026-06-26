using OrderApi.DTOs.SalesInvoices;

namespace OrderApi.Services
{
    public interface ISalesInvoiceService
    {
        Task<SalesInvoiceDto?> GetByIdAsync(int invoiceId);
        Task<SalesInvoiceDto?> GetByOrderIdAsync(int orderId);
        Task<List<SalesInvoiceDto>> GetAllAsync(int? customerId = null, string? status = null);
        Task<SalesInvoiceDto> CreateOrGetAsync(CreateSalesInvoiceDto dto);
    }
}
