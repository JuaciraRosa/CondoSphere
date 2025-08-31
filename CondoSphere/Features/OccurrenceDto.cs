namespace CondoSphere.Features.Ocurrences
{

    public class OccurrenceDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public int CondominiumId { get; set; }
        public string UnitNumber { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "Open"; // Open, InProgress, Resolved
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        public string CreatedBy { get; set; } = ""; // email
    }
}
