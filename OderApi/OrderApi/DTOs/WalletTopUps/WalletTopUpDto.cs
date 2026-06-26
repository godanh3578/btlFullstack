namespace OrderApi.DTOs.WalletTopUps
{
    public class CreateWalletTopUpRequestDto
    {
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "BankTransfer";
        public string Note { get; set; } = "";
    }

    public class ReviewWalletTopUpRequestDto
    {
        public string ReviewedBy { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public class WalletTopUpRequestDto
    {
        public int WalletTopUpRequestId { get; set; }
        public string RequestCode { get; set; } = "";
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "";
        public string Status { get; set; } = "";
        public string Note { get; set; } = "";
        public DateTime RequestedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string ReviewedBy { get; set; } = "";
        public decimal? CustomerWalletBalance { get; set; }
    }
}
