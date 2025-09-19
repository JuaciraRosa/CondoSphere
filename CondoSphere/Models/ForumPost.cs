using CondoSphere.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class ForumPost
    {
        public int Id { get; set; }

        [Required]
        public int TopicId { get; set; }
        [ValidateNever] public ForumTopic? Topic { get; set; }

        [Required, StringLength(8000)]
        public string Body { get; set; } = "";

        [Required]
        public string CreatedById { get; set; } = "";

        public User? CreatedBy { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

      

        public DateTime? EditedAtUtc { get; set; }
        public string? DeleteReason { get; set; }
        public bool IsDeleted { get; set; }
        public string? DeletedById { get; set; }

        // anexos
        public ICollection<ForumAttachment> Attachments { get; set; } = new List<ForumAttachment>();

    }

}
