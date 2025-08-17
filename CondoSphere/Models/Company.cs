using CondoSphere.Data;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Name can't exceed 100 characters.")]
        public string Name { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Tax Number can't exceed 20 characters.")]
        public string TaxNumber { get; set; }


        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }


        public ICollection<User> Users { get; set; }
        public ICollection<Condominium> Condominiums { get; set; }
    }

}
