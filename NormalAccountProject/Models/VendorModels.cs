namespace NormalAccountProject.Models
{
    // ── Save / Update Request ─────────────────────────────────────────────
    public class VendorTab
    {
        public string? Userid { get; set; }
        public string? VendorCode { get; set; }
        public string? Vendor { get; set; }
        public string? ContactNo { get; set; }
        public bool IsActive { get; set; }
        public bool IsUpdate { get; set; }
    }

    // ── Delete Request ────────────────────────────────────────────────────
    public class VendorDeleteRequest
    {
        public string? Userid { get; set; }
        public string? VendorCode { get; set; }
    }
}
