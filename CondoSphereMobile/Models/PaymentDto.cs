using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CondoSphereMobile.Models.Enums;

namespace CondoSphereMobile.Models
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int QuotaId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethodType Method { get; set; }
        public PaymentStatusType Status { get; set; }
        public string Provider { get; set; } = "stripe";
        public string ProviderPaymentId { get; set; } = "";
        public string? ProviderReference { get; set; }
        public string? ReceiptUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
