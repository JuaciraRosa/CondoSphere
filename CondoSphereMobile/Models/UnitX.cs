using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class UnitX
    {
        public int Id { get; set; }
        public string UnitNumber { get; set; } = "";
        public List<QuotaX> Quotas { get; set; } = new();
    }
}
