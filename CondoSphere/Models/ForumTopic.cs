using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class ForumTopic
    {
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }
        [ValidateNever] public ForumCategory? Category { get; set; }

        [Required, StringLength(140)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string CreatedById { get; set; } = default!;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public bool IsLocked { get; set; }
        public bool IsPinned { get; set; }
        public int ViewsCount { get; set; } = 0;

        public int? CompanyId { get; set; }           // escopo por empresa
        public int? CondominiumId { get; set; }       // escopo por condomínio

        public DateTime LastPostAtUtc { get; set; } = DateTime.UtcNow;

        [ValidateNever]
        public ICollection<ForumPost> Posts { get; set; } = new List<ForumPost>();

        [ValidateNever]
        public ICollection<ForumSubscription> Subscriptions { get; set; } = new List<ForumSubscription>();
    }
}
