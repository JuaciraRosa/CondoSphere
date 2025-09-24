using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class ChatMessageItem
    {
        public int Id { get; set; }
        public string Role { get; set; } = "";
        public string? UserId { get; set; }
        public string Text { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
