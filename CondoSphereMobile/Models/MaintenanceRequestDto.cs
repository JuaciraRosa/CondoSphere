using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CondoSphereMobile.Models.Enums;

namespace CondoSphereMobile.Models
{
    public class MaintenanceRequestDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public DateTime SubmittedAt { get; set; }
        public RequestStatus Status { get; set; } = RequestStatus.Open;

        public int CondominiumId { get; set; }
        public string SubmittedById { get; set; } = "";
    }
}
