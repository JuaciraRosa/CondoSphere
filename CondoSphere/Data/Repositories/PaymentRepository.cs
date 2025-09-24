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
            

            await _context.SaveChangesAsync();
        }

        public async Task<Payment?> GetByQuotaIdAsync(int quotaId) =>
    await _context.Payments
        .Include(p => p.Quota)
        .FirstOrDefaultAsync(p => p.QuotaId == quotaId);


        public async Task<decimal> GetPaidTotalAsync(DateTime fromUtc, DateTime toUtc)
          => await _context.Payments
              .AsNoTracking()
              .Where(p =>
                  p.Status == PaymentStatusType.Succeeded &&          // enum comparison
                  p.PaidAt.HasValue &&
                  p.PaidAt.Value >= fromUtc &&
                  p.PaidAt.Value < toUtc)
              .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        public async Task<int> CountOpenAsync()
            => await _context.Payments
                .AsNoTracking()
                .CountAsync(p => p.Status != PaymentStatusType.Succeeded); // enum comparison

        public async Task<int[]> CountPaidByMonthAsync(int monthsBack)
        {
            if (monthsBack < 1) monthsBack = 1;

            var now = DateTime.UtcNow;
            var start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                            .AddMonths(-(monthsBack - 1));
            var end = start.AddMonths(monthsBack);

            var grouped = await _context.Payments
                .AsNoTracking()
                .Where(p =>
                    p.Status == PaymentStatusType.Succeeded &&          // enum comparison
                    p.PaidAt.HasValue &&
                    p.PaidAt.Value >= start &&
                    p.PaidAt.Value < end)
                .GroupBy(p => new { p.PaidAt!.Value.Year, p.PaidAt!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var result = new int[monthsBack];
            for (int i = 0; i < monthsBack; i++)
            {
                var cursor = start.AddMonths(i);
                var hit = grouped.FirstOrDefault(x => x.Year == cursor.Year && x.Month == cursor.Month);
                result[i] = hit?.Count ?? 0;
            }
            return result;
        }

    }
}
