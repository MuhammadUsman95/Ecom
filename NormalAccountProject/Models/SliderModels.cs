namespace NormalAccountProject.Models
{
    // Slider Tab Model
    public class SliderTab
    {
        public string? SliderId { get; set; }
        public string? SliderName { get; set; }
        public string? HeadingSlider { get; set; }
        public string? DescriptionSlider { get; set; }
        public string? SliderType { get; set; }
        public string? VendorId { get; set; }
        public bool IsActive { get; set; }
        public bool IsUpdate { get; set; }
        public string? Userid { get; set; }
        public string? SliderImageAttachmentfilename { get; set; }
        public string? SliderImageAttachmentfilenameold { get; set; }
        public string? SliderImageAttachmentbase64 { get; set; }
        public string? FtpPath { get; set; }
        public int SliderMovingTimer { get; set; } = 0;
    }

    // Slider Delete Request Model
    public class SliderDeleteRequest
    {
        public string? SliderId { get; set; }
        public string? Userid { get; set; }
        public string? SliderImageAttachmentfilenameold { get; set; }
        public string? FtpPath { get; set; }
    }

    // Slider Type Dropdown Model
    public class SliderTypeDD
    {
        public string? SliderType { get; set; }
    }

    // Vendor Dropdown Model (if not already exists)
    public class VendorDD1
    {
        public string? VendorId { get; set; }
        public string? Vendor { get; set; }
    }

    // Info Tab Model (if not already exists)
    public class nInfoTab1
    {
        public string? Userid { get; set; }
    }
}
