using CondoSphere.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Request Title")]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required, DataType(DataType.DateTime)]
        [Display(Name = "Submitted At")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Status")]
        public RequestStatus Status { get; set; } = RequestStatus.Open;


        [Required]
        [Display(Name = "Condominium")]
        public int CondominiumId { get; set; }

        [ValidateNever]
        public Condominium? Condominium { get; set; }

        [Required]
        [Display(Name = "Submitted By")]
        public string SubmittedById { get; set; } = string.Empty;

        [ValidateNever]
        public User? SubmittedBy { get; set; }
    }

}
