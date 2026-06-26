namespace OrderApi.DTOs.Debts
{
    public class CreateDebtPaymentDto
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string Note { get; set; } = "";
    }

    public class UpdateDebtStatusDto
    {
        public string DebtStatus { get; set; } = "";
    }

    public class DebtDto
    {
        public int DebtId { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int OrderId { get; set; }
        public decimal DebtAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime? DueDate { get; set; }
        public string DebtStatus { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CustomerDebtsDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public List<DebtDto> Debts { get; set; } = new();
    }
}
