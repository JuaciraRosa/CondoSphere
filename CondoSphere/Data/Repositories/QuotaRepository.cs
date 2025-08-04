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
    }

}
