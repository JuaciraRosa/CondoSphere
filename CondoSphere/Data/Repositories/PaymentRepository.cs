using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Payment>> GetAllDetailedAsync() =>
            await _context.Payments
                .AsNoTracking()
                .Include(p => p.Quota)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

        public async Task<Payment?> GetByIdDetailedAsync(int id) =>
            await _context.Payments
                .Include(p => p.Quota)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId) =>
            await _context.Payments
                .FirstOrDefaultAsync(p => p.ProviderPaymentId == providerPaymentId);

        public async Task MarkSucceededAsync(string providerPaymentId, string? receiptUrl)
        {
            var p = await _context.Payments.Include(x => x.Quota)
                    .FirstOrDefaultAsync(x => x.ProviderPaymentId == providerPaymentId);
            if (p == null) return;

            p.Status = PaymentStatusType.Succeeded;
            p.PaidAt = DateTime.UtcNow;
            p.ReceiptUrl = receiptUrl;
            if (p.Quota != null) p.Quota.IsPaid = true;

            await _context.SaveChangesAsync();
        }

        public async Task<Payment?> GetByQuotaIdAsync(int quotaId) =>
    await _context.Payments
        .Include(p => p.Quota)
        .FirstOrDefaultAsync(p => p.QuotaId == quotaId);
    }

}
