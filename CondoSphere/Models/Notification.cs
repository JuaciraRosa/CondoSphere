using CondoSphere.Data;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Notification
    {
        public int Id { get; set; }
        [Required, StringLength(500)]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [Required, DataType(DataType.DateTime)]
        [Display(Name = "Sent At")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Condominium")]
        public int CondominiumId { get; set; }

        [ValidateNever]
        public Condominium? Condominium { get; set; }

        [ValidateNever]
        [Display(Name = "Recipients")]
        public ICollection<User> Recipients { get; set; } = new List<User>();
    }

}
