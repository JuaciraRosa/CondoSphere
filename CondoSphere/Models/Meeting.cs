using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Meeting
    {
        public int Id { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime ScheduledDate { get; set; }

        [Required]
        [StringLength(300, ErrorMessage = "Agenda can't exceed 300 characters.")]
        public string Agenda { get; set; }


        [StringLength(200)]
        public string MinutesDocumentPath { get; set; }

        [Required]

        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }
    }

}
