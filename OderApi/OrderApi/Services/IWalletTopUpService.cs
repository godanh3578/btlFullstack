using OrderApi.DTOs.WalletTopUps;

namespace OrderApi.Services
{
    public interface IWalletTopUpService
    {
        Task<List<WalletTopUpRequestDto>> GetAllAsync(string? status = null);
        Task<List<WalletTopUpRequestDto>> GetByCustomerIdAsync(int customerId);
        Task<WalletTopUpRequestDto?> GetByIdAsync(int id);
        Task<WalletTopUpRequestDto> CreateAsync(CreateWalletTopUpRequestDto dto);
        Task<WalletTopUpRequestDto> ApproveAsync(int id, ReviewWalletTopUpRequestDto dto);
        Task<WalletTopUpRequestDto> RejectAsync(int id, ReviewWalletTopUpRequestDto dto);
    }
}
