using CondoSphere.Models;

namespace CondoSphere.Data.Interfaces
{
    public interface IExpenseRepository : IGenericRepository<Expense>
    {
        Task<IEnumerable<Expense>> GetByCondominiumIdAsync(int condominiumId);
        Task<IEnumerable<Expense>> GetAllDetailedAsync();
        Task<Expense?> GetByIdDetailedAsync(int id);
    }

}
