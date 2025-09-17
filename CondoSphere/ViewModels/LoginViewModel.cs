using System.ComponentModel.DataAnnotations;

namespace CondoSphere.ViewModels
{
    public class LoginViewModel
    {
        [Required, Display(Name = "E-mail ou utilizador")]
        public string EmailOrUser { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Lembrar-me")]
        public bool RememberMe { get; set; } = true;

        public string? ReturnUrl { get; set; }
    }
}
