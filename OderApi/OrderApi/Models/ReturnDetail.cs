using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public class ReturnDetail
    {
        public int ReturnDetailId { get; set; }

        [Required]
        public int ReturnId { get; set; }
        public Return? Return { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string ProductCode { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = "";

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal SubTotal { get; set; }
    }
}
