using CondoSphere.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Poll
    {
        public int Id { get; set; }

        [Required, StringLength(140)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        public DateTime StartsAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime EndsAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);

        public int? CondominiumId { get; set; }
        [ValidateNever] public Condominium? Condominium { get; set; }

        public bool AllowSingleChoice { get; set; } = true;

     
        [Required] public string CreatedById { get; set; } = default!;
        [ValidateNever] public User? CreatedBy { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();

        [ValidateNever]
        public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
    }
}
