using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class MeetingDocumentItem
    {
        public int Id { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Agenda { get; set; }
        public bool HasDocument { get; set; }
        public string DownloadUrl { get; set; }
    }
}
