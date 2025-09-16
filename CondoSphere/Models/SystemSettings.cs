using System.ComponentModel.DataAnnotations;

namespace CondoSphere.Models
{
 
  
    public class SystemSettings
    {
        public int Id { get; set; }

        // opcional multi-tenant (deixa null = global/app-wide)
        [MaxLength(64)]
        public string? TenantId { get; set; }

        // Branding
        [MaxLength(120)]
        public string? CompanyDisplayName { get; set; }
        [EmailAddress, MaxLength(160)]
        public string? SupportEmail { get; set; }

        // Taxas/Parâmetros
        [Range(0, 100)]
        public decimal DefaultLateFeePercent { get; set; }          // multa (%)
        [Range(0, 100)]
        public decimal DefaultInterestMonthlyPercent { get; set; }  // juros mês (%)
        [Range(0, 90)]
        public int GraceDaysForQuotas { get; set; }                 // carência (dias)

        // Templates de e-mail (HTML)
        [MaxLength(160)]
        public string? WelcomeUserEmailSubject { get; set; }
        public string? WelcomeUserEmailHtml { get; set; }

        [MaxLength(160)]
        public string? PasswordResetEmailSubject { get; set; }
        public string? PasswordResetEmailHtml { get; set; }

        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public bool EmailsEnabled { get; set; } = true;               // master switch
        public bool WelcomeEmailEnabled { get; set; } = true;         // “conta criada”
        public bool PasswordResetEmailEnabled { get; set; } = true;   // “reset password”
        public bool PaymentReceiptEmailEnabled { get; set; } = true;

        [MaxLength(160)]
        public string? PaymentReceiptEmailSubject { get; set; }
        public string? PaymentReceiptEmailHtml { get; set; }

    }

}
