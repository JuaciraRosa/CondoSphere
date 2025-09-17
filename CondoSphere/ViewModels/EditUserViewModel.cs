using CondoSphere.Models;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string FullName { get; set; } = "";
        public int? CompanyId { get; set; }
        [Required] public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
