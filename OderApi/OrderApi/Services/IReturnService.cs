using OrderApi.DTOs.Returns;

namespace OrderApi.Services
{
    public interface IReturnService
    {
        Task<ReturnDto?> GetByIdAsync(int returnId);
        Task<List<ReturnDto>> GetAllAsync(string? search = null, string? status = null);
        Task<List<ReturnDto>> GetByOrderIdAsync(int orderId);
        Task<List<ReturnDto>> GetByCustomerIdAsync(int customerId);
        Task<ReturnDto> CreateAsync(CreateReturnDto dto);
        Task<ReturnDto> UpdateStatusAsync(int returnId, string status);
    }
}
