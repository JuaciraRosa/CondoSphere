using CondoSphere.Models;
using Microsoft.AspNetCore.Identity;

namespace CondoSphere.Data
{
    public class User : IdentityUser
    {

        public string FullName { get; set; }
        public UserRole Role { get; set; }  // Enum: Administrator, Manager, Resident, Staff
        public bool IsActive { get; set; }

        public int? CompanyId { get; set; }
        public Company Company { get; set; }

        public ICollection<Unit> OwnedUnits { get; set; } // For residents

        public string ProfileImagePath { get; set; } // ex: "/uploads/avatars/abc.jpg"
    }
}
