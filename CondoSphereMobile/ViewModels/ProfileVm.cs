using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.ViewModels
{
    public class ProfileVm
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public List<ProfileUnitVm> OwnedUnits { get; set; } = new();
    }
}
