using System.ComponentModel.DataAnnotations;

namespace CondoSphere.ViewModels
{
    public class PollCreateVm
    {
        [Required, StringLength(140)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int CondominiumId { get; set; }

        public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime EndsAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);

        public bool AllowSingleChoice { get; set; } = true;

        // uma string por opção
        public List<string> Options { get; set; } = new();
    }
}
