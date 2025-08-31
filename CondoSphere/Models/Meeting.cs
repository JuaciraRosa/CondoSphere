using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CondoSphere.Models
{
    public class Meeting
    {
        public int Id { get; set; }

        [Required, DataType(DataType.DateTime)]
        [Display(Name = "Scheduled Date")]
        public DateTime ScheduledDate { get; set; }

        [Required, StringLength(300)]
        [Display(Name = "Agenda")]
        public string Agenda { get; set; } = string.Empty;

        [StringLength(400)]
        [Display(Name = "Minutes Document")]
        public string? MinutesDocumentPath { get; set; }


        [NotMapped]
        [Display(Name = "Minutes File (PDF/Word)")]
        public IFormFile? MinutesFile { get; set; }        // só para binding no form

        [Required]
        [Display(Name = "Condominium")]
        public int CondominiumId { get; set; }

        [ValidateNever]
        public Condominium? Condominium { get; set; }
    }

}
