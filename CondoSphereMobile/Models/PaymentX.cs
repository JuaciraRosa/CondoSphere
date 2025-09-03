using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class PaymentX
    {
        public int Id { get; set; }
        public string Method { get; set; } = "";
        public string? ProviderReference { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
