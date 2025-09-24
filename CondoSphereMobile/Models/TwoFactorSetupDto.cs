using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class TwoFactorSetupDto
    {
        public string Key { get; set; }
        public string OtpauthUri { get; set; }
        public string QrCodeBase64 { get; set; }
    }
}
