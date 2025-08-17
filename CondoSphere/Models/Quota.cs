using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Quota
    {
        public int Id { get; set; }

        [Required]
        public int UnitId { get; set; }
        public Unit Unit { get; set; }


        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }
        public bool IsPaid { get; set; }

        public Payment Payment { get; set; }
    }

}
