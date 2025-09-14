using CondoSphere.Models;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.ViewModels
{
    public class PollEditVm
    {
        public int Id { get; set; }

        [Required, StringLength(140)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int CondominiumId { get; set; }

        public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime EndsAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);

        public bool AllowSingleChoice { get; set; } = true;

        // exibir opções existentes (somente leitura aqui)
        public List<PollOption> ExistingOptions { get; set; } = new();

        // permitir adicionar novas (não removemos as antigas se tiverem votos)
        public List<string> NewOptions { get; set; } = new();
    }
}
