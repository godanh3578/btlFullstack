namespace OrderApi.DTOs.Customers
{
    public class CreateCustomerDto
    {
        public string CustomerCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
    }

    public class UpdateCustomerDto
    {
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string Status { get; set; } = "Active";
        public string MembershipTier { get; set; } = "";
    }

    public class UpdateCustomerProfileDto
    {
        public string Phone { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string Gender { get; set; } = ""; 
        public string? DateOfBirth { get; set; } // Giữ là string để nhận từ Frontend
        public string? AvatarUrl { get; set; }
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string Gender { get; set; } = "";
        public DateOnly? DateOfBirth { get; set; }
        public string? AvatarUrl { get; set; }
        public decimal TotalSpent { get; set; }
        public string MembershipTier { get; set; } = "";
        public decimal WalletBalance { get; set; }
        public decimal CurrentDebt { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CustomerPurchaseHistoryDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public decimal TotalSpent { get; set; }
        public decimal CurrentDebt { get; set; }
        public List<PurchaseHistoryItemDto> Orders { get; set; } = new();
    }

    public class PurchaseHistoryItemDto
    {
        public string OrderCode { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentStatus { get; set; } = "";
    }
}
