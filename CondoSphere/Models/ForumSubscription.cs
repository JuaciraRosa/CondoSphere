
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class ForumSubscription
    {
        public int Id { get; set; }

        [Required]
        public int TopicId { get; set; }

        [Required]
        public string UserId { get; set; } = default!;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
