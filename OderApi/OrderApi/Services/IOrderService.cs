using OrderApi.DTOs.Orders;

namespace OrderApi.Services
{
    public interface IOrderService
    {
        Task<OrderDto?> GetOrderByIdAsync(int orderId);
        Task<OrderDto?> GetOrderByCodeAsync(string orderCode);
        Task<OrderDto?> LookupOrderAsync(string orderCode, string phone);
        Task<List<OrderDto>> LookupByPhoneAsync(string phone);
        Task<List<OrderDto>> GetAllOrdersAsync(
            string? search = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
        Task<List<OrderDto>> GetOrdersByCustomerIdAsync(
            int customerId,
            string? search = null,
            string? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
        Task<OrderDto> UpdateOrderStatusAsync(int orderId, string status, string? approvedBy = null);
        Task<bool> CancelOrderAsync(int orderId);
        Task<bool> CancelOrderForCustomerAsync(int orderId, string phone);
        Task<bool> DeleteOrderAsync(int orderId);
    }
}
