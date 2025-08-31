using CondoSphere.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CondoSphere.Data
{
    public class User : IdentityUser
    {

        public string FullName { get; set; } = "";
        public UserRole Role { get; set; }  // Enum: Administrator, Manager, Resident, Staff
        public bool IsActive { get; set; }= true;

        public int? CompanyId { get; set; }

        [ValidateNever]
        public Company? Company { get; set; }


        [ValidateNever]
        public ICollection<Unit> OwnedUnits { get; set; } // For residents

        public string ProfileImagePath { get; set; } = "";
    }
}
