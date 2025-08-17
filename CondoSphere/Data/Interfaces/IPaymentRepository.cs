using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<IEnumerable<Payment>> GetAllDetailedAsync();      // include Quota
        Task<Payment?> GetByIdDetailedAsync(int id);           // include Quota
        Task<Payment?> GetByProviderPaymentIdAsync(string providerPaymentId);

        Task MarkSucceededAsync(string providerPaymentId, string? receiptUrl);

        Task<Payment?> GetByQuotaIdAsync(int quotaId);
    }
}
