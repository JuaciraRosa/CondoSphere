using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.ViewModels
{
    public class VoteCastVM
    {
        [Required]
        public int MeetingId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string UnitNumber { get; set; } = "";

        [Required]
        public string Choice { get; set; } = "";

        public bool AlreadyVoted { get; set; }

        public IEnumerable<SelectListItem> Units { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
