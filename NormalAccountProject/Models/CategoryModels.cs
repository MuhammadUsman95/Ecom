namespace NormalAccountProject.Models
{
    // Category Tab Model
    public class CategoryTab
    {
        public string? CategoryId { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public bool IsUpdate { get; set; }
        public string? Userid { get; set; }
    }

    // Category Delete Request Model
    public class CategoryDeleteRequest
    {
        public string? CategoryId { get; set; }
        public string? Userid { get; set; }
    }

    // Info Tab Model (if not already exists)
    public class nInfoTab2
    {
        public string? Userid { get; set; }
    }
}