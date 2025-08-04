using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class ExpenseRepository : GenericRepository<Expense>, IExpenseRepository
    {
        public ExpenseRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Expense>> GetByCondominiumIdAsync(int condominiumId)
        {
            return await _context.Expenses
                .Where(e => e.CondominiumId == condominiumId)
                .ToListAsync();
        }
    }

}
