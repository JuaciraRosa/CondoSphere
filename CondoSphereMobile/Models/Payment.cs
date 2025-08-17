using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int QuotaId { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }   // "MB Way", "Multibanco", etc.
        public string ReceiptNumber { get; set; }
    }
}
