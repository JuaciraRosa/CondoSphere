using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment> GetByReceiptNumberAsync(string receiptNumber);
        Task<IEnumerable<Payment>> GetAllDetailedAsync();      // include Quota
        Task<Payment?> GetByIdDetailedAsync(int id);           // include Quota
        Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId);
    }
}
