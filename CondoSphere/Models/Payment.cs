using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Payment
    {
        public int Id { get; set; }
        [Required]
        [Display(Name = "Quota")]
        public int QuotaId { get; set; }

        [ValidateNever]
        public Quota? Quota { get; set; }

        [Range(0.01, double.MaxValue)]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }   // precision (18,2)

        [Display(Name = "Payment Method")]
        public PaymentMethodType Method { get; set; } = PaymentMethodType.Card;

        [Display(Name = "Payment Status")]
        public PaymentStatusType Status { get; set; } = PaymentStatusType.Pending;


        [Required, StringLength(50)]
        [Display(Name = "Provider")]
        public string Provider { get; set; } = "stripe";

        [Required, StringLength(120)]
        [Display(Name = "Provider Payment Id")]
        public string ProviderPaymentId { get; set; } = string.Empty;

        [StringLength(120)]
        [Display(Name = "Reference")]
        public string? ProviderReference { get; set; }

        [StringLength(500)]
        [Display(Name = "Receipt URL")]
        public string? ReceiptUrl { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Paid At")]
        public DateTime? PaidAt { get; set; }
    }
}
