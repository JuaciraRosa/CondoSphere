using CondoSphere.Data;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Title can't exceed 100 characters.")]
        public string Title { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Description can't exceed 500 characters.")]
        public string Description { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime SubmittedAt { get; set; }

        [Required]
        public RequestStatus Status { get; set; }

        [Required]
        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }


        [Required]
        public string SubmittedById { get; set; }
        public User SubmittedBy { get; set; }
    }

}
