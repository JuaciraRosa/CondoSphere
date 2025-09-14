using CondoSphere.Data;

namespace CondoSphere.Models
{
    public class AnnouncementRead
    {
        public int Id { get; set; }
        public int AnnouncementId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime ReadAtUtc { get; set; } = DateTime.UtcNow;

        public Announcement? Announcement { get; set; }
        public User? User { get; set; }
    }
}
