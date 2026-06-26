using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public class OrderDetail
    {
        public int OrderDetailId { get; set; }

        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = "";

        [StringLength(500)]
        public string? ProductImage { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0;

        public decimal SubTotal { get; set; }
    }
}
