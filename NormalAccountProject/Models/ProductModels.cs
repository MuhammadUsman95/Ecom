namespace NormalAccountProject.Models
{
    // ✅ Info Tab - Used for loading dropdowns and grid data
    public class nInfoTab
    {
        public string Userid { get; set; }
    }

    // ✅ Category Dropdown Model
    public class CategoryDD
    {
        public string CategoryId { get; set; }
        public string Category { get; set; }
    }

    // ✅ Vendor Dropdown Model
    public class VendorDD
    {
        public int VendorId { get; set; }
        public string Vendor { get; set; }
    }

    // ✅ Product Tab - Main Product Model
    public class ProductTab
    {
        public string? Userid { get; set; }
        public string? ProductId { get; set; }
        public string? Product { get; set; }
        public string? ProductDescription { get; set; }
        public bool IsActive { get; set; }
        public string? CategoryId { get; set; }
        public string? VendorId { get; set; }
        public string? Prices { get; set; }
        public string? DiscountAmount { get; set; }
        public bool IsUpdate { get; set; }

        // Image Fields
        public string? ProductImageAttachmentfilename { get; set; }
        public string? ProductImageAttachmentfilenameold { get; set; }
        public string? ProductImageAttachmentbase64 { get; set; }
        public string? FtpPath { get; set; }
    }

    // ✅ Product Delete Request Model
    public class ProductDeleteRequest
    {
        public string? Userid { get; set; }
        public string? ProductId { get; set; }
        public string? ProductImageAttachmentfilenameold { get; set; }
        public string? FtpPath { get; set; }
    }
}