namespace CondoSphere.Models
{
    public class Kpi
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "bi-circle";
        public string Value { get; set; } = "0";
        public string? Subtext { get; set; }
        public string Variant { get; set; } = "primary";
    }
}
