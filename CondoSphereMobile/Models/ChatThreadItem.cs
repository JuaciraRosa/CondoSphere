using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class ChatThreadItem
    {
        public int Id { get; set; }
        public string Subject { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public int UnreadForMe { get; set; }
        public string? LastPreview { get; set; }
        public bool HasAttachments { get; set; }
    }
}
