using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.Data.Repositories
{
    public class QuotaRepository : GenericRepository<Quota>, IQuotaRepository
    {
        public QuotaRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Quota>> GetByUnitIdAsync(int unitId)
        {
            return await _context.Quotas
                .Where(q => q.UnitId == unitId)
                .ToListAsync();
        }

        public Task<bool> ExistsAsync(int unitId, int year, int month)
        {
            var first = new DateTime(year, month, 1);
            var next = first.AddMonths(1);
            return _context.Quotas.AnyAsync(q =>
                q.UnitId == unitId &&
                q.DueDate >= first && q.DueDate < next
            );
        }


        public async Task<IEnumerable<Quota>> GetAllWithUnitAsync()
        {
            return await _context.Quotas
                .Include(q => q.Unit)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Quota?> GetByIdWithUnitAsync(int id)
        {
            return await _context.Quotas
                .Include(q => q.Unit)
                .FirstOrDefaultAsync(q => q.Id == id);
        }


        // ➕ carrega Unidade e Condomínio
        public async Task<List<Quota>> GetAllDetailedAsync()
        {
            return await _context.Quotas
                .Include(q => q.Unit)
                    .ThenInclude(u => u.Condominium)
                .AsNoTracking()
                .ToListAsync();
        }
    }

}
