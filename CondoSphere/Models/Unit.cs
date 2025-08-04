using CondoSphere.Data;

namespace CondoSphere.Models
{
    public class Unit
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public double Area { get; set; }

        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }

        public int OwnerId { get; set; }
        public User Owner { get; set; }
    }

}
