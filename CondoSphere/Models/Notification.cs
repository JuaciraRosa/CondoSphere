using CondoSphere.Data;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Notification
    {
        public int Id { get; set; }
        [Required]
        [StringLength(500, ErrorMessage = "Message can't exceed 500 characters.")]
        public string Message { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime SentAt { get; set; }

        [Required]
        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }

        public ICollection<User> Recipients { get; set; }
    }

}
