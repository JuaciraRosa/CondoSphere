using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class ChatThread
    {
        public int Id { get; set; }
        public string ResidentId { get; set; } = "";
        public int? CondominiumId { get; set; }
        public string Subject { get; set; } = "Suporte";
        public string Status { get; set; } = "Open";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

        // NOVOS
        public int UnreadForAdmin { get; set; }
        public int UnreadForResident { get; set; }
        [StringLength(140)]
        public string? LastPreview { get; set; }
        public bool HasAttachments { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
