namespace OrderApi.DTOs.Debts
{
    public class DebtReportDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public decimal TotalDebt { get; set; }
        public decimal TotalPaid { get; set; }
        public int TotalUnpaidOrders { get; set; }
    }
}
