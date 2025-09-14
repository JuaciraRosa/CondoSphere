using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class PollOption
    {
        public int Id { get; set; }
        public int PollId { get; set; }
        [ValidateNever] public Poll? Poll { get; set; }

        [Required, StringLength(160)]
        public string Text { get; set; } = string.Empty;

        [ValidateNever]
        public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
    }
}
