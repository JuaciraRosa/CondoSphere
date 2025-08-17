using CondoSphere.Data;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Unit
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Unit number must be up to 20 characters.")]
        public string Number { get; set; }


        [Required]
        [Range(1, 10000, ErrorMessage = "Area must be between 1 and 10,000 m².")]
        public double Area { get; set; }

        [Required]
        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }

        [Required]
        public int OwnerId { get; set; }
        public User Owner { get; set; }
    }

}
