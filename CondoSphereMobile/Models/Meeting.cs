using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class Meeting
    {
        public int Id { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Agenda { get; set; }


        [StringLength(200)]
        [Display(Name = "Meeting Minutes (Ata da Reunião)")]
        public string MinutesDocumentPath { get; set; }
        public int CondominiumId { get; set; }
    }
}
