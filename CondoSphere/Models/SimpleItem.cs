namespace CondoSphere.Models
{
    public class SimpleItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string? Subtitle { get; set; }
        public string? Url { get; set; }
        public string? Badge { get; set; }
        public string BadgeVariant { get; set; } = "secondary";
        public DateTime? When { get; set; }
        public int? Progress { get; set; } // 0..100 if you want a progress bar
    }
}
