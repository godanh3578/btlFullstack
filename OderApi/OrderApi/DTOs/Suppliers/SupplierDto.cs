namespace OrderApi.DTOs.Suppliers
{
    public class CreateSupplierDto
    {
        public string SupplierCode { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string TaxCode { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public class UpdateSupplierDto
    {
        public string SupplierName { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string TaxCode { get; set; } = "";
        public string Note { get; set; } = "";
        public string Status { get; set; } = "Active";
    }

    public class SupplierDto
    {
        public int SupplierId { get; set; }
        public string SupplierCode { get; set; } = "";
        public string SupplierName { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string TaxCode { get; set; } = "";
        public string Note { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
