using System.ComponentModel.DataAnnotations;

namespace CondoSphere.ViewModels
{
    public class LoginWithRecoveryCodeViewModel
    {
        [Required, Display(Name = "Código de recuperação")]
        public string RecoveryCode { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
