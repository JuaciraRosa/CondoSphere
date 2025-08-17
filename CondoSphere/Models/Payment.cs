using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Payment
    {
        public int Id { get; set; }
        [Required]
        public int QuotaId { get; set; }
        public Quota Quota { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime PaymentDate { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Payment method must be up to 50 characters.")]
        public string PaymentMethod { get; set; } // MB Way, Multibanco, etc.


        [StringLength(100, ErrorMessage = "Receipt number must be up to 100 characters.")]
        public string ReceiptNumber { get; set; }
    }

}
