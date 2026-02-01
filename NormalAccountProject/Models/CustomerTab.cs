namespace NormalAccountProject.Models
{
    public class CustomerTab
    {
        public string? CustomerId { get; set; }
        public string? Customer { get; set; }
        public string? ContactNo { get; set; }
        public bool IsActive { get; set; }
        public string? Type { get; set; }
        public string? Userid { get; set; }
        public bool IsUpdate { get; set; }
        public string? CustomerImageAttachmentfilename { get; set; }
        public string? CustomerImageAttachmentfilenameold { get; set; }
        public string? CustomerImageAttachmentbase64 { get; set; }

        public string? TimeIn { get; set; }
        public string? TimeOut { get; set; }

        public string? DeliveryCharges { get; set; }
        public string? PerProductAmount { get; set; }

        public string? DepartmentId { get; set; }
        public string? Department { get; set; }
    }

    public class CustomerTypedd
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
