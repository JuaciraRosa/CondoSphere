using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Quota
    {
        public int Id { get; set; }

        [Required]
        public int UnitId { get; set; }

        [ValidateNever]
        public Unit? Unit { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; } // precision (18,2) no OnModelCreating

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }
        public bool IsPaid { get; set; }

        [ValidateNever]
        public Payment? Payment { get; set; } // relação 1:1

       
        public string? DebtorUserId { get; set; }
        public string? DebtorEmail { get; set; }
        public string? DebtorName { get; set; }
    }

}
