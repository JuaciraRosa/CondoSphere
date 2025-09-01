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
        public string Choice { get; set; } = ""; // "A favor" | "Contra" | "Abstenção"

        // Dropdown de unidades
        public IEnumerable<SelectListItem> Units { get; set; } = new List<SelectListItem>();

        // Para travar o formulário e mostrar a mensagem
        public bool AlreadyVoted { get; set; }
    }
}
