namespace NormalAccountProject.Models
{
    // Product Tab Model
    public class ProductTab
    {
        public string? Userid { get; set; }
        public string? ProductId { get; set; }
        public string? Product { get; set; }
        public string? CategoryId { get; set; }
        public string? VendorId { get; set; }  // ✅ Added
        public bool IsActive { get; set; }
        public string? ProductImageAttachmentfilename { get; set; }
        public string? ProductImageAttachmentfilenameold { get; set; }
        public string? ProductImageAttachmentbase64 { get; set; }
        public string? Prices { get; set; }
        public string? DiscountAmount { get; set; }
        public bool IsUpdate { get; set; }
    }

    // Category Dropdown Model
    public class CategoryDD
    {
        public string? CategoryId { get; set; }
        public string? Category { get; set; }
    }

    // ✅ Vendor Dropdown Model (NEW)
    public class VendorDD
    {
        public string? VendorId { get; set; }
        public string? Vendor { get; set; }
    }
}