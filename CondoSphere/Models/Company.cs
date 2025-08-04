using CondoSphere.Data;

namespace CondoSphere.Models
{
    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string TaxNumber { get; set; }
        public string Email { get; set; }
        public ICollection<User> Users { get; set; }
        public ICollection<Condominium> Condominiums { get; set; }
    }

}
