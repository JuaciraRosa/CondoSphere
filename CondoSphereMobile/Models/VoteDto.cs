using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class VoteDto
    {
        public int PollId { get; set; }
        public string Question { get; set; }
        public List<string> Options { get; set; } = new();
        public string Selected { get; set; }     // para binding local
    }
}
