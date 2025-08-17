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

        public Task<Payment> GetByReceiptNumberAsync(string receiptNumber)
        {
            throw new NotImplementedException();
        }
    }

}
