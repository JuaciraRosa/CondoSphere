using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
    public class Payment
    {
        public int Id { get; set; }

        // Ligação à quota
        public int QuotaId { get; set; }
        public Quota Quota { get; set; }

        // Montante efetivo cobrado
        public decimal Amount { get; set; }

        public PaymentMethodType Method { get; set; }
        public PaymentStatusType Status { get; set; }

        // Dados do provedor (Stripe)
        public string Provider { get; set; }            // "stripe"
        public string ProviderPaymentId { get; set; }   // PaymentIntent Id
        public string ProviderReference { get; set; }   // Multibanco: entidade/referência numa string
        public string ReceiptUrl { get; set; }          // URL do recibo do provedor (opcional)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }

}
