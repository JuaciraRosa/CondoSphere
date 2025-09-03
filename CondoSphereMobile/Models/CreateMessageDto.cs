using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class CreateMessageDto
    {
        public string? To { get; set; }   // opcional, default "admin@condosphere-web-app.somee.com" no backend
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
    }
}
