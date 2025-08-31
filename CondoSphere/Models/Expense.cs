using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Expense
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150, ErrorMessage = "Description can't exceed 150 characters.")]
        public string Description { get; set; }


        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        public int CondominiumId { get; set; }

        [ValidateNever]
        public Condominium? Condominium { get; set; }
    }
}
