using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment> GetByReceiptNumberAsync(string receiptNumber);
    }

}
