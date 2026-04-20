namespace NormalAccountProject.Models
{
    // ── Save / Update Request ──────────────────────────────────────────────
    public class CustomerTab
    {
        public string? Userid { get; set; }
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public string? ContactNo { get; set; }
        public bool IsActive { get; set; }
        public bool IsUpdate { get; set; }
    }

    // ── Delete Request ────────────────────────────────────────────────────
    public class CustomerDeleteRequest
    {
        public string? Userid { get; set; }
        public string? CustomerCode { get; set; }
    }
}
