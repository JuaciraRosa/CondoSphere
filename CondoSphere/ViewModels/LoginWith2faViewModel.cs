using System.ComponentModel.DataAnnotations;

namespace CondoSphere.ViewModels
{
    public class LoginWith2faViewModel
    {
        [Required, Display(Name = "Código do autenticador")]
        public string TwoFactorCode { get; set; } = string.Empty;

        // manter sessão autenticada
        public bool RememberMe { get; set; }

        // lembrar este dispositivo (não pede 2FA aqui de novo)
        [Display(Name = "Lembrar esta máquina")]
        public bool RememberMachine { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
