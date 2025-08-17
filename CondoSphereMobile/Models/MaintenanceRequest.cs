using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime SubmittedAt { get; set; }
        public RequestStatus Status { get; set; }
        public int CondominiumId { get; set; }
        public int SubmittedById { get; set; }
    }
}
