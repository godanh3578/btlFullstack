namespace OrderApi.DTOs.Payments
{
    public class CreatePaymentDto
    {
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public decimal Amount { get; set; }
        public string Note { get; set; } = "";
    }

    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string PaymentCode { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentStatus { get; set; } = "";
        public string Note { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
