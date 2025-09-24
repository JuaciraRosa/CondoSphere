using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CondoSphereMobile.Models
{
    public class ProfileDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string CompanyName { get; set; }
        public string Role { get; set; }
        public string ProfileImagePath { get; set; }
    }
}
