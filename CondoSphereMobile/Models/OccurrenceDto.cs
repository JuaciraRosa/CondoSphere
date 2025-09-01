using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class OccurrenceDto
    {
        public string Id { get; set; }           // se o teu for string (GUID) no JSON file
        public int CondominiumId { get; set; }
        public string UnitNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }       // Open / InProgress / Resolved
        public DateTime CreatedAt { get; set; }
    }
}
