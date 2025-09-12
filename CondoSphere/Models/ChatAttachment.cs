using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class ChatAttachment
    {
        public int Id { get; set; }
        public int MessageId { get; set; }
        public ChatMessage Message { get; set; } = null!;
        [Required, StringLength(260)] public string FileName { get; set; } = "";
        [Required, StringLength(120)] public string ContentType { get; set; } = "";
        public long Size { get; set; }
        [Required, StringLength(400)] public string StoragePath { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
