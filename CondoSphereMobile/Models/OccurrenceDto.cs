using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class OccurrenceDto
    {
        public string? Id { get; set; }
        public int CondominiumId { get; set; }
        public string? UnitNumber { get; set; }     // <- igual ao web
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "Open";
        public string? CreatedBy { get; set; }      // <- gravado como email
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
