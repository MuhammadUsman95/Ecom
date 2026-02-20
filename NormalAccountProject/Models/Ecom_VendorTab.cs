namespace NormalAccountProject.Models
{
    public class Ecom_VendorTab
    {
        public string? VendorId { get; set; }
        public string? Vendor { get; set; }
        public string? ContactNo { get; set; }
        public bool IsActive { get; set; }
        public string? Type { get; set; }
        public string? Userid { get; set; }
        public bool IsUpdate { get; set; }

        public string? Address { get; set; }
        public string? VendorImageAttachmentfilename { get; set; }
        public string? VendorImageAttachmentfilenameold { get; set; }
        public string? VendorImageAttachmentbase64 { get; set; }

        public string? TimeIn { get; set; }
        public string? TimeOut { get; set; }

        public string? DeliveryCharges { get; set; }
        public string? PerProductAmount { get; set; }

        public string? DepartmentId { get; set; }
        public string? Department { get; set; }
    }

    public class VendorTypedd
    {
        public string? TypeId { get; set; }
        public string? Type { get; set; }
    }

    public class DepartmentDD
    {
        public int DepartmentId { get; set; }
        public string Department { get; set; }
    }
}
