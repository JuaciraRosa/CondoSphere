using CondoSphere.Data;
using CondoSphere.Infrastructure;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Services
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ApplicationDbContext _ctx;
        private readonly ITenantProvider _tenant;

        public SystemSettingsService(ApplicationDbContext ctx, ITenantProvider tenant)
        {
            _ctx = ctx; _tenant = tenant;
        }

        public async Task<SystemSettings> GetCurrentAsync()
        {
            // usa o CompanyId do tenant como “chave” (texto) ou global (null)
            var tid = _tenant?.CompanyId?.ToString();

            SystemSettings? s = null;

            // tenta settings específicos da empresa
            if (!string.IsNullOrEmpty(tid))
                s = await _ctx.SystemSettings.FirstOrDefaultAsync(x => x.TenantId == tid);

            // fallback para settings globais
            if (s == null)
                s = await _ctx.SystemSettings.FirstOrDefaultAsync(x => x.TenantId == null);

            // se não existir nenhum, cria um (associado ao tenant atual ou global)
            if (s == null)
            {
                s = new SystemSettings
                {
                    TenantId = tid,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _ctx.SystemSettings.Add(s);
                await _ctx.SaveChangesAsync();
            }

            return s;
        }

        public async Task UpdateAsync(SystemSettings input)
        {
            var current = await _ctx.SystemSettings.FindAsync(input.Id);
            if (current == null) throw new InvalidOperationException("Settings not found");

            current.CompanyDisplayName = input.CompanyDisplayName;
            current.SupportEmail = input.SupportEmail;
            current.DefaultLateFeePercent = input.DefaultLateFeePercent;
            current.DefaultInterestMonthlyPercent = input.DefaultInterestMonthlyPercent;
            current.GraceDaysForQuotas = input.GraceDaysForQuotas;
            current.WelcomeUserEmailSubject = input.WelcomeUserEmailSubject;
            current.WelcomeUserEmailHtml = input.WelcomeUserEmailHtml;
            current.PasswordResetEmailSubject = input.PasswordResetEmailSubject;
            current.PasswordResetEmailHtml = input.PasswordResetEmailHtml;
            current.UpdatedAt = DateTimeOffset.UtcNow;
            current.EmailsEnabled = input.EmailsEnabled;
            current.WelcomeEmailEnabled = input.WelcomeEmailEnabled;
            current.PasswordResetEmailEnabled = input.PasswordResetEmailEnabled;
            current.PaymentReceiptEmailEnabled = input.PaymentReceiptEmailEnabled;
            current.PaymentReceiptEmailSubject = input.PaymentReceiptEmailSubject;
            current.PaymentReceiptEmailHtml = input.PaymentReceiptEmailHtml;


            await _ctx.SaveChangesAsync();
        }

        // substitui placeholders {{Key}}
        public string RenderTemplate(string? html, IDictionary<string, string> data)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var result = html;
            foreach (var kv in data)
                result = result.Replace("{{" + kv.Key + "}}", kv.Value ?? "", StringComparison.OrdinalIgnoreCase);
            return result;
        }
    }
}
