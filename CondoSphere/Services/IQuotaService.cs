namespace CondoSphere.Services
{
    public interface IQuotaService
    {
        Task<int> EnsureMonthlyAsync(int condominiumId, int year, int month, decimal amountPerUnit);
        Task<int> EnsureQuarterAsync(int condominiumId, int year, int quarter, decimal amountPerUnit);
        Task<int> EnsureRangeMonthlyAsync(int condominiumId, DateTime from, DateTime to, decimal amountPerUnit);
    }

}
