using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class UnitOption
    {
        public int Id { get; set; }
        public int CondominiumId { get; set; }
        public string UnitNumber { get; set; } = "";
        public string Display => string.IsNullOrWhiteSpace(UnitNumber) ? $"Unidade #{Id}" : UnitNumber;
        public override string ToString() => Display;
    }
}
