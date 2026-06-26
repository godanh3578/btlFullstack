using System.ComponentModel.DataAnnotations;

namespace OrderApi.Models
{
    public enum SupplierStatus
    {
        Active,
        Inactive,
        Blocked
    }

    public class Supplier
    {
        public int SupplierId { get; set; }

        [Required]
        [StringLength(50)]
        public string SupplierCode { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string SupplierName { get; set; } = "";

        [StringLength(200)]
        public string ContactPerson { get; set; } = "";

        [Phone]
        [StringLength(20)]
        public string Phone { get; set; } = "";

        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = "";

        [StringLength(500)]
        public string Address { get; set; } = "";

        [StringLength(20)]
        public string TaxCode { get; set; } = "";

        [StringLength(500)]
        public string Note { get; set; } = "";

        public SupplierStatus Status { get; set; } = SupplierStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
