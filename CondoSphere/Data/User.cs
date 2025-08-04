using CondoSphere.Models;

namespace CondoSphere.Data
{
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }  // Enum: Administrator, Manager, Resident, Staff
        public bool IsActive { get; set; }

        public int? CompanyId { get; set; }
        public Company Company { get; set; }

        public ICollection<Unit> OwnedUnits { get; set; } // For residents
    }

}
