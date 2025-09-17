using CondoSphere.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace CondoSphere.ViewModels
{
    public class CreateUserViewModel
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string FullName { get; set; } = "";
        public int? CompanyId { get; set; }
        [Required] public UserRole Role { get; set; }

        [BindNever]
        public bool IsActive { get; set; } = true;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required, DataType(DataType.Password), Compare("Password",
            ErrorMessage = "The passwords don't match.")]
        public string ConfirmPassword { get; set; } = "";
    }

}
