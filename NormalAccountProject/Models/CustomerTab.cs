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

    }

    public class CustomerTypedd
    {
        public string? TypeId { get; set; }
        public string? Type { get; set; }
    }

}
