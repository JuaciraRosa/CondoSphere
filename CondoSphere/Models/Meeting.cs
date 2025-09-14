using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CondoSphere.Models
{
    public class Meeting
    {
        public int Id { get; set; }

        [Required, DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
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


        // NOVO
        public bool IsOnline { get; set; } = false;
        public string? OnlineProvider { get; set; }   // "Zoom" | "Teams" | "Google"
        public string? OnlineMeetingId { get; set; }  // id no provedor
        public string? OnlineJoinUrl { get; set; }    // link para participantes
        public string? OnlineStartUrl { get; set; }   // link do host (Zoom)
    }

}
