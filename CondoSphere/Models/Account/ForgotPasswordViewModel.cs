using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models.Account
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}
