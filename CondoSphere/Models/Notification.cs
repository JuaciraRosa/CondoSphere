using CondoSphere.Data;

namespace CondoSphere.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime SentAt { get; set; }

        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }

        public ICollection<User> Recipients { get; set; }
    }

}
