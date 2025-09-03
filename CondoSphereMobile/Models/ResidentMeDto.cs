using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class ResidentMeDto
    {
        public string Email { get; set; } = "";
        public string? FullName { get; set; }
        public List<OwnedUnitDto> OwnedUnits { get; set; } = new();
    }
}
