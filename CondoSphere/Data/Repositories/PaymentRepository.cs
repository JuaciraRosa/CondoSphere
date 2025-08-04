using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(ApplicationDbContext context) : base(context) { }

        public async Task<Payment> GetByReceiptNumberAsync(string receiptNumber)
        {
            return await _context.Payments
                .FirstOrDefaultAsync(p => p.ReceiptNumber == receiptNumber);
        }
    }

}
