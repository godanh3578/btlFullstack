using System.ComponentModel.DataAnnotations;

namespace OrderApi.DTOs.Returns
{
    public class CreateReturnItemDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class CreateReturnDto
    {
        [Required]
        public int OrderId { get; set; }
        [Required]
        public int CustomerId { get; set; }
        public string? Reason { get; set; }
        [Range(0, double.MaxValue)]
        public decimal RefundAmount { get; set; }
        public string CreatedBy { get; set; } = "system";
        public List<CreateReturnItemDto> Items { get; set; } = new();
    }

    public class UpdateReturnStatusDto
    {
        [Required]
        public string Status { get; set; } = "";
    }
}
