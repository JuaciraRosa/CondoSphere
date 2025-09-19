using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class ForumCategory
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Slug { get; set; }

        public bool IsLocked { get; set; }
        public int SortOrder { get; set; } = 0;

        public int? CompanyId { get; set; }           // escopo por empresa
        public int? CondominiumId { get; set; }       // escopo por condomínio

        [ValidateNever]
        public ICollection<ForumTopic> Topics { get; set; } = new List<ForumTopic>();
    }
}
