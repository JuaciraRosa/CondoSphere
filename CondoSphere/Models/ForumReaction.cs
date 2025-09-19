namespace CondoSphere.Models
{
    public class ForumReaction
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string UserId { get; set; } = default!;
        public string Emoji { get; set; } = "👍";
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

}
