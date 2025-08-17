using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class ExpenseRepository : GenericRepository<Expense>, IExpenseRepository
    {
        public ExpenseRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Expense>> GetByCondominiumIdAsync(int condominiumId) =>
        await _context.Expenses
            .AsNoTracking()
            .Where(e => e.CondominiumId == condominiumId)
            .Include(e => e.Condominium)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        public async Task<IEnumerable<Expense>> GetAllDetailedAsync() =>
       await _context.Expenses
           .AsNoTracking()
           .Include(e => e.Condominium)
           .OrderByDescending(e => e.Date)
           .ToListAsync();

        public async Task<Expense?> GetByIdDetailedAsync(int id) =>
            await _context.Expenses
                .Include(e => e.Condominium)
                .FirstOrDefaultAsync(e => e.Id == id);
    }

}
