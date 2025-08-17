using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int QuotaId { get; set; }
        public Quota Quota { get; set; }

        public decimal Amount { get; set; }
        public PaymentMethodType Method { get; set; }
        public PaymentStatusType Status { get; set; }

        public string Provider { get; set; }           // "stripe"
        public string ProviderPaymentId { get; set; }  // PaymentIntent Id

        public string? ProviderReference { get; set; } // <- agora nullable
        public string? ReceiptUrl { get; set; }        // <- agora nullable

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}
