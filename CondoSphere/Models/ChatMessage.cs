using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public ChatRole Role { get; set; }
        public string? UserId { get; set; }
        [Required, StringLength(4000)]
        public string Text { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // NOVOS
        public bool IsReadByAdmin { get; set; }
        public bool IsReadByResident { get; set; }

        public ChatThread? Thread { get; set; }
        public ICollection<ChatAttachment> Attachments { get; set; } = new List<ChatAttachment>();
    }
}
