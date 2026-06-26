using OrderApi.DTOs.Debts;

namespace OrderApi.Services
{
    public interface IDebtService
    {
        Task<DebtDto?> GetDebtByIdAsync(int debtId);
        Task<CustomerDebtsDto> GetDebtsByCustomerIdAsync(int customerId);
        Task<List<DebtDto>> GetAllDebtsAsync();
        Task<DebtDto> PayDebtAsync(int debtId, CreateDebtPaymentDto dto);
        Task<DebtDto> UpdateDebtStatusAsync(int debtId, UpdateDebtStatusDto dto);
        Task<List<DebtReportDto>> GetDebtReportAsync();
    }
}
