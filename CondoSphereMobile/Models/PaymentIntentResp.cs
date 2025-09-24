using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class PaymentIntentResp
    {
        public string ClientSecret { get; set; } = "";
        public string IntentId { get; set; } = "";
        public string PublishableKey { get; set; } = "";
    }

}
