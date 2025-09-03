using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class MessageDto
    {
        public string Id { get; set; } = "";
        public string FromEmail { get; set; } = "";
        public string To { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
