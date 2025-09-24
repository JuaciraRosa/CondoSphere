using CondoSphere.Data;
using CondoSphere.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Unit 
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Unit number must be up to 20 characters.")]
        public string Number { get; set; } = string.Empty;


        [Required]
        [Range(1, 10000, ErrorMessage = "Area must be between 1 and 10,000 m².")]
        public double? Area { get; set; }

        [Required]
        public int CondominiumId { get; set; }


        [ValidateNever]
        public Condominium? Condominium { get; set; }


        // FK -> User (Owner)
        public string? OwnerId { get; set; }

        [ValidateNever]
        public User? Owner { get; set; }


        [ValidateNever]
        public ICollection<Quota> Quotas { get; set; }

        public bool IsActive { get; set; } = true;   // soft delete

        [ValidateNever]
        public ICollection<UnitOwnership> OwnershipHistory { get; set; } = new List<UnitOwnership>();

    }

}
