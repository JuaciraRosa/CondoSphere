using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class OwnedUnitDto
    {
        public int Id { get; set; }
        public string UnitNumber { get; set; } = "";
        public decimal Area { get; set; }
        public int CondominiumId { get; set; }
        public List<QuotaOwnedDto> Quotas { get; set; } = new();
    }
}
