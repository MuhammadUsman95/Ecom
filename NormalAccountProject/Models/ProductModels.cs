namespace NormalAccountProject.Models
{
    // Product Tab Model
    public class ProductTab
    {
        public string? Userid { get; set; }
        public string? ProductId { get; set; }
        public string? Product { get; set; }
        public string? CategoryId { get; set; }
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
        public string? CategoryIdId { get; set; }
        public string? CategoryId { get; set; }
    }
}
